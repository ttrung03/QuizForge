using QuestionBank.Web.Domain.Entities;

namespace QuestionBank.Web.Application.Interfaces;

public interface ICauHoiRepository
{
    Task<List<CauHoi>> GetByPhanAsync(Guid maPhan);
    Task<int> GetMaxMaSoCauHoiAsync(Guid maPhan);
    Task<HashSet<string>> GetExistingNoiDungAsync(Guid maPhan);
    Task AddWithAnswersAsync(CauHoi cauHoi, List<CauTraLoi> answers);
    Task BulkImportAsync(List<CauHoi> cauHois, List<CauTraLoi> cauTraLois, List<FileDinhKem>? files = null);
    Task SoftDeleteAsync(Guid id);
    Task UpdateAsync(Guid maCauHoi, string noiDung, short capDo, List<(Guid id, string noiDung, bool laDapAn)> answers);
    Task UpdateCauTraLoiThuTuAsync(List<(Guid maCauTraLoi, int thuTu)> orders);
    Task ReplaceAudioAsync(Guid maFile, string newTenFile);
    Task ReplaceImageAsync(Guid maFile, string newTenFile);
    Task<Guid> AddImageAsync(Guid maCauHoi, string tenFile);
}
