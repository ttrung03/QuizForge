using QuestionBank.Web.Application.DTOs;
using QuestionBank.Web.Application.Interfaces;
using QuestionBank.Web.Domain.Entities;

namespace QuestionBank.Web.Application.Services;

public class DeThiService(IDeThiRepository repo)
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

    private static DeThiDto MapToDto(DeThi d) => new()
    {
        MaDeThi  = d.MaDeThi,
        MaMonHoc = d.MaMonHoc,
        TenDeThi = d.TenDeThi,
        NgayTao  = d.NgayTao,
        DaDuyet  = d.DaDuyet,
        ChiTietDeThis = d.ChiTietDeThis
            .OrderBy(c => c.ThuTu)
            .Select(c => new ChiTietDeThiDto
            {
                MaCauHoi    = c.MaCauHoi,
                MaPhan      = c.MaPhan,
                TenPhan     = c.MaPhanNavigation?.TenPhan ?? "",
                ThuTu       = c.ThuTu,
                MaSoCauHoi  = c.MaCauHoiNavigation?.MaSoCauHoi ?? 0,
                NoiDung     = c.MaCauHoiNavigation?.NoiDung,
                CapDo       = c.MaCauHoiNavigation?.CapDo ?? 1,
                CauTraLois  = c.MaCauHoiNavigation?.CauTraLois
                    .OrderBy(a => a.ThuTu)
                    .Select(a => new CauTraLoiDto
                    {
                        MaCauTraLoi = a.MaCauTraLoi,
                        NoiDung     = a.NoiDung,
                        ThuTu       = a.ThuTu,
                        LaDapAn     = a.LaDapAn,
                        HoanVi      = a.HoanVi
                    }).ToList() ?? []
            })
            .ToList()
    };
}
