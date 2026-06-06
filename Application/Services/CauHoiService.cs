using QuestionBank.Web.Application.DTOs;
using QuestionBank.Web.Application.Interfaces;
using QuestionBank.Web.Domain.Entities;

namespace QuestionBank.Web.Application.Services;

public class CauHoiService(ICauHoiRepository repo, WordImportService importService)
{
    public async Task<List<CauHoiDto>> GetByPhanAsync(Guid maPhan)
    {
        var list = await repo.GetByPhanAsync(maPhan);
        return list.Select(MapToDto).ToList();
    }

    private static CauHoiDto MapToDto(Domain.Entities.CauHoi c) => new()
    {
        MaCauHoi    = c.MaCauHoi,
        MaPhan      = c.MaPhan,
        MaSoCauHoi  = c.MaSoCauHoi,
        NoiDung     = c.NoiDung,
        HoanVi      = c.HoanVi,
        CapDo       = c.CapDo,
        SoCauHoiCon = c.SoCauHoiCon,
        CauTraLois  = c.CauTraLois
            .OrderBy(a => a.ThuTu)
            .Select(a => new CauTraLoiDto
            {
                MaCauTraLoi = a.MaCauTraLoi,
                NoiDung     = a.NoiDung,
                ThuTu       = a.ThuTu,
                LaDapAn     = a.LaDapAn,
                HoanVi      = a.HoanVi
            }).ToList(),
        CauHoiCons  = c.CauHoiCons
            .OrderBy(con => con.MaSoCauHoi)
            .Select(MapToDto)
            .ToList(),
        Files = c.Files
            .Where(f => f.LoaiFile.HasValue && f.TenFile != null)
            .Select(f => new FileDinhKemDto
            {
                MaFile   = f.MaFile,
                LoaiFile = f.LoaiFile!.Value,
                TenFile  = f.TenFile!
            }).ToList()
    };

    /// <summary>Parse stream .docx và trả về danh sách câu hỏi + nhóm + cảnh báo để preview.</summary>
    public (List<ImportCauHoiDto> Questions, List<ImportCauHoiNhomDto> Groups, List<string> Warnings) ParseWordFile(Stream stream)
        => importService.Parse(stream);

    /// <summary>Lưu câu hỏi đơn và câu hỏi nhóm đã parse vào Phan được chọn.
    /// Toàn bộ thao tác được thực hiện trong một lần SaveChanges để đảm bảo atomic.
    /// Câu hỏi có nội dung trùng với câu đã tồn tại trong Phần sẽ bị bỏ qua.</summary>
    /// <returns>(Imported: số câu được lưu, Skipped: số câu bị bỏ qua do trùng)</returns>
    public async Task<(int Imported, int Skipped)> ImportAsync(
        List<ImportCauHoiDto>    questions,
        List<ImportCauHoiNhomDto> groups,
        Guid                     maPhan,
        short                    defaultCapDo = 1)
    {
        var existing = await repo.GetExistingNoiDungAsync(maPhan);
        int maxSo    = await repo.GetMaxMaSoCauHoiAsync(maPhan);
        int counter  = maxSo + 1;
        var now      = DateTime.Now;
        int skipped  = 0;

        var allCauHois    = new List<CauHoi>();
        var allCauTraLois = new List<CauTraLoi>();
        var allFiles      = new List<FileDinhKem>();

        // Câu hỏi đơn
        foreach (var q in questions)
        {
            if (existing.Contains(Normalize(q.NoiDung))) { skipped++; continue; }

            var cauHoi = new CauHoi
            {
                MaCauHoi    = Guid.NewGuid(),
                MaPhan      = maPhan,
                MaSoCauHoi  = counter++,
                NoiDung     = q.NoiDung,
                HoanVi      = true,
                CapDo       = q.CapDo ?? defaultCapDo,
                SoCauHoiCon = 0,
                NgayTao     = now
            };
            allCauHois.Add(cauHoi);
            allCauTraLois.AddRange(MapAnswers(q.Answers, cauHoi.MaCauHoi));
            allFiles.AddRange(MapFiles(q.AnhFiles, q.AmThanhFiles, cauHoi.MaCauHoi));
        }

        // Câu hỏi nhóm — kiểm tra theo nội dung passage của nhóm
        foreach (var group in groups)
        {
            if (existing.Contains(Normalize(group.NoiDung))) { skipped += 1 + group.CauHoiCons.Count; continue; }

            var validSubs = group.CauHoiCons
                .Where(sub => !existing.Contains(Normalize(sub.NoiDung)))
                .ToList();
            skipped += group.CauHoiCons.Count - validSubs.Count;

            var parentId = Guid.NewGuid();
            var parent   = new CauHoi
            {
                MaCauHoi    = parentId,
                MaPhan      = maPhan,
                MaSoCauHoi  = counter++,
                NoiDung     = group.NoiDung,
                HoanVi      = false,
                CapDo       = group.CapDo ?? defaultCapDo,
                SoCauHoiCon = validSubs.Count,
                NgayTao     = now
            };
            allCauHois.Add(parent);
            allFiles.AddRange(MapFiles(group.AnhFiles, group.AmThanhFiles, parentId));

            foreach (var sub in validSubs)
            {
                var child = new CauHoi
                {
                    MaCauHoi    = Guid.NewGuid(),
                    MaPhan      = maPhan,
                    MaSoCauHoi  = counter++,
                    NoiDung     = sub.NoiDung,
                    HoanVi      = true,
                    CapDo       = sub.CapDo ?? defaultCapDo,
                    SoCauHoiCon = 0,
                    MaCauHoiCha = parentId,
                    NgayTao     = now
                };
                allCauHois.Add(child);
                allCauTraLois.AddRange(MapAnswers(sub.Answers, child.MaCauHoi));
                allFiles.AddRange(MapFiles(sub.AnhFiles, sub.AmThanhFiles, child.MaCauHoi));
            }
        }

        if (allCauHois.Count > 0)
            await repo.BulkImportAsync(allCauHois, allCauTraLois, allFiles);

        int imported = allCauHois.Count;
        return (imported, skipped);
    }

    private static string Normalize(string s)
        => System.Text.RegularExpressions.Regex.Replace(s.Trim(), @"\s+", " ");

    public async Task UpdateAsync(UpdateCauHoiDto dto)
    {
        var answers = dto.CauTraLois
            .Select(a => (a.MaCauTraLoi, a.NoiDung, a.LaDapAn))
            .ToList();
        await repo.UpdateAsync(dto.MaCauHoi, dto.NoiDung, dto.CapDo, answers);
    }

    public async Task DeleteAsync(Guid id)
        => await repo.SoftDeleteAsync(id);

    public async Task ReplaceAudioAsync(Guid maFile, string newTenFile)
        => await repo.ReplaceAudioAsync(maFile, newTenFile);

    private static List<CauTraLoi> MapAnswers(List<ImportCauTraLoiDto> src, Guid maCauHoi)
        => src.Select(a => new CauTraLoi
        {
            MaCauTraLoi = Guid.NewGuid(),
            MaCauHoi    = maCauHoi,
            NoiDung     = a.NoiDung,
            ThuTu       = a.ThuTu,
            LaDapAn     = a.LaDapAn,
            HoanVi      = a.HoanVi
        }).ToList();

    private static List<FileDinhKem> MapFiles(List<string> anhFiles, List<string> amThanhFiles, Guid maCauHoi)
    {
        var result = new List<FileDinhKem>();
        foreach (var f in anhFiles.Where(f => !string.IsNullOrWhiteSpace(f)))
            result.Add(new FileDinhKem { MaFile = Guid.NewGuid(), MaCauHoi = maCauHoi, TenFile = f.Trim(), LoaiFile = 1 });
        foreach (var f in amThanhFiles.Where(f => !string.IsNullOrWhiteSpace(f)))
            result.Add(new FileDinhKem { MaFile = Guid.NewGuid(), MaCauHoi = maCauHoi, TenFile = f.Trim(), LoaiFile = 2 });
        return result;
    }
}
