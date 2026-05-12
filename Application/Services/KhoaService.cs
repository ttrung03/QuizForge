using QuestionBank.Web.Application.DTOs;
using QuestionBank.Web.Application.Interfaces;
using QuestionBank.Web.Domain.Entities;

namespace QuestionBank.Web.Application.Services;

/// <summary>
/// Chứa logic nghiệp vụ của Khoa.
/// Gọi IKhoaRepository để thao tác DB — không biết cách query cụ thể.
/// </summary>
public class KhoaService
{
    private readonly IKhoaRepository _repo;

    public KhoaService(IKhoaRepository repo) => _repo = repo;

    /// Lấy tất cả Khoa chưa xóa mềm, trả về DTO để UI dùng.
    public async Task<List<KhoaDto>> GetAllAsync()
    {
        var list = await _repo.GetAllAsync();

        // Entity → DTO (ánh xạ thủ công, không cần AutoMapper)
        return list.Select(k => new KhoaDto
        {
            MaKhoa = k.MaKhoa,
            TenKhoa = k.TenKhoa
        }).ToList();
    }

 
    /// Tạo mới hoặc cập nhật Khoa tuỳ theo dto.MaKhoa.
  
    public async Task SaveAsync(SaveKhoaDto dto)
    {
        if (dto.MaKhoa == null)
        {
            // ── Tạo mới ──────────────────────────────────────────────────────
            await _repo.AddAsync(new Khoa
            {
                TenKhoa = dto.TenKhoa.Trim()
            });
        }
        else
        {
            // ── Cập nhật ─────────────────────────────────────────────────────
            var khoa = await _repo.GetByIdAsync(dto.MaKhoa.Value)
                       ?? throw new Exception("Không tìm thấy khoa cần cập nhật.");

            khoa.TenKhoa = dto.TenKhoa.Trim();
            await _repo.UpdateAsync(khoa);
        }
    }

    /// Xóa mềm: đánh dấu XoaTamKhoa = true, không xóa khỏi DB.</summary>
    public async Task DeleteAsync(Guid id) => await _repo.SoftDeleteAsync(id);
}