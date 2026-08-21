using System.ComponentModel.DataAnnotations;

namespace LibraryAppPrototype.Data.Entities;

public class Author
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Biography { get; set; }

    public List<Book> Books { get; set; } = [];
}
