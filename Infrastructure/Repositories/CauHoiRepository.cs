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
               .Include(c => c.Files)
               .Include(c => c.CauHoiCons.Where(con => con.XoaTamCauHoi != true))
                   .ThenInclude(con => con.CauTraLois)
               .Include(c => c.CauHoiCons.Where(con => con.XoaTamCauHoi != true))
                   .ThenInclude(con => con.Files)
               .Where(c => c.MaPhan == maPhan && c.XoaTamCauHoi != true && c.MaCauHoiCha == null)
               .OrderBy(c => c.MaSoCauHoi)
               .ToListAsync();

    public async Task<HashSet<string>> GetExistingNoiDungAsync(Guid maPhan)
    {
        var list = await context.CauHois
            .Where(c => c.MaPhan == maPhan && c.XoaTamCauHoi != true && c.NoiDung != null)
            .Select(c => c.NoiDung!)
            .ToListAsync();
        return list.Select(Normalize).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

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

    public async Task BulkImportAsync(List<CauHoi> cauHois, List<CauTraLoi> cauTraLois, List<FileDinhKem>? files = null)
    {
        context.CauHois.AddRange(cauHois);
        context.CauTraLois.AddRange(cauTraLois);
        if (files is { Count: > 0 })
            context.Files.AddRange(files);
        await context.SaveChangesAsync();
    }

    private static string Normalize(string s)
        => System.Text.RegularExpressions.Regex.Replace(s.Trim(), @"\s+", " ");

    public async Task UpdateAsync(Guid maCauHoi, string noiDung, short capDo, List<(Guid id, string noiDung, bool laDapAn)> answers)
    {
        var cauHoi = await context.CauHois
            .Include(c => c.CauTraLois)
            .FirstOrDefaultAsync(c => c.MaCauHoi == maCauHoi);
        if (cauHoi is null) return;

        cauHoi.NoiDung  = noiDung;
        cauHoi.CapDo    = capDo;
        cauHoi.NgaySua  = DateTime.Now;

        foreach (var (id, nd, laDapAn) in answers)
        {
            var a = cauHoi.CauTraLois.FirstOrDefault(x => x.MaCauTraLoi == id);
            if (a is null) continue;
            a.NoiDung = nd;
            a.LaDapAn = laDapAn;
        }

        await context.SaveChangesAsync();
    }

    public async Task UpdateCauTraLoiThuTuAsync(List<(Guid maCauTraLoi, int thuTu)> orders)
    {
        var lookup = orders.ToDictionary(o => o.maCauTraLoi, o => o.thuTu);
        var rows = await context.CauTraLois
            .Where(a => lookup.Keys.Contains(a.MaCauTraLoi))
            .ToListAsync();
        foreach (var row in rows)
            row.ThuTu = lookup[row.MaCauTraLoi];
        await context.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(Guid id)
    {
        var cauHoi = await context.CauHois
            .Include(c => c.CauHoiCons)
            .FirstOrDefaultAsync(c => c.MaCauHoi == id);
        if (cauHoi is null) return;
        cauHoi.XoaTamCauHoi = true;
        foreach (var con in cauHoi.CauHoiCons)
            con.XoaTamCauHoi = true;
        await context.SaveChangesAsync();
    }
}
