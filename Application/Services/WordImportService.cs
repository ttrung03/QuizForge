using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QuestionBank.Web.Application.DTOs;

namespace QuestionBank.Web.Application.Services;

/// <summary>
/// Parse file .docx theo định dạng chuẩn HUTECH (Số 73/HD-KT).
/// Hỗ trợ câu hỏi đơn, câu hỏi nhóm, và media (hình ảnh, âm thanh) qua marker
/// [&lt;img&gt;filename.png&lt;/img&gt;] và [&lt;audio&gt;filename.mp3&lt;/audio&gt;].
/// </summary>
public class WordImportService
{
    private static readonly Regex CloPrefix    = new(@"^\(CLO[^)]*\)\s*", RegexOptions.Compiled);
    private static readonly Regex AnswerLine   = new(@"^([ABCD])\.\s*(.*)", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex ImgMarker    = new(@"\[<img>([^<]+)</img>\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex AudioMarker  = new(@"\[<audio>([^<]+)</audio>\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
//    private static readonly Regex CapDoPrefix  = new(@"^\(<(\d+)>\)", RegexOptions.Compiled);
    private const string EndMarker = "[<br>]";

    private enum ParseState { Normal, CollectingPassage, CollectingSubQuestions }

    public (List<ImportCauHoiDto> Questions, List<ImportCauHoiNhomDto> Groups, List<string> Warnings) Parse(Stream stream)
    {
        var questions = new List<ImportCauHoiDto>();
        var groups    = new List<ImportCauHoiNhomDto>();
        var warnings  = new List<string>();

        using var doc  = WordprocessingDocument.Open(stream, isEditable: false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) return (questions, groups, warnings);

        var paragraphs = body.Elements<Paragraph>().ToList();

        ParseState state = ParseState.Normal;

        // Normal state
        ImportCauHoiDto? current = null;

        // Group state
        ImportCauHoiNhomDto? currentGroup = null;
        List<string>         passageLines = [];
        ImportCauHoiDto?     currentSub   = null;

        int paraIndex = 0;

        foreach (var para in paragraphs)
        {
            paraIndex++;
            string text = GetParagraphText(para).Trim();
            if (string.IsNullOrEmpty(text)) continue;

            // ── CollectingPassage ────────────────────────────────────────────
            if (state == ParseState.CollectingPassage)
            {
                if (text == "[<egc>]")
                {
                    var rawPassage = string.Join("\n", passageLines).Trim();
                    var (cleanPassage, anhPassage, amThanhPassage) = ExtractMedia(rawPassage);
                    currentGroup!.NoiDung      = cleanPassage;
                    currentGroup.AnhFiles      = anhPassage;
                    currentGroup.AmThanhFiles  = amThanhPassage;
                    passageLines.Clear();
                    state = ParseState.CollectingSubQuestions;
                }
                else
                {
                    passageLines.Add(text);
                }
                continue;
            }

            // ── CollectingSubQuestions ───────────────────────────────────────
            if (state == ParseState.CollectingSubQuestions)
            {
                if (text == "[</sg>]")
                {
                    FinalizeSubQuestion(currentSub, currentGroup!, warnings, paraIndex);
                    currentSub = null;

                    if (currentGroup!.CauHoiCons.Count > 0)
                        groups.Add(currentGroup);
                    else
                        warnings.Add($"Nhóm câu hỏi \"{Truncate(currentGroup.NoiDung)}\" không có câu hỏi con hợp lệ, bị bỏ qua.");

                    currentGroup = null;
                    state = ParseState.Normal;
                }
                else if (text == EndMarker)
                {
                    FinalizeSubQuestion(currentSub, currentGroup!, warnings, paraIndex);
                    currentSub = null;
                }
                else if (IsCloLine(text))
                {
                    // (<n>) tại đây là chỉ số câu con trong nhóm, KHÔNG phải cấp độ
                    var rawSub = CloPrefix.Replace(StripSubQuestionIndex(text), "").Trim();
                    var (cleanSub, anhSub, amThanhSub) = ExtractMedia(rawSub);
                    currentSub = new ImportCauHoiDto
                    {
                        CloText      = ExtractClo(text),
                        CapDo        = null,
                        NoiDung      = cleanSub,
                        AnhFiles     = anhSub,
                        AmThanhFiles = amThanhSub
                    };
                }
                else
                {
                    var m = AnswerLine.Match(text);
                    if (m.Success && currentSub is not null)
                    {
                        char letter = m.Groups[1].Value[0];
                        currentSub.Answers.Add(new ImportCauTraLoiDto
                        {
                            ThuTu   = "ABCD".IndexOf(letter) + 1,
                            NoiDung = m.Groups[2].Value.Trim(),
                            LaDapAn = HasUnderline(para),
                            HoanVi  = !HasItalic(para)
                        });
                    }
                }
                continue;
            }

            // ── Normal ───────────────────────────────────────────────────────

            if (text == "[<sg>]")
            {
                if (current is not null)
                {
                    warnings.Add($"Câu hỏi \"{Truncate(current.NoiDung)}\" bị ngắt bởi [<sg>], bị bỏ qua.");
                    current = null;
                }
                currentGroup = new ImportCauHoiNhomDto();
                passageLines.Clear();
                state = ParseState.CollectingPassage;
                continue;
            }

            // Marker lạc trong Normal → bỏ qua
            if (text is "[</sg>]" or "[<egc>]") continue;

            if (text == EndMarker)
            {
                if (current is not null)
                {
                    var warning = ValidateQuestion(current, paraIndex);
                    if (warning is null) questions.Add(current);
                    else warnings.Add(warning);
                }
                current = null;
                continue;
            }

            if (IsCloLine(text))
            {
                var rawNoiDung = CloPrefix.Replace(StripSubQuestionIndex(text), "").Trim();
                var (cleanNoiDung, anh, amThanh) = ExtractMedia(rawNoiDung);
                current = new ImportCauHoiDto
                {
                    CloText      = ExtractClo(text),
                   // CapDo      = ExtractCapDo(text),
                    NoiDung      = cleanNoiDung,
                    AnhFiles     = anh,
                    AmThanhFiles = amThanh
                };
                continue;
            }

            var match = AnswerLine.Match(text);
            if (match.Success && current is not null)
            {
                char letter = match.Groups[1].Value[0];
                current.Answers.Add(new ImportCauTraLoiDto
                {
                    ThuTu   = "ABCD".IndexOf(letter) + 1,
                    NoiDung = match.Groups[2].Value.Trim(),
                    LaDapAn = HasUnderline(para),
                    HoanVi  = !HasItalic(para)
                });
            }
        }

        // Dọn cuối file
        if (current is not null)
            warnings.Add($"Câu hỏi cuối file chưa có [<br>] kết thúc, bị bỏ qua: \"{Truncate(current.NoiDung)}\"");

        if (state != ParseState.Normal)
            warnings.Add("File kết thúc trong khi đang xử lý câu hỏi nhóm chưa hoàn chỉnh, bị bỏ qua.");

        return (questions, groups, warnings);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static void FinalizeSubQuestion(ImportCauHoiDto? sub, ImportCauHoiNhomDto group,
        List<string> warnings, int paraIndex)
    {
        if (sub is null) return;
        var w = ValidateQuestion(sub, paraIndex);
        if (w is null) group.CauHoiCons.Add(sub);
        else warnings.Add(w);
    }

    /// <summary>Tách marker [&lt;img&gt;] và [&lt;audio&gt;] ra khỏi text, trả về text sạch và hai danh sách tên file.</summary>
    private static (string CleanText, List<string> AnhFiles, List<string> AmThanhFiles) ExtractMedia(string text)
    {
        var anhFiles     = ImgMarker.Matches(text).Select(m => m.Groups[1].Value.Trim()).ToList();
        var amThanhFiles = AudioMarker.Matches(text).Select(m => m.Groups[1].Value.Trim()).ToList();
        var clean = ImgMarker.Replace(AudioMarker.Replace(text, ""), "").Trim();
        return (clean, anhFiles, amThanhFiles);
    }

    private static string GetParagraphText(Paragraph para)
        => string.Concat(para.Elements<Run>().Select(r => r.InnerText));

    private static bool IsCloLine(string text)
    {
        var stripped = StripSubQuestionIndex(text);
        return stripped.StartsWith("(CLO", StringComparison.OrdinalIgnoreCase);
    }

    private static string StripSubQuestionIndex(string text)
    {
        var m = Regex.Match(text, @"^\(<\d+>\)\s*(.+)");
        return m.Success ? m.Groups[1].Value : text;
    }

    private static string? ExtractClo(string text)
    {
        var stripped = StripSubQuestionIndex(text);
        var m = Regex.Match(stripped, @"^\(CLO[^)]*\)", RegexOptions.IgnoreCase);
        return m.Success ? m.Value : null;
    }

    /// <summary>Trích cấp độ từ prefix (&lt;n&gt;) — chỉ dùng trong Normal state. Trả null nếu không có prefix.</summary>
    // private static short? ExtractCapDo(string text)
    // {
    //     var m = CapDoPrefix.Match(text);
    //     if (!m.Success) return null;
    //     var n = short.Parse(m.Groups[1].Value);
    //     return (n >= 1 && n <= 3) ? n : (short)1;
    // }

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
