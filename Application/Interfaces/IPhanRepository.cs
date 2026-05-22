using QuestionBank.Web.Domain.Entities;

namespace QuestionBank.Web.Application.Interfaces;

/// <summary>
/// 'Hợp đồng' cho PhanRepository.
/// Định nghĩa những việc có thể làm với bảng Phan — không quan tâm cách làm.
/// </summary>
public interface IPhanRepository
{
    Task<List<Phan>> GetAllAsync();
    Task<List<Phan>> GetAllByMonHocAsync(Guid maMonHoc);
    Task<Phan?> GetByIdAsync(Guid id);
    Task AddAsync(Phan phan);
    Task UpdateAsync(Phan phan);
    Task DeleteAsync(Guid id);      // xóa cứng (dùng khi cần)
    Task SoftDeleteAsync(Guid id);  // xóa mềm: XoaTamPhan = true
}
