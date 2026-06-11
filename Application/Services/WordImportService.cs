using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Hosting;
using QuestionBank.Web.Application.DTOs;
using WpParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using DwBlip = DocumentFormat.OpenXml.Drawing.Blip;

namespace QuestionBank.Web.Application.Services;

/// <summary>
/// Parse file .docx theo định dạng chuẩn HUTECH (Số 73/HD-KT).
/// Hỗ trợ câu hỏi đơn, câu hỏi nhóm, và media (hình ảnh, âm thanh) qua marker
/// [&lt;img&gt;filename.png&lt;/img&gt;] và [&lt;audio&gt;filename.mp3&lt;/audio&gt;],
/// cũng như ảnh nhúng trực tiếp vào file Word (Insert → Picture).
/// </summary>
public class WordImportService(IWebHostEnvironment env)
{
    private static readonly Regex CloPrefix   = new(@"^\(CLO[^)]*\)\s*", RegexOptions.Compiled);
    private static readonly Regex AnswerLine  = new(@"^([ABCD])\.\s*(.*)", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex ImgMarker   = new(@"\[<img>\]?([^\[<]+?)\]?\[?</img>\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex AudioMarker = new(@"\[<audio>\]?([^\[<]+?)\]?\[?</audio>\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private const string EndMarker = "[<br>]";

    private enum ParseState { Normal, CollectingPassage, CollectingSubQuestions }

    public (List<ImportCauHoiDto> Questions, List<ImportCauHoiNhomDto> Groups, List<string> Warnings) Parse(Stream stream)
    {
        var questions = new List<ImportCauHoiDto>();
        var groups    = new List<ImportCauHoiNhomDto>();
        var warnings  = new List<string>();

        using var doc  = WordprocessingDocument.Open(stream, isEditable: false);
        var mainPart = doc.MainDocumentPart;
        var body     = mainPart?.Document?.Body;
        if (body is null) return (questions, groups, warnings);

        var paragraphs = body.Elements<WpParagraph>().ToList();

        ParseState state = ParseState.Normal;

        ImportCauHoiDto?     current      = null;
        ImportCauHoiNhomDto? currentGroup = null;
        List<string>         passageLines = [];
        List<string>         passageEmbeddedImages = [];
        ImportCauHoiDto?     currentSub   = null;

        int paraIndex = 0;

        foreach (var para in paragraphs)
        {
            paraIndex++;
            string text = GetParagraphText(para).Trim();

            // ── CollectingPassage ────────────────────────────────────────────
            if (state == ParseState.CollectingPassage)
            {
                if (text == "[<egc>]")
                {
                    var rawPassage = string.Join("\n", passageLines).Trim();
                    var (cleanPassage, anhPassage, amThanhPassage) = ExtractMedia(rawPassage);
                    anhPassage.AddRange(passageEmbeddedImages);
                    currentGroup!.NoiDung     = cleanPassage;
                    currentGroup.AnhFiles     = anhPassage;
                    currentGroup.AmThanhFiles = amThanhPassage;
                    passageLines.Clear();
                    passageEmbeddedImages.Clear();
                    state = ParseState.CollectingSubQuestions;
                }
                else
                {
                    if (!string.IsNullOrEmpty(text))
                        passageLines.Add(text);
                    passageEmbeddedImages.AddRange(SaveEmbeddedImages(para, mainPart!));
                }
                continue;
            }

            if (string.IsNullOrEmpty(text))
            {
                // Đoạn trống nhưng có thể chứa ảnh nhúng
                var imgs = SaveEmbeddedImages(para, mainPart!);
                if (imgs.Count > 0)
                {
                    if (state == ParseState.CollectingSubQuestions)
                    {
                        if (currentSub is not null) currentSub.AnhFiles.AddRange(imgs);
                        else currentGroup?.AnhFiles.AddRange(imgs);
                    }
                    else
                        current?.AnhFiles.AddRange(imgs);
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
                    var rawSub = CloPrefix.Replace(StripSubQuestionIndex(text), "").Trim();
                    var (cleanSub, anhSub, amThanhSub) = ExtractMedia(rawSub);
                    anhSub.AddRange(SaveEmbeddedImages(para, mainPart!));
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
                    var embSub = SaveEmbeddedImages(para, mainPart!);
                    if (embSub.Count > 0 && currentSub is not null)
                        currentSub.AnhFiles.AddRange(embSub);

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
                passageEmbeddedImages.Clear();
                state = ParseState.CollectingPassage;
                continue;
            }

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
                anh.AddRange(SaveEmbeddedImages(para, mainPart!));
                current = new ImportCauHoiDto
                {
                    CloText      = ExtractClo(text),
                    NoiDung      = cleanNoiDung,
                    AnhFiles     = anh,
                    AmThanhFiles = amThanh
                };
                continue;
            }

            // Đoạn không phải CLO, không phải đáp án — có thể là đoạn ảnh độc lập
            var embNormal = SaveEmbeddedImages(para, mainPart!);
            if (embNormal.Count > 0 && current is not null)
                current.AnhFiles.AddRange(embNormal);

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

        if (current is not null)
            warnings.Add($"Câu hỏi cuối file chưa có [<br>] kết thúc, bị bỏ qua: \"{Truncate(current.NoiDung)}\"");

        if (state != ParseState.Normal)
            warnings.Add("File kết thúc trong khi đang xử lý câu hỏi nhóm chưa hoàn chỉnh, bị bỏ qua.");

        return (questions, groups, warnings);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Trích ảnh nhúng (Drawing/Picture) từ đoạn văn, lưu vào uploads/images/,
    /// trả về danh sách tên file đã lưu.
    /// </summary>
    private List<string> SaveEmbeddedImages(WpParagraph para, MainDocumentPart mainPart)
    {
        var names = new List<string>();

        foreach (var blip in para.Descendants<DwBlip>())
        {
            if (blip.Embed?.Value is not string embedId) continue;

            OpenXmlPart? part;
            try { part = mainPart.GetPartById(embedId); }
            catch { continue; }

            if (part is not ImagePart imagePart) continue;

            var ext = ContentTypeToExtension(imagePart.ContentType);
            var fileName = $"img_{Guid.NewGuid():N}{ext}";
            var dir  = Path.Combine(env.WebRootPath, "uploads", "images");
            Directory.CreateDirectory(dir);

            using var fs = File.Create(Path.Combine(dir, fileName));
            imagePart.GetStream().CopyTo(fs);

            names.Add(fileName);
        }

        return names;
    }

    private static string ContentTypeToExtension(string contentType) => contentType switch
    {
        "image/png"  => ".png",
        "image/jpeg" => ".jpg",
        "image/gif"  => ".gif",
        "image/webp" => ".webp",
        "image/bmp"  => ".bmp",
        _            => ".png"
    };

    private static void FinalizeSubQuestion(ImportCauHoiDto? sub, ImportCauHoiNhomDto group,
        List<string> warnings, int paraIndex)
    {
        if (sub is null) return;
        var w = ValidateQuestion(sub, paraIndex);
        if (w is null) group.CauHoiCons.Add(sub);
        else warnings.Add(w);
    }

    private static (string CleanText, List<string> AnhFiles, List<string> AmThanhFiles) ExtractMedia(string text)
    {
        var anhFiles     = ImgMarker.Matches(text).Select(m => m.Groups[1].Value.Trim()).ToList();
        var amThanhFiles = AudioMarker.Matches(text).Select(m => m.Groups[1].Value.Trim()).ToList();
        var clean = ImgMarker.Replace(AudioMarker.Replace(text, ""), "").Trim();
        return (clean, anhFiles, amThanhFiles);
    }

    private static string GetParagraphText(WpParagraph para)
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

    private static bool HasUnderline(WpParagraph para)
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

    private static bool HasItalic(WpParagraph para)
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
        if (string.IsNullOrWhiteSpace(q.NoiDung) && q.AnhFiles.Count == 0)
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
