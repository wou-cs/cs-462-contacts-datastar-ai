namespace ContactList.Models;

public interface IContactRepository
{
    IEnumerable<Contact> GetAll();
    Contact? GetById(int id);
    void Add(Contact contact);
    void Remove(int id);
    IEnumerable<string> GetCategories();
}
