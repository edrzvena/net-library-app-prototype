using LibraryAppPrototype.Data;
using LibraryAppPrototype.Data.Entities;
using LibraryAppPrototype.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryAppPrototype.Services;

public class MemberService(IDbContextFactory<AppDbContext> dbFactory, IClock clock)
{
    // BR-15: bentrok nomor urut ditangani dengan retry saat unique index melempar.
    private const int RegisterRetryCount = 5;

    // FR-12: cari by nama/kode/email dengan paging.
    public async Task<PagedList<Member>> SearchAsync(
        string? keyword = null,
        MemberStatus? status = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var query = db.Members.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(m =>
                m.FullName.Contains(k) ||
                m.Code.Contains(k) ||
                m.Email.Contains(k));
        }

        if (status is not null)
            query = query.Where(m => m.Status == status);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(m => m.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedList<Member> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<Member?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    // FR-09. Menegakkan BR-15 (kode otomatis) dan BR-16 (email unik, disimpan lowercase).
    public async Task<OperationResult<int>> RegisterAsync(Member member, CancellationToken ct = default)
    {
        // BR-16
        var email = (member.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (email.Length == 0)
            return OperationResult<int>.Fail("VALIDATION", "Email wajib diisi.");

        for (var attempt = 1; attempt <= RegisterRetryCount; attempt++)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            // BR-16: dicek di sini supaya pesannya ramah (AC-14); unique index tetap jadi jaring terakhir.
            if (await db.Members.AnyAsync(m => m.Email == email, ct))
                return OperationResult<int>.Fail("BR-16", $"Email {email} sudah dipakai anggota lain.");

            var entity = new Member
            {
                // BR-15: kode SELALU dibuat service. Nilai apa pun yang datang dari form diabaikan.
                Code = await NextMemberCodeAsync(db, clock.Today.Year, ct),
                FullName = (member.FullName ?? string.Empty).Trim(),
                Email = email,
                PhoneNumber = member.PhoneNumber?.Trim(),
                Address = member.Address?.Trim(),
                // JoinedAt juga milik service, bukan form.
                JoinedAt = clock.Today,
                Status = MemberStatus.Active
            };

            db.Members.Add(entity);

            try
            {
                await db.SaveChangesAsync(ct);
                return OperationResult<int>.Ok(entity.Id);
            }
            catch (DbUpdateException) when (attempt < RegisterRetryCount)
            {
                // BR-15: dua petugas mendaftar bersamaan dan mendapat nomor urut yang sama.
                // Unique index menolak salah satunya — ulangi, nomor berikutnya akan berbeda.
            }
        }

        return OperationResult<int>.Fail("CONFLICT",
            "Gagal membuat kode anggota karena bentrok berulang. Coba lagi.");
    }

    // FR-10. BR-16 berlaku lagi karena email boleh diubah.
    public async Task<OperationResult> UpdateAsync(Member member, CancellationToken ct = default)
    {
        var email = (member.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (email.Length == 0)
            return OperationResult.Fail("VALIDATION", "Email wajib diisi.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var existing = await db.Members.FirstOrDefaultAsync(m => m.Id == member.Id, ct);
        if (existing is null)
            return OperationResult.Fail("NOT_FOUND", "Anggota tidak ditemukan.");

        // BR-16
        if (await db.Members.AnyAsync(m => m.Email == email && m.Id != member.Id, ct))
            return OperationResult.Fail("BR-16", $"Email {email} sudah dipakai anggota lain.");

        // Code, JoinedAt, dan Status TIDAK ikut diubah dari form — service yang pegang.
        existing.FullName = (member.FullName ?? string.Empty).Trim();
        existing.Email = email;
        existing.PhoneNumber = member.PhoneNumber?.Trim();
        existing.Address = member.Address?.Trim();

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    // FR-11
    public Task<OperationResult> SuspendAsync(int id, CancellationToken ct = default) =>
        SetStatusAsync(id, MemberStatus.Suspended, ct);

    public Task<OperationResult> ReactivateAsync(int id, CancellationToken ct = default) =>
        SetStatusAsync(id, MemberStatus.Active, ct);

    // Dipakai UI untuk menampilkan sisa kuota; penegakan BR-01 tetap di LoanService.BorrowAsync.
    public async Task<int> GetActiveLoanCountAsync(int memberId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Loans.CountAsync(l => l.MemberId == memberId && l.Status == LoanStatus.Active, ct);
    }

    // Dipakai UI untuk FR-13; penegakan BR-03 tetap di LoanService.BorrowAsync.
    public async Task<decimal> GetOutstandingFineAsync(int memberId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Fines
            .Where(f => f.MemberId == memberId && f.Status == FineStatus.Unpaid)
            .SumAsync(f => (decimal?)f.Amount, ct) ?? 0m;
    }

    private async Task<OperationResult> SetStatusAsync(int id, MemberStatus status, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var member = await db.Members.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (member is null)
            return OperationResult.Fail("NOT_FOUND", "Anggota tidak ditemukan.");

        member.Status = status;
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    // BR-15: format MBR-{YYYY}-{00000}, nomor urut = MAX(urutan tahun ini) + 1.
    private static async Task<string> NextMemberCodeAsync(AppDbContext db, int year, CancellationToken ct)
    {
        var prefix = $"MBR-{year}-";

        // Lebar 5 digit, jadi urutan string = urutan angka selama belum tembus 99999 anggota per tahun.
        var last = await db.Members
            .Where(m => m.Code.StartsWith(prefix))
            .OrderByDescending(m => m.Code)
            .Select(m => m.Code)
            .FirstOrDefaultAsync(ct);

        var next = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n))
            next = n + 1;

        return $"{prefix}{next:00000}";
    }
}
