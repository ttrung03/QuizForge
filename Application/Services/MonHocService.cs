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
        return list.Select(m => new MonHocDto
        {
            MaMonHoc   = m.MaMonHoc,
            MaKhoa     = m.MaKhoa,
            TenKhoa    = m.Khoa?.TenKhoa,
            MaSoMonHoc = m.MaSoMonHoc,
            TenMonHoc  = m.TenMonHoc
        }).ToList();
    }

    /// <summary>
    /// Lấy môn học của một Khoa: gộp môn trực tiếp (LaMonChung=false)
    /// và môn dùng chung (LaMonChung=true), sắp xếp theo tên.
    /// </summary>
    public async Task<List<MonHocDto>> GetAllByKhoaAsync(Guid maKhoa)
    {
        var direct = await _repo.GetAllByKhoaAsync(maKhoa);
        var shared = await _repo.GetSharedByKhoaAsync(maKhoa);

        var result = direct.Select(m => new MonHocDto
        {
            MaMonHoc   = m.MaMonHoc,
            MaKhoa     = m.MaKhoa,
            TenKhoa    = m.Khoa?.TenKhoa,
            MaSoMonHoc = m.MaSoMonHoc,
            TenMonHoc  = m.TenMonHoc,
            LaMonChung = false
        })
        .Concat(shared.Select(m => new MonHocDto
        {
            MaMonHoc   = m.MaMonHoc,
            MaKhoa     = m.MaKhoa,
            TenKhoa    = m.Khoa?.TenKhoa,
            MaSoMonHoc = m.MaSoMonHoc,
            TenMonHoc  = m.TenMonHoc,
            LaMonChung = true
        }))
        .OrderBy(m => m.TenMonHoc)
        .ToList();

        return result;
    }

    /// <summary>
    /// Lấy môn học chưa có mặt trong khoa này — dùng cho dialog "Thêm môn học có sẵn".
    /// Trả về cả môn chưa gán khoa lẫn môn đang ở khoa khác.
    /// </summary>
    public async Task<List<MonHocDto>> GetAvailableForKhoaAsync(Guid maKhoa)
    {
        var list = await _repo.GetAvailableForKhoaAsync(maKhoa);
        return list.Select(m => new MonHocDto
        {
            MaMonHoc   = m.MaMonHoc,
            MaKhoa     = m.MaKhoa,
            TenKhoa    = m.Khoa?.TenKhoa,
            MaSoMonHoc = m.MaSoMonHoc,
            TenMonHoc  = m.TenMonHoc
        }).ToList();
    }

    /// <summary>
    /// Thêm môn học vào khoa:
    /// - Nếu môn chưa thuộc khoa nào (MaKhoa == null) → gán trực tiếp.
    /// - Nếu môn đã thuộc khoa khác → thêm vào MonHoc_KhoaChung (dùng chung).
    /// </summary>
    public async Task AssignToKhoaAsync(List<Guid> maMonHocList, Guid maKhoa)
    {
        foreach (var maMonHoc in maMonHocList)
        {
            var monHoc = await _repo.GetByIdAsync(maMonHoc);
            if (monHoc is null) continue;

            if (monHoc.MaKhoa == null)
                await _repo.AssignToKhoaAsync(maMonHoc, maKhoa);
            else
                await _repo.AddSharedAsync(maMonHoc, maKhoa);
        }
    }

    /// <summary>Gỡ môn dùng chung khỏi một khoa (xóa khỏi MonHoc_KhoaChung).</summary>
    public async Task RemoveSharedAsync(Guid maMonHoc, Guid maKhoa)
        => await _repo.RemoveSharedAsync(maMonHoc, maKhoa);

    /// Tạo mới hoặc cập nhật Môn học tuỳ theo dto.MaMonHoc.
    public async Task SaveAsync(SaveMonHocDto dto)
    {
        if (dto.MaMonHoc == null)
        {
            await _repo.AddAsync(new MonHoc
            {
                MaKhoa     = dto.MaKhoa,
                MaSoMonHoc = dto.MaSoMonHoc.Trim(),
                TenMonHoc  = dto.TenMonHoc.Trim()
            });
        }
        else
        {
            var monHoc = await _repo.GetByIdAsync(dto.MaMonHoc.Value)
                         ?? throw new Exception("Không tìm thấy môn học cần cập nhật.");

            monHoc.MaSoMonHoc = dto.MaSoMonHoc.Trim();
            monHoc.TenMonHoc  = dto.TenMonHoc.Trim();
            await _repo.UpdateAsync(monHoc);
        }
    }

    /// Xóa môn học khỏi khoa sở hữu.
    /// Nếu có khoa khác đang dùng chung → chuyển sở hữu sang khoa đó thay vì xóa mềm.
    /// Nếu không có khoa nào dùng chung → xóa mềm bình thường.
    public async Task DeleteAsync(Guid id)
    {
        var firstSharedKhoa = await _repo.GetFirstSharedKhoaAsync(id);
        if (firstSharedKhoa.HasValue)
        {
            // Chuyển sở hữu: gỡ KhoaChung → gán MaKhoa trực tiếp
            await _repo.RemoveSharedAsync(id, firstSharedKhoa.Value);
            await _repo.AssignToKhoaAsync(id, firstSharedKhoa.Value);
        }
        else
        {
            await _repo.SoftDeleteAsync(id);
        }
    }
}
