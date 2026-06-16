namespace QuestionBank.Web.Application.DTOs;

public class DeThiDto
{
    public Guid MaDeThi { get; set; }
    public Guid MaMonHoc { get; set; }
    public string TenDeThi { get; set; } = "";
    public DateTime NgayTao { get; set; }
    public bool? DaDuyet { get; set; }
    public int? MaDe { get; set; }
    public List<ChiTietDeThiDto> ChiTietDeThis { get; set; } = [];

    public int SoCauHoi => ChiTietDeThis.Count;
}

public class ChiTietDeThiDto
{
    public Guid MaCauHoi { get; set; }
    public Guid MaPhan { get; set; }
    public string TenPhan { get; set; } = "";
    public int ThuTu { get; set; }
    public int MaSoCauHoi { get; set; }
    public string? NoiDung { get; set; }
    public short CapDo { get; set; }
    public bool LaCauNhom { get; set; }
    public List<ChiTietDeThiDto> CauHoiCons { get; set; } = [];
    public List<CauTraLoiDto> CauTraLois { get; set; } = [];
    public List<FileDinhKemDto> Files { get; set; } = [];
}

public class SaveDeThiDto
{
    public Guid MaMonHoc { get; set; }
    public string TenDeThi { get; set; } = "";
    public List<SelectedCauHoiDto> CauHois { get; set; } = [];
}

public class SelectedCauHoiDto
{
    public Guid MaCauHoi { get; set; }
    public Guid MaPhan { get; set; }
}

/// <summary>Yêu cầu sinh N bản đề từ 1 đề gốc.</summary>
public class SinhNhieuMaDeDto
{
    public Guid MaDeThiGoc   { get; set; }
    public Guid MaMonHoc     { get; set; }
    public int  SoBanDe      { get; set; } = 4;
    public bool HoanViCauHoi { get; set; } = true;
}

/// <summary>Yêu cầu xuất đề thi ra file Word.</summary>
public class ExportRequestDto
{
    public DeThiDto DeThi           { get; set; } = null!;
    public string   TenMonHoc       { get; set; } = "";
    public string   TenKhoa         { get; set; } = "";
    public bool     ShowAnswers      { get; set; }
    public bool     IncludeAnswerKey { get; set; }
}

/// <summary>Thông tin tóm tắt 1 bản đề vừa được sinh ra.</summary>
public class KetQuaSinhMaDeDto
{
    public Guid   MaDeThi  { get; set; }
    public int    MaDe     { get; set; }
    public string TenDeThi { get; set; } = "";
    public int    SoCauHoi { get; set; }
}
