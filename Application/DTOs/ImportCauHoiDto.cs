namespace QuestionBank.Web.Application.DTOs;

public class ImportCauHoiDto
{
    public string NoiDung { get; set; } = string.Empty;
    public string? CloText { get; set; }
    /// <summary>1=Dễ, 2=Trung bình, 3=Khó. Lấy từ prefix (&lt;n&gt;) trong file Word. null nếu file không có prefix (sẽ dùng giá trị mặc định do người dùng chọn khi import).</summary>
    public short? CapDo { get; set; }
    public List<ImportCauTraLoiDto> Answers { get; set; } = [];
    /// <summary>Tên file hình ảnh (từ marker [&lt;img&gt;...&lt;/img&gt;]) — đã tách khỏi NoiDung.</summary>
    public List<string> AnhFiles { get; set; } = [];
    /// <summary>Tên file âm thanh (từ marker [&lt;audio&gt;...&lt;/audio&gt;]) — đã tách khỏi NoiDung.</summary>
    public List<string> AmThanhFiles { get; set; } = [];
}

/// <summary>Nhóm câu hỏi: passage thu thập giữa [&lt;sg&gt;] và [&lt;egc&gt;], câu con thu thập giữa [&lt;egc&gt;] và [&lt;/sg&gt;].</summary>
public class ImportCauHoiNhomDto
{
    public string NoiDung { get; set; } = string.Empty;
    public short? CapDo { get; set; }
    public List<ImportCauHoiDto> CauHoiCons { get; set; } = [];
    /// <summary>Tên file hình ảnh gắn với đoạn văn nhóm.</summary>
    public List<string> AnhFiles { get; set; } = [];
    /// <summary>Tên file âm thanh gắn với đoạn văn nhóm.</summary>
    public List<string> AmThanhFiles { get; set; } = [];
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
