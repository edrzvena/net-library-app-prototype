# PRD — Pustaka Prototype: Library Management System (Single Project)

> **Dokumen ini adalah sumber kebenaran (single source of truth) untuk proyek `LibraryAppPrototype`.**
> Setiap AI agent / developer yang masuk ke repo ini WAJIB membaca dokumen ini sebelum menulis kode.
> Jika ada konflik antara kode dan dokumen ini, tanyakan ke pemilik proyek — jangan diam-diam mengubah aturan bisnis.

| Field | Nilai |
|---|---|
| Nama Produk | **Pustaka Prototype** — Sistem Manajemen Peminjaman Perpustakaan |
| Project | `LibraryAppPrototype.csproj` — **satu project, tanpa layering** |
| Root Namespace | `LibraryAppPrototype.*` |
| Versi Dokumen | 1.0 |
| Tanggal | 2026-08-21 |
| Tujuan Utama | **Media pembelajaran Blazor Server + EF Core** — arsitektur sengaja dibuat sesederhana mungkin |
| Status | Greenfield — belum dibuat, mulai dari `dotnet new blazor` |
| Dokumen saudara | [`PRD.md`](PRD.md) — domain yang **sama persis**, tapi dikerjakan dengan Clean Architecture (4 project) |

> **Kenapa ada dua dokumen?**
> `PRD.md` dan `PRD-simple.md` membangun **produk yang sama, dengan aturan bisnis yang sama** (BR-01 s/d BR-23
> bernomor identik di kedua dokumen), tapi dengan arsitektur yang berlawanan. Tujuannya supaya perbedaan
> Clean Architecture vs pendekatan sederhana bisa dilihat langsung, bukan cuma dibaca di teori.
> Perbandingan lengkapnya ada di **bagian 16**.

---

## 1. Latar Belakang & Tujuan

### 1.1 Kenapa versi "tanpa Clean Architecture" ini perlu ada?

Clean Architecture punya biaya masuk yang mahal: sebelum satu layar pun muncul, kamu sudah harus paham
Entity, Value Object, Aggregate, DTO, Repository, Unit of Work, dan Dependency Inversion. Buat orang yang
**baru mau belajar Blazor dan EF Core**, biaya itu justru mengaburkan yang mau dipelajari.

Proyek ini membalik prioritasnya:

- **Cepat kelihatan hasilnya.** Halaman pertama yang menampilkan data dari database bisa jadi di hari pertama.
- **Sedikit tempat untuk nyasar.** Satu project, tiga folder utama: `Data/`, `Services/`, `Components/`.
- **Fokus ke skill yang konkret.** Render mode Blazor, `EditForm` + validasi, EF Core query & migration,
  lifetime `DbContext` — bukan ke penataan folder.

Yang **tidak** kita korbankan: aturan bisnisnya tetap kaya. Domain perpustakaan dipilih karena punya aturan
yang bisa **menolak** sebuah aksi (anggota kena limit, denda tertunggak, kopi habis, denda telat, syarat
perpanjangan). Bedanya, di sini aturan itu tinggal di **service**, bukan di entity.

### 1.2 Tujuan Pembelajaran (Learning Objectives)

| ID | Tujuan | Terbukti lewat |
|---|---|---|
| LO-01 | Memahami **render mode** Blazor (Static SSR vs Interactive Server) | `Routes.razor` + `@rendermode` per halaman |
| LO-02 | Memahami **lifetime `DbContext` di Blazor Server** dan kenapa `AddDbContextFactory` wajib | bagian 11.1 — ini jebakan paling sering di Blazor |
| LO-03 | Bisa menulis **EF Core Code-First** + migration + relasi 1-ke-banyak | `AppDbContext` + `InitialCreate` |
| LO-04 | Bisa menulis **query LINQ** dengan filter, paging, `Include`, dan projection | `BookService.SearchAsync` |
| LO-05 | Bisa bikin **form + validasi** dengan `EditForm` + DataAnnotations | `BookForm.razor`, `MemberForm.razor` |
| LO-06 | Bisa bikin **komponen reusable** dengan `[Parameter]` dan `RenderFragment` | folder `Components/Shared/` |
| LO-07 | Memahami **service layer sederhana**: kenapa aturan bisnis jangan ditulis di `.razor` | folder `Services/` |
| LO-08 | Bisa **membedakan** kapan pendekatan ini cukup dan kapan mulai jadi beban | bagian 17 |

### 1.3 Non-Goals (di luar cakupan)

- Clean Architecture / DDD / Repository pattern / Unit of Work — **itu ada di `PRD.md`, bukan di sini**
- Autentikasi/otorisasi multi-role (opsional Fase 5)
- Multi-tenant, notifikasi email/SMS, upload sampul buku, reservasi/antrian
- Public API (REST/GraphQL) — UI langsung memanggil service
- Deployment ke cloud, containerization, CI/CD
- Unit test yang lengkap (opsional Fase 5 — dan bagian 17 menjelaskan kenapa di arsitektur ini testnya lebih ribet)

---

## 2. Tech Stack

| Kategori | Teknologi | Versi | Catatan |
|---|---|---|---|
| Runtime | .NET | **10.0** | `net10.0` |
| Bahasa | C# | 14 | `Nullable=enable`, `ImplicitUsings=enable` |
| UI Framework | **Blazor Web App** | .NET 10 | Interactivity = **Server** |
| ORM | **Entity Framework Core** | 10.x | Code-First + Migrations |
| Database | **SQL Server Express** | 2022 | Instance lokal `.\SQLEXPRESS` |
| CSS Framework | **Bootstrap** | 5.3.x | Bawaan template, lokal di `wwwroot/lib/bootstrap` — JANGAN pakai CDN |
| Ikon | Bootstrap Icons | 1.11.x | **Tidak ikut template**, wajib dipasang lokal (bagian 2.4) |
| CSS Scoping | Blazor CSS Isolation | built-in | `*.razor.css` |
| Validasi | DataAnnotations + `EditForm` | built-in | atribut ditulis langsung di entity |
| Testing | xUnit | opsional | Fase 5 |
| IDE | JetBrains Rider | — | |

### 2.1 Membuat project

Template yang dipakai namanya **Blazor Web App** (short name `blazor`). Template lama `blazorserver`
sudah dihapus sejak .NET 9 — sekarang "Blazor Server" adalah **pilihan render mode**, bukan template:

```bash
# dijalankan dari folder .../blazorserver/
dotnet new blazor -n LibraryAppPrototype -int Server

# opsional, kalau mau semua halaman interaktif tanpa perlu @rendermode satu-satu:
# dotnet new blazor -n LibraryAppPrototype -int Server -ai
```

Nilai `-int` (`--interactivity`) yang tersedia: `None` | `Server` | `WebAssembly` | `Auto`.
Proyek ini **wajib `Server`**.

> Project ini berdiri sendiri di folder `LibraryAppPrototype/`, **sejajar** dengan folder `LibraryApp/`
> (proyek Clean Architecture). Dua-duanya tidak saling mereferensikan. Tidak perlu file `.slnx` —
> satu project cukup dibuka langsung.

### 2.2 NuGet Packages yang perlu ditambahkan

```
Microsoft.EntityFrameworkCore.SqlServer      10.x
Microsoft.EntityFrameworkCore.Design         10.x
```

Cuma dua. Tidak ada AutoMapper, tidak ada MediatR, tidak ada FluentValidation — semuanya ditulis manual
supaya kelihatan apa yang sebenarnya terjadi.

### 2.3 Connection String & Migration

`appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=db_library_prototype;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Encrypt=False"
  }
}
```

> Nama database **sengaja dibedakan** (`db_library_prototype`) dari proyek Clean Architecture
> (`db_library`), supaya dua proyek bisa jalan berdampingan tanpa saling menimpa skema.

```bash
# sekali saja per mesin
dotnet tool install --global dotnet-ef

# dijalankan dari dalam folder LibraryAppPrototype/ — cukup 1 project, tanpa --startup-project
dotnet ef migrations add InitialCreate --output-dir Data/Migrations
dotnet ef database update
```

> Perhatikan betapa pendeknya perintah ini dibanding versi Clean Architecture yang butuh
> `--project` + `--startup-project`. Ini salah satu keuntungan nyata single project.

### 2.4 Bootstrap Icons (wajib dipasang lokal)

Template `blazor` cuma membawa `wwwroot/lib/bootstrap`. Bootstrap Icons tidak ikut, padahal dipakai di
`NavMenu` dan tombol-tombol aksi. Karena CDN dilarang (aturan 14.2), pasang lokal:

```bash
# dijalankan dari dalam folder LibraryAppPrototype/
dotnet tool install --global Microsoft.Web.LibraryManager.Cli   # sekali saja
libman install bootstrap-icons@1.11.3 --provider unpkg --destination wwwroot/lib/bootstrap-icons --files font/bootstrap-icons.css --files font/fonts/bootstrap-icons.woff --files font/fonts/bootstrap-icons.woff2
```

Daftarkan di `Components/App.razor`, tepat di bawah baris Bootstrap yang sudah ada:

```razor
<link rel="stylesheet" href="@Assets["lib/bootstrap-icons/font/bootstrap-icons.css"]" />
```

---

## 3. Arsitektur: Tiga Folder, Satu Project

### 3.1 Diagram

```
        +----------------------------------------------+
        |            Components/  (Razor)              |
        |  Halaman, layout, komponen reusable          |
        |  Boleh: panggil Service, tampilkan Entity    |
        |  Dilarang: aturan bisnis, query LINQ ke DB   |
        +----------------------+-----------------------+
                               | inject
                               v
        +----------------------------------------------+
        |               Services/                      |
        |  SEMUA aturan bisnis ada di sini (BR-01..23) |
        |  Bikin DbContext per operasi lewat factory   |
        |  Kembalikan OperationResult, bukan exception |
        +----------------------+-----------------------+
                               | pakai
                               v
        +----------------------------------------------+
        |                 Data/                        |
        |  AppDbContext, Entities (POCO), Enums, Seeder|
        |  Entity = bag of properties + DataAnnotations|
        |  TIDAK ada logika bisnis di entity           |
        +----------------------------------------------+
```

Arahnya satu arah ke bawah: `Components/` → `Services/` → `Data/`.
Tidak ada interface, tidak ada dependency inversion. Kalau `LoanService` butuh `AppDbContext`, dia
memakainya langsung — tidak ada `ILoanRepository` di tengah.

### 3.2 `Data/` — Model & Akses Database

**Buat apa?** Menggambarkan bentuk data dan cara menyimpannya. Tidak lebih.

**Boleh berisi:**
- Entity POCO: property + navigasi + DataAnnotations (`[Required]`, `[MaxLength]`, `[Precision]`)
- `AppDbContext` dengan `DbSet<>` dan `OnModelCreating` untuk index & relasi
- Enum
- `DbSeeder`
- Migrations (auto-generated)

**DILARANG berisi:**
- Aturan bisnis. Tidak ada `if (activeLoans >= 3)` di dalam entity maupun `AppDbContext`.
- Referensi ke Razor component / `NavigationManager`

**Contoh entity (memang anemic — dan itu disengaja):**

```csharp
// Data/Entities/Loan.cs
public class Loan
{
    public int Id { get; set; }

    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public int BookCopyId { get; set; }
    public BookCopy BookCopy { get; set; } = null!;

    public DateOnly BorrowedAt { get; set; }
    public DateOnly DueDate    { get; set; }
    public DateOnly? ReturnedAt { get; set; }
    public int RenewalCount    { get; set; }
    public LoanStatus Status   { get; set; }

    // Satu-satunya "logika" yang boleh nempel di entity: perhitungan turunan
    // yang murni baca property sendiri, tanpa akses database. Lihat BR-19.
    public bool IsOverdue(DateOnly today) => ReturnedAt is null
                                          && Status == LoanStatus.Active
                                          && DueDate < today;

    public int DaysLate(DateOnly today) =>
        Math.Max(0, (ReturnedAt ?? today).DayNumber - DueDate.DayNumber);
}
```

> **Ini Anemic Domain Model, dan di proyek ini itu keputusan yang sah.** `PRD.md` melarangnya karena
> di sana Domain-lah yang jadi bintang utama. Di sini yang mau dipelajari Blazor + EF Core, jadi entity
> sengaja dibiarkan tipis. Yang **tidak boleh** adalah menganggap ini satu-satunya cara yang benar —
> bagian 17 menjelaskan kapan pendekatan ini mulai jadi beban.

### 3.3 `Services/` — Tempat Semua Aturan Bisnis

**Buat apa?** Satu class per modul, berisi seluruh operasi modul itu. Inilah satu-satunya tempat aturan
bisnis boleh ditulis.

**Boleh berisi:**
- Query EF Core (`Include`, `Where`, `Skip/Take`, projection)
- Penegakan aturan bisnis BR-01 s/d BR-23
- Pembuatan & mutasi entity
- `SaveChangesAsync()`

**DILARANG berisi:**
- Referensi ke Razor component / `NavigationManager` / `IJSRuntime`
- Melempar exception untuk kasus bisnis yang normal (misal "kuota habis") — pakai `OperationResult`.
  Exception hanya untuk kondisi yang memang tidak diharapkan (data korup, bug).

**Pola service standar (dipakai konsisten di seluruh proyek):**

```csharp
public class LoanService(IDbContextFactory<AppDbContext> dbFactory, IClock clock)
{
    public async Task<OperationResult<Loan>> BorrowAsync(
        int memberId, int bookId, CancellationToken ct = default)
    {
        // 1. Satu DbContext untuk satu operasi — WAJIB, lihat bagian 11.1
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
            MemberId   = memberId,
            BookCopyId = copy.Id,
            BorrowedAt = today,
            DueDate    = today.AddDays(LoanPolicy.LoanDurationDays),   // BR-02
            Status     = LoanStatus.Active
        };
        db.Loans.Add(loan);

        await db.SaveChangesAsync(ct);   // satu transaksi: kopi + pinjaman
        return OperationResult<Loan>.Ok(loan);
    }
}
```

> Bandingkan blok ini dengan `PRD.md` bagian 3.3. Di sana pengecekan BR-01/03/04 ada di
> `Member.EnsureCanBorrow()` dan BR-05 di `Book.TakeAvailableCopy()`. Di sini semuanya di satu method.
> Lebih gampang dibaca sekarang; lebih susah dites dan lebih gampang terduplikasi nanti.

### 3.4 `Models/` — Bentuk Data yang Bukan Entity

**Buat apa?** Cuma untuk data yang **tidak punya tabel**. Ini bukan DTO — proyek ini sengaja **tidak**
memakai DTO, entity dipakai langsung di Razor.

Isinya hanya 4 file: `PagedList<T>`, `LoanFilter`, `DashboardSummary`, `ReturnSummary`.

### 3.5 `Components/` — Presentation

**Buat apa?** Menampilkan data dan menerima input. Setipis mungkin.

**Boleh berisi:**
- Razor Components (`.razor`) + CSS isolation (`.razor.css`)
- Layout & navigasi, komponen reusable
- Memanggil service dan menampilkan `OperationResult.Error`

**DILARANG berisi:**
- `AppDbContext` atau `IDbContextFactory` — komponen **tidak boleh** menyentuh database langsung
- Query LINQ ke database
- Perhitungan denda, cek batas pinjam, atau aturan bisnis apapun

**Aturan injeksi di component:**

```razor
@inject LoanService Loans        @* BENAR — inject service *@
@inject AppDbContext Db          @* DILARANG KERAS *@
@inject IDbContextFactory<AppDbContext> F   @* DILARANG KERAS *@
```

> Ini satu-satunya aturan arsitektur yang **tidak boleh dilanggar** di proyek ini, walaupun kita
> "tidak pakai Clean Architecture". Begitu ada `@inject AppDbContext` di `.razor`, aturan bisnis akan
> pelan-pelan pindah ke UI, dan proyek jadi tidak bisa diselamatkan.

---

## 4. Struktur Folder Lengkap

```
LibraryAppPrototype/
├── PRD-simple.md                             <- dokumen ini (boleh disalin ke folder project)
├── LibraryAppPrototype.csproj
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
│
├── Data/
│   ├── AppDbContext.cs                       (DbSet + OnModelCreating: index, relasi, precision)
│   ├── DbSeeder.cs                           (data awal, hanya Development)
│   ├── Enums.cs                              (5 enum dalam 1 file — lihat catatan di bawah)
│   ├── Entities/
│   │   ├── Author.cs
│   │   ├── Category.cs
│   │   ├── Book.cs
│   │   ├── BookCopy.cs
│   │   ├── Member.cs
│   │   ├── Loan.cs
│   │   └── Fine.cs
│   └── Migrations/                           (auto-generated)
│
├── Services/
│   ├── OperationResult.cs                    (OperationResult + OperationResult<T>)
│   ├── LoanPolicy.cs                         (konstanta aturan pinjam)
│   ├── IsbnHelper.cs                         (normalisasi + checksum — BR-13)
│   ├── Clock.cs                              (IClock + SystemClock — satu-satunya abstraksi yang dipertahankan)
│   ├── BookService.cs
│   ├── MemberService.cs
│   ├── LoanService.cs
│   ├── FineService.cs
│   ├── LookupService.cs
│   └── DashboardService.cs
│
├── Models/
│   ├── PagedList.cs                          (Items, TotalCount, Page, PageSize, TotalPages)
│   ├── LoanFilter.cs                         (enum: Active/Overdue/Returned/Lost — lihat BR-19)
│   ├── DashboardSummary.cs
│   └── ReturnSummary.cs                      (hasil pengembalian: telat berapa hari, denda berapa)
│
├── Components/
│   ├── App.razor
│   ├── Routes.razor
│   ├── _Imports.razor
│   ├── Layout/
│   │   ├── MainLayout.razor(.css)
│   │   ├── NavMenu.razor(.css)
│   │   └── ReconnectModal.razor(.css/.js)
│   ├── Shared/
│   │   ├── PageHeader.razor
│   │   ├── SearchBar.razor
│   │   ├── PaginationControl.razor
│   │   ├── StatusBadge.razor
│   │   ├── ConfirmDialog.razor
│   │   └── ErrorAlert.razor
│   └── Pages/
│       ├── Home.razor                        (dashboard)
│       ├── Books/
│       │   ├── BookList.razor
│       │   ├── BookDetail.razor
│       │   └── BookForm.razor
│       ├── Members/
│       │   ├── MemberList.razor
│       │   ├── MemberDetail.razor
│       │   └── MemberForm.razor
│       ├── Loans/
│       │   ├── BorrowBook.razor
│       │   ├── LoanList.razor
│       │   └── OverdueList.razor
│       ├── Fines/
│       │   └── FineList.razor
│       ├── Lookups/
│       │   └── LookupList.razor
│       ├── Error.razor
│       └── NotFound.razor
│
└── wwwroot/
    ├── app.css
    ├── favicon.png
    └── lib/
        ├── bootstrap/                        (bawaan template)
        └── bootstrap-icons/                  (dipasang manual — bagian 2.4)
```

> **Kenapa 5 enum ditumpuk di satu `Enums.cs`?** Ini penyederhanaan yang disengaja dan boleh dilakukan
> untuk **enum saja**. Enum-nya pendek, saling berkaitan, dan lebih enak dibaca berdampingan.
> Aturan "satu class publik per file" tetap berlaku untuk entity, service, dan model.
> Pengecualian lain yang diizinkan: `OperationResult` + `OperationResult<T>` dalam satu file, dan
> `IClock` + `SystemClock` dalam satu file.

---

## 5. Aturan Bisnis (Business Rules)

> Nomor BR di dokumen ini **identik** dengan `PRD.md` supaya dua arsitektur bisa dibandingkan langsung.
> Yang berubah cuma kolom "Ditegakkan di".
> Setiap kode yang menegakkan aturan **wajib** diberi komentar ID-nya, contoh `// BR-01`.

| ID | Aturan | Ditegakkan di |
|---|---|---|
| **BR-01** | Member aktif maksimal meminjam **3 buku** dalam waktu bersamaan | `LoanService.BorrowAsync()` |
| **BR-02** | Masa pinjam default **7 hari** sejak tanggal pinjam | `LoanPolicy.LoanDurationDays` |
| **BR-03** | Member dengan **denda belum lunas** tidak boleh meminjam | `LoanService.BorrowAsync()` |
| **BR-04** | Member berstatus `Suspended`/`Inactive` tidak boleh meminjam | `LoanService.BorrowAsync()` |
| **BR-05** | Buku hanya bisa dipinjam jika ada `BookCopy` berstatus `Available` | `LoanService.BorrowAsync()` |
| **BR-06** | Denda keterlambatan = **Rp 1.000 x jumlah hari telat** | `LoanService.ReturnAsync()` + `LoanPolicy.FinePerLateDay` |
| **BR-07** | Perpanjangan maksimal **1x**, menambah **7 hari** dari `DueDate` lama | `LoanService.RenewAsync()` |
| **BR-08** | Perpanjangan **ditolak** jika pinjaman sudah lewat jatuh tempo | `LoanService.RenewAsync()` |
| **BR-09** | Perpanjangan **ditolak** jika status bukan `Active` | `LoanService.RenewAsync()` |
| **BR-10** | Pinjaman yang sudah dikembalikan tidak bisa dikembalikan lagi | `LoanService.ReturnAsync()` |
| **BR-11** | Saat dikembalikan, `BookCopy` kembali ke status `Available` | `LoanService.ReturnAsync()` — satu `SaveChangesAsync` bareng update `Loan` |
| **BR-12** | Buku dinyatakan **hilang** → denda = harga penggantian, kopi jadi `Lost`, pinjaman jadi `Lost` | `LoanService.MarkAsLostAsync()` |
| **BR-13** | `Isbn` harus valid ISBN-10 atau ISBN-13 (checksum diverifikasi) dan **disimpan ternormalisasi** (13 digit, tanpa `-`/spasi) supaya unique index tidak bocor | `IsbnHelper.TryNormalize()`, dipanggil `BookService` |
| **BR-14** | `InventoryCode` pada `BookCopy` harus **unik global** | `BookService` (cek dulu) + unique index di DB |
| **BR-15** | `MemberCode` unik, format `MBR-{YYYY}-{00000}`; nomor urut = `MAX(sequence tahun ini) + 1`, bentrok ditangani **retry** saat unique index melempar | `MemberService.RegisterAsync()` + unique index |
| **BR-16** | Email member harus unik & valid | `[EmailAddress]` di entity + cek di `MemberService` + unique index |
| **BR-17** | Buku tidak boleh dihapus jika masih punya pinjaman aktif | `BookService.DeleteAsync()` |
| **BR-18** | Nominal uang (`Amount`, `ReplacementCost`) tidak boleh negatif | `[Range(0, ...)]` di entity + CHECK di DB |
| **BR-19** | Status `Overdue` bersifat **turunan** (`DueDate < today && ReturnedAt == null`) — tidak disimpan permanen | `Loan.IsOverdue(today)` + `LoanFilter` (bagian 7.3) |
| **BR-20** | Denda bisa di-`Waive` (dihapuskan) petugas, dengan alasan wajib (min 5 karakter) | `FineService.WaiveAsync()` |
| **BR-21** | `BookCopy` berstatus `OnLoan` tidak boleh dihapus, di-`Retire`, atau diubah statusnya jadi `Available` secara manual | `BookService.RemoveCopyAsync()` / `ChangeCopyStatusAsync()` |
| **BR-22** | Pembayaran denda hanya **lunas penuh** — pembayaran sebagian tidak didukung di versi ini | `FineService.PayAsync()` |
| **BR-23** | Kopi yang hilang/rusak juga menerbitkan denda: `LostBook` = `ReplacementCost`, `DamagedBook` = 50% `ReplacementCost` | `LoanService.MarkAsLostAsync()` / `BookService.ChangeCopyStatusAsync()` |

> **Perhatikan pergeserannya.** Di `PRD.md`, kolom kanan berisi 12 tempat berbeda (`Member`, `Book`, `Loan`,
> `Fine`, `FineCalculator`, value object, handler). Di sini, 9 dari 23 aturan menumpuk di `LoanService`.
> Itulah trade-off-nya: lebih gampang dicari sekarang, lebih gampang jadi class raksasa nanti.

### 5.1 Konstanta `LoanPolicy`

```csharp
namespace LibraryAppPrototype.Services;

public static class LoanPolicy
{
    public const int MaxActiveLoansPerMember  = 3;      // BR-01
    public const int LoanDurationDays         = 7;      // BR-02
    public const int MaxRenewalCount          = 1;      // BR-07
    public const int RenewalExtensionDays     = 7;      // BR-07
    public const decimal FinePerLateDay       = 1_000m; // BR-06 (IDR)
    public const decimal DamagedBookFineRatio = 0.5m;   // BR-23
}
```

> Angka-angka ini **tidak boleh** ditulis ulang sebagai literal di service atau di `.razor`.
> Selalu rujuk konstanta ini — kalau tidak, mengubah batas dari 3 jadi 5 berarti berburu angka `3`
> di seluruh project.

---

## 6. Kebutuhan Fungsional (Functional Requirements)

> Nomor FR juga identik dengan `PRD.md`.

### 6.1 Modul Buku

| ID | Kebutuhan | Prioritas |
|---|---|---|
| FR-01 | Petugas dapat menambah buku baru (judul, ISBN, penulis, kategori, penerbit, tahun, deskripsi, harga penggantian, jumlah kopi awal) | Must |
| FR-02 | Petugas dapat mengubah data buku | Must |
| FR-03 | Petugas dapat menghapus buku (ditolak jika ada pinjaman aktif — BR-17) | Must |
| FR-04 | Petugas dapat menambah kopi fisik buku dengan kode inventaris unik | Must |
| FR-05 | Petugas dapat mengubah status kopi (Rusak / Hilang / Ditarik); kopi yang sedang `OnLoan` ditolak (BR-21) | Should |
| FR-06 | Pencarian buku by judul/ISBN/penulis dengan paging | Must |
| FR-07 | Filter buku by kategori dan ketersediaan | Should |
| FR-08 | Halaman detail buku menampilkan daftar kopi + statusnya | Must |

### 6.2 Modul Anggota

| ID | Kebutuhan | Prioritas |
|---|---|---|
| FR-09 | Petugas dapat mendaftarkan anggota baru (kode otomatis, nama, email, telepon, alamat) | Must |
| FR-10 | Petugas dapat mengubah data anggota | Must |
| FR-11 | Petugas dapat menangguhkan (suspend) & mengaktifkan kembali anggota | Should |
| FR-12 | Pencarian anggota by nama/kode/email dengan paging | Must |
| FR-13 | Halaman detail anggota menampilkan pinjaman aktif, riwayat, dan total denda | Must |

### 6.3 Modul Peminjaman

| ID | Kebutuhan | Prioritas |
|---|---|---|
| FR-14 | Petugas dapat meminjamkan buku; sistem menolak jika melanggar BR-01/03/04/05 dengan pesan jelas | Must |
| FR-15 | Sistem otomatis mengalokasikan `BookCopy` yang tersedia | Must |
| FR-16 | Petugas dapat memproses pengembalian; denda dihitung otomatis (BR-06) | Must |
| FR-17 | Petugas dapat memperpanjang pinjaman (BR-07/08/09) | Should |
| FR-18 | Petugas dapat menandai pinjaman sebagai buku hilang; denda `ReplacementCost` terbit otomatis (BR-12/BR-23) | Could |
| FR-19 | Daftar pinjaman dengan filter `LoanFilter` (Active/Overdue/Returned/Lost) + paging | Must |
| FR-20 | Halaman khusus daftar keterlambatan, diurutkan dari yang paling lama | Should |

### 6.4 Modul Denda

| ID | Kebutuhan | Prioritas |
|---|---|---|
| FR-21 | Melihat daftar denda per anggota | Must |
| FR-22 | Petugas dapat mencatat pembayaran denda (lunas penuh — BR-22) | Must |
| FR-23 | Petugas dapat menghapuskan denda dengan alasan (BR-20) | Could |

### 6.5 Dashboard

| ID | Kebutuhan | Prioritas |
|---|---|---|
| FR-24 | Ringkasan: total buku, total kopi, kopi tersedia, anggota aktif, pinjaman aktif, terlambat, total denda belum lunas | Must |
| FR-25 | Daftar 5 pinjaman terbaru & 5 keterlambatan terparah | Should |

### 6.6 Modul Lookup

| ID | Kebutuhan | Prioritas |
|---|---|---|
| FR-26 | Petugas dapat menambah penulis baru (nama unik, biografi opsional) | Should |
| FR-27 | Petugas dapat menambah kategori baru (nama unik, deskripsi opsional) | Should |

---

## 7. Rekapitulasi Jumlah File

| Folder | Kategori | Jumlah |
|---|---|---:|
| **Root** | `Program.cs` | 1 |
| **Data** | `AppDbContext`, `DbSeeder`, `Enums.cs` | 3 |
| | Entities | 7 |
| | **Subtotal Data** | **10** |
| **Services** | `OperationResult`, `LoanPolicy`, `IsbnHelper`, `Clock` | 4 |
| | Service per modul | 6 |
| | **Subtotal Services** | **10** |
| **Models** | `PagedList`, `LoanFilter`, `DashboardSummary`, `ReturnSummary` | 4 |
| **Components** | Root (App, Routes, _Imports) | 3 |
| | Layout | 3 |
| | Shared | 6 |
| | Pages | 14 |
| | **Subtotal Components** | **26** |
| | **TOTAL** | **± 51 file** |

> 51 file, dibanding **159 file** di `PRD.md` — sekitar **sepertiganya**, untuk fitur yang persis sama.
> Kalau hasil implementasi meleset dari angka ini, **perbarui tabelnya** — jangan bikin file kosong
> supaya angkanya cocok.

### 7.1 Rincian 6 Service

| Service | Method | Aturan yang ditegakkan |
|---|---|---|
| `BookService` | `SearchAsync(keyword, categoryId?, onlyAvailable, page, pageSize)` → `PagedList<Book>` | — |
| | `GetByIdAsync(id)` → `Book?` (dengan `Include(b => b.Copies)`) | — |
| | `CreateAsync(Book book, int initialCopyCount)` → `OperationResult<int>` | BR-13, BR-14 |
| | `UpdateAsync(Book book)` → `OperationResult` | BR-13 |
| | `DeleteAsync(id)` → `OperationResult` | BR-17 |
| | `AddCopyAsync(bookId, inventoryCode)` → `OperationResult<BookCopy>` | BR-14 |
| | `RemoveCopyAsync(copyId)` → `OperationResult` | BR-21 |
| | `ChangeCopyStatusAsync(copyId, newStatus)` → `OperationResult` | BR-21, BR-23 |
| `MemberService` | `SearchAsync(keyword, status?, page, pageSize)` → `PagedList<Member>` | — |
| | `GetByIdAsync(id)` → `Member?` | — |
| | `RegisterAsync(Member member)` → `OperationResult<int>` | BR-15, BR-16 |
| | `UpdateAsync(Member member)` → `OperationResult` | BR-16 |
| | `SuspendAsync(id)` / `ReactivateAsync(id)` → `OperationResult` | — |
| | `GetActiveLoanCountAsync(memberId)` → `int` | dipakai BR-01 |
| | `GetOutstandingFineAsync(memberId)` → `decimal` | dipakai BR-03 |
| `LoanService` | `BorrowAsync(memberId, bookId)` → `OperationResult<Loan>` | BR-01..BR-05 |
| | `ReturnAsync(loanId)` → `OperationResult<ReturnSummary>` | BR-06, BR-10, BR-11 |
| | `RenewAsync(loanId)` → `OperationResult<Loan>` | BR-07, BR-08, BR-09 |
| | `MarkAsLostAsync(loanId)` → `OperationResult<Fine>` | BR-12, BR-23 |
| | `SearchAsync(keyword, LoanFilter?, memberId?, page, pageSize)` → `PagedList<Loan>` | BR-19 |
| | `GetOverdueAsync()` → `List<Loan>` | BR-19 |
| | `GetActiveByMemberAsync(memberId)` → `List<Loan>` | — |
| `FineService` | `GetByMemberAsync(memberId)` → `List<Fine>` | — |
| | `SearchAsync(status?, page, pageSize)` → `PagedList<Fine>` | — |
| | `GetUnpaidTotalAsync(memberId)` → `decimal` | dipakai BR-03 |
| | `PayAsync(fineId)` → `OperationResult` | BR-22 |
| | `WaiveAsync(fineId, reason)` → `OperationResult` | BR-20 |
| `LookupService` | `GetAuthorsAsync()` / `GetCategoriesAsync()` | — |
| | `CreateAuthorAsync(name, biography?)` → `OperationResult<int>` | FR-26 |
| | `CreateCategoryAsync(name, description?)` → `OperationResult<int>` | FR-27 |
| `DashboardService` | `GetSummaryAsync()` → `DashboardSummary` | FR-24, FR-25 |

> **Batas ukuran:** kalau satu service tembus **~300 baris**, pecah per aksi (misal
> `LoanService.Borrow.cs` sebagai `partial class`) — jangan biarkan tumbuh terus. Kalau sudah butuh
> dipecah dua kali, itu sinyal pertama untuk baca bagian 17.

### 7.2 `OperationResult` — error handling tanpa exception

```csharp
// Services/OperationResult.cs
public record OperationResult(bool Succeeded, string? Code, string? Message)
{
    public static OperationResult Ok() => new(true, null, null);
    public static OperationResult Fail(string code, string message) => new(false, code, message);
}

public record OperationResult<T>(bool Succeeded, T? Value, string? Code, string? Message)
    : OperationResult(Succeeded, Code, Message)
{
    public static OperationResult<T> Ok(T value) => new(true, value, null, null);
    public static new OperationResult<T> Fail(string code, string message) => new(false, default, code, message);
}
```

Aturan pemakaian:

- `Code` **selalu** diisi ID aturan (`"BR-01"`, `"BR-05"`, …) atau kode teknis (`"NOT_FOUND"`, `"CONFLICT"`).
- `Message` ditulis dalam **Bahasa Indonesia** karena langsung tampil di `ErrorAlert`.
- Service **tidak melempar exception** untuk kasus bisnis yang wajar. Exception hanya untuk hal yang
  memang bug (data korup, null yang mustahil).

### 7.3 `LoanFilter` — kenapa perlu enum sendiri

```csharp
// Models/LoanFilter.cs
public enum LoanFilter { Active, Overdue, Returned, Lost }
```

`Overdue` **bukan** nilai `LoanStatus` (BR-19: statusnya turunan, tidak disimpan). Kalau UI langsung
memakai `LoanStatus?` sebagai filter, `Overdue` tidak bisa diwakili — dan akan muncul godaan menambahkan
`Overdue` ke `LoanStatus`, yang justru merusak BR-19. Terjemahannya di `LoanService.SearchAsync`:

| Filter | Predikat |
|---|---|
| `Active` | `Status == Active && DueDate >= today` |
| `Overdue` | `Status == Active && ReturnedAt == null && DueDate < today` |
| `Returned` | `Status == Returned` |
| `Lost` | `Status == Lost` |

---

## 8. Model Data — Detail Entity

> Semua entity adalah `public class` biasa dengan property `{ get; set; }` publik.
> Validasi ditulis sebagai **DataAnnotations langsung di entity**, dan dipakai dua kali: oleh EF Core
> untuk menentukan skema, dan oleh `EditForm` untuk memvalidasi input.

### 8.1 `Book`

| Property | Tipe | Anotasi / Catatan |
|---|---|---|
| `Id` | `int` | PK |
| `Title` | `string` | `[Required] [MaxLength(200)]` |
| `Isbn` | `string` | `[Required] [MaxLength(13)]`, disimpan ternormalisasi (BR-13), unik |
| `AuthorId` / `Author` | `int` / `Author` | FK |
| `CategoryId` / `Category` | `int` / `Category` | FK |
| `Publisher` | `string?` | `[MaxLength(150)]` |
| `PublishedYear` | `int?` | `[Range(1450, 2100)]` |
| `Description` | `string?` | `[MaxLength(2000)]` |
| `ReplacementCost` | `decimal` | `[Range(0, 99999999)] [Precision(18, 2)]` (BR-18) |
| `CreatedAt` | `DateTime` | UTC, diisi `BookService` dari `IClock.UtcNow` |
| `Copies` | `List<BookCopy>` | navigasi, `= []` |

**Property turunan (tidak dipetakan ke kolom):**

```csharp
[NotMapped] public int TotalCopies     => Copies.Count;
[NotMapped] public int AvailableCopies => Copies.Count(c => c.Status == BookCopyStatus.Available);
```

> `[NotMapped]` hanya boleh untuk perhitungan murni dari koleksi yang **sudah di-`Include`**.
> Jangan pakai di query LINQ ke database — EF tidak bisa menerjemahkannya.

### 8.2 `BookCopy`

| Property | Tipe | Anotasi / Catatan |
|---|---|---|
| `Id` | `int` | PK |
| `BookId` / `Book` | `int` / `Book` | FK |
| `InventoryCode` | `string` | `[Required] [MaxLength(40)]`, unik global (BR-14) |
| `Status` | `BookCopyStatus` | Available / OnLoan / Lost / Damaged / Retired |
| `AcquiredAt` | `DateOnly` | |

### 8.3 `Member`

| Property | Tipe | Anotasi / Catatan |
|---|---|---|
| `Id` | `int` | PK |
| `Code` | `string` | `[MaxLength(20)]`, unik, format `MBR-{YYYY}-{00000}` (BR-15) — **diisi service, bukan user** |
| `FullName` | `string` | `[Required] [MaxLength(120)]` |
| `Email` | `string` | `[Required] [EmailAddress] [MaxLength(160)]`, unik, disimpan lowercase (BR-16) |
| `PhoneNumber` | `string?` | `[MaxLength(25)] [Phone]` |
| `Address` | `string?` | `[MaxLength(300)]` |
| `JoinedAt` | `DateOnly` | diisi service dari `IClock.Today` |
| `Status` | `MemberStatus` | Active / Suspended / Inactive |

> **Catatan form:** `Code` dan `JoinedAt` tidak boleh di-bind di `MemberForm.razor`. Karena entity dipakai
> langsung sebagai model form (kita tidak pakai DTO), field yang tidak boleh diisi user cukup **tidak
> ditampilkan** — dan service **selalu** menimpanya sendiri, tidak percaya nilai yang datang dari form.
> Ini konsekuensi nyata dari tidak memakai DTO; ingat baik-baik.

### 8.4 `Loan`

| Property | Tipe | Anotasi / Catatan |
|---|---|---|
| `Id` | `int` | PK |
| `MemberId` / `Member` | `int` / `Member` | FK |
| `BookCopyId` / `BookCopy` | `int` / `BookCopy` | FK |
| `BorrowedAt` | `DateOnly` | |
| `DueDate` | `DateOnly` | `BorrowedAt + 7` (BR-02) |
| `ReturnedAt` | `DateOnly?` | null = masih dipinjam |
| `RenewalCount` | `int` | max 1 (BR-07) |
| `Status` | `LoanStatus` | Active / Returned / Lost |

**Method turunan yang diizinkan** (murni baca property sendiri, tanpa DB): `IsOverdue(today)`, `DaysLate(today)` — lihat contoh di 3.2.

### 8.5 `Fine`

| Property | Tipe | Anotasi / Catatan |
|---|---|---|
| `Id` | `int` | PK |
| `LoanId` / `Loan` | `int` / `Loan` | FK |
| `MemberId` / `Member` | `int` / `Member` | FK (denormalisasi, supaya query denda per anggota cepat) |
| `Amount` | `decimal` | `[Range(0, 99999999)] [Precision(18, 2)]` (BR-18) |
| `Reason` | `FineReason` | LateReturn / LostBook / DamagedBook |
| `IssuedAt` | `DateOnly` | |
| `PaidAt` | `DateOnly?` | |
| `Status` | `FineStatus` | Unpaid / Paid / Waived |
| `WaiveReason` | `string?` | `[MaxLength(300)]`, wajib jika `Waived` (BR-20) |

### 8.6 `Author` & `Category`

| Entity | Property |
|---|---|
| `Author` | `Id`, `Name` (`[Required] [MaxLength(120)]`, unik), `Biography?` (`[MaxLength(1000)]`), `List<Book> Books` |
| `Category` | `Id`, `Name` (`[Required] [MaxLength(80)]`, unik), `Description?` (`[MaxLength(400)]`), `List<Book> Books` |

### 8.7 `Enums.cs`

```csharp
namespace LibraryAppPrototype.Data;

public enum BookCopyStatus : byte { Available = 0, OnLoan = 1, Lost = 2, Damaged = 3, Retired = 4 }
public enum MemberStatus   : byte { Active = 0, Suspended = 1, Inactive = 2 }
public enum LoanStatus     : byte { Active = 0, Returned = 1, Lost = 2 }
public enum FineStatus     : byte { Unpaid = 0, Paid = 1, Waived = 2 }
public enum FineReason     : byte { LateReturn = 0, LostBook = 1, DamagedBook = 2 }
```

> `: byte` membuat EF Core otomatis memetakannya ke `TINYINT` tanpa perlu converter.
> **`LoanStatus` tidak boleh punya nilai `Overdue`** (BR-19) — pakai `LoanFilter` (7.3).

---

## 9. Struktur Database (SQL Server Express)

### 9.1 ERD

```
Categories ──1───<── Books ──>───1── Authors
                       │
                       │ 1
                       │
                       v *
                   BookCopies
                       │ 1
                       │
                       v *
   Members ──1───<── Loans ──1───<── Fines
       │                              ^
       └──────────────1───────────────┘
                (MemberId, denormalisasi)
```

Struktur tabelnya **hampir identik** dengan `PRD.md`. Tiga perbedaan yang disengaja:

1. **Tidak ada soft delete.** Kolom `Books.IsDeleted` dihapus. Penghapusan buku benar-benar `DELETE`,
   dan dijaga BR-17. Konsekuensinya unique index `Isbn` jadi index biasa (tanpa filter) — lebih sederhana.
2. **Tidak ada value object**, jadi tidak ada converter apapun. `Isbn` = `NVARCHAR(13)`,
   `Money` = `DECIMAL(18,2)`.
3. **Konfigurasi lewat DataAnnotations** untuk panjang & precision; `OnModelCreating` hanya dipakai
   untuk index, `DeleteBehavior`, dan CHECK constraint.

### 9.2 Tabel & Kolom

#### `Authors`
| Kolom | Tipe | Constraint |
|---|---|---|
| `Id` | `INT IDENTITY(1,1)` | PK |
| `Name` | `NVARCHAR(120)` | NOT NULL, UNIQUE |
| `Biography` | `NVARCHAR(1000)` | NULL |

#### `Categories`
| Kolom | Tipe | Constraint |
|---|---|---|
| `Id` | `INT IDENTITY(1,1)` | PK |
| `Name` | `NVARCHAR(80)` | NOT NULL, UNIQUE |
| `Description` | `NVARCHAR(400)` | NULL |

#### `Books`
| Kolom | Tipe | Constraint |
|---|---|---|
| `Id` | `INT IDENTITY(1,1)` | PK |
| `Title` | `NVARCHAR(200)` | NOT NULL |
| `Isbn` | `NVARCHAR(13)` | NOT NULL, **UNIQUE**, ternormalisasi (BR-13) |
| `AuthorId` | `INT` | NOT NULL, FK → `Authors(Id)` ON DELETE NO ACTION |
| `CategoryId` | `INT` | NOT NULL, FK → `Categories(Id)` ON DELETE NO ACTION |
| `Publisher` | `NVARCHAR(150)` | NULL |
| `PublishedYear` | `INT` | NULL, CHECK (1450..2100) |
| `Description` | `NVARCHAR(2000)` | NULL |
| `ReplacementCost` | `DECIMAL(18,2)` | NOT NULL, CHECK >= 0 |
| `CreatedAt` | `DATETIME2(0)` | NOT NULL, **tanpa DEFAULT** — diisi `BookService` dari `IClock.UtcNow` |

#### `BookCopies`
| Kolom | Tipe | Constraint |
|---|---|---|
| `Id` | `INT IDENTITY(1,1)` | PK |
| `BookId` | `INT` | NOT NULL, FK → `Books(Id)` ON DELETE CASCADE |
| `InventoryCode` | `NVARCHAR(40)` | NOT NULL, **UNIQUE** (BR-14) |
| `Status` | `TINYINT` | NOT NULL DEFAULT 0 |
| `AcquiredAt` | `DATE` | NOT NULL |

#### `Members`
| Kolom | Tipe | Constraint |
|---|---|---|
| `Id` | `INT IDENTITY(1,1)` | PK |
| `Code` | `NVARCHAR(20)` | NOT NULL, **UNIQUE** (BR-15) |
| `FullName` | `NVARCHAR(120)` | NOT NULL |
| `Email` | `NVARCHAR(160)` | NOT NULL, **UNIQUE** (BR-16) |
| `PhoneNumber` | `NVARCHAR(25)` | NULL |
| `Address` | `NVARCHAR(300)` | NULL |
| `JoinedAt` | `DATE` | NOT NULL |
| `Status` | `TINYINT` | NOT NULL DEFAULT 0 |

#### `Loans`
| Kolom | Tipe | Constraint |
|---|---|---|
| `Id` | `INT IDENTITY(1,1)` | PK |
| `MemberId` | `INT` | NOT NULL, FK → `Members(Id)` ON DELETE NO ACTION |
| `BookCopyId` | `INT` | NOT NULL, FK → `BookCopies(Id)` ON DELETE NO ACTION |
| `BorrowedAt` | `DATE` | NOT NULL |
| `DueDate` | `DATE` | NOT NULL, CHECK (`DueDate >= BorrowedAt`) |
| `ReturnedAt` | `DATE` | NULL |
| `RenewalCount` | `INT` | NOT NULL DEFAULT 0, CHECK (0..1) |
| `Status` | `TINYINT` | NOT NULL DEFAULT 0 |

#### `Fines`
| Kolom | Tipe | Constraint |
|---|---|---|
| `Id` | `INT IDENTITY(1,1)` | PK |
| `LoanId` | `INT` | NOT NULL, FK → `Loans(Id)` ON DELETE NO ACTION |
| `MemberId` | `INT` | NOT NULL, FK → `Members(Id)` ON DELETE NO ACTION |
| `Amount` | `DECIMAL(18,2)` | NOT NULL, CHECK >= 0 (BR-18) |
| `Reason` | `TINYINT` | NOT NULL |
| `IssuedAt` | `DATE` | NOT NULL |
| `PaidAt` | `DATE` | NULL |
| `Status` | `TINYINT` | NOT NULL DEFAULT 0 |
| `WaiveReason` | `NVARCHAR(300)` | NULL |

### 9.3 Index yang wajib dibuat

| Index | Tabel | Kolom | Alasan |
|---|---|---|---|
| `UX_Books_Isbn` | Books | `Isbn` | BR-13 |
| `IX_Books_Title` | Books | `Title` | pencarian FR-06 |
| `IX_Books_CategoryId` | Books | `CategoryId` | filter FR-07 |
| `UX_BookCopies_InventoryCode` | BookCopies | `InventoryCode` | BR-14 |
| `IX_BookCopies_BookId_Status` | BookCopies | `BookId, Status` | cari kopi tersedia (BR-05) |
| `UX_Members_Code` | Members | `Code` | BR-15 |
| `UX_Members_Email` | Members | `Email` | BR-16 |
| `IX_Members_FullName` | Members | `FullName` | pencarian FR-12 |
| `IX_Loans_MemberId_Status` | Loans | `MemberId, Status` | hitung pinjaman aktif (BR-01) |
| `IX_Loans_DueDate_ReturnedAt` | Loans | `DueDate, ReturnedAt` | daftar terlambat (FR-20) |
| `IX_Loans_BookCopyId` | Loans | `BookCopyId` | riwayat per kopi |
| `IX_Fines_MemberId_Status` | Fines | `MemberId, Status` | cek denda tertunggak (BR-03) |

### 9.4 `OnModelCreating` — hanya yang tidak bisa ditulis sebagai atribut

```csharp
protected override void OnModelCreating(ModelBuilder b)
{
    b.Entity<Author>().HasIndex(a => a.Name).IsUnique();
    b.Entity<Category>().HasIndex(c => c.Name).IsUnique();

    b.Entity<Book>(e =>
    {
        e.HasIndex(x => x.Isbn).IsUnique();
        e.HasIndex(x => x.Title);
        e.HasIndex(x => x.CategoryId);
        e.HasOne(x => x.Author).WithMany(a => a.Books).OnDelete(DeleteBehavior.NoAction);
        e.HasOne(x => x.Category).WithMany(c => c.Books).OnDelete(DeleteBehavior.NoAction);
        e.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Books_Year", "PublishedYear IS NULL OR PublishedYear BETWEEN 1450 AND 2100");
            t.HasCheckConstraint("CK_Books_Cost", "ReplacementCost >= 0");
        });
    });

    b.Entity<BookCopy>(e =>
    {
        e.HasIndex(x => x.InventoryCode).IsUnique();
        e.HasIndex(x => new { x.BookId, x.Status });
        e.HasOne(x => x.Book).WithMany(x => x.Copies).OnDelete(DeleteBehavior.Cascade);
    });

    b.Entity<Member>(e =>
    {
        e.HasIndex(x => x.Code).IsUnique();
        e.HasIndex(x => x.Email).IsUnique();
        e.HasIndex(x => x.FullName);
    });

    b.Entity<Loan>(e =>
    {
        e.HasIndex(x => new { x.MemberId, x.Status });
        e.HasIndex(x => new { x.DueDate, x.ReturnedAt });
        e.HasIndex(x => x.BookCopyId);
        e.HasOne(x => x.Member).WithMany().OnDelete(DeleteBehavior.NoAction);
        e.HasOne(x => x.BookCopy).WithMany().OnDelete(DeleteBehavior.NoAction);
        e.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Loans_DueDate", "DueDate >= BorrowedAt");
            t.HasCheckConstraint("CK_Loans_Renewal", "RenewalCount BETWEEN 0 AND 1");
        });
    });

    b.Entity<Fine>(e =>
    {
        e.HasIndex(x => new { x.MemberId, x.Status });
        e.HasOne(x => x.Loan).WithMany().OnDelete(DeleteBehavior.NoAction);
        e.HasOne(x => x.Member).WithMany().OnDelete(DeleteBehavior.NoAction);
        e.ToTable(t => t.HasCheckConstraint("CK_Fines_Amount", "Amount >= 0"));
    });
}
```

Catatan mapping lain:

- **`DateOnly` → `date`** didukung native EF Core 8+, tidak perlu converter.
- **Enum `: byte` → `TINYINT`** otomatis, tidak perlu `HasConversion`.
- **`[Precision(18, 2)]`** di entity sudah cukup untuk kolom `decimal` — jangan diulang di `OnModelCreating`.

### 9.5 Seed Data (`DbSeeder`)

Dijalankan hanya jika tabel kosong, dan hanya saat `Environment.IsDevelopment()`:

- 5 `Categories`: Fiksi, Non-Fiksi, Teknologi, Sejarah, Anak
- 8 `Authors`
- 15 `Books`, masing-masing 2–3 `BookCopies` (total ± 35 kopi)
- 6 `Members` (5 Active, 1 Suspended)
- 8 `Loans`: 4 aktif belum jatuh tempo, 2 aktif sudah lewat jatuh tempo, 2 sudah dikembalikan
- 2 `Fines` belum lunas (agar BR-03 bisa langsung dicoba)

> Seed **wajib memakai tanggal relatif** terhadap `IClock.Today` (misal `today.AddDays(-10)`), bukan
> tanggal hardcode. Kalau hardcode, skenario AC-07 (telat 3 hari) akan meleset begitu dijalankan di hari lain.

---

## 10. Spesifikasi Halaman (Blazor + Bootstrap 5)

Semua styling memakai **Bootstrap 5 lokal**. Dilarang menambahkan CDN, Tailwind, atau CSS framework lain.

| Route | Component | Render Mode | Isi |
|---|---|---|---|
| `/` | `Home.razor` | `InteractiveServer` | Dashboard: 7 kartu statistik (`row row-cols-md-4 g-3`), tabel pinjaman terbaru & terlambat |
| `/books` | `BookList.razor` | `InteractiveServer` | `SearchBar` + dropdown kategori + checkbox "hanya tersedia" + `table table-hover` + `PaginationControl` |
| `/books/new` | `BookForm.razor` | `InteractiveServer` | `EditForm` + `DataAnnotationsValidator`, layout `row g-3` |
| `/books/{Id:int}` | `BookDetail.razor` | `InteractiveServer` | Info buku + tabel kopi dengan `StatusBadge` + tombol tambah kopi + ubah status kopi |
| `/books/{Id:int}/edit` | `BookForm.razor` | `InteractiveServer` | Form yang sama, mode edit |
| `/members` | `MemberList.razor` | `InteractiveServer` | Tabel anggota + badge status + kolom denda tertunggak |
| `/members/new` | `MemberForm.razor` | `InteractiveServer` | Form pendaftaran (tanpa field `Code` & `JoinedAt` — lihat 8.3) |
| `/members/{Id:int}` | `MemberDetail.razor` | `InteractiveServer` | Profil + pinjaman aktif + riwayat + denda (`nav nav-tabs`) |
| `/members/{Id:int}/edit` | `MemberForm.razor` | `InteractiveServer` | Mode edit |
| `/loans` | `LoanList.razor` | `InteractiveServer` | Filter `LoanFilter` (`btn-group`) + tabel + aksi Kembalikan / Perpanjang / Tandai Hilang |
| `/loans/borrow` | `BorrowBook.razor` | `InteractiveServer` | Pilih anggota → pilih buku → tombol Pinjam; pesan penolakan BR tampil di `ErrorAlert` |
| `/loans/overdue` | `OverdueList.razor` | `InteractiveServer` | Tabel keterlambatan + estimasi denda berjalan, diurutkan paling lama telat |
| `/fines` | `FineList.razor` | `InteractiveServer` | Daftar denda + tombol Bayar / Hapuskan (modal alasan) |
| `/lookups` | `LookupList.razor` | `InteractiveServer` | Kelola penulis & kategori (`nav nav-tabs` + form tambah inline) |
| — | `Error.razor` | static | Dipasang lewat `ErrorBoundary` di `MainLayout` |
| — | `NotFound.razor` | static | Didaftarkan lewat `<Router NotFoundPage="typeof(NotFound)">` (.NET 10) |

### 10.1 Pola pemanggilan service di halaman

```razor
@page "/loans/borrow"
@rendermode InteractiveServer
@inject LoanService Loans
@inject NavigationManager Nav

<ErrorAlert Code="@_errorCode" Message="@_errorMessage" OnDismiss="() => _errorMessage = null" />

<button class="btn btn-primary" disabled="@_busy" @onclick="BorrowAsync">
    @if (_busy) { <span class="spinner-border spinner-border-sm me-1"></span> }
    Pinjam
</button>

@code {
    private bool _busy;
    private string? _errorCode;
    private string? _errorMessage;

    private async Task BorrowAsync()
    {
        _busy = true;
        _errorMessage = null;

        var result = await Loans.BorrowAsync(_selectedMemberId, _selectedBookId);

        _busy = false;

        if (!result.Succeeded)
        {
            _errorCode = result.Code;          // "BR-01", "BR-03", ...
            _errorMessage = result.Message;    // sudah Bahasa Indonesia dari service
            return;
        }

        Nav.NavigateTo($"/members/{_selectedMemberId}");
    }
}
```

Tiga hal yang wajib ada di setiap aksi: **flag `_busy`** (cegah klik ganda), **reset pesan error** sebelum
memanggil, dan **tidak ada satupun `if` aturan bisnis** di blok `@code`.

### 10.2 Konvensi UI Bootstrap

| Kebutuhan | Class Bootstrap |
|---|---|
| Tabel data | `table table-hover align-middle` di dalam `table-responsive` |
| Tombol utama | `btn btn-primary` |
| Tombol hapus | `btn btn-outline-danger btn-sm` |
| Badge Available / Active / Paid | `badge text-bg-success` |
| Badge OnLoan / Unpaid | `badge text-bg-warning` |
| Badge Overdue / Lost / Suspended | `badge text-bg-danger` |
| Badge Returned / Waived / Retired | `badge text-bg-secondary` |
| Kartu statistik | `card shadow-sm` + `card-body` |
| Notifikasi error | `alert alert-danger` (via `ErrorAlert`) |
| Loading | `spinner-border spinner-border-sm` inline di tombol |
| Modal konfirmasi | `modal fade` (via `ConfirmDialog`) |
| Form | `row g-3` + `form-label` + `InputText`/`InputNumber`/`InputSelect` + `ValidationMessage` |

### 10.3 Komponen Shared

| Komponen | Parameter |
|---|---|
| `PageHeader` | `Title`, `Subtitle?`, `RenderFragment? Actions` |
| `SearchBar` | `Value`, `ValueChanged`, `Placeholder`, `OnSearch` |
| `PaginationControl` | `Page`, `PageSize`, `TotalCount`, `OnPageChanged` |
| `StatusBadge` | `Text`, `Variant` (success/warning/danger/secondary) |
| `ConfirmDialog` | `Title`, `Message`, `RequireReason` (bool, untuk BR-20), `OnConfirm`, `OnCancel` |
| `ErrorAlert` | `Code?`, `Message?`, `OnDismiss` |

> `LoadingSpinner` dan `EmptyState` dari `PRD.md` **sengaja tidak dibuat** di sini — spinner cukup inline
> di tombol, dan empty state cukup satu baris `<p class="text-muted">`. Contoh kecil bahwa proyek prototype
> tidak perlu meng-abstraksi segalanya.

---

## 11. Wiring Dependency Injection

### 11.1 `AddDbContextFactory`, BUKAN `AddDbContext` — ini wajib

Di Blazor Server, satu **DI scope dibuat per circuit** — bukan per klik, bukan per request. Circuit hidup
selama tab browser terbuka. Artinya `services.AddDbContext<AppDbContext>()` (Scoped) menghasilkan **satu
`DbContext` yang hidup berjam-jam** dan dipakai bersama seluruh komponen di tab itu. Akibatnya:

1. **Change tracker menggelembung** — setiap entity yang pernah dibaca menempel di memori sampai tab ditutup.
2. **Data basi** — entity yang sudah ada di tracker tidak dibaca ulang dari DB, jadi perubahan dari petugas
   lain tidak pernah muncul. Ini bug yang paling bikin bingung waktu belajar.
3. **`InvalidOperationException: A second operation was started on this context...`** — begitu dua operasi
   async tumpang tindih di satu circuit (user klik dua kali, atau dua komponen memuat data bersamaan).

> Ini jebakan nomor satu di Blazor Server, dan **tidak ada hubungannya dengan Clean Architecture** —
> `PRD.md` kena masalah yang sama persis dan menyelesaikannya dengan cara yang sama.

Aturan proyek ini: **satu `DbContext` untuk satu operasi service**, dibuat dan dibuang di dalam method.

### 11.2 `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Factory, BUKAN AddDbContext — lihat 11.1
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
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();
```

> Service didaftarkan `Scoped` karena itu default yang paling tidak mengejutkan. Mereka **stateless**
> (cuma bergantung pada `IDbContextFactory` yang Singleton dan `IClock` yang Singleton), jadi umur
> circuit-nya tidak jadi masalah — yang berbahaya cuma `DbContext`, dan itu sudah dibuat per operasi.

### 11.3 `IClock` — satu-satunya abstraksi yang dipertahankan

```csharp
// Services/Clock.cs
public interface IClock
{
    DateOnly Today  { get; }
    DateTime UtcNow { get; }
}

public class SystemClock : IClock
{
    public DateOnly Today  => DateOnly.FromDateTime(DateTime.Now);
    public DateTime UtcNow => DateTime.UtcNow;
}
```

Kenapa yang ini tidak ikut disederhanakan? Karena **BR-06, BR-08, dan BR-19 semuanya bergantung pada
"hari ini"**. Tanpa `IClock`, satu-satunya cara menguji "telat 3 hari" adalah mengubah jam sistem.
Biayanya cuma satu file, manfaatnya besar — ini contoh abstraksi yang **layak** dipertahankan bahkan di
proyek sederhana.

Konsekuensinya: `DateTime.Now` / `DateTime.UtcNow` / `DateTime.Today` **dilarang muncul di mana pun
selain `SystemClock.cs`**.

### 11.4 Transaksi lintas operasi

Setiap method service = satu `DbContext` = satu `SaveChangesAsync()` = satu transaksi implisit.
Kalau suatu saat butuh dua operasi jadi satu transaksi, jangan panggil dua service berurutan — bungkus
eksplisit di dalam **satu** method:

```csharp
await using var db = await dbFactory.CreateDbContextAsync(ct);
await using var tx = await db.Database.BeginTransactionAsync(ct);
// ... beberapa perubahan ...
await db.SaveChangesAsync(ct);
await tx.CommitAsync(ct);
```

> **Jangan pernah** memanggil `LoanService` dari dalam `FineService` (atau sebaliknya). Service tidak
> boleh saling memanggil — mereka akan memakai `DbContext` yang berbeda dan transaksinya pecah.
> Kalau butuh, pindahkan logikanya ke satu service atau duplikasi query kecilnya.

---

## 12. Roadmap Implementasi

Dikerjakan berurutan **dari data ke layar**. Berbeda dengan `PRD.md`, di sini UI boleh muncul lebih awal —
justru itu keuntungannya.

### Fase 0 — Persiapan
- [ ] `dotnet new blazor -n LibraryAppPrototype -int Server` (bagian 2.1)
- [ ] Install `dotnet-ef` global tool
- [ ] Tambahkan 2 NuGet package (bagian 2.2)
- [ ] Pasang Bootstrap Icons lokal + daftarkan di `App.razor` (bagian 2.4)
- [ ] Isi connection string (bagian 2.3)
- [ ] Hapus `Counter.razor` & `Weather.razor` bawaan template

### Fase 1 — Data
- [ ] `Data/Enums.cs` (5 enum)
- [ ] `Data/Entities/` (7 entity + DataAnnotations)
- [ ] `Data/AppDbContext.cs` + `OnModelCreating` (bagian 9.4)
- [ ] Migration `InitialCreate` + `database update`
- [ ] **Checkpoint:** database `db_library_prototype` terbentuk dengan 7 tabel + 12 index

### Fase 2 — Fondasi Service
- [ ] `Services/Clock.cs`, `OperationResult.cs`, `LoanPolicy.cs`, `IsbnHelper.cs`
- [ ] `Models/`: `PagedList`, `LoanFilter`, `DashboardSummary`, `ReturnSummary`
- [ ] `Data/DbSeeder.cs` (tanggal relatif!)
- [ ] Wiring DI di `Program.cs` (bagian 11.2)
- [ ] **Checkpoint:** aplikasi jalan, seed masuk, bisa dicek lewat SSMS/Rider

### Fase 3 — Service
- [ ] `LookupService` (paling sederhana — buat pemanasan)
- [ ] `BookService` (BR-13, BR-14, BR-17, BR-21)
- [ ] `MemberService` (BR-15, BR-16)
- [ ] `LoanService` (BR-01 s/d BR-12 — inti proyek, kerjakan `BorrowAsync` dulu)
- [ ] `FineService` (BR-20, BR-22)
- [ ] `DashboardService`
- [ ] **Checkpoint:** setiap penegakan aturan sudah diberi komentar `// BR-xx`

### Fase 4 — UI
- [ ] 6 komponen `Components/Shared/`
- [ ] `NavMenu` diperbarui (Dashboard, Buku, Anggota, Peminjaman, Denda, Lookup)
- [ ] Halaman Lookups → Books → Members → Loans → Fines → Dashboard
- [ ] **Checkpoint:** semua AC di bagian 13 lolos dicoba manual

### Fase 5 — Opsional
- [ ] `[Timestamp] public byte[] RowVersion` di `BookCopy` — mencegah dua petugas meminjam kopi yang sama
      bersamaan (race condition di BR-05), lalu tangani `DbUpdateConcurrencyException`
- [ ] Project test `LibraryAppPrototype.Tests` (xUnit) — pakai `UseInMemoryDatabase` atau SQL Server LocalDB
      (baca bagian 17 dulu: di arsitektur ini testnya memang lebih ribet daripada di `PRD.md`)
- [ ] Autentikasi ASP.NET Core Identity (`-au Individual` kalau mulai dari awal)
- [ ] Export daftar keterlambatan ke CSV
- [ ] Migrasi ke Clean Architecture — pakai `PRD.md` sebagai target, dan rasakan bedanya

---

## 13. Kriteria Penerimaan (Acceptance Criteria)

Proyek dianggap **selesai** jika semua skenario berikut lolos. AC-01 s/d AC-15 identik dengan `PRD.md`
supaya hasil dua arsitektur bisa dibandingkan apple-to-apple.

| # | Skenario | Hasil yang diharapkan |
|---|---|---|
| AC-01 | Anggota aktif tanpa denda meminjam buku yang tersedia | Pinjaman dibuat, `DueDate = hari ini + 7`, kopi jadi `OnLoan` |
| AC-02 | Anggota yang sudah punya 3 pinjaman aktif meminjam lagi | Ditolak, pesan menyebut batas 3 buku (BR-01) |
| AC-03 | Anggota dengan denda `Unpaid` meminjam | Ditolak, pesan menyebut nominal denda tertunggak (BR-03) |
| AC-04 | Anggota `Suspended` meminjam | Ditolak (BR-04) |
| AC-05 | Meminjam buku yang semua kopinya `OnLoan` | Ditolak (BR-05) |
| AC-06 | Mengembalikan tepat waktu | Tidak ada denda, kopi jadi `Available` |
| AC-07 | Mengembalikan telat 3 hari | Denda Rp 3.000 tercatat `Unpaid` (BR-06) |
| AC-08 | Memperpanjang pinjaman aktif belum jatuh tempo | `DueDate` +7 hari, `RenewalCount` = 1 |
| AC-09 | Memperpanjang kedua kali | Ditolak (BR-07) |
| AC-10 | Memperpanjang pinjaman yang sudah telat | Ditolak (BR-08) |
| AC-11 | Menghapus buku yang punya pinjaman aktif | Ditolak (BR-17) |
| AC-12 | Input ISBN tidak valid | Ditolak dengan pesan validasi (BR-13) |
| AC-13 | Daftar `InventoryCode` duplikat | Ditolak (BR-14) |
| AC-14 | Daftar email duplikat | Ditolak (BR-16) |
| AC-15 | Membayar denda | Status jadi `Paid`, `PaidAt` terisi, anggota bisa meminjam lagi |
| AC-16 | Menandai pinjaman sebagai buku hilang | Kopi jadi `Lost`, pinjaman jadi `Lost`, denda `ReplacementCost` terbit `Unpaid` (BR-12/BR-23) |
| AC-17 | Mengubah status kopi yang sedang `OnLoan` | Ditolak (BR-21) |
| AC-18 | Menghapuskan denda tanpa mengisi alasan | Ditolak, form menampilkan pesan validasi (BR-20) |
| AC-19 | Menambah penulis dengan nama yang sudah ada | Ditolak dengan pesan ramah, bukan stack trace (FR-26) |
| AC-20 | Input ISBN sama dengan tanda hubung berbeda (`978-0-306-40615-7` vs `9780306406157`) | Ditolak sebagai duplikat (BR-13) |
| AC-21 | Filter `Overdue` di `/loans` | Menampilkan pinjaman `Active` yang lewat jatuh tempo; enum `LoanStatus` tetap tidak punya nilai `Overdue` (BR-19) |
| AC-22 | Buka dua tab, ubah data anggota di tab A, muat ulang daftar di tab B | Tab B menampilkan data terbaru — bukti `DbContext` tidak berumur circuit (bagian 11.1) |
| AC-23 | Klik tombol "Pinjam" dua kali dengan cepat | Tidak ada `InvalidOperationException`; tombol ter-disable oleh flag `_busy` (bagian 10.1) |
| AC-24 | Cek kebocoran layer | Tidak ada `.razor` yang meng-inject `AppDbContext`/`IDbContextFactory`; tidak ada `if` aturan bisnis di blok `@code` |

---

## 14. Aturan untuk AI Agent yang Bekerja di Repo Ini

### 14.1 Wajib

1. **Baca `PRD-simple.md` ini sebelum menulis kode apapun.** Jangan menerapkan aturan dari `PRD.md` —
   itu dokumen untuk proyek yang berbeda dengan arsitektur yang berbeda.
2. Kerjakan **sesuai urutan fase** di bagian 12.
3. Taruh setiap file di **folder yang ditentukan** bagian 4. Jangan bikin folder baru tanpa alasan.
4. Setiap kode yang menegakkan aturan bisnis **wajib** diberi komentar ID-nya, contoh `// BR-01`.
5. Setiap method service dibuka dengan `await using var db = await dbFactory.CreateDbContextAsync(ct);`
   — satu `DbContext` untuk satu operasi (bagian 11.1).
6. Service mengembalikan `OperationResult` / `OperationResult<T>` untuk semua aksi yang bisa ditolak,
   dengan `Code` diisi ID aturan.
7. Nama file = nama class, satu class publik per file. Pengecualian yang diizinkan hanya tiga:
   `Enums.cs`, `OperationResult.cs`, `Clock.cs`.
8. Pakai `DateOnly` untuk tanggal domain (bukan `DateTime`), dan ambil "hari ini" **hanya** dari `IClock`.
9. Semua nilai kebijakan pinjam dirujuk dari `LoanPolicy`, jangan ditulis sebagai literal.
10. Pakai Bootstrap 5 lokal untuk semua styling.
11. Komentar & pesan error yang dilihat pengguna ditulis dalam **Bahasa Indonesia**;
    nama class/method/variabel dalam **Bahasa Inggris**.
12. Setiap tombol aksi punya flag `_busy` untuk mencegah klik ganda (bagian 10.1).

### 14.2 Dilarang

1. Memakai `services.AddDbContext<AppDbContext>(...)`. Yang benar `AddDbContextFactory` (bagian 11.1).
2. Inject `AppDbContext` atau `IDbContextFactory` ke Razor component.
3. Menulis query LINQ ke database di dalam `.razor`.
4. Menaruh aturan bisnis di entity, di `AppDbContext`, atau di `.razor`. Semua di `Services/`.
5. Memakai `DateTime.Now` / `DateTime.UtcNow` / `DateTime.Today` di mana pun kecuali `SystemClock.cs`.
6. Memanggil service dari dalam service lain (bagian 11.4).
7. Menambahkan nilai `Overdue` ke enum `LoanStatus` (BR-19). Filter UI memakai `LoanFilter`.
8. Memakai CDN untuk Bootstrap/ikon.
9. Mengubah nilai aturan bisnis (misal batas 3 buku jadi 5) tanpa memperbarui dokumen ini.
10. Menambahkan library baru (AutoMapper, MediatR, Tailwind, FluentValidation, dsb.) tanpa persetujuan —
    proyek ini sengaja manual demi pembelajaran.
11. **Menambahkan project baru, interface repository, atau folder `Domain`/`Application`.** Kalau merasa
    proyek ini "butuh" itu, jangan diam-diam menambahkannya — tulis alasannya ke pemilik proyek dan
    rujuk bagian 17.

### 14.3 Cara memverifikasi arsitektur masih waras

```bash
# DbContext HARUS dibuat lewat factory
grep -rn "AddDbContext<" .   # harus kosong (yang benar: AddDbContextFactory<)

# Komponen tidak boleh menyentuh database
grep -rn "AppDbContext\|IDbContextFactory" Components/   # harus kosong

# Tidak ada DateTime.Now selain di Clock
grep -rn "DateTime.Now\|DateTime.UtcNow\|DateTime.Today" --include=*.cs --include=*.razor .   # hanya Clock.cs

# LoanStatus tidak boleh punya Overdue
grep -rn "Overdue" Data/   # harus kosong (Overdue hanya ada di Models/, Services/, Components/)

# Angka kebijakan tidak boleh jadi literal
grep -rn "AddDays(7)\|>= 3\|1000m\|1_000m" Services/ Components/   # hanya boleh di LoanPolicy.cs

# Service tidak boleh saling panggil
grep -rn "BookService\|MemberService\|LoanService\|FineService" Services/   # hanya di deklarasi class-nya sendiri
```

---

## 15. Glosarium

| Istilah | Arti dalam proyek ini |
|---|---|
| **Book** | Judul buku (data bibliografi). Bukan benda fisik. |
| **BookCopy** | Eksemplar fisik dengan kode inventaris. Yang dipinjam adalah ini. |
| **Member** | Anggota perpustakaan yang berhak meminjam. |
| **Loan** | Satu transaksi peminjaman: satu member, satu `BookCopy`. |
| **Fine** | Denda, timbul dari keterlambatan, kehilangan, atau kerusakan. |
| **Service** | Satu class per modul, berisi seluruh aturan bisnis modul itu. |
| **Entity** | Class POCO yang dipetakan ke tabel. Sengaja tanpa logika bisnis (anemic). |
| **Anemic Domain Model** | Entity yang cuma berisi property. Di proyek ini disengaja; di `PRD.md` justru dilarang. |
| **DataAnnotations** | Atribut di entity yang dipakai dua kali: menentukan skema DB, dan memvalidasi `EditForm`. |
| **`OperationResult`** | Cara service melaporkan penolakan aturan bisnis tanpa melempar exception. |
| **Render mode** | Cara Blazor menjalankan komponen. Proyek ini memakai `InteractiveServer`. |
| **Circuit** | Koneksi SignalR antara browser dan server di Blazor Server. Umurnya = umur tab. |

---

## 16. Perbandingan dengan `PRD.md` (Clean Architecture)

Fitur, aturan bisnis, dan skema database dua proyek ini **sama**. Yang berbeda cuma cara menatanya.

| Aspek | `PRD.md` (Clean Architecture) | `PRD-simple.md` (proyek ini) |
|---|---|---|
| Jumlah project | 4 (`Domain`, `Application`, `Infrastructure`, `Web`) | **1** |
| Jumlah file | ± 159 | **± 51** |
| Perintah migration | `--project` + `--startup-project` | `dotnet ef migrations add X` |
| Aturan bisnis tinggal di | Entity + Value Object + Domain Service | **Service** |
| Model domain | Rich (`loan.Return(today)`) | **Anemic** (`LoanService.ReturnAsync(id)`) |
| Batas layer | DTO — entity tidak pernah bocor ke Razor | **Tidak ada DTO** — entity langsung dipakai di Razor |
| Akses data | `IUnitOfWork` + 6 repository interface | **`IDbContextFactory` langsung di service** |
| Validasi | Value Object (`Isbn`, `Money`, `EmailAddress`) | **DataAnnotations + helper statis** |
| Error handling | `Result<T>` + `Error` + `DomainException.RuleId` | **`OperationResult`** |
| Konfigurasi EF | `IEntityTypeConfiguration` terpisah per entity | **DataAnnotations + satu `OnModelCreating`** |
| Soft delete | Ya (`IsDeleted` + query filter) | **Tidak** (hard delete, dijaga BR-17) |
| Abstraksi waktu | `IDateTimeProvider` | `IClock` (**sama konsepnya**) |
| Lifetime `DbContext` | `AddDbContextFactory` | `AddDbContextFactory` (**sama persis**) |
| Uji aturan bisnis tanpa DB | Mudah — `Loan` murni C# | **Sulit** — butuh `DbContext` (lihat 17) |
| Waktu sampai layar pertama jalan | Lama (Domain dulu) | **Cepat** (Fase 2 sudah kelihatan) |
| Biaya menambah 1 fitur CRUD | ~6 file (DTO in/out, handler, repo method, page) | **~2 file** (method di service, page) |

**Yang identik di kedua dokumen** (dan itu bukan kebetulan — ini hal yang benar terlepas dari arsitektur):

- `AddDbContextFactory` di Blazor Server (bagian 11.1 di dua dokumen)
- Abstraksi waktu supaya aturan yang bergantung tanggal bisa diuji
- `Overdue` sebagai status turunan, bukan kolom (BR-19)
- ISBN disimpan ternormalisasi supaya unique index tidak bocor (BR-13)
- Aturan bisnis **tidak boleh** ada di Razor component
- Pesan error membawa ID aturan

---

## 17. Kapan Pendekatan Ini Mulai Jadi Beban

Pendekatan single-project ini **bukan versi jelek** dari Clean Architecture — dia pilihan yang tepat untuk
skala tertentu. Tapi dia punya titik patah. Berikut sinyal-sinyal konkret, urut dari yang paling awal muncul:

| Sinyal | Artinya | Yang bisa dilakukan |
|---|---|---|
| Satu service tembus 300 baris | Terlalu banyak tanggung jawab dalam satu class | Pecah jadi `partial class` per aksi |
| Aturan yang sama ditulis di dua service | Logika mulai terduplikasi — sumber bug klasik | Tarik ke method statis, atau ke entity |
| Mau menulis unit test tapi harus setup `DbContext` dulu | Aturan bisnis terjerat akses data | Tarik perhitungan murni ke method entity (seperti `IsOverdue`) |
| Ada `if (member.Status == ...)` di dalam `.razor` | UI mulai mengambil alih aturan | Kembalikan ke service **sekarang juga** — ini pelanggaran 14.2.4 |
| Butuh mengganti SQL Server ke database lain | Service terikat langsung ke EF Core | Baru di sini repository pattern jadi masuk akal |
| Tim bertambah dan orang saling menabrak di file yang sama | Batas modul tidak jelas | Pisahkan per-fitur dulu (folder `Features/Loans/`), belum perlu 4 project |
| Aturan bisnis mulai punya cabang kondisi berlapis | Anemic model sudah tidak sanggup | Mulai pindahkan state + invariant ke entity → pelan-pelan jadi rich model |

**Aturan praktisnya:** selama aplikasi ini dikerjakan sendiri, ukurannya di bawah ~60 file, dan databasenya
tidak akan diganti — pendekatan ini menang telak. Begitu dua dari tujuh sinyal di atas muncul bersamaan,
baca ulang `PRD.md` dan pertimbangkan migrasi bertahap (mulai dari mengekstrak `Domain`, bukan langsung
empat project sekaligus).

Yang **tidak boleh** terjadi: menerapkan setengah-setengah. Punya `IBookRepository` tapi service masih
memanggil `db.Books` langsung, atau punya folder `Domain/` tapi aturan bisnisnya masih di service — itu
lebih buruk daripada dua-duanya.

---

## 18. Changelog

### v1.0 — 2026-08-21

Dokumen awal. Dibuat sebagai pasangan pembanding untuk `PRD.md`: domain, aturan bisnis (BR-01 s/d BR-23),
dan kebutuhan fungsional (FR-01 s/d FR-27) dibuat identik, sementara arsitekturnya sengaja dibalik menjadi
single project tanpa layering.
