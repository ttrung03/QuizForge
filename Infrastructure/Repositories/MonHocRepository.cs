using Microsoft.EntityFrameworkCore;
using QuestionBank.Web.Application.Interfaces;
using QuestionBank.Web.Domain.Entities;
using QuestionBank.Web.Infrastructure.Data;

namespace QuestionBank.Web.Infrastructure.Repositories;

/// <summary>
/// Thực thi IMonHocRepository — toàn bộ query EF Core nằm ở đây.
/// Service không biết gì về EF Core hay SQL.
/// </summary>
public class MonHocRepository : IMonHocRepository
{
    private readonly QuestionBankDbContext _context;

    public MonHocRepository(QuestionBankDbContext context)
    {
        _context = context;
    }

    /// <summary>Lấy tất cả Môn học chưa bị xóa mềm, include Khoa, sắp xếp theo tên.</summary>
    public async Task<List<MonHoc>> GetAllAsync()
        => await _context.MonHocs
               .Include(m => m.Khoa)
               .Where(m => m.XoaTamMonHoc != true)
               .OrderBy(m => m.TenMonHoc)
               .ToListAsync();

    public async Task<MonHoc?> GetByIdAsync(Guid id)
        => await _context.MonHocs.FindAsync(id);

    public async Task AddAsync(MonHoc monHoc)
    {
        monHoc.MaMonHoc = Guid.NewGuid();
        _context.MonHocs.Add(monHoc);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(MonHoc monHoc)
    {
        _context.MonHocs.Update(monHoc);
        await _context.SaveChangesAsync();
    }

    /// <summary>Xóa cứng — dùng khi thực sự muốn xóa khỏi DB.</summary>
    public async Task DeleteAsync(Guid id)
    {
        var monHoc = await _context.MonHocs.FindAsync(id);
        if (monHoc is null) return;

        _context.MonHocs.Remove(monHoc);
        await _context.SaveChangesAsync();
    }

    /// <summary>Xóa mềm — chỉ đánh dấu, dữ liệu vẫn còn trong DB.</summary>
    public async Task SoftDeleteAsync(Guid id)
    {
        var monHoc = await _context.MonHocs.FindAsync(id);
        if (monHoc is null) return;

        monHoc.XoaTamMonHoc = true;
        await _context.SaveChangesAsync();
    }
}
