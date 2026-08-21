using System.Globalization;
using LibraryAppPrototype.Components;
using LibraryAppPrototype.Data;
using LibraryAppPrototype.Services;
using Microsoft.EntityFrameworkCore;

// Pesan error dan tampilan angka ditulis untuk pengguna Indonesia (aturan 14.1 no. 11),
// jadi format mata uang ":C0" harus menghasilkan "Rp4.000", bukan "$4,000" milik culture mesin.
var appCulture = new CultureInfo("id-ID");
CultureInfo.DefaultThreadCurrentCulture = appCulture;
CultureInfo.DefaultThreadCurrentUICulture = appCulture;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Factory, BUKAN AddDbContext — lihat PRD bagian 11.1
builder.Services.AddDbContextFactory<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IClock, SystemClock>();

builder.Services.AddScoped<BookService>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<LoanService>();
builder.Services.AddScoped<FineService>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<DashboardService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await using var db = await app.Services
        .GetRequiredService<IDbContextFactory<AppDbContext>>()
        .CreateDbContextAsync();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, app.Services.GetRequiredService<IClock>());
}
else
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
