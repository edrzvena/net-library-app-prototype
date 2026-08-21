namespace LibraryAppPrototype.Data.Entities;

public class Loan
{
    public int Id { get; set; }

    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public int BookCopyId { get; set; }
    public BookCopy BookCopy { get; set; } = null!;

    public DateOnly BorrowedAt { get; set; }

    // BR-02: BorrowedAt + LoanPolicy.LoanDays
    public DateOnly DueDate { get; set; }

    // null = masih dipinjam
    public DateOnly? ReturnedAt { get; set; }

    // BR-07: maksimal 1
    public int RenewalCount { get; set; }

    public LoanStatus Status { get; set; }

    // Satu-satunya "logika" yang boleh nempel di entity: perhitungan turunan
    // yang murni baca property sendiri, tanpa akses database. Lihat BR-19.
    public bool IsOverdue(DateOnly today) => ReturnedAt is null
                                          && Status == LoanStatus.Active
                                          && DueDate < today;

    public int DaysLate(DateOnly today) =>
        Math.Max(0, (ReturnedAt ?? today).DayNumber - DueDate.DayNumber);
}
