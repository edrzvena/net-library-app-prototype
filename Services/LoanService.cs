using LibraryAppPrototype.Data;
using LibraryAppPrototype.Data.Entities;
using LibraryAppPrototype.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryAppPrototype.Services;

// Inti proyek: 9 dari 23 aturan bisnis menumpuk di sini (PRD bagian 5).
public class LoanService(IDbContextFactory<AppDbContext> dbFactory, IClock clock)
{
    // FR-14 & FR-15. Menegakkan BR-01 s/d BR-05.
    public async Task<OperationResult<Loan>> BorrowAsync(
        int memberId, int bookId, CancellationToken ct = default)
    {
        // Satu DbContext untuk satu operasi — WAJIB, lihat PRD 11.1
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var today = clock.Today;

        var member = await db.Members.FindAsync([memberId], ct);
        if (member is null)
            return OperationResult<Loan>.Fail("NOT_FOUND", "Anggota tidak ditemukan.");

        // BR-04 — anggota harus aktif
        if (member.Status != MemberStatus.Active)
            return OperationResult<Loan>.Fail("BR-04", "Anggota sedang ditangguhkan atau tidak aktif.");

        // BR-01 — maksimal 3 pinjaman aktif
        var activeLoans = await db.Loans.CountAsync(
            l => l.MemberId == memberId && l.Status == LoanStatus.Active, ct);
        if (activeLoans >= LoanPolicy.MaxActiveLoansPerMember)
            return OperationResult<Loan>.Fail("BR-01",
                $"Anggota sudah meminjam {activeLoans} buku (maksimal {LoanPolicy.MaxActiveLoansPerMember}).");

        // BR-03 — tidak boleh punya denda tertunggak
        var unpaid = await db.Fines
            .Where(f => f.MemberId == memberId && f.Status == FineStatus.Unpaid)
            .SumAsync(f => (decimal?)f.Amount, ct) ?? 0m;
        if (unpaid > 0)
            return OperationResult<Loan>.Fail("BR-03",
                $"Anggota masih punya denda tertunggak sebesar {unpaid:C0}.");

        // BR-05 — harus ada kopi tersedia
        var copy = await db.BookCopies.FirstOrDefaultAsync(
            c => c.BookId == bookId && c.Status == BookCopyStatus.Available, ct);
        if (copy is null)
            return OperationResult<Loan>.Fail("BR-05", "Tidak ada eksemplar yang tersedia untuk buku ini.");

        copy.Status = BookCopyStatus.OnLoan;
        var loan = new Loan
        {
            MemberId = memberId,
            BookCopyId = copy.Id,
            BorrowedAt = today,
            DueDate = today.AddDays(LoanPolicy.LoanDurationDays),   // BR-02
            Status = LoanStatus.Active
        };
        db.Loans.Add(loan);

        await db.SaveChangesAsync(ct);   // satu transaksi: kopi + pinjaman
        return OperationResult<Loan>.Ok(loan);
    }

    // FR-16. Menegakkan BR-06 (denda telat), BR-10 (tidak bisa dikembalikan dua kali), BR-11 (kopi jadi Available).
    public async Task<OperationResult<ReturnSummary>> ReturnAsync(int loanId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var today = clock.Today;

        var loan = await db.Loans
            .Include(l => l.BookCopy)
            .FirstOrDefaultAsync(l => l.Id == loanId, ct);

        if (loan is null)
            return OperationResult<ReturnSummary>.Fail("NOT_FOUND", "Pinjaman tidak ditemukan.");

        // BR-10
        if (loan.ReturnedAt is not null || loan.Status == LoanStatus.Returned)
            return OperationResult<ReturnSummary>.Fail("BR-10", "Pinjaman ini sudah dikembalikan sebelumnya.");

        if (loan.Status == LoanStatus.Lost)
            return OperationResult<ReturnSummary>.Fail("BR-10",
                "Pinjaman ini sudah ditandai sebagai buku hilang.");

        loan.ReturnedAt = today;
        loan.Status = LoanStatus.Returned;

        // BR-11 — kopi kembali tersedia, disimpan dalam SaveChanges yang sama dengan update Loan.
        loan.BookCopy.Status = BookCopyStatus.Available;

        // BR-06 — denda = Rp 1.000 x jumlah hari telat.
        var daysLate = loan.DaysLate(today);
        Fine? fine = null;
        if (daysLate > 0)
        {
            fine = new Fine
            {
                LoanId = loan.Id,
                MemberId = loan.MemberId,
                Amount = daysLate * LoanPolicy.FinePerLateDay,
                Reason = FineReason.LateReturn,
                IssuedAt = today,
                Status = FineStatus.Unpaid
            };
            db.Fines.Add(fine);
        }

        await db.SaveChangesAsync(ct); // satu transaksi: pinjaman + kopi + denda

        return OperationResult<ReturnSummary>.Ok(new ReturnSummary
        {
            Loan = loan,
            ReturnedAt = today,
            DaysLate = daysLate,
            Fine = fine
        });
    }

    // FR-17. Menegakkan BR-07 (maks 1x, +7 hari), BR-08 (tolak kalau telat), BR-09 (tolak kalau bukan Active).
    public async Task<OperationResult<Loan>> RenewAsync(int loanId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var today = clock.Today;

        var loan = await db.Loans.FirstOrDefaultAsync(l => l.Id == loanId, ct);
        if (loan is null)
            return OperationResult<Loan>.Fail("NOT_FOUND", "Pinjaman tidak ditemukan.");

        // BR-09
        if (loan.Status != LoanStatus.Active)
            return OperationResult<Loan>.Fail("BR-09", "Hanya pinjaman berstatus aktif yang bisa diperpanjang.");

        // BR-08 — pakai IsOverdue supaya definisi "telat" cuma ada di satu tempat (BR-19).
        if (loan.IsOverdue(today))
            return OperationResult<Loan>.Fail("BR-08",
                $"Pinjaman sudah lewat jatuh tempo {loan.DaysLate(today)} hari, tidak bisa diperpanjang.");

        // BR-07
        if (loan.RenewalCount >= LoanPolicy.MaxRenewalCount)
            return OperationResult<Loan>.Fail("BR-07",
                $"Pinjaman hanya bisa diperpanjang {LoanPolicy.MaxRenewalCount}x.");

        loan.DueDate = loan.DueDate.AddDays(LoanPolicy.RenewalExtensionDays); // BR-07
        loan.RenewalCount++;

        await db.SaveChangesAsync(ct);
        return OperationResult<Loan>.Ok(loan);
    }

    // FR-18. Menegakkan BR-12 dan BR-23 (denda = ReplacementCost).
    public async Task<OperationResult<Fine>> MarkAsLostAsync(int loanId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var today = clock.Today;

        var loan = await db.Loans
            .Include(l => l.BookCopy)
            .ThenInclude(c => c.Book)
            .FirstOrDefaultAsync(l => l.Id == loanId, ct);

        if (loan is null)
            return OperationResult<Fine>.Fail("NOT_FOUND", "Pinjaman tidak ditemukan.");

        if (loan.Status != LoanStatus.Active)
            return OperationResult<Fine>.Fail("BR-12",
                "Hanya pinjaman aktif yang bisa ditandai sebagai buku hilang.");

        // BR-12 — kopi jadi Lost, pinjaman jadi Lost.
        loan.Status = LoanStatus.Lost;
        loan.BookCopy.Status = BookCopyStatus.Lost;

        // BR-23 — denda sebesar harga penggantian.
        var fine = new Fine
        {
            LoanId = loan.Id,
            MemberId = loan.MemberId,
            Amount = loan.BookCopy.Book.ReplacementCost,
            Reason = FineReason.LostBook,
            IssuedAt = today,
            Status = FineStatus.Unpaid
        };
        db.Fines.Add(fine);

        await db.SaveChangesAsync(ct); // satu transaksi: pinjaman + kopi + denda
        return OperationResult<Fine>.Ok(fine);
    }

    // FR-19. Terjemahan LoanFilter -> predikat ada di sini (PRD 7.3, BR-19).
    public async Task<PagedList<Loan>> SearchAsync(
        string? keyword = null,
        LoanFilter? filter = null,
        int? memberId = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var today = clock.Today;

        var query = BaseQuery(db);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(l =>
                l.Member.FullName.Contains(k) ||
                l.Member.Code.Contains(k) ||
                l.BookCopy.Book.Title.Contains(k) ||
                l.BookCopy.InventoryCode.Contains(k));
        }

        if (memberId is > 0)
            query = query.Where(l => l.MemberId == memberId);

        // BR-19: Overdue dihitung dari DueDate, tidak pernah disimpan sebagai LoanStatus (AC-21).
        query = filter switch
        {
            LoanFilter.Active => query.Where(l => l.Status == LoanStatus.Active && l.DueDate >= today),
            LoanFilter.Overdue => query.Where(l => l.Status == LoanStatus.Active
                                               && l.ReturnedAt == null
                                               && l.DueDate < today),
            LoanFilter.Returned => query.Where(l => l.Status == LoanStatus.Returned),
            LoanFilter.Lost => query.Where(l => l.Status == LoanStatus.Lost),
            _ => query
        };

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(l => l.BorrowedAt)
            .ThenByDescending(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedList<Loan> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    // FR-20: daftar keterlambatan, paling lama di atas.
    public async Task<List<Loan>> GetOverdueAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var today = clock.Today;

        // BR-19
        return await BaseQuery(db)
            .Where(l => l.Status == LoanStatus.Active && l.ReturnedAt == null && l.DueDate < today)
            .OrderBy(l => l.DueDate)
            .ToListAsync(ct);
    }

    // FR-13: dipakai halaman detail anggota.
    public async Task<List<Loan>> GetActiveByMemberAsync(int memberId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        return await BaseQuery(db)
            .Where(l => l.MemberId == memberId && l.Status == LoanStatus.Active)
            .OrderBy(l => l.DueDate)
            .ToListAsync(ct);
    }

    private static IQueryable<Loan> BaseQuery(AppDbContext db) =>
        db.Loans
            .AsNoTracking()
            .Include(l => l.Member)
            .Include(l => l.BookCopy)
            .ThenInclude(c => c.Book);
}
