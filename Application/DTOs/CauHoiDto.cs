namespace QuestionBank.Web.Application.DTOs;

public class CauHoiDto
{
    public Guid MaCauHoi { get; set; }
    public Guid MaPhan { get; set; }
    public int MaSoCauHoi { get; set; }
    public string? NoiDung { get; set; }
    public bool HoanVi { get; set; }
    public short CapDo { get; set; }
    public int SoCauHoiCon { get; set; }
    public List<CauTraLoiDto> CauTraLois { get; set; } = [];
    public List<CauHoiDto> CauHoiCons { get; set; } = [];
    public List<FileDinhKemDto> Files { get; set; } = [];

    public bool LaCauNhom => SoCauHoiCon > 0;
}

public class FileDinhKemDto
{
    public Guid MaFile { get; set; }
    /// <summary>1 = Hình ảnh, 2 = Âm thanh</summary>
    public int LoaiFile { get; set; }
    public string TenFile { get; set; } = string.Empty;
    /// <summary>URL tương đối để trình duyệt tải file (ví dụ /uploads/images/abc.png)</summary>
    public string Url => LoaiFile == 1 ? $"/uploads/images/{TenFile}" : $"/uploads/audio/{TenFile}";
}

public class CauTraLoiDto
{
    public Guid MaCauTraLoi { get; set; }
    public string? NoiDung { get; set; }
    public int ThuTu { get; set; }
    public bool LaDapAn { get; set; }
    public bool HoanVi { get; set; }
}

public class UpdateCauHoiDto
{
    public Guid MaCauHoi { get; set; }
    public string NoiDung { get; set; } = "";
    public short CapDo { get; set; }
    public List<UpdateCauTraLoiDto> CauTraLois { get; set; } = [];
}

public class UpdateCauTraLoiDto
{
    public Guid MaCauTraLoi { get; set; }
    public string NoiDung { get; set; } = "";
    public bool LaDapAn { get; set; }
}
