using System.ComponentModel.DataAnnotations;

namespace LibraryAppPrototype.Data.Entities;

public class BookCopy
{
    public int Id { get; set; }

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    // BR-14: unik secara global, bukan hanya per buku.
    [Required]
    [MaxLength(40)]
    public string InventoryCode { get; set; } = string.Empty;

    public BookCopyStatus Status { get; set; }

    public DateOnly AcquiredAt { get; set; }
}
