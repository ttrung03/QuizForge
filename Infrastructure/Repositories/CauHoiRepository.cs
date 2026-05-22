using Microsoft.EntityFrameworkCore;
using QuestionBank.Web.Application.Interfaces;
using QuestionBank.Web.Domain.Entities;
using QuestionBank.Web.Infrastructure.Data;

namespace QuestionBank.Web.Infrastructure.Repositories;

public class CauHoiRepository(QuestionBankDbContext context) : ICauHoiRepository
{
    public async Task<List<CauHoi>> GetByPhanAsync(Guid maPhan)
        => await context.CauHois
               .Include(c => c.CauTraLois)
               .Where(c => c.MaPhan == maPhan && c.XoaTamCauHoi != true && c.MaCauHoiCha == null)
               .OrderBy(c => c.MaSoCauHoi)
               .ToListAsync();

    public async Task<int> GetMaxMaSoCauHoiAsync(Guid maPhan)
    {
        bool any = await context.CauHois.AnyAsync(c => c.MaPhan == maPhan && c.XoaTamCauHoi != true);
        if (!any) return 0;
        return await context.CauHois
            .Where(c => c.MaPhan == maPhan && c.XoaTamCauHoi != true)
            .MaxAsync(c => c.MaSoCauHoi);
    }

    public async Task AddWithAnswersAsync(CauHoi cauHoi, List<CauTraLoi> answers)
    {
        cauHoi.MaCauHoi = Guid.NewGuid();
        foreach (var a in answers)
        {
            a.MaCauTraLoi = Guid.NewGuid();
            a.MaCauHoi = cauHoi.MaCauHoi;
        }
        context.CauHois.Add(cauHoi);
        context.CauTraLois.AddRange(answers);
        await context.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(Guid id)
    {
        var cauHoi = await context.CauHois.FindAsync(id);
        if (cauHoi is null) return;
        cauHoi.XoaTamCauHoi = true;
        await context.SaveChangesAsync();
    }
}
