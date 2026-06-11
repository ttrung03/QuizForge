using ClosedXML.Excel;
using QuestionBank.Web.Application.DTOs;

namespace QuestionBank.Web.Application.Services;

/// <summary>
/// Parse file .xlsx theo template chuẩn:
/// Cột A: Nội dung câu hỏi
/// Cột B: Đáp án A
/// Cột C: Đáp án B
/// Cột D: Đáp án C
/// Cột E: Đáp án D
/// Cột F: Đáp án đúng (A/B/C/D)
/// Cột G: Cấp độ (1/2/3) — tuỳ chọn
/// Cột H: CLO — tuỳ chọn (ví dụ: CLO1)
/// Hàng đầu tiên là tiêu đề, dữ liệu bắt đầu từ hàng 2.
/// </summary>
public class ExcelImportService
{
    public (List<ImportCauHoiDto> Questions, List<ImportCauHoiNhomDto> Groups, List<string> Warnings) Parse(Stream stream)
    {
        var questions = new List<ImportCauHoiDto>();
        var groups    = new List<ImportCauHoiNhomDto>();
        var warnings  = new List<string>();

        XLWorkbook workbook;
        try { workbook = new XLWorkbook(stream); }
        catch (Exception ex)
        {
            warnings.Add($"Không thể đọc file .xlsx: {ex.Message}");
            return (questions, groups, warnings);
        }

        var sheet = workbook.Worksheets.FirstOrDefault();
        if (sheet is null)
        {
            warnings.Add("File Excel không có sheet nào.");
            return (questions, groups, warnings);
        }

        int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            string noiDung = GetCell(sheet, row, 1);
            string dapAnA  = GetCell(sheet, row, 2);
            string dapAnB  = GetCell(sheet, row, 3);
            string dapAnC  = GetCell(sheet, row, 4);
            string dapAnD  = GetCell(sheet, row, 5);
            string dapAnDung = GetCell(sheet, row, 6).ToUpperInvariant().Trim();
            string capDoRaw  = GetCell(sheet, row, 7).Trim();
            string cloRaw    = GetCell(sheet, row, 8).Trim();

            if (string.IsNullOrWhiteSpace(noiDung)) continue;

            if (string.IsNullOrWhiteSpace(dapAnA) || string.IsNullOrWhiteSpace(dapAnB))
            {
                warnings.Add($"Hàng {row}: Câu hỏi \"{Truncate(noiDung)}\" thiếu đáp án, bị bỏ qua.");
                continue;
            }

            if (!new[] { "A", "B", "C", "D" }.Contains(dapAnDung))
            {
                warnings.Add($"Hàng {row}: Câu hỏi \"{Truncate(noiDung)}\" có đáp án đúng không hợp lệ (\"{dapAnDung}\"), bị bỏ qua.");
                continue;
            }

            short? capDo = null;
            if (!string.IsNullOrEmpty(capDoRaw) && short.TryParse(capDoRaw, out short capDoParsed) && capDoParsed is >= 1 and <= 3)
                capDo = capDoParsed;

            string? cloText = string.IsNullOrEmpty(cloRaw) ? null
                : (cloRaw.StartsWith("(", StringComparison.Ordinal) ? cloRaw : $"({cloRaw})");

            var answers = new List<ImportCauTraLoiDto>();
            var answerTexts = new[] { dapAnA, dapAnB, dapAnC, dapAnD };
            var letters = new[] { "A", "B", "C", "D" };

            for (int idx = 0; idx < 4; idx++)
            {
                if (string.IsNullOrWhiteSpace(answerTexts[idx])) continue;
                answers.Add(new ImportCauTraLoiDto
                {
                    ThuTu   = (short)(idx + 1),
                    NoiDung = answerTexts[idx].Trim(),
                    LaDapAn = letters[idx] == dapAnDung,
                    HoanVi  = true
                });
            }

            questions.Add(new ImportCauHoiDto
            {
                NoiDung      = noiDung.Trim(),
                CloText      = cloText,
                CapDo        = capDo,
                Answers      = answers,
                AnhFiles     = [],
                AmThanhFiles = []
            });
        }

        return (questions, groups, warnings);
    }

    private static string GetCell(IXLWorksheet sheet, int row, int col)
        => sheet.Cell(row, col).GetString() ?? string.Empty;

    private static string Truncate(string s, int max = 60)
        => s.Length <= max ? s : s[..max] + "…";
}
