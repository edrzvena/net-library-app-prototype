using System.ComponentModel.DataAnnotations;

namespace LibraryAppPrototype.Data.Entities;

public class Category
{
    public int Id { get; set; }

    [Required]
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(400)]
    public string? Description { get; set; }

    public List<Book> Books { get; set; } = [];
}
