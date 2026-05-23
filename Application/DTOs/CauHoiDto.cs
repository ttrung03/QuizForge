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

    public bool LaCauNhom => SoCauHoiCon > 0;
}

public class CauTraLoiDto
{
    public Guid MaCauTraLoi { get; set; }
    public string? NoiDung { get; set; }
    public int ThuTu { get; set; }
    public bool LaDapAn { get; set; }
    public bool HoanVi { get; set; }
}
