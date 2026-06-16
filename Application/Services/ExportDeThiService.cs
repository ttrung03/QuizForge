using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Hosting;
using QuestionBank.Web.Application.DTOs;

namespace QuestionBank.Web.Application.Services;

/// <summary>
/// Sinh file Word (.docx) từ dữ liệu đề thi.
/// </summary>
public class ExportDeThiService(IWebHostEnvironment env)
{
    private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Xuất đề thi ra byte[] .docx.
    /// </summary>
    /// <param name="deThi">DTO đề thi (theo thứ tự hiển thị — đã hoán vị nếu cần).</param>
    /// <param name="tenMonHoc">Tên môn học (hiển thị trên header).</param>
    /// <param name="tenKhoa">Tên khoa (hiển thị trên header).</param>
    /// <param name="showAnswers">true → in bảng đáp án cuối file.</param>
    /// <param name="includeAnswerKey">true → in bảng đáp án cuối file (dùng chung với showAnswers).</param>
    public byte[] ExportToWord(
        DeThiDto    deThi,
        string      tenMonHoc,
        string      tenKhoa,
        bool        showAnswers,
        bool        includeAnswerKey)
    {
        using var stream = new MemoryStream();
        using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            // Main document part
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            var body = mainPart.Document.Body!;

            // Styles
            AddStyles(mainPart);

            // Page margins (A4)
            SetPageLayout(body);

            // ── Header ───────────────────────────────────────────────────────
            AppendHeader(body, deThi, tenMonHoc, tenKhoa);

            // ── Câu hỏi ──────────────────────────────────────────────────────
            int stt = 0;
            foreach (var cauHoi in deThi.ChiTietDeThis)
            {
                if (cauHoi.LaCauNhom)
                {
                    // Câu nhóm: in đoạn văn cha rồi các câu con
                    AppendGroupHeader(body, cauHoi);
                    foreach (var con in cauHoi.CauHoiCons)
                    {
                        stt++;
                        AppendQuestion(body, con, stt, showAnswers, isChild: true);
                    }
                }
                else
                {
                    stt++;
                    AppendQuestion(body, cauHoi, stt, showAnswers, isChild: false);
                }
            }

            // ── Bảng đáp án ──────────────────────────────────────────────────
            if (includeAnswerKey)
            {
                AppendAnswerKey(body, deThi);
            }

            mainPart.Document.Save();
        }
        return stream.ToArray();
    }

    // ─── Header ───────────────────────────────────────────────────────────────

    private static void AppendHeader(Body body, DeThiDto deThi, string tenMonHoc, string tenKhoa)
    {
        // Tên trường (placeholder — có thể cấu hình sau)
        var truong = Paragraph(
            "TRƯỜNG ĐẠI HỌC",
            bold: true, fontSize: 12, alignment: JustificationValues.Center);
        body.Append(truong);

        if (!string.IsNullOrWhiteSpace(tenKhoa))
        {
            body.Append(Paragraph(
                $"KHOA: {tenKhoa.ToUpper()}",
                bold: false, fontSize: 11, alignment: JustificationValues.Center));
        }

        // Dòng trống
        body.Append(EmptyParagraph());

        // Tên đề thi
        body.Append(Paragraph(
            deThi.TenDeThi.ToUpper(),
            bold: true, fontSize: 14, alignment: JustificationValues.Center));

        // Môn học
        body.Append(Paragraph(
            $"Môn học: {tenMonHoc}",
            bold: false, fontSize: 12, alignment: JustificationValues.Center));

        // Mã đề + Ngày tạo + Số câu
        var meta = new[]
        {
            deThi.MaDe.HasValue ? $"Mã đề: {deThi.MaDe.Value:D3}" : null,
            $"Ngày: {deThi.NgayTao:dd/MM/yyyy}",
            $"Số câu: {deThi.SoCauHoi}"
        }.Where(x => x != null);
        body.Append(Paragraph(
            string.Join("     |     ", meta),
            bold: false, fontSize: 11, alignment: JustificationValues.Center));

        // Divider
        body.Append(HorizontalLine());
        body.Append(EmptyParagraph());
    }

    // ─── Câu nhóm (đoạn văn cha) ─────────────────────────────────────────────

    private static void AppendGroupHeader(Body body, ChiTietDeThiDto nhom)
    {
        var p = new Paragraph();
        var pPr = new ParagraphProperties(
            new ParagraphBorders(
                new LeftBorder { Val = BorderValues.Single, Size = 12, Color = "5C6BC0" }
            ),
            new Indentation { Left = "360" }
        );
        p.Append(pPr);

        var rPr = new RunProperties(
            new Bold(),
            new Color { Val = "5C6BC0" },
            new FontSize { Val = "20" }  // 10pt
        );
        p.Append(new Run(rPr, new Text($"[Nhóm câu hỏi – {nhom.CauHoiCons.Count} câu con]") { Space = SpaceProcessingModeValues.Preserve }));
        body.Append(p);

        if (!string.IsNullOrWhiteSpace(nhom.NoiDung))
        {
            var pText = new Paragraph();
            var pTextPr = new ParagraphProperties(
                new Indentation { Left = "360" },
                new SpacingBetweenLines { After = "80" }
            );
            pText.Append(pTextPr);
            pText.Append(new Run(new Text(nhom.NoiDung) { Space = SpaceProcessingModeValues.Preserve }));
            body.Append(pText);
        }
    }

    // ─── Câu hỏi đơn / câu con ───────────────────────────────────────────────

    private static void AppendQuestion(
        Body body, ChiTietDeThiDto cauHoi, int stt, bool showAnswers, bool isChild)
    {
        var indent = isChild ? "720" : "0";

        // Câu hỏi
        var pQ = new Paragraph();
        var pQPr = new ParagraphProperties(
            new Indentation { Left = indent },
            new SpacingBetweenLines { Before = "120", After = "60" },
            new KeepNext()
        );
        pQ.Append(pQPr);

        // Số thứ tự (bold)
        var rStt = new Run(
            new RunProperties(new Bold(), new FontSize { Val = "22" }),
            new Text($"Câu {stt}. ") { Space = SpaceProcessingModeValues.Preserve }
        );
        pQ.Append(rStt);

        // Nội dung câu hỏi
        var rContent = new Run(
            new RunProperties(new FontSize { Val = "22" }),
            new Text(cauHoi.NoiDung ?? "(không có nội dung)") { Space = SpaceProcessingModeValues.Preserve }
        );
        pQ.Append(rContent);
        body.Append(pQ);

        // Các đáp án
        var sortedAnswers = cauHoi.CauTraLois.OrderBy(a => a.ThuTu).ToList();
        for (int i = 0; i < sortedAnswers.Count; i++)
        {
            var ans = sortedAnswers[i];
            var letter = i < Letters.Length ? Letters[i].ToString() : $"{i + 1}";
            bool isCorrect = ans.LaDapAn && showAnswers;

            var pA = new Paragraph();
            var pAPr = new ParagraphProperties(
                new Indentation { Left = isChild ? "1080" : "360", Hanging = "0" },
                new SpacingBetweenLines { Before = "0", After = "40" }
            );
            pA.Append(pAPr);

            var rLetter = new Run(
                new RunProperties(
                    isCorrect ? new Bold() : new Bold { Val = false },
                    new Color { Val = isCorrect ? "2E7D32" : "000000" },
                    new FontSize { Val = "22" }
                ),
                new Text($"{letter}. ") { Space = SpaceProcessingModeValues.Preserve }
            );
            pA.Append(rLetter);

            var rAns = new Run(
                new RunProperties(
                    isCorrect ? new Bold() : new Bold { Val = false },
                    new Color { Val = isCorrect ? "2E7D32" : "000000" },
                    new FontSize { Val = "22" }
                ),
                new Text(ans.NoiDung ?? "") { Space = SpaceProcessingModeValues.Preserve }
            );
            pA.Append(rAns);

            // Đánh dấu đáp án đúng nếu showAnswers
            if (isCorrect)
            {
                pA.Append(new Run(
                    new RunProperties(new Bold(), new Color { Val = "2E7D32" }, new FontSize { Val = "20" }),
                    new Text("  ✓") { Space = SpaceProcessingModeValues.Preserve }
                ));
            }

            body.Append(pA);
        }

        body.Append(EmptyParagraph(after: "60"));
    }

    // ─── Bảng đáp án ─────────────────────────────────────────────────────────

    private static void AppendAnswerKey(Body body, DeThiDto deThi)
    {
        body.Append(HorizontalLine());
        body.Append(Paragraph("BẢNG ĐÁP ÁN", bold: true, fontSize: 12, alignment: JustificationValues.Center));
        body.Append(EmptyParagraph());

        // Thu thập câu hỏi phẳng
        var flatList = new List<(int Stt, ChiTietDeThiDto CauHoi)>();
        int stt = 0;
        foreach (var ch in deThi.ChiTietDeThis)
        {
            if (ch.LaCauNhom)
                foreach (var con in ch.CauHoiCons) { stt++; flatList.Add((stt, con)); }
            else { stt++; flatList.Add((stt, ch)); }
        }

        // Bảng N cột (mỗi hàng 5 câu)
        const int cols = 5;
        var rows = (int)Math.Ceiling(flatList.Count / (double)cols);

        var table = new Table();
        table.Append(new TableProperties(
            new TableBorders(
                new TopBorder    { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder   { Val = BorderValues.Single, Size = 4 },
                new RightBorder  { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder   { Val = BorderValues.Single, Size = 4 }
            ),
            new TableWidth { Width = "9072", Type = TableWidthUnitValues.Dxa }
        ));

        // Header hàng
        var headerRow = new TableRow();
        for (int c = 0; c < cols; c++)
        {
            headerRow.Append(TableCell("Câu", bold: true, shading: "EEEEEE"));
            headerRow.Append(TableCell("Đáp án", bold: true, shading: "EEEEEE"));
        }
        table.Append(headerRow);

        for (int r = 0; r < rows; r++)
        {
            var tr = new TableRow();
            for (int c = 0; c < cols; c++)
            {
                int idx = r * cols + c;
                if (idx < flatList.Count)
                {
                    var (no, q) = flatList[idx];
                    var correct = q.CauTraLois
                        .OrderBy(a => a.ThuTu)
                        .Select((a, i) => (a, i))
                        .FirstOrDefault(x => x.a.LaDapAn);

                    string answerLetter = correct.a is null
                        ? "—"
                        : (correct.i < Letters.Length ? Letters[correct.i].ToString() : "?");

                    tr.Append(TableCell($"{no}", bold: false));
                    tr.Append(TableCell(answerLetter, bold: true, color: "1565C0"));
                }
                else
                {
                    tr.Append(TableCell(""));
                    tr.Append(TableCell(""));
                }
            }
            table.Append(tr);
        }

        body.Append(table);
    }

    // ─── OpenXml Helpers ──────────────────────────────────────────────────────

    private static Paragraph Paragraph(
        string text,
        bool bold = false,
        int fontSize = 11,
        JustificationValues? alignment = null)
    {
        var p = new Paragraph();
        p.Append(new ParagraphProperties(
            new Justification { Val = alignment ?? JustificationValues.Left },
            new SpacingBetweenLines { After = "80" }
        ));
        var rPr = new RunProperties(new FontSize { Val = $"{fontSize * 2}" });
        if (bold) rPr.Append(new Bold());
        p.Append(new Run(rPr, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        return p;
    }

    private static Paragraph EmptyParagraph(string after = "120")
    {
        var p = new Paragraph();
        p.Append(new ParagraphProperties(new SpacingBetweenLines { After = after }));
        return p;
    }

    private static Paragraph HorizontalLine()
    {
        var p = new Paragraph();
        p.Append(new ParagraphProperties(
            new ParagraphBorders(
                new BottomBorder { Val = BorderValues.Single, Size = 6, Color = "AAAAAA" }
            ),
            new SpacingBetweenLines { After = "120" }
        ));
        return p;
    }

    private static TableCell TableCell(
        string text,
        bool bold = false,
        string? color = null,
        string? shading = null)
    {
        var tc = new TableCell();
        if (shading != null)
        {
            tc.Append(new TableCellProperties(
                new Shading { Val = ShadingPatternValues.Clear, Fill = shading, Color = "auto" }
            ));
        }

        var rPr = new RunProperties(new FontSize { Val = "20" });
        if (bold)  rPr.Append(new Bold());
        if (color != null) rPr.Append(new Color { Val = color });

        var p = new Paragraph();
        p.Append(new ParagraphProperties(
            new SpacingBetweenLines { After = "0" },
            new Justification { Val = JustificationValues.Center }
        ));
        p.Append(new Run(rPr, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        tc.Append(p);
        return tc;
    }

    private static void AddStyles(MainDocumentPart mainPart)
    {
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles(
            new DocDefaults(
                new RunPropertiesDefault(
                    new RunPropertiesBaseStyle(
                        new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                        new FontSize { Val = "22" }  // 11pt default
                    )
                )
            )
        );
        stylesPart.Styles.Save();
    }

    private static void SetPageLayout(Body body)
    {
        body.Append(new SectionProperties(
            new PageSize { Width = 11906, Height = 16838 },  // A4
            new PageMargin { Top = 1134, Bottom = 1134, Left = 1134, Right = 1134 }  // ~2cm
        ));
    }
}
