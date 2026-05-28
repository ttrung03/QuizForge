using QuestionBank.Web.Domain.Entities;

namespace QuestionBank.Web.Application.Interfaces;

public interface IDeThiRepository
{
    Task<List<DeThi>> GetByMonHocAsync(Guid maMonHoc);
    Task<DeThi?> GetByIdAsync(Guid maDeThi);
    Task AddAsync(DeThi deThi, List<ChiTietDeThi> chiTiets);
    Task DeleteAsync(Guid maDeThi);
    Task UpdateDaDuyetAsync(Guid maDeThi, bool daDuyet);
    Task RemoveCauHoisAsync(Guid maDeThi, List<Guid> maCauHois);
    Task AddCauHoisAsync(Guid maDeThi, List<(Guid maCauHoi, Guid maPhan)> cauHois);
    Task UpdateThuTuAsync(Guid maDeThi, List<(Guid maCauHoi, int thuTu)> orders);
}
