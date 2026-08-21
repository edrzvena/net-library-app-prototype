using LibraryAppPrototype.Data.Entities;
using LibraryAppPrototype.Services;
using Microsoft.EntityFrameworkCore;

namespace LibraryAppPrototype.Data;

// Data awal untuk Development saja. Dijalankan hanya kalau tabel masih kosong.
//
// SEMUA tanggal relatif terhadap clock.Today — jangan pernah hardcode.
// Kalau di-hardcode, skenario AC-07 ("telat 3 hari") akan meleset begitu aplikasi
// dijalankan di hari yang berbeda.
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IClock clock, CancellationToken ct = default)
    {
        if (await db.Books.AnyAsync(ct)) return;

        var today = clock.Today;
        var now = clock.UtcNow;

        // --- 5 Categories ---------------------------------------------------
        var categories = new List<Category>
        {
            new() { Name = "Fiksi", Description = "Novel, cerpen, dan karya rekaan lainnya." },
            new() { Name = "Non-Fiksi", Description = "Biografi, esai, dan tulisan berbasis fakta." },
            new() { Name = "Teknologi", Description = "Pemrograman, rekayasa, dan sains terapan." },
            new() { Name = "Sejarah", Description = "Sejarah Indonesia dan dunia." },
            new() { Name = "Anak", Description = "Bacaan untuk pembaca usia dini." }
        };
        db.Categories.AddRange(categories);

        // --- 8 Authors ------------------------------------------------------
        var authors = new List<Author>
        {
            new() { Name = "Pramoedya Ananta Toer", Biography = "Sastrawan Indonesia, penulis Tetralogi Buru." },
            new() { Name = "Andrea Hirata", Biography = "Penulis Laskar Pelangi." },
            new() { Name = "Dee Lestari", Biography = "Penulis serial Supernova." },
            new() { Name = "Martin Fowler", Biography = "Penulis buku-buku rekayasa perangkat lunak." },
            new() { Name = "Robert C. Martin", Biography = "Dikenal sebagai Uncle Bob." },
            new() { Name = "Yuval Noah Harari", Biography = "Sejarawan, penulis Sapiens." },
            new() { Name = "Tere Liye", Biography = "Penulis produktif fiksi populer Indonesia." },
            new() { Name = "Raditya Dika", Biography = "Penulis komedi dan pembuat konten." }
        };
        db.Authors.AddRange(authors);

        // Categories & Authors perlu Id sebelum dipakai Books.
        await db.SaveChangesAsync(ct);

        // --- 15 Books, masing-masing 2-3 BookCopy ---------------------------
        // ISBN sudah dalam bentuk ternormalisasi 13 digit dengan checksum valid (BR-13).
        var bookSeeds = new (string Isbn, string Title, int AuthorIdx, int CategoryIdx, string Publisher, int Year, decimal Cost, int Copies)[]
        {
            ("9786023110001", "Bumi Manusia",              0, 0, "Hasta Mitra",     1980, 145_000m, 3),
            ("9786023110070", "Anak Semua Bangsa",         0, 0, "Hasta Mitra",     1980, 140_000m, 2),
            ("9786023110148", "Jejak Langkah",             0, 0, "Hasta Mitra",     1985, 150_000m, 2),
            ("9786023110216", "Laskar Pelangi",            1, 0, "Bentang Pustaka", 2005,  98_000m, 3),
            ("9786023110285", "Sang Pemimpi",              1, 0, "Bentang Pustaka", 2006,  95_000m, 2),
            ("9786023110353", "Supernova: Ksatria",        2, 0, "Bentang Pustaka", 2001, 110_000m, 2),
            ("9786023110421", "Filosofi Kopi",             2, 0, "Truedee Books",   2006,  89_000m, 3),
            ("9786023110490", "Refactoring",               3, 2, "Addison-Wesley",  2018, 750_000m, 2),
            ("9786023110568", "Patterns of Enterprise App", 3, 2, "Addison-Wesley",  2002, 690_000m, 2),
            ("9786023110636", "Clean Code",                4, 2, "Prentice Hall",   2008, 620_000m, 3),
            ("9786023110704", "Clean Architecture",        4, 2, "Prentice Hall",   2017, 640_000m, 2),
            ("9786023110773", "Sapiens",                   5, 3, "Harper",          2011, 320_000m, 3),
            ("9786023110841", "Homo Deus",                 5, 3, "Harper",          2015, 335_000m, 2),
            ("9786023110919", "Bumi",                      6, 4, "Gramedia",        2014,  85_000m, 3),
            ("9786023110988", "Kambing Jantan",            7, 1, "Gagas Media",     2005,  75_000m, 2)
        };

        var books = new List<Book>();
        var copyNumber = 1;

        foreach (var s in bookSeeds)
        {
            var book = new Book
            {
                Title = s.Title,
                Isbn = s.Isbn,
                AuthorId = authors[s.AuthorIdx].Id,
                CategoryId = categories[s.CategoryIdx].Id,
                Publisher = s.Publisher,
                PublishedYear = s.Year,
                ReplacementCost = s.Cost,
                CreatedAt = now
            };

            for (var i = 0; i < s.Copies; i++)
            {
                book.Copies.Add(new BookCopy
                {
                    // BR-14: unik global, bukan per buku.
                    InventoryCode = $"INV-{copyNumber++:00000}",
                    Status = BookCopyStatus.Available,
                    AcquiredAt = today.AddDays(-365 - copyNumber)
                });
            }

            books.Add(book);
        }

        db.Books.AddRange(books);

        // --- 6 Members (5 Active, 1 Suspended) ------------------------------
        // BR-15: Code diisi kode, bukan user. Di seeder pun formatnya harus sama.
        var year = today.Year;
        var members = new List<Member>
        {
            new() { Code = $"MBR-{year}-00001", FullName = "Budi Santoso",   Email = "budi.santoso@example.com",   PhoneNumber = "081234567001", Address = "Jl. Merdeka No. 1, Jakarta",  JoinedAt = today.AddDays(-400), Status = MemberStatus.Active },
            new() { Code = $"MBR-{year}-00002", FullName = "Siti Rahayu",    Email = "siti.rahayu@example.com",    PhoneNumber = "081234567002", Address = "Jl. Diponegoro No. 12, Bandung", JoinedAt = today.AddDays(-320), Status = MemberStatus.Active },
            new() { Code = $"MBR-{year}-00003", FullName = "Agus Prasetyo",  Email = "agus.prasetyo@example.com",  PhoneNumber = "081234567003", Address = "Jl. Pahlawan No. 8, Surabaya", JoinedAt = today.AddDays(-250), Status = MemberStatus.Active },
            new() { Code = $"MBR-{year}-00004", FullName = "Dewi Lestari",   Email = "dewi.lestari@example.com",   PhoneNumber = "081234567004", Address = "Jl. Sudirman No. 45, Semarang", JoinedAt = today.AddDays(-180), Status = MemberStatus.Active },
            new() { Code = $"MBR-{year}-00005", FullName = "Eko Wijaya",     Email = "eko.wijaya@example.com",     PhoneNumber = "081234567005", Address = "Jl. Gajah Mada No. 3, Medan", JoinedAt = today.AddDays(-90),  Status = MemberStatus.Active },
            new() { Code = $"MBR-{year}-00006", FullName = "Fitri Handayani", Email = "fitri.handayani@example.com", PhoneNumber = "081234567006", Address = "Jl. Ahmad Yani No. 20, Makassar", JoinedAt = today.AddDays(-60), Status = MemberStatus.Suspended }
        };
        db.Members.AddRange(members);

        // Books, BookCopies, dan Members perlu Id sebelum dipakai Loans.
        await db.SaveChangesAsync(ct);

        // --- 8 Loans --------------------------------------------------------
        // 4 aktif belum jatuh tempo, 2 aktif sudah lewat jatuh tempo, 2 sudah dikembalikan.
        var copies = books.SelectMany(b => b.Copies).OrderBy(c => c.Id).ToList();
        var loans = new List<Loan>();

        // 4 aktif, belum jatuh tempo (dipinjam 1-4 hari lalu, tenggat masih di depan).
        for (var i = 0; i < 4; i++)
        {
            var borrowedAt = today.AddDays(-(i + 1));
            loans.Add(new Loan
            {
                MemberId = members[i].Id,
                BookCopyId = copies[i].Id,
                BorrowedAt = borrowedAt,
                DueDate = borrowedAt.AddDays(LoanPolicy.LoanDurationDays), // BR-02
                Status = LoanStatus.Active
            });
            copies[i].Status = BookCopyStatus.OnLoan; // BR-05
        }

        // 2 aktif sudah lewat jatuh tempo — telat 3 hari dan 10 hari (BR-19: turunan, bukan kolom).
        var overdueDays = new[] { 3, 10 };
        for (var i = 0; i < overdueDays.Length; i++)
        {
            var borrowedAt = today.AddDays(-(LoanPolicy.LoanDurationDays + overdueDays[i]));
            var copy = copies[4 + i];
            loans.Add(new Loan
            {
                MemberId = members[i].Id,
                BookCopyId = copy.Id,
                BorrowedAt = borrowedAt,
                DueDate = borrowedAt.AddDays(LoanPolicy.LoanDurationDays),
                Status = LoanStatus.Active
            });
            copy.Status = BookCopyStatus.OnLoan;
        }

        // 2 sudah dikembalikan — keduanya telat, jadi menghasilkan denda yang belum lunas.
        // Kopinya kembali Available (BR-11).
        var returnedLateDays = new[] { 4, 6 };
        for (var i = 0; i < returnedLateDays.Length; i++)
        {
            var borrowedAt = today.AddDays(-(LoanPolicy.LoanDurationDays + returnedLateDays[i] + 2));
            var dueDate = borrowedAt.AddDays(LoanPolicy.LoanDurationDays);
            loans.Add(new Loan
            {
                MemberId = members[2 + i].Id,
                BookCopyId = copies[6 + i].Id,
                BorrowedAt = borrowedAt,
                DueDate = dueDate,
                ReturnedAt = dueDate.AddDays(returnedLateDays[i]),
                Status = LoanStatus.Returned
            });
        }

        db.Loans.AddRange(loans);
        await db.SaveChangesAsync(ct);

        // --- 2 Fines belum lunas (supaya BR-03 bisa langsung dicoba) --------
        var returnedLoans = loans.Where(l => l.Status == LoanStatus.Returned).ToList();
        var fines = new List<Fine>();

        for (var i = 0; i < returnedLoans.Count; i++)
        {
            var loan = returnedLoans[i];
            var daysLate = loan.DaysLate(today);
            fines.Add(new Fine
            {
                LoanId = loan.Id,
                MemberId = loan.MemberId,
                Amount = daysLate * LoanPolicy.FinePerLateDay, // BR-06
                Reason = FineReason.LateReturn,
                IssuedAt = loan.ReturnedAt!.Value,
                Status = FineStatus.Unpaid
            });
        }

        db.Fines.AddRange(fines);
        await db.SaveChangesAsync(ct);
    }
}
