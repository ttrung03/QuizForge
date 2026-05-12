using QuestionBank.Web.Domain.Entities;

namespace QuestionBank.Web.Application.Interfaces;

/// <summary>
/// 'Hợp đồng' cho KhoaRepository.
/// Định nghĩa những việc có thể làm với bảng Khoa — không quan tâm cách làm.
/// </summary>
public interface IKhoaRepository
{
    Task<List<Khoa>> GetAllAsync();
    Task<Khoa?> GetByIdAsync(Guid id);
    Task AddAsync(Khoa khoa);
    Task UpdateAsync(Khoa khoa);
    Task DeleteAsync(Guid id);      // xóa cứng (dùng khi cần)
    Task SoftDeleteAsync(Guid id);  // xóa mềm: XoaTamKhoa = true
}
