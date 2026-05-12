using System;
using System.Collections.Generic;

namespace QuestionBank.Web.Domain.Entities;

public partial class ChiTietDeThi
{
    public Guid MaDeThi { get; set; }

    public Guid MaPhan { get; set; }

    public Guid MaCauHoi { get; set; }

    public int ThuTu { get; set; }

    public virtual CauHoi MaCauHoiNavigation { get; set; } = null!;

    public virtual DeThi MaDeThiNavigation { get; set; } = null!;

    public virtual Phan MaPhanNavigation { get; set; } = null!;
}
