using LibraryAppPrototype.Data;
using LibraryAppPrototype.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryAppPrototype.Services;

// Master data penulis & kategori (FR-26, FR-27).
public class LookupService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<List<Author>> GetAuthorsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Authors.AsNoTracking().OrderBy(a => a.Name).ToListAsync(ct);
    }

    public async Task<List<Category>> GetCategoriesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);
    }

    // FR-26: nama unik. Dicek dulu di sini supaya pesannya ramah, bukan stack trace (AC-19).
    public async Task<OperationResult<int>> CreateAuthorAsync(
        string name, string? biography = null, CancellationToken ct = default)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length == 0)
            return OperationResult<int>.Fail("VALIDATION", "Nama penulis wajib diisi.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        if (await db.Authors.AnyAsync(a => a.Name == name, ct))
            return OperationResult<int>.Fail("CONFLICT", $"Penulis \"{name}\" sudah terdaftar.");

        var author = new Author { Name = name, Biography = biography?.Trim() };
        db.Authors.Add(author);
        await db.SaveChangesAsync(ct);

        return OperationResult<int>.Ok(author.Id);
    }

    // FR-27: nama unik.
    public async Task<OperationResult<int>> CreateCategoryAsync(
        string name, string? description = null, CancellationToken ct = default)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length == 0)
            return OperationResult<int>.Fail("VALIDATION", "Nama kategori wajib diisi.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        if (await db.Categories.AnyAsync(c => c.Name == name, ct))
            return OperationResult<int>.Fail("CONFLICT", $"Kategori \"{name}\" sudah terdaftar.");

        var category = new Category { Name = name, Description = description?.Trim() };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return OperationResult<int>.Ok(category.Id);
    }
}
