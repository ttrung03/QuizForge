using Microsoft.EntityFrameworkCore;
using QuestionBank.Web.Application.Interfaces;
using QuestionBank.Web.Domain.Entities;
using QuestionBank.Web.Infrastructure.Data;

namespace QuestionBank.Web.Infrastructure.Repositories;

/// <summary>
/// Thực thi IKhoaRepository — toàn bộ query EF Core nằm ở đây.
/// Service không biết gì về EF Core hay SQL.
/// </summary>
public class KhoaRepository : IKhoaRepository
{
    private readonly QuestionBankDbContext _context;

    // Constructor Injection: ASP.NET Core tự inject DbContext vào đây
    public KhoaRepository(QuestionBankDbContext context)
    {
        _context = context;
    }

    /// <summary>Lấy tất cả Khoa chưa bị xóa mềm, sắp xếp theo tên.</summary>
    public async Task<List<Khoa>> GetAllAsync()
        => await _context.Khoas
               .Where(k => k.XoaTamKhoa != true)
               .OrderBy(k => k.TenKhoa)
               .ToListAsync();

    public async Task<Khoa?> GetByIdAsync(Guid id)
        => await _context.Khoas.FindAsync(id);

    public async Task AddAsync(Khoa khoa)
    {
        // Kiểm tra tên khoa đã tồn tại trong hệ thống chưa
        bool existed = await _context.Khoas.AnyAsync(k =>
            k.TenKhoa == khoa.TenKhoa &&
            k.XoaTamKhoa != true);

        if (existed)
        {
            throw new InvalidOperationException(
                $"Tên khoa '{khoa.TenKhoa}' đã tồn tại.");
        }

        khoa.MaKhoa = Guid.NewGuid();
        _context.Khoas.Add(khoa);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Khoa khoa)
    {
        // Kiểm tra tên khoa trùng (loại trừ chính bản ghi đang sửa)
        bool existed = await _context.Khoas.AnyAsync(k =>
            k.TenKhoa == khoa.TenKhoa &&
            k.MaKhoa != khoa.MaKhoa &&
            k.XoaTamKhoa != true);

        if (existed)
        {
            throw new InvalidOperationException(
                $"Tên khoa '{khoa.TenKhoa}' đã tồn tại.");
        }

        _context.Khoas.Update(khoa);
        await _context.SaveChangesAsync();
    }

    /// <summary>Xóa cứng — dùng khi thực sự muốn xóa khỏi DB.</summary>
    public async Task DeleteAsync(Guid id)
    {
        var khoa = await _context.Khoas.FindAsync(id);
        if (khoa is null) return;

        _context.Khoas.Remove(khoa);
        await _context.SaveChangesAsync();
    }

    /// <summary>Xóa mềm — chỉ đánh dấu, dữ liệu vẫn còn trong DB.</summary>
    public async Task SoftDeleteAsync(Guid id)
    {
        var khoa = await _context.Khoas.FindAsync(id);
        if (khoa is null) return;

        khoa.XoaTamKhoa = true;
        await _context.SaveChangesAsync();
    }
}