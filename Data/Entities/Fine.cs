using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LibraryAppPrototype.Data.Entities;

public class Fine
{
    public int Id { get; set; }

    public int LoanId { get; set; }
    public Loan Loan { get; set; } = null!;

    // Denormalisasi: disimpan supaya query denda per anggota tidak perlu join ke Loans.
    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;

    // BR-18
    [Range(0, 99999999)]
    [Precision(18, 2)]
    public decimal Amount { get; set; }

    public FineReason Reason { get; set; }

    public DateOnly IssuedAt { get; set; }

    public DateOnly? PaidAt { get; set; }

    public FineStatus Status { get; set; }

    // BR-20: wajib diisi kalau Status == Waived.
    [MaxLength(300)]
    public string? WaiveReason { get; set; }
}
