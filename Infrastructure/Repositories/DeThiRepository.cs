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
}
