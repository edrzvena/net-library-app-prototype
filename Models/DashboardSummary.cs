using LibraryAppPrototype.Data.Entities;

namespace LibraryAppPrototype.Models;

// FR-24 (angka ringkasan) + FR-25 (dua daftar pendek). Diisi DashboardService.
public class DashboardSummary
{
    public int TotalBooks { get; init; }
    public int TotalCopies { get; init; }
    public int AvailableCopies { get; init; }
    public int ActiveMembers { get; init; }
    public int ActiveLoans { get; init; }

    // BR-19: dihitung dari DueDate < today && ReturnedAt == null, bukan dibaca dari kolom status.
    public int OverdueLoans { get; init; }

    public decimal OutstandingFineTotal { get; init; }

    // FR-25
    public IReadOnlyList<Loan> RecentLoans { get; init; } = [];
    public IReadOnlyList<Loan> WorstOverdueLoans { get; init; } = [];
}
