using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuestionBank.Web.Domain.Entities;

[Table("MonHoc")]
public class MonHoc
{
    [Key]
    public Guid MaMonHoc { get; set; }

    public Guid MaKhoa { get; set; }

    [Required]
    [MaxLength(50)]
    public string MaSoMonHoc { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string TenMonHoc { get; set; } = string.Empty;

    public bool? XoaTamMonHoc { get; set; }

    // Navigation
    [ForeignKey(nameof(MaKhoa))]
    public virtual Khoa Khoa { get; set; } = null!;

    public virtual ICollection<Phan> Phans { get; set; } = new List<Phan>();
    public virtual ICollection<DeThi> DeThis { get; set; } = new List<DeThi>();
}