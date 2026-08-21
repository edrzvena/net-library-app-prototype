using System.ComponentModel.DataAnnotations;

namespace LibraryAppPrototype.Data.Entities;

public class Member
{
    public int Id { get; set; }

    // BR-15: format MBR-{YYYY}-{00000}, SELALU diisi MemberService — jangan di-bind di form.
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    // BR-16: disimpan lowercase, unik.
    [Required]
    [EmailAddress]
    [MaxLength(160)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(25)]
    [Phone]
    public string? PhoneNumber { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    // Diisi MemberService dari IClock.Today — jangan di-bind di form.
    public DateOnly JoinedAt { get; set; }

    public MemberStatus Status { get; set; }
}
