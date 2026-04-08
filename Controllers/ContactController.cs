using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using ContactList.Models;
using StarFederation.Datastar.DependencyInjection;

namespace ContactList.Controllers;

public class ContactController : Controller
{
    private readonly IContactRepository _repo;
    private readonly ICompositeViewEngine _viewEngine;

    public ContactController(IContactRepository repo, ICompositeViewEngine viewEngine)
    {
        _repo = repo;
        _viewEngine = viewEngine;
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
        await PatchContactTable(dss, _repo.GetAll());
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

        await PatchContactTable(dss, contacts);
    }

    // POST: /Contact/Validate — SSE endpoint for inline validation as the user types
    [HttpPost]
    public async Task Validate([FromServices] IDatastarService dss)
    {
        var contact = await BuildContactFromSignals(dss);
        var errors = ContactValidator.GetErrors(contact);

        await dss.PatchSignalsAsync(BuildErrorSignals(errors));
    }

    // POST: /Contact/Create — SSE endpoint that adds a contact and refreshes the list
    [HttpPost]
    public async Task Create([FromServices] IDatastarService dss)
    {
        var contact = await BuildContactFromSignals(dss);
        var errors = ContactValidator.GetErrors(contact);

        if (errors.Count > 0)
        {
            await dss.PatchSignalsAsync(BuildErrorSignals(errors));
            return;
        }

        _repo.Add(contact);

        // Send back the updated contact list
        await PatchContactTable(dss, _repo.GetAll());

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

    // Map validation errors to Datastar signal names
    private static object BuildErrorSignals(Dictionary<string, string> errors) => new
    {
        nameError = errors.GetValueOrDefault("Name", ""),
        emailError = errors.GetValueOrDefault("Email", ""),
        phoneError = errors.GetValueOrDefault("Phone", ""),
        categoryError = errors.GetValueOrDefault("Category", "")
    };

    // GET: /Contact/Edit/3 — SSE endpoint that shows the inline edit form for a contact
    [HttpGet]
    public async Task Edit(int id, [FromServices] IDatastarService dss)
    {
        var contact = _repo.GetById(id);
        if (contact == null)
        {
            await PatchContactTable(dss, _repo.GetAll());
            return;
        }

        await PatchContactTable(dss, _repo.GetAll(), editingId: id);

        // Pre-fill the edit form signals with the contact's current values
        await dss.PatchSignalsAsync(new
        {
            editId = contact.Id,
            editName = contact.Name,
            editEmail = contact.Email ?? "",
            editPhone = contact.Phone ?? "",
            editCategory = contact.Category,
            editNotes = contact.Notes ?? "",
            editNameError = "",
            editEmailError = "",
            editPhoneError = "",
            editCategoryError = ""
        });
    }

    // POST: /Contact/ValidateEdit — SSE endpoint for inline validation while editing
    [HttpPost]
    public async Task ValidateEdit([FromServices] IDatastarService dss)
    {
        var contact = await BuildEditContactFromSignals(dss);
        var errors = ContactValidator.GetErrors(contact);

        await dss.PatchSignalsAsync(BuildEditErrorSignals(errors));
    }

    // POST: /Contact/Update/3 — SSE endpoint that validates, updates, and refreshes the list
    [HttpPost]
    public async Task Update(int id, [FromServices] IDatastarService dss)
    {
        var contact = await BuildEditContactFromSignals(dss);
        contact.Id = id;
        var errors = ContactValidator.GetErrors(contact);

        if (errors.Count > 0)
        {
            await dss.PatchSignalsAsync(BuildEditErrorSignals(errors));
            return;
        }

        _repo.Update(contact);

        await PatchContactTable(dss, _repo.GetAll());

        // Clear edit signals
        await dss.PatchSignalsAsync(new
        {
            editId = 0,
            editName = "",
            editEmail = "",
            editPhone = "",
            editCategory = "",
            editNotes = "",
            editNameError = "",
            editEmailError = "",
            editPhoneError = "",
            editCategoryError = ""
        });
    }

    // GET: /Contact/CancelEdit — SSE endpoint that returns to normal table view
    [HttpGet]
    public async Task CancelEdit([FromServices] IDatastarService dss)
    {
        await PatchContactTable(dss, _repo.GetAll());

        await dss.PatchSignalsAsync(new
        {
            editId = 0,
            editName = "",
            editEmail = "",
            editPhone = "",
            editCategory = "",
            editNotes = "",
            editNameError = "",
            editEmailError = "",
            editPhoneError = "",
            editCategoryError = ""
        });
    }

    // DELETE: /Contact/Delete/3 — SSE endpoint that removes a contact
    [HttpDelete]
    public async Task Delete(int id, [FromServices] IDatastarService dss)
    {
        _repo.Remove(id);

        await PatchContactTable(dss, _repo.GetAll());
    }

    // Build a Contact from the edit form Datastar signals
    private async Task<Contact> BuildEditContactFromSignals(IDatastarService dss)
    {
        var signals = await dss.ReadSignalsAsync<EditContactSignals>();
        return new Contact
        {
            Id = signals.EditId,
            Name = signals.EditName ?? "",
            Email = signals.EditEmail,
            Phone = signals.EditPhone,
            Category = signals.EditCategory ?? "",
            Notes = signals.EditNotes
        };
    }

    // Map validation errors to edit form Datastar signal names
    private static object BuildEditErrorSignals(Dictionary<string, string> errors) => new
    {
        editNameError = errors.GetValueOrDefault("Name", ""),
        editEmailError = errors.GetValueOrDefault("Email", ""),
        editPhoneError = errors.GetValueOrDefault("Phone", ""),
        editCategoryError = errors.GetValueOrDefault("Category", "")
    };

    // Render the contact table partial and patch it into the page via SSE
    private async Task PatchContactTable(IDatastarService dss, IEnumerable<Contact> contacts, int? editingId = null)
    {
        if (editingId.HasValue)
        {
            ViewData["EditingId"] = editingId.Value;
        }
        var html = await RenderPartialToString("_ContactTable", contacts);
        await dss.PatchElementsAsync(html);
    }

    // Render a partial view to an HTML string for SSE patching
    private async Task<string> RenderPartialToString(string viewName, object model)
    {
        ViewData.Model = model;
        using var writer = new StringWriter();
        var viewResult = _viewEngine.FindView(ControllerContext, viewName, false);
        var viewContext = new ViewContext(
            ControllerContext, viewResult.View, ViewData, TempData, writer, new HtmlHelperOptions());
        await viewResult.View.RenderAsync(viewContext);
        return writer.ToString();
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

public class EditContactSignals
{
    public int EditId { get; set; }
    public string? EditName { get; set; }
    public string? EditEmail { get; set; }
    public string? EditPhone { get; set; }
    public string? EditCategory { get; set; }
    public string? EditNotes { get; set; }
}
