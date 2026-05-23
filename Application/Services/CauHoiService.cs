using QuestionBank.Web.Application.DTOs;
using QuestionBank.Web.Application.Interfaces;
using QuestionBank.Web.Domain.Entities;

namespace QuestionBank.Web.Application.Services;

public class CauHoiService(ICauHoiRepository repo, WordImportService importService)
{
    public async Task<List<CauHoiDto>> GetByPhanAsync(Guid maPhan)
    {
        var list = await repo.GetByPhanAsync(maPhan);
        return list.Select(c => new CauHoiDto
        {
            MaCauHoi    = c.MaCauHoi,
            MaPhan      = c.MaPhan,
            MaSoCauHoi  = c.MaSoCauHoi,
            NoiDung     = c.NoiDung,
            HoanVi      = c.HoanVi,
            CapDo       = c.CapDo,
            CauTraLois  = c.CauTraLois
                .OrderBy(a => a.ThuTu)
                .Select(a => new CauTraLoiDto
                {
                    MaCauTraLoi = a.MaCauTraLoi,
                    NoiDung     = a.NoiDung,
                    ThuTu       = a.ThuTu,
                    LaDapAn     = a.LaDapAn,
                    HoanVi      = a.HoanVi
                }).ToList()
        }).ToList();
    }

    /// <summary>Parse stream .docx và trả về danh sách câu hỏi + cảnh báo để preview.</summary>
    public (List<ImportCauHoiDto> Questions, List<string> Warnings) ParseWordFile(Stream stream)
        => importService.Parse(stream);

    /// <summary>Lưu danh sách câu hỏi đã parse vào Phan được chọn. defaultCapDo áp cho các câu không có prefix cấp độ trong file Word.</summary>
    public async Task<int> ImportAsync(List<ImportCauHoiDto> questions, Guid maPhan, short defaultCapDo = 1)
    {
        int maxSo = await repo.GetMaxMaSoCauHoiAsync(maPhan);
        int counter = maxSo + 1;

        foreach (var q in questions)
        {
            var cauHoi = new CauHoi
            {
                MaPhan      = maPhan,
                MaSoCauHoi  = counter++,
                NoiDung     = q.NoiDung,
                HoanVi      = true,
                CapDo       = q.CapDo ?? defaultCapDo,
                SoCauHoiCon = 0,
                NgayTao     = DateTime.Now
            };

            var answers = q.Answers.Select(a => new CauTraLoi
            {
                NoiDung = a.NoiDung,
                ThuTu   = a.ThuTu,
                LaDapAn = a.LaDapAn,
                HoanVi  = a.HoanVi
            }).ToList();

            await repo.AddWithAnswersAsync(cauHoi, answers);
        }

        return questions.Count;
    }

    public async Task DeleteAsync(Guid id)
        => await repo.SoftDeleteAsync(id);
}
