namespace ContactList.Models;

public class DbContactRepository : IContactRepository
{
    private readonly ContactDbContext _db;

    public DbContactRepository(ContactDbContext db)
    {
        _db = db;
    }

    public IEnumerable<Contact> GetAll() => _db.Contacts.OrderBy(c => c.Name).ToList();

    public Contact? GetById(int id) => _db.Contacts.Find(id);

    public void Add(Contact contact)
    {
        _db.Contacts.Add(contact);
        _db.SaveChanges();
    }

    public void Update(Contact contact)
    {
        var existing = _db.Contacts.Find(contact.Id);
        if (existing != null)
        {
            existing.Name = contact.Name;
            existing.Email = contact.Email;
            existing.Phone = contact.Phone;
            existing.Category = contact.Category;
            existing.Notes = contact.Notes;
            _db.SaveChanges();
        }
    }

    public void Remove(int id)
    {
        var contact = _db.Contacts.Find(id);
        if (contact != null)
        {
            _db.Contacts.Remove(contact);
            _db.SaveChanges();
        }
    }

    public IEnumerable<string> GetCategories() => Contact.GetCategories();
}
