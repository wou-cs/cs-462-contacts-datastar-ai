using System.ComponentModel.DataAnnotations;

namespace ContactList.Models;

public class Contact
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    [Required]
    public string Category { get; set; } = string.Empty;

    public string? Notes { get; set; }

    static public IEnumerable<string> GetCategories() =>
        new[] { "Friend", "Family", "Work", "Other" };
}
