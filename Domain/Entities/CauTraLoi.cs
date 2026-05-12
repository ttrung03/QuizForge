using System;
using System.Collections.Generic;

namespace QuestionBank.Web.Domain.Entities;

public partial class CauTraLoi
{
    public Guid MaCauTraLoi { get; set; }

    public Guid MaCauHoi { get; set; }

    public string? NoiDung { get; set; }

    public int ThuTu { get; set; }

    public bool LaDapAn { get; set; }

    public bool HoanVi { get; set; }

    public virtual CauHoi MaCauHoiNavigation { get; set; } = null!;
}
