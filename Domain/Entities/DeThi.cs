using System;
using System.Collections.Generic;

namespace QuestionBank.Web.Domain.Entities;

public partial class DeThi
{
    public Guid MaDeThi { get; set; }

    public Guid MaMonHoc { get; set; }

    public string TenDeThi { get; set; } = null!;

    public DateTime NgayTao { get; set; }

    public bool? DaDuyet { get; set; }

    public int? MaDe { get; set; }

    public virtual ICollection<ChiTietDeThi> ChiTietDeThis { get; set; } = new List<ChiTietDeThi>();

    public virtual MonHoc MaMonHocNavigation { get; set; } = null!;
}
