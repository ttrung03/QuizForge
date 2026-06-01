using QuestionBank.Web.Domain.Entities;

namespace QuestionBank.Web.Application.Interfaces;

public interface ICauHoiRepository
{
    Task<List<CauHoi>> GetByPhanAsync(Guid maPhan);
    Task<int> GetMaxMaSoCauHoiAsync(Guid maPhan);
    Task<HashSet<string>> GetExistingNoiDungAsync(Guid maPhan);
    Task AddWithAnswersAsync(CauHoi cauHoi, List<CauTraLoi> answers);
    Task BulkImportAsync(List<CauHoi> cauHois, List<CauTraLoi> cauTraLois);
    Task SoftDeleteAsync(Guid id);
}
