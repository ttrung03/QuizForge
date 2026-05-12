using System;
using System.Collections.Generic;

namespace QuestionBank.Web.Domain.Entities;

public partial class Khoa
{
    public Guid MaKhoa { get; set; }

    public string TenKhoa { get; set; } = null!;

    public bool? XoaTamKhoa { get; set; }

    public virtual ICollection<MonHoc> MonHocs { get; set; } = new List<MonHoc>();
}
