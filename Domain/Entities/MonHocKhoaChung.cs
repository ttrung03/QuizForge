using System.ComponentModel.DataAnnotations.Schema;

namespace QuestionBank.Web.Domain.Entities;

/// <summary>
/// Bảng phụ — cho phép một Môn học được dùng chung ở nhiều Khoa.
/// MaKhoa trong bảng này là khoa "sử dụng chung" (khác với MaKhoa trong MonHoc là khoa sở hữu trực tiếp).
/// </summary>
[Table("MonHoc_KhoaChung")]
public class MonHocKhoaChung
{
    public Guid MaMonHoc { get; set; }
    public Guid MaKhoa   { get; set; }

    [ForeignKey(nameof(MaMonHoc))]
    public virtual MonHoc MonHoc { get; set; } = null!;

    [ForeignKey(nameof(MaKhoa))]
    public virtual Khoa Khoa { get; set; } = null!;
}
