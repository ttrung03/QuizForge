using Microsoft.EntityFrameworkCore;
using QuestionBank.Web.Domain.Entities;

namespace QuestionBank.Web.Infrastructure.Data;

/// <summary>
/// DbContext chính — cầu nối giữa EF Core và SQL Server database 2013_QuestionBank.
/// Được tạo theo phương pháp DB First: cấu trúc phản ánh đúng schema hiện có.
/// </summary>
public class QuestionBankDbContext : DbContext
{
    public QuestionBankDbContext(DbContextOptions<QuestionBankDbContext> options)
        : base(options) { }

    // ── DbSet — mỗi property tương ứng 1 bảng ────────────────────────────────
    public DbSet<Khoa> Khoas { get; set; }
    public DbSet<MonHoc> MonHocs { get; set; }
    public DbSet<MonHocKhoaChung> MonHocKhoaChungs { get; set; }
    public DbSet<Phan> Phans { get; set; }
    public DbSet<CauHoi> CauHois { get; set; }
    public DbSet<CauTraLoi> CauTraLois { get; set; }
    public DbSet<DeThi> DeThis { get; set; }
    public DbSet<ChiTietDeThi> ChiTietDeThis { get; set; }
    public DbSet<FileDinhKem> Files { get; set; }
    public DbSet<YeuCauRutTrich> YeuCauRutTriches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ════════════════════════════════════════════════════════════════════
        // KHOA
        // ════════════════════════════════════════════════════════════════════
        modelBuilder.Entity<Khoa>(entity =>
        {
            entity.HasKey(e => e.MaKhoa);
            entity.ToTable("Khoa");
            entity.Property(e => e.TenKhoa).IsRequired().HasMaxLength(250);
        });

        // ════════════════════════════════════════════════════════════════════
        // MONHOC
        // ════════════════════════════════════════════════════════════════════
        modelBuilder.Entity<MonHoc>(entity =>
        {
            entity.HasKey(e => e.MaMonHoc);
            entity.ToTable("MonHoc");
            entity.Property(e => e.MaSoMonHoc).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TenMonHoc).IsRequired().HasMaxLength(250);

            // FK: MonHoc → Khoa (nullable, SET NULL khi xóa Khoa)
            entity.HasOne(e => e.Khoa)
                  .WithMany(k => k.MonHocs)
                  .HasForeignKey(e => e.MaKhoa)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ════════════════════════════════════════════════════════════════════
        // MONHOC_KHOACHUNG — môn học dùng chung nhiều khoa
        // ════════════════════════════════════════════════════════════════════
        modelBuilder.Entity<MonHocKhoaChung>(entity =>
        {
            entity.HasKey(e => new { e.MaMonHoc, e.MaKhoa });
            entity.ToTable("MonHoc_KhoaChung");

            entity.HasOne(e => e.MonHoc)
                  .WithMany()
                  .HasForeignKey(e => e.MaMonHoc)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Khoa)
                  .WithMany()
                  .HasForeignKey(e => e.MaKhoa)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ════════════════════════════════════════════════════════════════════
        // PHAN — tự tham chiếu (cây cha-con)
        // ════════════════════════════════════════════════════════════════════
        modelBuilder.Entity<Phan>(entity =>
        {
            entity.HasKey(e => e.MaPhan);
            entity.ToTable("Phan");
            entity.Property(e => e.TenPhan).IsRequired().HasMaxLength(250);
            entity.Property(e => e.LaCauHoiNhom).HasDefaultValue(false);

            // FK: Phan → MonHoc (ON UPDATE CASCADE)
            entity.HasOne(e => e.MaMonHocNavigation)
                  .WithMany(m => m.Phans)
                  .HasForeignKey(e => e.MaMonHoc)
                  .OnDelete(DeleteBehavior.Cascade);

            // FK tự tham chiếu: Phan → Phan (MaPhanCha) — NO ACTION để tránh multiple cascade
            entity.HasOne<Phan>()
                  .WithMany()
                  .HasForeignKey(e => e.MaPhanCha)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ════════════════════════════════════════════════════════════════════
        // CAUHOI — tự tham chiếu (câu hỏi nhóm)
        // ════════════════════════════════════════════════════════════════════
        modelBuilder.Entity<CauHoi>(entity =>
        {
            entity.HasKey(e => e.MaCauHoi);
            entity.ToTable("CauHoi");
            entity.Property(e => e.SoCauHoiCon).HasDefaultValue(0);

            // FK: CauHoi → Phan (ON UPDATE CASCADE)
            entity.HasOne(e => e.Phan)
                  .WithMany(p => p.CauHois)
                  .HasForeignKey(e => e.MaPhan)
                  .OnDelete(DeleteBehavior.Cascade);

            // FK tự tham chiếu: CauHoi → CauHoi (MaCauHoiCha) — NO ACTION
            entity.HasOne(e => e.CauHoiCha)
                  .WithMany(c => c.CauHoiCons)
                  .HasForeignKey(e => e.MaCauHoiCha)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ════════════════════════════════════════════════════════════════════
        // CAUTRALOI
        // ════════════════════════════════════════════════════════════════════
        modelBuilder.Entity<CauTraLoi>(entity =>
        {
            entity.HasKey(e => e.MaCauTraLoi);
            entity.ToTable("CauTraLoi");

            // FK: CauTraLoi → CauHoi (ON UPDATE CASCADE)
            entity.HasOne(e => e.MaCauHoiNavigation)
                  .WithMany(c => c.CauTraLois)
                  .HasForeignKey(e => e.MaCauHoi)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ════════════════════════════════════════════════════════════════════
        // DETHI
        // ════════════════════════════════════════════════════════════════════
        modelBuilder.Entity<DeThi>(entity =>
        {
            entity.HasKey(e => e.MaDeThi);
            entity.ToTable("DeThi");
            entity.Property(e => e.TenDeThi).IsRequired().HasMaxLength(250);
            entity.Property(e => e.MaDe);

            // FK: DeThi → MonHoc (ON UPDATE CASCADE)
            entity.HasOne(e => e.MaMonHocNavigation)
                  .WithMany(m => m.DeThis)
                  .HasForeignKey(e => e.MaMonHoc)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ════════════════════════════════════════════════════════════════════
        // CHITIETDETHI — khoá chính tổ hợp (MaDeThi, MaPhan, MaCauHoi)
        // ════════════════════════════════════════════════════════════════════
        modelBuilder.Entity<ChiTietDeThi>(entity =>
        {
            entity.HasKey(e => new { e.MaDeThi, e.MaPhan, e.MaCauHoi });
            entity.ToTable("ChiTietDeThi");

            // FK: → DeThi
            entity.HasOne(e => e.MaDeThiNavigation)
                  .WithMany(d => d.ChiTietDeThis)
                  .HasForeignKey(e => e.MaDeThi)
                  .OnDelete(DeleteBehavior.NoAction);

            // FK: → Phan
            entity.HasOne(e => e.MaPhanNavigation)
                  .WithMany(p => p.ChiTietDeThis)
                  .HasForeignKey(e => e.MaPhan)
                  .OnDelete(DeleteBehavior.NoAction);

            // FK: → CauHoi
            entity.HasOne(e => e.MaCauHoiNavigation)
                  .WithMany(c => c.ChiTietDeThis)
                  .HasForeignKey(e => e.MaCauHoi)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ════════════════════════════════════════════════════════════════════
        // FILES (FileDinhKem) — map về tên bảng "Files"
        // ════════════════════════════════════════════════════════════════════
        modelBuilder.Entity<FileDinhKem>(entity =>
        {
            entity.HasKey(e => e.MaFile);
            entity.ToTable("Files"); // tên bảng trong DB

            // FK: → CauHoi (nullable)
            entity.HasOne(e => e.CauHoi)
                  .WithMany(c => c.Files)
                  .HasForeignKey(e => e.MaCauHoi)
                  .OnDelete(DeleteBehavior.NoAction);

            // FK: → CauTraLoi (nullable) — CauTraLoi không có nav prop Files, dùng HasMany từ phía FileDinhKem
            entity.HasOne(e => e.CauTraLoi)
                  .WithMany()
                  .HasForeignKey(e => e.MaCauTraLoi)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ════════════════════════════════════════════════════════════════════
        // YEUCAURUTRICH
        // ════════════════════════════════════════════════════════════════════
        modelBuilder.Entity<YeuCauRutTrich>(entity =>
        {
            entity.HasKey(e => e.MaYeuCauDe);
            entity.ToTable("YeuCauRutTrich");
        });
    }
}