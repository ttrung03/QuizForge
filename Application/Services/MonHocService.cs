using QuestionBank.Web.Application.DTOs;
using QuestionBank.Web.Application.Interfaces;
using QuestionBank.Web.Domain.Entities;

namespace QuestionBank.Web.Application.Services;

/// <summary>
/// Chứa logic nghiệp vụ của Môn học.
/// Gọi IMonHocRepository để thao tác DB — không biết cách query cụ thể.
/// </summary>
public class MonHocService
{
    private readonly IMonHocRepository _repo;

    public MonHocService(IMonHocRepository repo) => _repo = repo;

    /// Lấy tất cả Môn học chưa xóa mềm, trả về DTO để UI dùng.
    public async Task<List<MonHocDto>> GetAllAsync()
    {
        var list = await _repo.GetAllAsync();

        // Entity → DTO (ánh xạ thủ công, không cần AutoMapper)
        return list.Select(m => new MonHocDto
        {
            MaMonHoc   = m.MaMonHoc,
            MaKhoa     = m.MaKhoa,
            TenKhoa    = m.Khoa?.TenKhoa ?? string.Empty,
            MaSoMonHoc = m.MaSoMonHoc,
            TenMonHoc  = m.TenMonHoc
        }).ToList();
    }

    /// Tạo mới hoặc cập nhật Môn học tuỳ theo dto.MaMonHoc.
    public async Task SaveAsync(SaveMonHocDto dto)
    {
        if (dto.MaMonHoc == null)
        {
            // ── Tạo mới ──────────────────────────────────────────────────────
            await _repo.AddAsync(new MonHoc
            {
                MaKhoa     = dto.MaKhoa,
                MaSoMonHoc = dto.MaSoMonHoc.Trim(),
                TenMonHoc  = dto.TenMonHoc.Trim()
            });
        }
        else
        {
            // ── Cập nhật ─────────────────────────────────────────────────────
            var monHoc = await _repo.GetByIdAsync(dto.MaMonHoc.Value)
                         ?? throw new Exception("Không tìm thấy môn học cần cập nhật.");

            monHoc.MaKhoa     = dto.MaKhoa;
            monHoc.MaSoMonHoc = dto.MaSoMonHoc.Trim();
            monHoc.TenMonHoc  = dto.TenMonHoc.Trim();
            await _repo.UpdateAsync(monHoc);
        }
    }

    /// Xóa mềm: đánh dấu XoaTamMonHoc = true, không xóa khỏi DB.
    public async Task DeleteAsync(Guid id) => await _repo.SoftDeleteAsync(id);
}
