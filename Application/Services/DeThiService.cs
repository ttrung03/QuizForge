using QuestionBank.Web.Application.DTOs;
using QuestionBank.Web.Application.Interfaces;
using QuestionBank.Web.Domain.Entities;

namespace QuestionBank.Web.Application.Services;

public class DeThiService(IDeThiRepository repo, ICauHoiRepository cauHoiRepo)
{
    public async Task<List<DeThiDto>> GetByMonHocAsync(Guid maMonHoc)
    {
        var list = await repo.GetByMonHocAsync(maMonHoc);
        return list.Select(MapToDto).ToList();
    }

    public async Task SaveAsync(SaveDeThiDto dto)
    {
        var deThi = new DeThi
        {
            MaDeThi  = Guid.NewGuid(),
            MaMonHoc = dto.MaMonHoc,
            TenDeThi = dto.TenDeThi.Trim(),
            NgayTao  = DateTime.Now,
            DaDuyet  = false
        };

        var chiTiets = dto.CauHois
            .Select((ch, idx) => new ChiTietDeThi
            {
                MaDeThi  = deThi.MaDeThi,
                MaCauHoi = ch.MaCauHoi,
                MaPhan   = ch.MaPhan,
                ThuTu    = idx + 1
            })
            .ToList();

        await repo.AddAsync(deThi, chiTiets);
    }

    public async Task DeleteAsync(Guid maDeThi)
        => await repo.DeleteAsync(maDeThi);

    public async Task RemoveCauHoisAsync(Guid maDeThi, List<Guid> maCauHois)
        => await repo.RemoveCauHoisAsync(maDeThi, maCauHois);

    public async Task AddCauHoisAsync(Guid maDeThi, List<SelectedCauHoiDto> cauHois)
    {
        var pairs = cauHois.Select(c => (c.MaCauHoi, c.MaPhan)).ToList();
        await repo.AddCauHoisAsync(maDeThi, pairs);
    }

    public async Task DuyetAsync(Guid maDeThi)
        => await repo.UpdateDaDuyetAsync(maDeThi, true);

    /// <summary>Lưu thứ tự câu hỏi và đáp án từ màn hình hoán vị, rồi phê duyệt đề thi.</summary>
    public async Task DuyetVoiHoanViAsync(
        Guid maDeThi,
        List<(Guid maCauHoi, int thuTu)> cauHoiOrders,
        List<(Guid maCauTraLoi, int thuTu)> cauTraLoiOrders)
    {
        await repo.UpdateThuTuAsync(maDeThi, cauHoiOrders);
        if (cauTraLoiOrders.Count > 0)
            await cauHoiRepo.UpdateCauTraLoiThuTuAsync(cauTraLoiOrders);
        await repo.UpdateDaDuyetAsync(maDeThi, true);
    }

    public async Task HuyDuyetAsync(Guid maDeThi)
        => await repo.UpdateDaDuyetAsync(maDeThi, false);

    private static DeThiDto MapToDto(DeThi d)
    {
        var ordered = d.ChiTietDeThis.OrderBy(c => c.ThuTu).ToList();

        // Tách câu đơn và câu con (thuộc nhóm)
        var singles  = ordered.Where(c => c.MaCauHoiNavigation?.MaCauHoiCha == null).ToList();
        var children = ordered.Where(c => c.MaCauHoiNavigation?.MaCauHoiCha != null).ToList();

        // Gom câu con theo câu cha
        var groups = children
            .GroupBy(c => c.MaCauHoiNavigation!.MaCauHoiCha!.Value)
            .Select(g => new ChiTietDeThiDto
            {
                MaCauHoi   = g.Key,
                MaPhan     = g.First().MaPhan,
                TenPhan    = g.First().MaPhanNavigation?.TenPhan ?? "",
                ThuTu      = g.Min(c => c.ThuTu),
                NoiDung    = g.First().MaCauHoiNavigation!.CauHoiCha?.NoiDung,
                CapDo      = g.First().MaCauHoiNavigation!.CauHoiCha?.CapDo ?? 1,
                LaCauNhom  = true,
                CauHoiCons = g.OrderBy(c => c.ThuTu).Select(c => new ChiTietDeThiDto
                {
                    MaCauHoi   = c.MaCauHoi,
                    MaPhan     = c.MaPhan,
                    ThuTu      = c.ThuTu,
                    MaSoCauHoi = c.MaCauHoiNavigation?.MaSoCauHoi ?? 0,
                    NoiDung    = c.MaCauHoiNavigation?.NoiDung,
                    CapDo      = c.MaCauHoiNavigation?.CapDo ?? 1,
                    CauTraLois = MapCauTraLois(c.MaCauHoiNavigation?.CauTraLois)
                }).ToList()
            });

        var mappedSingles = singles.Select(c => new ChiTietDeThiDto
        {
            MaCauHoi   = c.MaCauHoi,
            MaPhan     = c.MaPhan,
            TenPhan    = c.MaPhanNavigation?.TenPhan ?? "",
            ThuTu      = c.ThuTu,
            MaSoCauHoi = c.MaCauHoiNavigation?.MaSoCauHoi ?? 0,
            NoiDung    = c.MaCauHoiNavigation?.NoiDung,
            CapDo      = c.MaCauHoiNavigation?.CapDo ?? 1,
            CauTraLois = MapCauTraLois(c.MaCauHoiNavigation?.CauTraLois)
        });

        return new DeThiDto
        {
            MaDeThi       = d.MaDeThi,
            MaMonHoc      = d.MaMonHoc,
            TenDeThi      = d.TenDeThi,
            NgayTao       = d.NgayTao,
            DaDuyet       = d.DaDuyet,
            ChiTietDeThis = mappedSingles.Concat(groups).OrderBy(x => x.ThuTu).ToList()
        };
    }

    private static List<CauTraLoiDto> MapCauTraLois(IEnumerable<CauTraLoi>? list)
        => list?.OrderBy(a => a.ThuTu).Select(a => new CauTraLoiDto
        {
            MaCauTraLoi = a.MaCauTraLoi,
            NoiDung     = a.NoiDung,
            ThuTu       = a.ThuTu,
            LaDapAn     = a.LaDapAn,
            HoanVi      = a.HoanVi
        }).ToList() ?? [];
}
