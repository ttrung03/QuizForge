using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuestionBank.Web.Domain.Entities;

[Table("CauHoi")]
public class CauHoi
{
    [Key]
    public Guid MaCauHoi { get; set; }

    public Guid MaPhan { get; set; }

    public int MaSoCauHoi { get; set; }

    public string? NoiDung { get; set; }

    /// <summary>true = cho phép hoán vị đáp án khi sinh mã đề.</summary>
    public bool HoanVi { get; set; }

    /// <summary>1 = Dễ, 2 = Trung bình, 3 = Khó.</summary>
    public short CapDo { get; set; }

    /// <summary>Số câu hỏi con (dùng cho câu hỏi nhóm). Mặc định = 0.</summary>
    public int SoCauHoiCon { get; set; }

    public double? DoPhanCachCauHoi { get; set; }

    /// <summary>null = câu hỏi độc lập. Có giá trị = câu con thuộc câu nhóm.</summary>
    public Guid? MaCauHoiCha { get; set; }

    public bool? XoaTamCauHoi { get; set; }

    public int? SoLanDuocThi { get; set; }

    public int? SoLanDung { get; set; }

    public DateTime? NgayTao { get; set; }

    public DateTime? NgaySua { get; set; }

    // Navigation
    [ForeignKey(nameof(MaPhan))]
    public virtual Phan Phan { get; set; } = null!;

    /// <summary>Câu hỏi cha (tự tham chiếu — câu nhóm).</summary>
    [ForeignKey(nameof(MaCauHoiCha))]
    public virtual CauHoi? CauHoiCha { get; set; }

    /// <summary>Các câu hỏi con (thuộc câu nhóm này).</summary>
    public virtual ICollection<CauHoi> CauHoiCons { get; set; } = new List<CauHoi>();
    public virtual ICollection<CauTraLoi> CauTraLois { get; set; } = new List<CauTraLoi>();
    public virtual ICollection<FileDinhKem> Files { get; set; } = new List<FileDinhKem>();
    public virtual ICollection<ChiTietDeThi> ChiTietDeThis { get; set; } = new List<ChiTietDeThi>();
}