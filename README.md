# 📚 QuestionBank Web

> Hệ thống quản lý ngân hàng câu hỏi và đề thi dành cho các cơ sở giáo dục — được xây dựng trên nền tảng **Blazor Server (.NET 10)** với kiến trúc **Clean Architecture**.

---

## 🖥️ Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Framework | ASP.NET Core / Blazor Server (.NET 10) |
| UI Component | [MudBlazor](https://mudblazor.com/) v9.4 |
| ORM | Entity Framework Core 10 (DB First) |
| Cơ sở dữ liệu | SQL Server |
| Validation | FluentValidation |
| Ngôn ngữ | C# 13 |

---

## 🏗️ Kiến trúc

Dự án áp dụng **Clean Architecture** với các tầng được tách biệt rõ ràng trong cùng một project:

```
QuestionBank.Web/
├── Domain/                  # Tầng Domain — Entities, business rules
│   └── Entities/
│       ├── CauHoi.cs        # Câu hỏi (hỗ trợ câu nhóm & câu con)
│       ├── CauTraLoi.cs     # Câu trả lời
│       ├── ChiTietDeThi.cs  # Chi tiết đề thi
│       ├── DeThi.cs         # Đề thi
│       ├── FileDinhKem.cs   # File đính kèm (ảnh, âm thanh...)
│       ├── Khoa.cs          # Khoa / Bộ môn
│       ├── MonHoc.cs        # Môn học
│       ├── Phan.cs          # Phần / Chương (hỗ trợ cây cha-con)
│       └── YeuCauRutTrich.cs
│
├── Application/             # Tầng Application — Use cases, interfaces
│   ├── DTOs/
│   ├── Interfaces/
│   └── Services/
│       └── KhoaService.cs
│
├── Infrastructure/          # Tầng Infrastructure — EF Core, repositories
│   ├── Data/
│   │   └── QuestionBankDbContext.cs
│   └── Repositories/
│       └── KhoaRepository.cs
│
├── Components/              # Shared Razor components
├── Pages/                   # Trang Blazor
│   ├── Index.razor
│   └── Danhmuc/
│       └── KhoaPage.razor
│
├── Program.cs
└── appsettings.json
```

---

## ✨ Tính năng

- **Quản lý danh mục**: Khoa, Môn học, Phần/Chương (hỗ trợ phân cấp cha-con)
- **Ngân hàng câu hỏi**: Thêm, sửa, xóa câu hỏi với nhiều loại (độc lập / câu nhóm)
- **Câu trả lời**: Quản lý các phương án trả lời cho mỗi câu hỏi
- **Mức độ khó**: Phân loại câu hỏi theo cấp độ (Dễ / Trung bình / Khó)
- **File đính kèm**: Hỗ trợ đính kèm ảnh/file cho câu hỏi và câu trả lời
- **Đề thi**: Tạo và quản lý đề thi từ ngân hàng câu hỏi
- **Rút trích đề thi**: Tự động chọn câu hỏi theo tiêu chí

---

## ⚙️ Yêu cầu hệ thống

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server 2019 trở lên (hoặc SQL Server Express)
- Visual Studio 2022+ hoặc VS Code với C# extension

---

## 🚀 Hướng dẫn cài đặt & chạy

### 1. Clone repository

```bash
git clone https://github.com/<your-username>/QuestionBank.git
cd QuestionBank
```

### 2. Cấu hình chuỗi kết nối database

Mở file `appsettings.json` và cập nhật connection string phù hợp với SQL Server của bạn:

```json
{
  "ConnectionStrings": {
    "Default": "Server=<TÊN_SERVER>;Database=2013_QuestionBank;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

> **Lưu ý:** Không commit `appsettings.json` chứa thông tin kết nối thật lên repository công khai. Sử dụng `appsettings.Development.json` hoặc biến môi trường cho môi trường local.

### 3. Tạo database

Đảm bảo SQL Server đang chạy, sau đó áp dụng migration (nếu chưa có database):

```bash
cd QuestionBank.Web
dotnet ef database update
```

Hoặc nếu đã có database cũ (`2013_QuestionBank`), schema sẽ được ánh xạ tự động qua **DB First**.

### 4. Chạy ứng dụng

```bash
dotnet run
```

Ứng dụng sẽ chạy tại:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

Hoặc mở solution bằng **Visual Studio 2022** và nhấn `F5`.

---

## 🗄️ Sơ đồ quan hệ thực thể (ERD)

```
Khoa ──< MonHoc ──< Phan (tự tham chiếu) ──< CauHoi (tự tham chiếu)
                                                    │
                                               ┌────┴────────────┐
                                          CauTraLoi          FileDinhKem
                                               │
                                        ChiTietDeThi >── DeThi
```

---

## 📁 Cấu trúc database

| Bảng | Mô tả |
|---|---|
| `Khoa` | Khoa / Bộ môn |
| `MonHoc` | Môn học (thuộc Khoa) |
| `Phan` | Phần / Chương (hỗ trợ phân cấp cha-con) |
| `CauHoi` | Câu hỏi (hỗ trợ câu nhóm — tự tham chiếu) |
| `CauTraLoi` | Câu trả lời / phương án |
| `DeThi` | Đề thi |
| `ChiTietDeThi` | Chi tiết đề thi (câu hỏi thuộc đề thi) |
| `Files` | File đính kèm cho câu hỏi / câu trả lời |
| `YeuCauRutTrich` | Yêu cầu rút trích đề thi tự động |

---

## 🤝 Đóng góp

1. Fork repository
2. Tạo branch mới: `git checkout -b feature/ten-tinh-nang`
3. Commit thay đổi: `git commit -m "feat: mô tả thay đổi"`
4. Push lên branch: `git push origin feature/ten-tinh-nang`
5. Tạo Pull Request

---

## 📄 License

Dự án được phân phối dưới giấy phép [MIT](LICENSE).

---

<p align="center">
  Xây dựng với ❤️ bằng <strong>Blazor Server</strong> &amp; <strong>MudBlazor</strong>
</p>
