using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QuestionBank.Web.Application.DTOs;

namespace QuestionBank.Web.Application.Services;

/// <summary>
/// Parse file .docx theo định dạng chuẩn HUTECH (Số 73/HD-KT).
/// Chỉ xử lý câu hỏi đơn; câu nhóm và câu nghe không nằm trong phạm vi.
/// </summary>
public class WordImportService
{
    private static readonly Regex CloPrefix = new(@"^\(CLO[^)]*\)\s*", RegexOptions.Compiled);
    private static readonly Regex AnswerLine = new(@"^([ABCD])\.\s*(.*)", RegexOptions.Compiled | RegexOptions.Singleline);
    private const string EndMarker = "[<br>]";

    public (List<ImportCauHoiDto> Questions, List<string> Warnings) Parse(Stream stream)
    {
        var questions = new List<ImportCauHoiDto>();
        var warnings = new List<string>();

        using var doc = WordprocessingDocument.Open(stream, isEditable: false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) return (questions, warnings);

        var paragraphs = body.Elements<Paragraph>().ToList();

        ImportCauHoiDto? current = null;
        int paraIndex = 0;

        foreach (var para in paragraphs)
        {
            paraIndex++;
            string text = GetParagraphText(para).Trim();

            if (string.IsNullOrEmpty(text))
                continue;

            // Bỏ qua ký hiệu nhóm — không xử lý trong phiên bản này
            if (text is "[<sg>]" or "[</sg>]" or "[<egc>]")
                continue;

            if (text == EndMarker)
            {
                if (current is not null)
                {
                    var warning = ValidateQuestion(current, paraIndex);
                    if (warning is null)
                        questions.Add(current);
                    else
                        warnings.Add(warning);
                }
                current = null;
                continue;
            }

            // Dòng bắt đầu câu hỏi: (CLO...) hoặc (<n>)(CLO...)
            if (IsCloLine(text))
            {
                current = new ImportCauHoiDto
                {
                    CloText = ExtractClo(text),
                    NoiDung = CloPrefix.Replace(StripSubQuestionIndex(text), "").Trim()
                };
                continue;
            }

            // Dòng đáp án: A. / B. / C. / D.
            var match = AnswerLine.Match(text);
            if (match.Success && current is not null)
            {
                char letter = match.Groups[1].Value[0];
                bool isCorrect = HasUnderline(para);
                bool noPermute = HasItalic(para);

                current.Answers.Add(new ImportCauTraLoiDto
                {
                    ThuTu = "ABCD".IndexOf(letter) + 1,
                    NoiDung = match.Groups[2].Value.Trim(),
                    LaDapAn = isCorrect,
                    HoanVi = !noPermute
                });
            }
        }

        // Câu dang dở cuối file (thiếu [<br>]) → bỏ qua
        if (current is not null)
            warnings.Add($"Câu hỏi cuối file chưa có [<br>] kết thúc, bị bỏ qua: \"{Truncate(current.NoiDung)}\"");

        return (questions, warnings);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static string GetParagraphText(Paragraph para)
        => string.Concat(para.Elements<Run>().Select(r => r.InnerText));

    private static bool IsCloLine(string text)
    {
        // Khớp: (CLO1) ... hoặc (<1>)(CLO1) ...
        var stripped = StripSubQuestionIndex(text);
        return stripped.StartsWith("(CLO", StringComparison.OrdinalIgnoreCase)
            || stripped.StartsWith("(clo", StringComparison.OrdinalIgnoreCase);
    }

    private static string StripSubQuestionIndex(string text)
    {
        // Loại bỏ prefix (<n>) nếu có, ví dụ: (<1>)(CLO1) ...
        var m = Regex.Match(text, @"^\(<\d+>\)\s*(.+)");
        return m.Success ? m.Groups[1].Value : text;
    }

    private static string? ExtractClo(string text)
    {
        var stripped = StripSubQuestionIndex(text);
        var m = Regex.Match(stripped, @"^\(CLO[^)]*\)", RegexOptions.IgnoreCase);
        return m.Success ? m.Value : null;
    }

    /// <summary>
    /// Kiểm tra run đầu tiên chứa ký tự A/B/C/D có underline không.
    /// Underline được đặt trên ký tự letter + dấu chấm để chỉ đáp án đúng.
    /// </summary>
    private static bool HasUnderline(Paragraph para)
    {
        foreach (var run in para.Elements<Run>())
        {
            var inner = run.InnerText;
            if (inner.Length == 0) continue;
            if (!"ABCD".Contains(inner[0])) continue;

            var u = run.RunProperties?.Underline;
            if (u is null) return false;
            var val = u.Val?.Value;
            return val is not null && val != UnderlineValues.None;
        }
        return false;
    }

    /// <summary>
    /// Kiểm tra run đầu tiên chứa ký tự A/B/C/D có italic không.
    /// Italic trên letter nghĩa là đáp án này không được hoán vị.
    /// </summary>
    private static bool HasItalic(Paragraph para)
    {
        foreach (var run in para.Elements<Run>())
        {
            var inner = run.InnerText;
            if (inner.Length == 0) continue;
            if (!"ABCD".Contains(inner[0])) continue;

            return run.RunProperties?.Italic != null;
        }
        return false;
    }

    private static string? ValidateQuestion(ImportCauHoiDto q, int paraIndex)
    {
        if (string.IsNullOrWhiteSpace(q.NoiDung))
            return $"Câu hỏi trống tại [<br>] dòng ~{paraIndex}, bị bỏ qua.";
        if (q.Answers.Count < 2)
            return $"Câu hỏi \"{Truncate(q.NoiDung)}\" có ít hơn 2 đáp án, bị bỏ qua.";
        if (!q.Answers.Any(a => a.LaDapAn))
            return $"Câu hỏi \"{Truncate(q.NoiDung)}\" không có đáp án đúng (chưa gạch chân), bị bỏ qua.";
        return null;
    }

    private static string Truncate(string s, int max = 60)
        => s.Length <= max ? s : s[..max] + "…";
}
