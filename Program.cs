using LibraryAppPrototype.Components;
using LibraryAppPrototype.Data;
using LibraryAppPrototype.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Factory, BUKAN AddDbContext — lihat PRD bagian 11.1
builder.Services.AddDbContextFactory<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IClock, SystemClock>();

// Registrasi 6 service modul menyusul di Fase 3.

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
