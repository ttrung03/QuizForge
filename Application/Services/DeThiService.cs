using QuestionBank.Web.Application.DTOs;
using QuestionBank.Web.Application.Interfaces;
using QuestionBank.Web.Domain.Entities;

namespace QuestionBank.Web.Application.Services;

public class DeThiService(IDeThiRepository repo, ICauHoiRepository cauHoiRepo)
{
    public async Task<List<DeThiDto>> GetByMonHocAsync(Guid maMonHoc)
    {
        var list = await repo.GetByMonHocAsync(maMonHoc);
        return list.Select(MapToDto).ToList();
    }

    public async Task SaveAsync(SaveDeThiDto dto)
    {
        var maDe = await repo.GetNextMaDeAsync(dto.MaMonHoc);
        var deThi = new DeThi
        {
            MaDeThi  = Guid.NewGuid(),
            MaMonHoc = dto.MaMonHoc,
            TenDeThi = dto.TenDeThi.Trim(),
            NgayTao  = DateTime.Now,
            DaDuyet  = false,
            MaDe     = maDe
        };

        var chiTiets = dto.CauHois
            .Select((ch, idx) => new ChiTietDeThi
            {
                MaDeThi  = deThi.MaDeThi,
                MaCauHoi = ch.MaCauHoi,
                MaPhan   = ch.MaPhan,
                ThuTu    = idx + 1
            })
            .ToList();

        await repo.AddAsync(deThi, chiTiets);
    }

    public async Task DeleteAsync(Guid maDeThi)
        => await repo.DeleteAsync(maDeThi);

    public async Task RemoveCauHoisAsync(Guid maDeThi, List<Guid> maCauHois)
        => await repo.RemoveCauHoisAsync(maDeThi, maCauHois);

    public async Task AddCauHoisAsync(Guid maDeThi, List<SelectedCauHoiDto> cauHois)
    {
        var pairs = cauHois.Select(c => (c.MaCauHoi, c.MaPhan)).ToList();
        await repo.AddCauHoisAsync(maDeThi, pairs);
    }

    public async Task DuyetAsync(Guid maDeThi)
        => await repo.UpdateDaDuyetAsync(maDeThi, true);

    /// <summary>Lưu thứ tự câu hỏi và đáp án từ màn hình hoán vị, rồi phê duyệt đề thi.</summary>
    public async Task DuyetVoiHoanViAsync(
        Guid maDeThi,
        List<(Guid maCauHoi, int thuTu)> cauHoiOrders,
        List<(Guid maCauTraLoi, int thuTu)> cauTraLoiOrders)
    {
        await repo.UpdateThuTuAsync(maDeThi, cauHoiOrders);
        if (cauTraLoiOrders.Count > 0)
            await cauHoiRepo.UpdateCauTraLoiThuTuAsync(cauTraLoiOrders);
        await repo.UpdateDaDuyetAsync(maDeThi, true);
    }

    public async Task HuyDuyetAsync(Guid maDeThi)
        => await repo.UpdateDaDuyetAsync(maDeThi, false);

    /// <summary>
    /// Sinh N bản đề từ 1 đề gốc.
    /// Mỗi bản đề là 1 DeThi mới với ThuTu câu hỏi được hoán vị ngẫu nhiên.
    /// Câu hỏi nhóm (có câu con) luôn được giữ nguyên cấu trúc nội bộ,
    /// chỉ hoán vị vị trí của cả nhóm so với các câu/nhóm khác.
    /// </summary>
    public async Task<List<KetQuaSinhMaDeDto>> SinhNhieuMaDeAsync(SinhNhieuMaDeDto request)
    {
        var deThiGoc = await repo.GetByIdAsync(request.MaDeThiGoc)
            ?? throw new InvalidOperationException("Không tìm thấy đề thi gốc.");

        // Lấy danh sách câu hỏi từ đề gốc, sắp xếp theo ThuTu
        var chiTiets = deThiGoc.ChiTietDeThis
            .OrderBy(c => c.ThuTu)
            .ToList();

        if (chiTiets.Count == 0)
            throw new InvalidOperationException("Đề thi gốc không có câu hỏi.");

        var rng     = new Random();
        var batches = new List<(DeThi, List<ChiTietDeThi>)>();
        var results = new List<KetQuaSinhMaDeDto>();

        // Nhóm các câu: đơn vs theo MaCauHoiCha
        // Mỗi "slot" là 1 đơn vị hoán vị (câu đơn hoặc cả nhóm câu con)
        var slots = BuildSlots(chiTiets);

        // Tất cả bản sinh ra dùng cùng MaDe với đề gốc để dễ nhận nhóm.
        // Phân biệt bằng chữ cái A, B, C, D… trong tên đề thi.
        var maDeGoc      = deThiGoc.MaDe ?? await repo.GetNextMaDeAsync(request.MaMonHoc);
        var tenDeThiGoc  = deThiGoc.TenDeThi;

        for (int i = 0; i < request.SoBanDe; i++)
        {
            // Chữ cái phiên bản: A, B, C … Z, AA, AB … (hỗ trợ tối đa 702 phiên bản)
            var phanBienLabel = ToVariantLabel(i);
            var maDe     = maDeGoc;
            var maDeThi  = Guid.NewGuid();
            var tenDeThi = $"{tenDeThiGoc} — {phanBienLabel}";

            var orderedSlots = request.HoanViCauHoi
                ? slots.OrderBy(_ => rng.Next()).ToList()
                : slots.ToList();

            int thuTu    = 1;
            var chiTietsMoi = new List<ChiTietDeThi>();
            foreach (var slot in orderedSlots)
            {
                foreach (var src in slot)
                {
                    chiTietsMoi.Add(new ChiTietDeThi
                    {
                        MaDeThi  = maDeThi,
                        MaCauHoi = src.MaCauHoi,
                        MaPhan   = src.MaPhan,
                        ThuTu    = thuTu++
                    });
                }
            }

            var deThi = new DeThi
            {
                MaDeThi  = maDeThi,
                MaMonHoc = request.MaMonHoc,
                TenDeThi = tenDeThi,
                NgayTao  = DateTime.Now,
                DaDuyet  = false,
                MaDe     = maDe
            };

            batches.Add((deThi, chiTietsMoi));
            results.Add(new KetQuaSinhMaDeDto
            {
                MaDeThi  = maDeThi,
                MaDe     = maDe,
                TenDeThi = tenDeThi,
                SoCauHoi = chiTietsMoi.Count
            });
        }

        await repo.AddManyAsync(batches);
        return results;
    }

    /// <summary>
    /// Xây danh sách "slot" hoán vị từ ChiTietDeThi.
    /// Câu đơn → 1 slot chứa 1 phần tử.
    /// Câu con của cùng 1 nhóm → 1 slot chứa tất cả câu con đó (giữ thứ tự nội bộ).
    /// </summary>
    private static List<List<ChiTietDeThi>> BuildSlots(List<ChiTietDeThi> chiTiets)
    {
        var slots   = new List<List<ChiTietDeThi>>();
        var grouped = new Dictionary<Guid, List<ChiTietDeThi>>(); // key = MaCauHoiCha

        foreach (var ct in chiTiets)
        {
            var chaCauHoi = ct.MaCauHoiNavigation?.MaCauHoiCha;
            if (chaCauHoi.HasValue)
            {
                // Câu con — gom vào nhóm theo câu cha
                if (!grouped.TryGetValue(chaCauHoi.Value, out var group))
                {
                    group = [];
                    grouped[chaCauHoi.Value] = group;
                    slots.Add(group); // thêm slot tham chiếu
                }
                group.Add(ct);
            }
            else
            {
                // Câu đơn — mỗi câu là 1 slot riêng
                slots.Add([ct]);
            }
        }

        return slots;
    }

    /// <summary>
    /// Chuyển index (0-based) thành nhãn chữ cái phiên bản kiểu Excel:
    /// 0→A, 1→B, …, 25→Z, 26→AA, 27→AB, …
    /// </summary>
    private static string ToVariantLabel(int index)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        if (index < 26)
            return chars[index].ToString();

        var sb = new System.Text.StringBuilder();
        index++; // shift to 1-based for multi-char
        while (index > 0)
        {
            index--;
            sb.Insert(0, chars[index % 26]);
            index /= 26;
        }
        return sb.ToString();
    }

    private static DeThiDto MapToDto(DeThi d)
    {
        var ordered = d.ChiTietDeThis.OrderBy(c => c.ThuTu).ToList();

        // Tách câu đơn và câu con (thuộc nhóm)
        var singles  = ordered.Where(c => c.MaCauHoiNavigation?.MaCauHoiCha == null).ToList();
        var children = ordered.Where(c => c.MaCauHoiNavigation?.MaCauHoiCha != null).ToList();

        // Gom câu con theo câu cha
        var groups = children
            .GroupBy(c => c.MaCauHoiNavigation!.MaCauHoiCha!.Value)
            .Select(g => new ChiTietDeThiDto
            {
                MaCauHoi   = g.Key,
                MaPhan     = g.First().MaPhan,
                TenPhan    = g.First().MaPhanNavigation?.TenPhan ?? "",
                ThuTu      = g.Min(c => c.ThuTu),
                NoiDung    = g.First().MaCauHoiNavigation!.CauHoiCha?.NoiDung,
                CapDo      = g.First().MaCauHoiNavigation!.CauHoiCha?.CapDo ?? 1,
                LaCauNhom  = true,
                Files      = MapFiles(g.First().MaCauHoiNavigation!.CauHoiCha?.Files),
                CauHoiCons = g.OrderBy(c => c.ThuTu).Select(c => new ChiTietDeThiDto
                {
                    MaCauHoi   = c.MaCauHoi,
                    MaPhan     = c.MaPhan,
                    ThuTu      = c.ThuTu,
                    MaSoCauHoi = c.MaCauHoiNavigation?.MaSoCauHoi ?? 0,
                    NoiDung    = c.MaCauHoiNavigation?.NoiDung,
                    CapDo      = c.MaCauHoiNavigation?.CapDo ?? 1,
                    Files      = MapFiles(c.MaCauHoiNavigation?.Files),
                    CauTraLois = MapCauTraLois(c.MaCauHoiNavigation?.CauTraLois)
                }).ToList()
            });

        var mappedSingles = singles.Select(c => new ChiTietDeThiDto
        {
            MaCauHoi   = c.MaCauHoi,
            MaPhan     = c.MaPhan,
            TenPhan    = c.MaPhanNavigation?.TenPhan ?? "",
            ThuTu      = c.ThuTu,
            MaSoCauHoi = c.MaCauHoiNavigation?.MaSoCauHoi ?? 0,
            NoiDung    = c.MaCauHoiNavigation?.NoiDung,
            CapDo      = c.MaCauHoiNavigation?.CapDo ?? 1,
            Files      = MapFiles(c.MaCauHoiNavigation?.Files),
            CauTraLois = MapCauTraLois(c.MaCauHoiNavigation?.CauTraLois)
        });

        return new DeThiDto
        {
            MaDeThi       = d.MaDeThi,
            MaMonHoc      = d.MaMonHoc,
            TenDeThi      = d.TenDeThi,
            NgayTao       = d.NgayTao,
            DaDuyet       = d.DaDuyet,
            MaDe          = d.MaDe,
            ChiTietDeThis = mappedSingles.Concat(groups).OrderBy(x => x.ThuTu).ToList()
        };
    }

    private static List<CauTraLoiDto> MapCauTraLois(IEnumerable<CauTraLoi>? list)
        => list?.OrderBy(a => a.ThuTu).Select(a => new CauTraLoiDto
        {
            MaCauTraLoi = a.MaCauTraLoi,
            NoiDung     = a.NoiDung,
            ThuTu       = a.ThuTu,
            LaDapAn     = a.LaDapAn,
            HoanVi      = a.HoanVi
        }).ToList() ?? [];

    private static List<FileDinhKemDto> MapFiles(IEnumerable<FileDinhKem>? files)
        => files?.Select(f => new FileDinhKemDto
        {
            MaFile   = f.MaFile,
            LoaiFile = f.LoaiFile ?? 0,
            TenFile  = f.TenFile ?? ""
        }).ToList() ?? [];
}
