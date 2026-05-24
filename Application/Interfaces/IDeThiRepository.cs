using QuestionBank.Web.Domain.Entities;

namespace QuestionBank.Web.Application.Interfaces;

public interface IDeThiRepository
{
    Task<List<DeThi>> GetByMonHocAsync(Guid maMonHoc);
    Task<DeThi?> GetByIdAsync(Guid maDeThi);
    Task AddAsync(DeThi deThi, List<ChiTietDeThi> chiTiets);
    Task DeleteAsync(Guid maDeThi);
}
