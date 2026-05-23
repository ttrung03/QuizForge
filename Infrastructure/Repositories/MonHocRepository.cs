using Microsoft.EntityFrameworkCore;
using QuestionBank.Web.Application.Interfaces;
using QuestionBank.Web.Domain.Entities;
using QuestionBank.Web.Infrastructure.Data;

namespace QuestionBank.Web.Infrastructure.Repositories;

/// <summary>
/// Thực thi IMonHocRepository — toàn bộ query EF Core nằm ở đây.
/// Service không biết gì về EF Core hay SQL.
/// </summary>
public class MonHocRepository : IMonHocRepository
{
    private readonly QuestionBankDbContext _context;

    public MonHocRepository(QuestionBankDbContext context)
    {
        _context = context;
    }

    /// <summary>Lấy tất cả Môn học chưa bị xóa mềm, include Khoa, sắp xếp theo tên.</summary>
    public async Task<List<MonHoc>> GetAllAsync()
        => await _context.MonHocs
               .Include(m => m.Khoa)
               .Where(m => m.XoaTamMonHoc != true)
               .OrderBy(m => m.TenMonHoc)
               .ToListAsync();

    /// <summary>Lấy môn học trực tiếp thuộc khoa (MaKhoa = maKhoa).</summary>
    public async Task<List<MonHoc>> GetAllByKhoaAsync(Guid maKhoa)
        => await _context.MonHocs
               .Include(m => m.Khoa)
               .Where(m => m.MaKhoa == maKhoa && m.XoaTamMonHoc != true)
               .OrderBy(m => m.TenMonHoc)
               .ToListAsync();

    /// <summary>Lấy môn học được dùng chung cho khoa này (trong bảng MonHoc_KhoaChung).</summary>
    public async Task<List<MonHoc>> GetSharedByKhoaAsync(Guid maKhoa)
        => await _context.MonHocKhoaChungs
               .Where(mkc => mkc.MaKhoa == maKhoa)
               .Select(mkc => mkc.MonHoc)
               .Where(m => m.XoaTamMonHoc != true)
               .OrderBy(m => m.TenMonHoc)
               .ToListAsync();

    /// <summary>
    /// Lấy môn học chưa có mặt trong khoa này (chưa trực tiếp và chưa dùng chung).
    /// Dùng cho dialog "Thêm môn học có sẵn".
    /// </summary>
    public async Task<List<MonHoc>> GetAvailableForKhoaAsync(Guid maKhoa)
    {
        // IDs đã có trong khoa qua bảng chung
        var sharedIds = await _context.MonHocKhoaChungs
            .Where(mkc => mkc.MaKhoa == maKhoa)
            .Select(mkc => mkc.MaMonHoc)
            .ToListAsync();

        return await _context.MonHocs
            .Include(m => m.Khoa)
            .Where(m => m.XoaTamMonHoc != true
                     && m.MaKhoa != maKhoa           // chưa trực tiếp thuộc khoa này
                     && !sharedIds.Contains(m.MaMonHoc)) // chưa được share cho khoa này
            .OrderBy(m => m.TenMonHoc)
            .ToListAsync();
    }

    public async Task<MonHoc?> GetByIdAsync(Guid id)
        => await _context.MonHocs.FindAsync(id);

    public async Task AddAsync(MonHoc monHoc)
    {
        // Kiểm tra trùng mã toàn cục
        var exists = await _context.MonHocs.AnyAsync(m =>
            m.MaSoMonHoc == monHoc.MaSoMonHoc &&
            m.XoaTamMonHoc != true);

        if (exists)
            throw new InvalidOperationException(
                $"Mã số môn học '{monHoc.MaSoMonHoc}' đã tồn tại.");

        monHoc.MaMonHoc = Guid.NewGuid();
        _context.MonHocs.Add(monHoc);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(MonHoc monHoc)
    {
        // Kiểm tra trùng mã toàn cục (loại trừ chính bản ghi đang sửa)
        var exists = await _context.MonHocs.AnyAsync(m =>
            m.MaSoMonHoc == monHoc.MaSoMonHoc &&
            m.MaMonHoc != monHoc.MaMonHoc &&
            m.XoaTamMonHoc != true);

        if (exists)
            throw new InvalidOperationException(
                $"Mã số môn học '{monHoc.MaSoMonHoc}' đã tồn tại.");

        _context.MonHocs.Update(monHoc);
        await _context.SaveChangesAsync();
    }

    /// <summary>Gán Môn học trực tiếp vào một Khoa (set MaKhoa).</summary>
    public async Task AssignToKhoaAsync(Guid maMonHoc, Guid maKhoa)
    {
        var monHoc = await _context.MonHocs.FindAsync(maMonHoc)
                     ?? throw new InvalidOperationException("Không tìm thấy môn học.");
        monHoc.MaKhoa = maKhoa;
        await _context.SaveChangesAsync();
    }

    /// <summary>Thêm bản ghi vào MonHoc_KhoaChung (dùng chung).</summary>
    public async Task AddSharedAsync(Guid maMonHoc, Guid maKhoa)
    {
        var alreadyExists = await _context.MonHocKhoaChungs
            .AnyAsync(mkc => mkc.MaMonHoc == maMonHoc && mkc.MaKhoa == maKhoa);
        if (alreadyExists) return;

        _context.MonHocKhoaChungs.Add(new MonHocKhoaChung
        {
            MaMonHoc = maMonHoc,
            MaKhoa   = maKhoa
        });
        await _context.SaveChangesAsync();
    }

    public async Task<Guid?> GetFirstSharedKhoaAsync(Guid maMonHoc)
    {
        var record = await _context.MonHocKhoaChungs
            .Where(mkc => mkc.MaMonHoc == maMonHoc)
            .FirstOrDefaultAsync();
        return record?.MaKhoa;
    }

    /// <summary>Xóa bản ghi khỏi MonHoc_KhoaChung (gỡ dùng chung).</summary>
    public async Task RemoveSharedAsync(Guid maMonHoc, Guid maKhoa)
    {
        var record = await _context.MonHocKhoaChungs
            .FirstOrDefaultAsync(mkc => mkc.MaMonHoc == maMonHoc && mkc.MaKhoa == maKhoa);
        if (record is null) return;

        _context.MonHocKhoaChungs.Remove(record);
        await _context.SaveChangesAsync();
    }



    /// <summary>Xóa mềm — chỉ đánh dấu, dữ liệu vẫn còn trong DB.</summary>
    public async Task SoftDeleteAsync(Guid id)
    {
        var monHoc = await _context.MonHocs.FindAsync(id);
        if (monHoc is null) return;

        monHoc.XoaTamMonHoc = true;
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Xóa cứng hoàn toàn môn học khỏi DB.
    /// Thứ tự xóa đúng FK: FileDinhKem → CauTraLoi → ChiTietDeThi → CauHoi → Phan → KhoaChung → MonHoc.
    /// </summary>
    public async Task DeleteAllCauHoiByMonAsync(Guid maMonHoc)
    {
        // Lấy tất cả MaPhan thuộc môn này
        var maPhanList = await _context.Phans
            .Where(p => p.MaMonHoc == maMonHoc)
            .Select(p => p.MaPhan)
            .ToListAsync();

        if (maPhanList.Count > 0)
        {
            // Lấy tất cả MaCauHoi thuộc các Phan này
            var maCauHoiList = await _context.CauHois
                .Where(c => maPhanList.Contains(c.MaPhan))
                .Select(c => c.MaCauHoi)
                .ToListAsync();

            if (maCauHoiList.Count > 0)
            {
                // 1a) Xóa FileDinhKem → CauHoi
                var files = await _context.Files
                    .Where(f => f.MaCauHoi.HasValue && maCauHoiList.Contains(f.MaCauHoi.Value))
                    .ToListAsync();
                if (files.Count > 0)
                    _context.Files.RemoveRange(files);

                // 1b) Xóa CauTraLoi → CauHoi
                var cauTraLois = await _context.CauTraLois
                    .Where(c => maCauHoiList.Contains(c.MaCauHoi))
                    .ToListAsync();
                if (cauTraLois.Count > 0)
                    _context.CauTraLois.RemoveRange(cauTraLois);

                // 1c) Xóa ChiTietDeThi → CauHoi
                var chiTiets = await _context.ChiTietDeThis
                    .Where(ct => maCauHoiList.Contains(ct.MaCauHoi))
                    .ToListAsync();
                if (chiTiets.Count > 0)
                    _context.ChiTietDeThis.RemoveRange(chiTiets);

                // 1d) Xóa CauHoi con trước (tự tham chiếu MaCauHoiCha)
                var cauHoiCons = await _context.CauHois
                    .Where(c => c.MaCauHoiCha.HasValue && maCauHoiList.Contains(c.MaCauHoiCha.Value))
                    .ToListAsync();
                if (cauHoiCons.Count > 0)
                    _context.CauHois.RemoveRange(cauHoiCons);

                // 1e) Xóa CauHoi cha (câu độc lập và câu nhóm)
                var cauHois = await _context.CauHois
                    .Where(c => maPhanList.Contains(c.MaPhan) && c.MaCauHoiCha == null)
                    .ToListAsync();
                if (cauHois.Count > 0)
                    _context.CauHois.RemoveRange(cauHois);
            }

            // 2) Xóa Phan
            var phans = await _context.Phans
                .Where(p => p.MaMonHoc == maMonHoc)
                .ToListAsync();
            if (phans.Count > 0)
                _context.Phans.RemoveRange(phans);
        }

        // 3) Xóa liên kết KhoaChung
        var links = await _context.MonHocKhoaChungs
            .Where(mkc => mkc.MaMonHoc == maMonHoc)
            .ToListAsync();
        if (links.Count > 0)
            _context.MonHocKhoaChungs.RemoveRange(links);

        // 4) Xóa chính MonHoc
        var monHoc = await _context.MonHocs.FindAsync(maMonHoc);
        if (monHoc is not null)
            _context.MonHocs.Remove(monHoc);

        await _context.SaveChangesAsync();
    }
}
