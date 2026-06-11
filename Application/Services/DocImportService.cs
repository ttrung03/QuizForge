using QuestionBank.Web.Application.DTOs;

namespace QuestionBank.Web.Application.Services;

/// <summary>
/// Xử lý file .doc (Word 97-2003 binary format).
/// Định dạng .doc là định dạng nhị phân cũ, không hỗ trợ đọc trực tiếp trên .NET hiện đại
/// mà không cần thư viện thương mại. Service này hướng dẫn người dùng convert sang .docx.
/// </summary>
public class DocImportService
{
    public (List<ImportCauHoiDto> Questions, List<ImportCauHoiNhomDto> Groups, List<string> Warnings) Parse(Stream stream)
    {
        var warnings = new List<string>
        {
            "Định dạng .doc (Word 97-2003) không được hỗ trợ import trực tiếp.",
            "Vui lòng mở file trong Microsoft Word → Lưu dưới dạng → Word Document (.docx) → import lại."
        };
        return ([], [], warnings);
    }
}
