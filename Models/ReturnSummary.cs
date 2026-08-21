using LibraryAppPrototype.Data.Entities;

namespace LibraryAppPrototype.Models;

// Hasil LoanService.ReturnAsync: telat berapa hari, denda berapa.
// IsLate/FineAmount sudah dihitung di service (BR-06) — UI cukup menampilkan.
public class ReturnSummary
{
    public required Loan Loan { get; init; }
    public DateOnly ReturnedAt { get; init; }
    public int DaysLate { get; init; }

    // null kalau tidak telat — tidak ada denda yang diterbitkan.
    public Fine? Fine { get; init; }

    public bool IsLate => DaysLate > 0;
    public decimal FineAmount => Fine?.Amount ?? 0m;
}
