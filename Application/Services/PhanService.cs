using QuestionBank.Web.Application.DTOs;
using QuestionBank.Web.Application.Interfaces;
using QuestionBank.Web.Domain.Entities;

namespace QuestionBank.Web.Application.Services;

/// <summary>
/// Chứa logic nghiệp vụ của Phần.
/// Gọi IPhanRepository để thao tác DB — không biết cách query cụ thể.
/// </summary>
public class PhanService
{
    private readonly IPhanRepository _repo;

    public PhanService(IPhanRepository repo) => _repo = repo;

    /// Lấy tất cả Phần chưa xóa mềm, trả về DTO để UI dùng.
    public async Task<List<PhanDto>> GetAllAsync()
    {
        var list = await _repo.GetAllAsync();

        // Entity → DTO (ánh xạ thủ công, không cần AutoMapper)
        return list.Select(p => new PhanDto
        {
            MaPhan       = p.MaPhan,
            MaMonHoc     = p.MaMonHoc,
            TenMonHoc    = p.MaMonHocNavigation?.TenMonHoc ?? string.Empty,
            TenPhan      = p.TenPhan,
            NoiDung      = p.NoiDung,
            ThuTu        = p.ThuTu,
            SoLuongCauHoi = p.SoLuongCauHoi,
            MaPhanCha    = p.MaPhanCha,
            MaSoPhan     = p.MaSoPhan,
            LaCauHoiNhom = p.LaCauHoiNhom
        }).ToList();
    }

    /// Lấy tất cả Phần thuộc một Môn học.
    public async Task<List<PhanDto>> GetAllByMonHocAsync(Guid maMonHoc)
    {
        var list = await _repo.GetAllByMonHocAsync(maMonHoc);

        return list.Select(p => new PhanDto
        {
            MaPhan       = p.MaPhan,
            MaMonHoc     = p.MaMonHoc,
            TenMonHoc    = p.MaMonHocNavigation?.TenMonHoc ?? string.Empty,
            TenPhan      = p.TenPhan,
            NoiDung      = p.NoiDung,
            ThuTu        = p.ThuTu,
            SoLuongCauHoi = p.SoLuongCauHoi,
            MaPhanCha    = p.MaPhanCha,
            MaSoPhan     = p.MaSoPhan,
            LaCauHoiNhom = p.LaCauHoiNhom
        }).ToList();
    }

    /// Tạo mới hoặc cập nhật Phần tuỳ theo dto.MaPhan.
    public async Task SaveAsync(SavePhanDto dto)
    {
        if (dto.MaPhan == null)
        {
            // ── Tạo mới ──────────────────────────────────────────────────────
            await _repo.AddAsync(new Phan
            {
                MaMonHoc     = dto.MaMonHoc,
                TenPhan      = dto.TenPhan.Trim(),
                NoiDung      = dto.NoiDung?.Trim(),
                ThuTu        = dto.ThuTu,
                SoLuongCauHoi = dto.SoLuongCauHoi,
                MaPhanCha    = dto.MaPhanCha,
                MaSoPhan     = dto.MaSoPhan,
                LaCauHoiNhom = dto.LaCauHoiNhom
            });
        }
        else
        {
            // ── Cập nhật ─────────────────────────────────────────────────────
            var phan = await _repo.GetByIdAsync(dto.MaPhan.Value)
                       ?? throw new Exception("Không tìm thấy phần cần cập nhật.");

            phan.MaMonHoc     = dto.MaMonHoc;
            phan.TenPhan      = dto.TenPhan.Trim();
            phan.NoiDung      = dto.NoiDung?.Trim();
            phan.ThuTu        = dto.ThuTu;
            phan.SoLuongCauHoi = dto.SoLuongCauHoi;
            phan.MaPhanCha    = dto.MaPhanCha;
            phan.MaSoPhan     = dto.MaSoPhan;
            phan.LaCauHoiNhom = dto.LaCauHoiNhom;
            await _repo.UpdateAsync(phan);
        }
    }

    /// Xóa mềm: đánh dấu XoaTamPhan = true, không xóa khỏi DB.
    public async Task DeleteAsync(Guid id) => await _repo.SoftDeleteAsync(id);
}
