namespace QuestionBank.Web.Application.DTOs;

/// <summary>
/// Dùng để hiển thị danh sách — chỉ chứa field cần thiết cho UI.
/// </summary>
public class PhanDto
{
    public Guid MaPhan { get; set; }
    public Guid MaMonHoc { get; set; }
    public string TenMonHoc { get; set; } = string.Empty;
    public string TenPhan { get; set; } = string.Empty;
    public string? NoiDung { get; set; }
    public int ThuTu { get; set; }
    public int SoLuongCauHoi { get; set; }
    public Guid? MaPhanCha { get; set; }
    public int? MaSoPhan { get; set; }
    public bool LaCauHoiNhom { get; set; }
}

/// <summary>
/// Dùng khi tạo mới hoặc cập nhật (dữ liệu từ form).
/// MaPhan == null  →  tạo mới
/// MaPhan có giá trị  →  cập nhật
/// </summary>
public class SavePhanDto
{
    public Guid? MaPhan { get; set; }
    public Guid MaMonHoc { get; set; }
    public string TenPhan { get; set; } = string.Empty;
    public string? NoiDung { get; set; }
    public int ThuTu { get; set; }
    public int SoLuongCauHoi { get; set; }
    public Guid? MaPhanCha { get; set; }
    public int? MaSoPhan { get; set; }
    public bool LaCauHoiNhom { get; set; }
}
