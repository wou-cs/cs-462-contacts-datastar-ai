using Microsoft.AspNetCore.Mvc;
using ContactList.Models;
using StarFederation.Datastar.DependencyInjection;

namespace ContactList.Controllers;

public class ContactController : Controller
{
    private readonly IContactRepository _repo;

    public ContactController(IContactRepository repo)
    {
        _repo = repo;
    }

    // GET: /Contact — serves the main page (regular Razor view)
    public IActionResult Index()
    {
        return View();
    }

    // GET: /Contact/List — SSE endpoint returning the contact list HTML
    [HttpGet]
    public async Task List([FromServices] IDatastarService dss)
    {
        var html = RenderContactTable(_repo.GetAll());
        await dss.PatchElementsAsync(html);
    }

    // GET: /Contact/Search — SSE endpoint returning filtered contacts
    [HttpGet]
    public async Task Search([FromServices] IDatastarService dss)
    {
        var signals = await dss.ReadSignalsAsync<SearchSignals>();
        var query = signals.Query ?? "";

        var contacts = string.IsNullOrWhiteSpace(query)
            ? _repo.GetAll()
            : _repo.GetAll().Where(c =>
                c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (c.Email != null && c.Email.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (c.Phone != null && c.Phone.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                c.Category.Contains(query, StringComparison.OrdinalIgnoreCase));

        var html = RenderContactTable(contacts);
        await dss.PatchElementsAsync(html);
    }

    // POST: /Contact/Validate — SSE endpoint for inline validation as the user types
    [HttpPost]
    public async Task Validate([FromServices] IDatastarService dss)
    {
        var contact = await BuildContactFromSignals(dss);
        var errors = GetValidationErrors(contact);

        await dss.PatchSignalsAsync(new
        {
            nameError = errors.GetValueOrDefault("Name", ""),
            emailError = errors.GetValueOrDefault("Email", ""),
            phoneError = errors.GetValueOrDefault("Phone", ""),
            categoryError = errors.GetValueOrDefault("Category", "")
        });
    }

    // POST: /Contact/Create — SSE endpoint that adds a contact and refreshes the list
    [HttpPost]
    public async Task Create([FromServices] IDatastarService dss)
    {
        var contact = await BuildContactFromSignals(dss);
        var errors = GetValidationErrors(contact);

        if (errors.Count > 0)
        {
            await dss.PatchSignalsAsync(new
            {
                nameError = errors.GetValueOrDefault("Name", ""),
                emailError = errors.GetValueOrDefault("Email", ""),
                phoneError = errors.GetValueOrDefault("Phone", ""),
                categoryError = errors.GetValueOrDefault("Category", "")
            });
            return;
        }

        _repo.Add(contact);

        // Send back the updated contact list
        var html = RenderContactTable(_repo.GetAll());
        await dss.PatchElementsAsync(html);

        // Hide the form, reset signals, and clear any validation errors
        await dss.PatchSignalsAsync(new
        {
            showForm = false,
            name = "",
            email = "",
            phone = "",
            category = "",
            notes = "",
            nameError = "",
            emailError = "",
            phoneError = "",
            categoryError = ""
        });
    }

    // Build a Contact from the current Datastar signals
    private async Task<Contact> BuildContactFromSignals(IDatastarService dss)
    {
        var signals = await dss.ReadSignalsAsync<ContactSignals>();
        return new Contact
        {
            Name = signals.Name ?? "",
            Email = signals.Email,
            Phone = signals.Phone,
            Category = signals.Category ?? "",
            Notes = signals.Notes
        };
    }

    // Validate a Contact and return a dictionary of field → error message (empty if valid)
    private Dictionary<string, string> GetValidationErrors(Contact contact)
    {
        ModelState.Clear();
        TryValidateModel(contact);
        return ModelState
            .Where(e => e.Value!.Errors.Count > 0)
            .ToDictionary(
                e => e.Key,
                e => e.Value!.Errors.First().ErrorMessage
            );
    }

    // DELETE: /Contact/Delete/3 — SSE endpoint that removes a contact
    [HttpDelete]
    public async Task Delete(int id, [FromServices] IDatastarService dss)
    {
        _repo.Remove(id);

        var html = RenderContactTable(_repo.GetAll());
        await dss.PatchElementsAsync(html);
    }

    // Render the contact table as an HTML string for SSE patching
    private static string RenderContactTable(IEnumerable<Contact> contacts)
    {
        var contactList = contacts.ToList();

        if (contactList.Count == 0)
        {
            return """<div id="contact-list"><p class="text-muted">No contacts found.</p></div>""";
        }

        var rows = string.Join("\n", contactList.Select(c => $"""
                <tr>
                    <td>{System.Net.WebUtility.HtmlEncode(c.Name)}</td>
                    <td>{System.Net.WebUtility.HtmlEncode(c.Email ?? "")}</td>
                    <td>{System.Net.WebUtility.HtmlEncode(c.Phone ?? "")}</td>
                    <td>{System.Net.WebUtility.HtmlEncode(c.Category)}</td>
                    <td>
                        <button class="btn btn-sm btn-outline-danger"
                                data-on:click="@delete('/Contact/Delete/{c.Id}')">
                            Delete
                        </button>
                    </td>
                </tr>
            """));

        return $"""
            <div id="contact-list">
                <table class="table table-striped">
                    <thead>
                        <tr>
                            <th>Name</th>
                            <th>Email</th>
                            <th>Phone</th>
                            <th>Category</th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {rows}
                    </tbody>
                </table>
            </div>
            """;
    }
}

// Signal models for Datastar deserialization
public class SearchSignals
{
    public string? Query { get; set; }
}

public class ContactSignals
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Category { get; set; }
    public string? Notes { get; set; }
}
