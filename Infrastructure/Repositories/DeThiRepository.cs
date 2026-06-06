using Microsoft.EntityFrameworkCore;
using QuestionBank.Web.Application.Interfaces;
using QuestionBank.Web.Domain.Entities;
using QuestionBank.Web.Infrastructure.Data;

namespace QuestionBank.Web.Infrastructure.Repositories;

public class DeThiRepository(QuestionBankDbContext context) : IDeThiRepository
{
    public async Task<List<DeThi>> GetByMonHocAsync(Guid maMonHoc)
        => await context.DeThis
            .Include(d => d.ChiTietDeThis)
                .ThenInclude(c => c.MaCauHoiNavigation)
                    .ThenInclude(ch => ch.CauTraLois)
            .Include(d => d.ChiTietDeThis)
                .ThenInclude(c => c.MaCauHoiNavigation)
                    .ThenInclude(ch => ch.Files)
            .Include(d => d.ChiTietDeThis)
                .ThenInclude(c => c.MaCauHoiNavigation)
                    .ThenInclude(ch => ch.CauHoiCha)
                        .ThenInclude(cha => cha!.Files)
            .Include(d => d.ChiTietDeThis)
                .ThenInclude(c => c.MaPhanNavigation)
            .Where(d => d.MaMonHoc == maMonHoc)
            .OrderByDescending(d => d.NgayTao)
            .ToListAsync();

    public async Task<DeThi?> GetByIdAsync(Guid maDeThi)
        => await context.DeThis
            .Include(d => d.ChiTietDeThis)
                .ThenInclude(c => c.MaCauHoiNavigation)
                    .ThenInclude(ch => ch.CauTraLois)
            .Include(d => d.ChiTietDeThis)
                .ThenInclude(c => c.MaCauHoiNavigation)
                    .ThenInclude(ch => ch.CauHoiCha)
            .Include(d => d.ChiTietDeThis)
                .ThenInclude(c => c.MaPhanNavigation)
            .FirstOrDefaultAsync(d => d.MaDeThi == maDeThi);

    public async Task AddAsync(DeThi deThi, List<ChiTietDeThi> chiTiets)
    {
        context.DeThis.Add(deThi);
        context.ChiTietDeThis.AddRange(chiTiets);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid maDeThi)
    {
        var deThi = await context.DeThis
            .Include(d => d.ChiTietDeThis)
            .FirstOrDefaultAsync(d => d.MaDeThi == maDeThi);
        if (deThi is null) return;
        context.ChiTietDeThis.RemoveRange(deThi.ChiTietDeThis);
        context.DeThis.Remove(deThi);
        await context.SaveChangesAsync();
    }

    public async Task UpdateDaDuyetAsync(Guid maDeThi, bool daDuyet)
    {
        var deThi = await context.DeThis.FindAsync(maDeThi);
        if (deThi is null) return;
        deThi.DaDuyet = daDuyet;
        await context.SaveChangesAsync();
    }

    public async Task RemoveCauHoisAsync(Guid maDeThi, List<Guid> maCauHois)
    {
        var rows = await context.ChiTietDeThis
            .Where(c => c.MaDeThi == maDeThi && maCauHois.Contains(c.MaCauHoi))
            .ToListAsync();
        context.ChiTietDeThis.RemoveRange(rows);
        await context.SaveChangesAsync();
    }

    public async Task UpdateThuTuAsync(Guid maDeThi, List<(Guid maCauHoi, int thuTu)> orders)
    {
        var lookup = orders.ToDictionary(o => o.maCauHoi, o => o.thuTu);
        var rows = await context.ChiTietDeThis
            .Where(c => c.MaDeThi == maDeThi && lookup.Keys.Contains(c.MaCauHoi))
            .ToListAsync();
        foreach (var row in rows)
            row.ThuTu = lookup[row.MaCauHoi];
        await context.SaveChangesAsync();
    }

    public async Task AddCauHoisAsync(Guid maDeThi, List<(Guid maCauHoi, Guid maPhan)> cauHois)
    {
        int maxThuTu = await context.ChiTietDeThis
            .Where(c => c.MaDeThi == maDeThi)
            .Select(c => (int?)c.ThuTu)
            .MaxAsync() ?? 0;

        var chiTiets = cauHois.Select((ch, idx) => new ChiTietDeThi
        {
            MaDeThi  = maDeThi,
            MaCauHoi = ch.maCauHoi,
            MaPhan   = ch.maPhan,
            ThuTu    = maxThuTu + idx + 1
        }).ToList();

        context.ChiTietDeThis.AddRange(chiTiets);
        await context.SaveChangesAsync();
    }

    public async Task<int> GetNextMaDeAsync(Guid maMonHoc)
    {
        var max = await context.DeThis
            .Where(d => d.MaMonHoc == maMonHoc && d.MaDe != null)
            .Select(d => (int?)d.MaDe)
            .MaxAsync();
        return (max ?? 100) + 1;
    }
}
