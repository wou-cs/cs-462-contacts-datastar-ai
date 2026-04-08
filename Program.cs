using ContactList.Models;
using Microsoft.EntityFrameworkCore;
using StarFederation.Datastar.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDatastar();

var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";
var connectionString = builder.Configuration.GetConnectionString("ContactDb")
    ?? throw new InvalidOperationException("Connection string 'ContactDb' was not found.");

builder.Services.AddDbContext<ContactDbContext>(options =>
{
    if (string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
        return;
    }

    if (string.Equals(databaseProvider, "SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(connectionString);
        return;
    }

    throw new InvalidOperationException($"Unsupported database provider '{databaseProvider}'.");
});
builder.Services.AddScoped<IContactRepository, DbContactRepository>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ContactDbContext>();

    if (db.Database.IsSqlite())
    {
        db.Database.EnsureCreated();
    }
    else
    {
        db.Database.Migrate();
    }
}

app.UseStaticFiles();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Contact}/{action=Index}/{id?}");

app.Run();
