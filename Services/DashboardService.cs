using LibraryAppPrototype.Data;
using LibraryAppPrototype.Data.Entities;
using LibraryAppPrototype.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryAppPrototype.Services;

// FR-24 (angka ringkasan) + FR-25 (5 pinjaman terbaru & 5 keterlambatan terparah).
public class DashboardService(IDbContextFactory<AppDbContext> dbFactory, IClock clock)
{
    private const int ListSize = 5;

    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var today = clock.Today;

        var recentLoans = await LoanQuery(db)
            .OrderByDescending(l => l.BorrowedAt)
            .ThenByDescending(l => l.Id)
            .Take(ListSize)
            .ToListAsync(ct);

        // BR-19: "terlambat" dihitung dari DueDate, bukan dibaca dari kolom status.
        var worstOverdue = await LoanQuery(db)
            .Where(l => l.Status == LoanStatus.Active && l.ReturnedAt == null && l.DueDate < today)
            .OrderBy(l => l.DueDate)
            .Take(ListSize)
            .ToListAsync(ct);

        return new DashboardSummary
        {
            TotalBooks = await db.Books.CountAsync(ct),
            TotalCopies = await db.BookCopies.CountAsync(ct),
            AvailableCopies = await db.BookCopies.CountAsync(c => c.Status == BookCopyStatus.Available, ct),
            ActiveMembers = await db.Members.CountAsync(m => m.Status == MemberStatus.Active, ct),
            ActiveLoans = await db.Loans.CountAsync(l => l.Status == LoanStatus.Active, ct),

            // BR-19
            OverdueLoans = await db.Loans.CountAsync(
                l => l.Status == LoanStatus.Active && l.ReturnedAt == null && l.DueDate < today, ct),

            OutstandingFineTotal = await db.Fines
                .Where(f => f.Status == FineStatus.Unpaid)
                .SumAsync(f => (decimal?)f.Amount, ct) ?? 0m,

            RecentLoans = recentLoans,
            WorstOverdueLoans = worstOverdue
        };
    }

    private static IQueryable<Loan> LoanQuery(AppDbContext db) =>
        db.Loans
            .AsNoTracking()
            .Include(l => l.Member)
            .Include(l => l.BookCopy)
            .ThenInclude(c => c.Book);
}
