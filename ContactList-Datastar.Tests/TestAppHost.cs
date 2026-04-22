using System.Net;
using System.Net.Sockets;
using ContactList.Controllers;
using ContactList.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarFederation.Datastar.DependencyInjection;

namespace ContactList.Tests;

/// <summary>
/// Hosts the contacts app in-process against a shared in-memory SQLite database.
/// The single SqliteConnection stays open for the lifetime of the test run —
/// closing it would discard the schema, since :memory: databases live only
/// while at least one connection is open.
/// </summary>
internal sealed class TestAppHost : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly SqliteConnection _connection;
    private readonly string _baseUrl;
    private bool _disposed;

    private TestAppHost(WebApplication app, SqliteConnection connection, string baseUrl)
    {
        _app = app;
        _connection = connection;
        _baseUrl = baseUrl;
    }

    private static readonly Lazy<TestAppHost> LazyInstance = new(() =>
        CreateAsync().GetAwaiter().GetResult());

    public static string BaseUrl => LazyInstance.Value._baseUrl;

    public static void Start() => _ = LazyInstance.Value;

    public static void Stop()
    {
        if (LazyInstance.IsValueCreated)
        {
            LazyInstance.Value.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Resets the Contacts table to the original seed data between scenarios.
    /// </summary>
    public static void ResetData()
    {
        var host = LazyInstance.Value;
        using var scope = host._app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ContactDbContext>();

        db.Database.ExecuteSqlRaw("""
            DELETE FROM Contacts;
            INSERT INTO Contacts (Id, Name, Email, Phone, Category, Notes) VALUES
                (1, 'Alice Smith', 'alice@example.com', '503-555-0101', 'Work', 'Project manager'),
                (2, 'Bob Jones', 'bob@example.com', '503-555-0102', 'Friend', NULL),
                (3, 'Carol White', 'carol@example.com', '503-555-0103', 'Family', 'Sister');
            """);
    }

    private static async Task<TestAppHost> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var port = GetOpenPort();
        var baseUrl = $"http://127.0.0.1:{port}";
        var mainAssembly = typeof(ContactController).Assembly;

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = mainAssembly.GetName().Name,
            ContentRootPath = GetProjectRoot(),
        });

        builder.WebHost.UseUrls(baseUrl);

        builder.Services
            .AddControllersWithViews()
            .AddApplicationPart(mainAssembly);

        builder.Services.AddDatastar();
        builder.Services.AddDbContext<ContactDbContext>(options =>
            options.UseSqlite(connection));
        builder.Services.AddScoped<IContactRepository, DbContactRepository>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ContactDbContext>();
            db.Database.EnsureCreated();
        }

        app.UseStaticFiles();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Contact}/{action=Index}/{id?}");

        await app.StartAsync();

        return new TestAppHost(app, connection, baseUrl);
    }

    private static string GetProjectRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static int GetOpenPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _app.StopAsync();
        await _app.DisposeAsync();
        _connection.Dispose();
    }
}
