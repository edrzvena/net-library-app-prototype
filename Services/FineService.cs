using LibraryAppPrototype.Data;
using LibraryAppPrototype.Data.Entities;
using LibraryAppPrototype.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryAppPrototype.Services;

public class FineService(IDbContextFactory<AppDbContext> dbFactory, IClock clock)
{
    // BR-20: alasan penghapusan denda minimal 5 karakter.
    public const int MinWaiveReasonLength = 5;

    // FR-21
    public async Task<List<Fine>> GetByMemberAsync(int memberId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        return await BaseQuery(db)
            .Where(f => f.MemberId == memberId)
            .OrderByDescending(f => f.IssuedAt)
            .ThenByDescending(f => f.Id)
            .ToListAsync(ct);
    }

    public async Task<PagedList<Fine>> SearchAsync(
        FineStatus? status = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var query = BaseQuery(db);
        if (status is not null)
            query = query.Where(f => f.Status == status);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(f => f.IssuedAt)
            .ThenByDescending(f => f.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedList<Fine> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<decimal> GetUnpaidTotalAsync(int memberId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Fines
            .Where(f => f.MemberId == memberId && f.Status == FineStatus.Unpaid)
            .SumAsync(f => (decimal?)f.Amount, ct) ?? 0m;
    }

    // FR-22 / BR-22: pembayaran hanya lunas penuh, tidak ada pembayaran sebagian.
    public async Task<OperationResult> PayAsync(int fineId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var fine = await db.Fines.FirstOrDefaultAsync(f => f.Id == fineId, ct);
        if (fine is null)
            return OperationResult.Fail("NOT_FOUND", "Denda tidak ditemukan.");

        // BR-22
        if (fine.Status == FineStatus.Paid)
            return OperationResult.Fail("BR-22", "Denda ini sudah lunas.");

        if (fine.Status == FineStatus.Waived)
            return OperationResult.Fail("BR-22", "Denda ini sudah dihapuskan, tidak perlu dibayar.");

        fine.Status = FineStatus.Paid;
        fine.PaidAt = clock.Today;

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    // FR-23 / BR-20: penghapusan denda wajib disertai alasan.
    public async Task<OperationResult> WaiveAsync(int fineId, string? reason, CancellationToken ct = default)
    {
        var trimmed = (reason ?? string.Empty).Trim();

        // BR-20
        if (trimmed.Length < MinWaiveReasonLength)
            return OperationResult.Fail("BR-20",
                $"Alasan penghapusan wajib diisi, minimal {MinWaiveReasonLength} karakter.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var fine = await db.Fines.FirstOrDefaultAsync(f => f.Id == fineId, ct);
        if (fine is null)
            return OperationResult.Fail("NOT_FOUND", "Denda tidak ditemukan.");

        if (fine.Status == FineStatus.Paid)
            return OperationResult.Fail("BR-20", "Denda yang sudah lunas tidak bisa dihapuskan.");

        if (fine.Status == FineStatus.Waived)
            return OperationResult.Fail("BR-20", "Denda ini sudah dihapuskan sebelumnya.");

        fine.Status = FineStatus.Waived;
        fine.WaiveReason = trimmed;

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    private static IQueryable<Fine> BaseQuery(AppDbContext db) =>
        db.Fines
            .AsNoTracking()
            .Include(f => f.Member)
            .Include(f => f.Loan)
            .ThenInclude(l => l.BookCopy)
            .ThenInclude(c => c.Book);
}
