using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuestionBank.Web.Domain.Entities;

/// <summary>
/// Ánh xạ bảng [Files] trong DB.
/// Đặt tên class là FileDinhKem để tránh xung đột với System.IO.File.
/// DbContext sẽ map về đúng tên bảng "Files" qua [Table("Files")].
/// </summary>
[Table("Files")]
public class FileDinhKem
{
    [Key]
    public Guid MaFile { get; set; }

    public Guid? MaCauHoi { get; set; }
    public Guid? MaCauTraLoi { get; set; }

    [MaxLength(250)]
    public string? TenFile { get; set; }

    public int? LoaiFile { get; set; }

    // Navigation
    [ForeignKey(nameof(MaCauHoi))]
    public virtual CauHoi? CauHoi { get; set; }

    [ForeignKey(nameof(MaCauTraLoi))]
    public virtual CauTraLoi? CauTraLoi { get; set; }
}