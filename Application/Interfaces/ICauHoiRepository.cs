using QuestionBank.Web.Domain.Entities;

namespace QuestionBank.Web.Application.Interfaces;

public interface ICauHoiRepository
{
    Task<List<CauHoi>> GetByPhanAsync(Guid maPhan);
    Task<int> GetMaxMaSoCauHoiAsync(Guid maPhan);
    Task AddWithAnswersAsync(CauHoi cauHoi, List<CauTraLoi> answers);
    Task SoftDeleteAsync(Guid id);
}
