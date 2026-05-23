using QuestionBank.Web.Domain.Entities;

namespace QuestionBank.Web.Application.Interfaces;

/// <summary>
/// 'Hợp đồng' cho MonHocRepository.
/// Định nghĩa những việc có thể làm với bảng MonHoc — không quan tâm cách làm.
/// </summary>
public interface IMonHocRepository
{
    Task<List<MonHoc>> GetAllAsync();

    /// <summary>Lấy môn học trực tiếp thuộc khoa (MaKhoa = maKhoa).</summary>
    Task<List<MonHoc>> GetAllByKhoaAsync(Guid maKhoa);

    /// <summary>Lấy môn học được dùng chung cho khoa (trong bảng MonHoc_KhoaChung).</summary>
    Task<List<MonHoc>> GetSharedByKhoaAsync(Guid maKhoa);

    /// <summary>Lấy môn học chưa thuộc khoa nào và chưa được share cho khoa này.</summary>
    Task<List<MonHoc>> GetAvailableForKhoaAsync(Guid maKhoa);

    Task<MonHoc?> GetByIdAsync(Guid id);
    Task AddAsync(MonHoc monHoc);
    Task UpdateAsync(MonHoc monHoc);

    /// <summary>Gán môn học trực tiếp vào khoa (set MaKhoa).</summary>
    Task AssignToKhoaAsync(Guid maMonHoc, Guid maKhoa);

    /// <summary>Thêm vào bảng MonHoc_KhoaChung (dùng chung).</summary>
    Task AddSharedAsync(Guid maMonHoc, Guid maKhoa);

    /// <summary>Xóa khỏi bảng MonHoc_KhoaChung (gỡ dùng chung).</summary>
    Task RemoveSharedAsync(Guid maMonHoc, Guid maKhoa);

    /// <summary>Lấy MaKhoa đầu tiên đang dùng chung môn này (null nếu không có).</summary>
    Task<Guid?> GetFirstSharedKhoaAsync(Guid maMonHoc);

    /// <summary>Xóa cứng hoàn toàn môn học: CauHoi → Phan → KhoaChung → MonHoc.</summary>
    Task DeleteAllCauHoiByMonAsync(Guid maMonHoc);

    Task SoftDeleteAsync(Guid id);  // xóa mềm: XoaTamMonHoc = true
}
