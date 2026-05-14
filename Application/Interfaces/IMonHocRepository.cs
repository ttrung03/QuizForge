using QuestionBank.Web.Domain.Entities;

namespace QuestionBank.Web.Application.Interfaces;

/// <summary>
/// 'Hợp đồng' cho MonHocRepository.
/// Định nghĩa những việc có thể làm với bảng MonHoc — không quan tâm cách làm.
/// </summary>
public interface IMonHocRepository
{
    Task<List<MonHoc>> GetAllAsync();
    Task<MonHoc?> GetByIdAsync(Guid id);
    Task AddAsync(MonHoc monHoc);
    Task UpdateAsync(MonHoc monHoc);
    Task DeleteAsync(Guid id);      // xóa cứng (dùng khi cần)
    Task SoftDeleteAsync(Guid id);  // xóa mềm: XoaTamMonHoc = true
}
