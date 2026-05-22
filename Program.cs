using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using QuestionBank.Web;
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

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<KhoaService>();
builder.Services.AddScoped<MonHocService>();
builder.Services.AddScoped<PhanService>();

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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
