using LibraryAppPrototype.Data;
using LibraryAppPrototype.Data.Entities;
using LibraryAppPrototype.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryAppPrototype.Services;

public class BookService(IDbContextFactory<AppDbContext> dbFactory, IClock clock)
{
    private const string InventoryPrefix = "INV-";

    // FR-06 (cari judul/ISBN/penulis) + FR-07 (filter kategori & ketersediaan), dengan paging.
    public async Task<PagedList<Book>> SearchAsync(
        string? keyword = null,
        int? categoryId = null,
        bool onlyAvailable = false,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var query = db.Books
            .AsNoTracking()
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Include(b => b.Copies)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();

            // Kalau input-nya ISBN yang valid, cocokkan ke bentuk ternormalisasinya (BR-13, AC-20).
            var isbn = IsbnHelper.TryNormalize(k, out var normalized) ? normalized : null;

            query = query.Where(b =>
                b.Title.Contains(k) ||
                b.Author.Name.Contains(k) ||
                b.Isbn.Contains(k) ||
                (isbn != null && b.Isbn == isbn));
        }

        if (categoryId is > 0)
            query = query.Where(b => b.CategoryId == categoryId);

        if (onlyAvailable)
            query = query.Where(b => b.Copies.Any(c => c.Status == BookCopyStatus.Available));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(b => b.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedList<Book> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    // FR-08: detail buku beserta daftar kopi.
    public async Task<Book?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Books
            .AsNoTracking()
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Include(b => b.Copies)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    // FR-01. Menegakkan BR-13 (ISBN valid & ternormalisasi) dan BR-14 (kode inventaris unik).
    public async Task<OperationResult<int>> CreateAsync(
        Book book, int initialCopyCount, CancellationToken ct = default)
    {
        // BR-13
        if (!IsbnHelper.TryNormalize(book.Isbn, out var isbn))
            return OperationResult<int>.Fail("BR-13", "ISBN tidak valid. Masukkan ISBN-10 atau ISBN-13 yang benar.");

        if (initialCopyCount < 0)
            return OperationResult<int>.Fail("VALIDATION", "Jumlah kopi awal tidak boleh negatif.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // BR-13: unique index tidak boleh bocor — "978-0-306-40615-7" dan "9780306406157" itu buku yang sama (AC-20).
        if (await db.Books.AnyAsync(b => b.Isbn == isbn, ct))
            return OperationResult<int>.Fail("BR-13", $"Buku dengan ISBN {isbn} sudah terdaftar.");

        if (!await db.Authors.AnyAsync(a => a.Id == book.AuthorId, ct))
            return OperationResult<int>.Fail("NOT_FOUND", "Penulis tidak ditemukan.");

        if (!await db.Categories.AnyAsync(c => c.Id == book.CategoryId, ct))
            return OperationResult<int>.Fail("NOT_FOUND", "Kategori tidak ditemukan.");

        book.Isbn = isbn;
        book.CreatedAt = clock.UtcNow;
        book.Copies.Clear();

        var nextNumber = await NextInventoryNumberAsync(db, ct);
        for (var i = 0; i < initialCopyCount; i++)
        {
            book.Copies.Add(new BookCopy
            {
                InventoryCode = $"{InventoryPrefix}{nextNumber++:00000}", // BR-14
                Status = BookCopyStatus.Available,
                AcquiredAt = clock.Today
            });
        }

        db.Books.Add(book);
        await db.SaveChangesAsync(ct);

        return OperationResult<int>.Ok(book.Id);
    }

    // FR-02. BR-13 berlaku lagi karena ISBN boleh diubah.
    public async Task<OperationResult> UpdateAsync(Book book, CancellationToken ct = default)
    {
        // BR-13
        if (!IsbnHelper.TryNormalize(book.Isbn, out var isbn))
            return OperationResult.Fail("BR-13", "ISBN tidak valid. Masukkan ISBN-10 atau ISBN-13 yang benar.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var existing = await db.Books.FirstOrDefaultAsync(b => b.Id == book.Id, ct);
        if (existing is null)
            return OperationResult.Fail("NOT_FOUND", "Buku tidak ditemukan.");

        if (await db.Books.AnyAsync(b => b.Isbn == isbn && b.Id != book.Id, ct))
            return OperationResult.Fail("BR-13", $"Buku lain dengan ISBN {isbn} sudah terdaftar.");

        if (!await db.Authors.AnyAsync(a => a.Id == book.AuthorId, ct))
            return OperationResult.Fail("NOT_FOUND", "Penulis tidak ditemukan.");

        if (!await db.Categories.AnyAsync(c => c.Id == book.CategoryId, ct))
            return OperationResult.Fail("NOT_FOUND", "Kategori tidak ditemukan.");

        // CreatedAt tidak ikut diubah — form tidak boleh menentukannya.
        existing.Title = book.Title;
        existing.Isbn = isbn;
        existing.AuthorId = book.AuthorId;
        existing.CategoryId = book.CategoryId;
        existing.Publisher = book.Publisher;
        existing.PublishedYear = book.PublishedYear;
        existing.Description = book.Description;
        existing.ReplacementCost = book.ReplacementCost;

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    // FR-03 / BR-17.
    public async Task<OperationResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var book = await db.Books.Include(b => b.Copies).FirstOrDefaultAsync(b => b.Id == id, ct);
        if (book is null)
            return OperationResult.Fail("NOT_FOUND", "Buku tidak ditemukan.");

        var copyIds = book.Copies.Select(c => c.Id).ToList();

        // BR-17
        if (await db.Loans.AnyAsync(l => copyIds.Contains(l.BookCopyId) && l.Status == LoanStatus.Active, ct))
            return OperationResult.Fail("BR-17", "Buku tidak bisa dihapus karena masih ada pinjaman aktif.");

        // Loans -> BookCopies dipasang ON DELETE NO ACTION, jadi riwayat pinjaman lama
        // akan membuat DELETE gagal di database. Dicek di sini supaya pesannya ramah.
        if (await db.Loans.AnyAsync(l => copyIds.Contains(l.BookCopyId), ct))
            return OperationResult.Fail("CONFLICT",
                "Buku tidak bisa dihapus karena masih punya riwayat peminjaman. Tarik (Retire) kopinya saja.");

        db.Books.Remove(book); // BookCopies ikut terhapus (cascade)
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    // FR-04 / BR-14.
    public async Task<OperationResult<BookCopy>> AddCopyAsync(
        int bookId, string? inventoryCode = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        if (!await db.Books.AnyAsync(b => b.Id == bookId, ct))
            return OperationResult<BookCopy>.Fail("NOT_FOUND", "Buku tidak ditemukan.");

        var code = inventoryCode?.Trim();
        if (string.IsNullOrEmpty(code))
            code = $"{InventoryPrefix}{await NextInventoryNumberAsync(db, ct):00000}";

        // BR-14: unik GLOBAL, bukan per buku (AC-13).
        if (await db.BookCopies.AnyAsync(c => c.InventoryCode == code, ct))
            return OperationResult<BookCopy>.Fail("BR-14", $"Kode inventaris \"{code}\" sudah dipakai kopi lain.");

        var copy = new BookCopy
        {
            BookId = bookId,
            InventoryCode = code,
            Status = BookCopyStatus.Available,
            AcquiredAt = clock.Today
        };

        db.BookCopies.Add(copy);
        await db.SaveChangesAsync(ct);

        return OperationResult<BookCopy>.Ok(copy);
    }

    // BR-21: kopi yang sedang OnLoan tidak boleh dihapus.
    public async Task<OperationResult> RemoveCopyAsync(int copyId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var copy = await db.BookCopies.FirstOrDefaultAsync(c => c.Id == copyId, ct);
        if (copy is null)
            return OperationResult.Fail("NOT_FOUND", "Kopi tidak ditemukan.");

        // BR-21
        if (copy.Status == BookCopyStatus.OnLoan)
            return OperationResult.Fail("BR-21", "Kopi sedang dipinjam, tidak bisa dihapus.");

        if (await db.Loans.AnyAsync(l => l.BookCopyId == copyId, ct))
            return OperationResult.Fail("CONFLICT",
                "Kopi tidak bisa dihapus karena punya riwayat peminjaman. Ubah statusnya jadi Retired saja.");

        db.BookCopies.Remove(copy);
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    // FR-05. Menegakkan BR-21 dan menerbitkan denda sesuai BR-23.
    public async Task<OperationResult> ChangeCopyStatusAsync(
        int copyId, BookCopyStatus newStatus, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var copy = await db.BookCopies.Include(c => c.Book).FirstOrDefaultAsync(c => c.Id == copyId, ct);
        if (copy is null)
            return OperationResult.Fail("NOT_FOUND", "Kopi tidak ditemukan.");

        if (copy.Status == newStatus)
            return OperationResult.Ok();

        // BR-21: OnLoan tidak boleh di-Retire atau dikembalikan ke Available secara manual —
        // jalur yang benar adalah LoanService.ReturnAsync (AC-17).
        if (copy.Status == BookCopyStatus.OnLoan &&
            newStatus is BookCopyStatus.Available or BookCopyStatus.Retired)
        {
            return OperationResult.Fail("BR-21",
                "Kopi sedang dipinjam. Proses pengembaliannya dulu lewat menu Peminjaman.");
        }

        // Status OnLoan hanya boleh diberikan oleh LoanService.BorrowAsync.
        if (newStatus == BookCopyStatus.OnLoan)
            return OperationResult.Fail("BR-21", "Status OnLoan hanya bisa diberikan lewat proses peminjaman.");

        var today = clock.Today;
        var wasOnLoan = copy.Status == BookCopyStatus.OnLoan;

        // BR-23: kopi yang hilang/rusak menerbitkan denda — tapi hanya kalau ada yang bisa ditagih,
        // yaitu saat kopi sedang dipinjam. Kopi rusak di rak tidak punya peminjam.
        if (wasOnLoan && newStatus is BookCopyStatus.Lost or BookCopyStatus.Damaged)
        {
            var loan = await db.Loans.FirstOrDefaultAsync(
                l => l.BookCopyId == copyId && l.Status == LoanStatus.Active, ct);

            if (loan is not null)
            {
                var amount = newStatus == BookCopyStatus.Lost
                    ? copy.Book.ReplacementCost                                  // BR-23
                    : copy.Book.ReplacementCost * LoanPolicy.DamagedBookFineRatio; // BR-23

                if (newStatus == BookCopyStatus.Lost)
                {
                    loan.Status = LoanStatus.Lost;
                }
                else
                {
                    // Buku fisiknya kembali, cuma dalam keadaan rusak — pinjamannya selesai.
                    loan.Status = LoanStatus.Returned;
                    loan.ReturnedAt = today;
                }

                db.Fines.Add(new Fine
                {
                    LoanId = loan.Id,
                    MemberId = loan.MemberId,
                    Amount = amount,
                    Reason = newStatus == BookCopyStatus.Lost ? FineReason.LostBook : FineReason.DamagedBook,
                    IssuedAt = today,
                    Status = FineStatus.Unpaid
                });
            }
        }

        copy.Status = newStatus;

        await db.SaveChangesAsync(ct); // satu transaksi: kopi + pinjaman + denda
        return OperationResult.Ok();
    }

    // Kode inventaris berurutan: INV-00001, INV-00002, ... Lebar 5 digit, jadi urutan
    // string sama dengan urutan angka selama nomornya belum tembus 99999.
    private static async Task<int> NextInventoryNumberAsync(AppDbContext db, CancellationToken ct)
    {
        var last = await db.BookCopies
            .Where(c => c.InventoryCode.StartsWith(InventoryPrefix))
            .OrderByDescending(c => c.InventoryCode)
            .Select(c => c.InventoryCode)
            .FirstOrDefaultAsync(ct);

        if (last is null) return 1;

        return int.TryParse(last[InventoryPrefix.Length..], out var n) ? n + 1 : 1;
    }
}
