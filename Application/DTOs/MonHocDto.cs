namespace QuestionBank.Web.Application.DTOs;

/// <summary>
/// Dùng để hiển thị danh sách — chỉ chứa field cần thiết cho UI.
/// </summary>
public class MonHocDto
{
    public Guid MaMonHoc { get; set; }
    public Guid? MaKhoa { get; set; }
    public string? TenKhoa { get; set; }
    public string MaSoMonHoc { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;

    /// <summary>
    /// true  → môn học được dùng chung (từ bảng MonHoc_KhoaChung), không thuộc trực tiếp khoa này.
    /// false → môn học thuộc trực tiếp khoa này (MaKhoa trong bảng MonHoc).
    /// </summary>
    public bool LaMonChung { get; set; }
}

/// <summary>
/// Dùng khi tạo mới hoặc cập nhật (dữ liệu từ form).
/// MaMonHoc == null  →  tạo mới
/// MaMonHoc có giá trị  →  cập nhật
/// </summary>
public class SaveMonHocDto
{
    public Guid? MaMonHoc { get; set; }
    public Guid? MaKhoa { get; set; }
    public string MaSoMonHoc { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
}
