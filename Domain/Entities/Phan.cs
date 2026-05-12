using System;
using System.Collections.Generic;

namespace QuestionBank.Web.Domain.Entities;

public partial class Phan
{
    public Guid MaPhan { get; set; }

    public Guid MaMonHoc { get; set; }

    public string TenPhan { get; set; } = null!;

    public string? NoiDung { get; set; }

    public int ThuTu { get; set; }

    public int SoLuongCauHoi { get; set; }

    public Guid? MaPhanCha { get; set; }

    public int? MaSoPhan { get; set; }

    public bool? XoaTamPhan { get; set; }

    public bool LaCauHoiNhom { get; set; }

    public virtual ICollection<CauHoi> CauHois { get; set; } = new List<CauHoi>();

    public virtual ICollection<ChiTietDeThi> ChiTietDeThis { get; set; } = new List<ChiTietDeThi>();

    public virtual MonHoc MaMonHocNavigation { get; set; } = null!;

}
