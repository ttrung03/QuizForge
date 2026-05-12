namespace QuestionBank.Web.Application.DTOs;

/// <summary>
/// Dùng để hiển thị danh sách — chỉ chứa field cần thiết cho UI.
/// Không expose toàn bộ Entity ra ngoài.
/// </summary>
public class KhoaDto
{
    public Guid MaKhoa { get; set; }
    public string TenKhoa { get; set; } = string.Empty;
}

/// <summary>
/// Dùng khi tạo mới hoặc cập nhật (dữ liệu từ form).
/// MaKhoa == null  →  tạo mới
/// MaKhoa có giá trị  →  cập nhật
/// </summary>
public class SaveKhoaDto
{
    public Guid? MaKhoa { get; set; }
    public string TenKhoa { get; set; } = string.Empty;
}