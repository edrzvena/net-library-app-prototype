using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryAppPrototype.Data.Entities;

public class Book
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    // BR-13: disimpan sudah ternormalisasi oleh BookService, dan unik.
    [Required]
    [MaxLength(13)]
    public string Isbn { get; set; } = string.Empty;

    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    [MaxLength(150)]
    public string? Publisher { get; set; }

    [Range(1450, 2100)]
    public int? PublishedYear { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    // BR-18
    [Range(0, 99999999)]
    [Precision(18, 2)]
    public decimal ReplacementCost { get; set; }

    // UTC, diisi BookService dari IClock.UtcNow — tanpa DEFAULT di database.
    public DateTime CreatedAt { get; set; }

    public List<BookCopy> Copies { get; set; } = [];

    // Perhitungan murni dari koleksi yang SUDAH di-Include.
    // Jangan dipakai di dalam query LINQ ke database — EF tidak bisa menerjemahkannya.
    [NotMapped] public int TotalCopies => Copies.Count;
    [NotMapped] public int AvailableCopies => Copies.Count(c => c.Status == BookCopyStatus.Available);
}
