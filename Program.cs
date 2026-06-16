using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using QuestionBank.Web;
using QuestionBank.Web.Application.DTOs;
using QuestionBank.Web.Application.Interfaces;
using QuestionBank.Web.Application.Services;
using QuestionBank.Web.Components;
using QuestionBank.Web.Infrastructure.Data;
using QuestionBank.Web.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ── Razor / Blazor ────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── MudBlazor ────────────────────────────────────────────────────────────────
builder.Services.AddMudServices();

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<QuestionBankDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IKhoaRepository, KhoaRepository>();
builder.Services.AddScoped<IMonHocRepository, MonHocRepository>();
builder.Services.AddScoped<IPhanRepository, PhanRepository>();
builder.Services.AddScoped<ICauHoiRepository, CauHoiRepository>();
builder.Services.AddScoped<IDeThiRepository, DeThiRepository>();

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<KhoaService>();
builder.Services.AddScoped<MonHocService>();
builder.Services.AddScoped<PhanService>();
builder.Services.AddScoped<WordImportService>();
builder.Services.AddScoped<DocImportService>();
builder.Services.AddScoped<ExcelImportService>();
builder.Services.AddScoped<CauHoiService>();
builder.Services.AddScoped<DeThiService>();
builder.Services.AddScoped<ExportDeThiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseStaticFiles(); // serve wwwroot/uploads/ (runtime-uploaded files)
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ── Export API ───────────────────────────────────────────────────────────────
app.MapPost("/api/export/word", (
    ExportRequestDto   req,
    ExportDeThiService exportSvc) =>
{
    var bytes = exportSvc.ExportToWord(
        deThi:           req.DeThi,
        tenMonHoc:       req.TenMonHoc,
        tenKhoa:         req.TenKhoa,
        showAnswers:     req.ShowAnswers,
        includeAnswerKey: req.IncludeAnswerKey);

    var fileName = SanitizeFileName(
        $"{req.DeThi.TenDeThi}{(req.DeThi.MaDe.HasValue ? $"_De{req.DeThi.MaDe.Value:D3}" : "")}.docx");

    return Results.File(
        bytes,
        contentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        fileDownloadName: fileName);
}).DisableAntiforgery();

app.Run();

static string SanitizeFileName(string name)
{
    var invalid = Path.GetInvalidFileNameChars();
    return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
}
