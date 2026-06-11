using Microsoft.EntityFrameworkCore;
using QuestionBank.Web.Application.Interfaces;
using QuestionBank.Web.Domain.Entities;
using QuestionBank.Web.Infrastructure.Data;

namespace QuestionBank.Web.Infrastructure.Repositories;

/// <summary>
/// Thực thi IPhanRepository — toàn bộ query EF Core nằm ở đây.
/// Service không biết gì về EF Core hay SQL.
/// </summary>
public class PhanRepository : IPhanRepository
{
    private readonly QuestionBankDbContext _context;

    public PhanRepository(QuestionBankDbContext context)
    {
        _context = context;
    }

    /// <summary>Lấy tất cả Phần chưa bị xóa mềm, include MonHoc, sắp xếp theo ThuTu.</summary>
    public async Task<List<Phan>> GetAllAsync()
        => await _context.Phans
               .Include(p => p.MaMonHocNavigation)
               .Include(p => p.CauHois)
               .Where(p => p.XoaTamPhan != true)
               .OrderBy(p => p.ThuTu)
               .ToListAsync();

    /// <summary>Lấy tất cả Phần của một MonHoc, chưa bị xóa mềm, sắp xếp theo ThuTu.</summary>
    public async Task<List<Phan>> GetAllByMonHocAsync(Guid maMonHoc)
        => await _context.Phans
               .Include(p => p.MaMonHocNavigation)
               .Include(p => p.CauHois)
               .Where(p => p.MaMonHoc == maMonHoc && p.XoaTamPhan != true)
               .OrderBy(p => p.ThuTu)
               .ToListAsync();

    public async Task<Phan?> GetByIdAsync(Guid id)
        => await _context.Phans.FindAsync(id);

    public async Task AddAsync(Phan phan)
    {
        // Kiểm tra tên phần đã tồn tại trong cùng MonHoc chưa
        bool existPhan = await _context.Phans.AnyAsync(p =>
            p.MaMonHoc == phan.MaMonHoc &&
            p.TenPhan == phan.TenPhan &&
            p.XoaTamPhan != true);

        if (existPhan)
        {
            throw new InvalidOperationException(
                $"Tên phần '{phan.TenPhan}' đã tồn tại trong môn học này.");
        }

        phan.MaPhan = Guid.NewGuid();
        _context.Phans.Add(phan);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Phan phan)
    {
        // Kiểm tra tên phần trùng trong cùng MonHoc (loại trừ chính bản ghi đang sửa)
        bool existPhan = await _context.Phans.AnyAsync(p =>
            p.MaMonHoc == phan.MaMonHoc &&
            p.TenPhan == phan.TenPhan &&
            p.MaPhan != phan.MaPhan &&
            p.XoaTamPhan != true);

        if (existPhan)
        {
            throw new InvalidOperationException(
                $"Tên phần '{phan.TenPhan}' đã tồn tại trong môn học này.");
        }

        _context.Phans.Update(phan);
        await _context.SaveChangesAsync();
    }

    /// <summary>Xóa cứng — dùng khi thực sự muốn xóa khỏi DB.</summary>
    public async Task DeleteAsync(Guid id)
    {
        var phan = await _context.Phans.FindAsync(id);
        if (phan is null) return;

        _context.Phans.Remove(phan);
        await _context.SaveChangesAsync();
    }

    /// <summary>Xóa mềm — chỉ đánh dấu, dữ liệu vẫn còn trong DB.</summary>
    public async Task SoftDeleteAsync(Guid id)
    {
        var phan = await _context.Phans.FindAsync(id);
        if (phan is null) return;

        phan.XoaTamPhan = true;
        await _context.SaveChangesAsync();
    }
}
