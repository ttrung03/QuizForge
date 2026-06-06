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
