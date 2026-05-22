namespace QuestionBank.Web.Application.DTOs;

public class ImportCauHoiDto
{
    public string NoiDung { get; set; } = string.Empty;
    public string? CloText { get; set; }
    public List<ImportCauTraLoiDto> Answers { get; set; } = [];
}

public class ImportCauTraLoiDto
{
    /// <summary>1=A, 2=B, 3=C, 4=D</summary>
    public int ThuTu { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    /// <summary>true nếu ký tự A./B./C./D. được gạch chân trong Word.</summary>
    public bool LaDapAn { get; set; }
    /// <summary>false nếu ký tự A./B./C./D. được in nghiêng trong Word (không hoán vị).</summary>
    public bool HoanVi { get; set; } = true;
}
