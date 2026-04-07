using Microsoft.EntityFrameworkCore;

namespace ContactList.Models;

public class ContactDbContext : DbContext
{
    public ContactDbContext(DbContextOptions<ContactDbContext> options) : base(options) { }

    public DbSet<Contact> Contacts => Set<Contact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contact>().HasData(
            new Contact { Id = 1, Name = "Alice Smith", Email = "alice@example.com",
                Phone = "503-555-0101", Category = "Work", Notes = "Project manager" },
            new Contact { Id = 2, Name = "Bob Jones", Email = "bob@example.com",
                Phone = "503-555-0102", Category = "Friend" },
            new Contact { Id = 3, Name = "Carol White", Email = "carol@example.com",
                Phone = "503-555-0103", Category = "Family", Notes = "Sister" }
        );
    }
}
