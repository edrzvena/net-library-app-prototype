using LibraryAppPrototype.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryAppPrototype.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<Fine> Fines => Set<Fine>();

    // Hanya berisi yang tidak bisa ditulis sebagai DataAnnotation:
    // index, DeleteBehavior, dan CHECK constraint. Panjang & precision ada di entity.
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Author>().HasIndex(a => a.Name).IsUnique();
        b.Entity<Category>().HasIndex(c => c.Name).IsUnique();

        b.Entity<Book>(e =>
        {
            e.HasIndex(x => x.Isbn).IsUnique();
            e.HasIndex(x => x.Title);
            e.HasIndex(x => x.CategoryId);
            e.HasOne(x => x.Author).WithMany(a => a.Books).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Category).WithMany(c => c.Books).OnDelete(DeleteBehavior.NoAction);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Books_Year", "PublishedYear IS NULL OR PublishedYear BETWEEN 1450 AND 2100");
                t.HasCheckConstraint("CK_Books_Cost", "ReplacementCost >= 0");
            });
        });

        b.Entity<BookCopy>(e =>
        {
            e.HasIndex(x => x.InventoryCode).IsUnique();
            e.HasIndex(x => new { x.BookId, x.Status });
            e.HasOne(x => x.Book).WithMany(x => x.Copies).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Member>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.FullName);
        });

        b.Entity<Loan>(e =>
        {
            e.HasIndex(x => new { x.MemberId, x.Status });
            e.HasIndex(x => new { x.DueDate, x.ReturnedAt });
            e.HasIndex(x => x.BookCopyId);
            e.HasOne(x => x.Member).WithMany().OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.BookCopy).WithMany().OnDelete(DeleteBehavior.NoAction);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Loans_DueDate", "DueDate >= BorrowedAt");
                t.HasCheckConstraint("CK_Loans_Renewal", "RenewalCount BETWEEN 0 AND 1");
            });
        });

        b.Entity<Fine>(e =>
        {
            e.HasIndex(x => new { x.MemberId, x.Status });
            e.HasOne(x => x.Loan).WithMany().OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Member).WithMany().OnDelete(DeleteBehavior.NoAction);
            e.ToTable(t => t.HasCheckConstraint("CK_Fines_Amount", "Amount >= 0"));
        });
    }
}
