using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Data.SqlClient;

namespace ContactList.Tests;

internal sealed class TestAppHost : IDisposable
{
    private const string TestDatabaseName = "ContactList_Test";

    private static readonly string MasterConnectionString =
        "Server=localhost,1433;Database=master;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true";

    private static readonly string TestConnectionString =
        $"Server=localhost,1433;Database={TestDatabaseName};User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true";

    private readonly Process _process;
    private readonly string _baseUrl;
    private readonly StringBuilder _output = new();
    private bool _disposed;

    private TestAppHost()
    {
        ResetTestDatabase();

        var projectRoot = GetProjectRoot();
        var port = GetOpenPort();
        _baseUrl = $"http://127.0.0.1:{port}";

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = projectRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(Path.Combine(projectRoot, "ContactList-Datastar.csproj"));
        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add(_baseUrl);
        startInfo.ArgumentList.Add("--ConnectionStrings:ContactDb");
        startInfo.ArgumentList.Add(TestConnectionString);

        _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _process.OutputDataReceived += (_, e) => AppendOutput(e.Data);
        _process.ErrorDataReceived += (_, e) => AppendOutput(e.Data);

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        WaitForServerReady().GetAwaiter().GetResult();
    }

    public static string BaseUrl => Instance.Value._baseUrl;

    private static readonly Lazy<TestAppHost> Instance = new(() => new TestAppHost());

    public static void Start() => _ = Instance.Value;

    public static void Stop()
    {
        if (Instance.IsValueCreated)
        {
            Instance.Value.Dispose();
        }
    }

    /// <summary>
    /// Resets the Contacts table to the original seed data between scenarios.
    /// </summary>
    public static void ResetData()
    {
        using var connection = new SqlConnection(TestConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM Contacts;
            SET IDENTITY_INSERT Contacts ON;
            INSERT INTO Contacts (Id, Name, Email, Phone, Category, Notes) VALUES
                (1, 'Alice Smith', 'alice@example.com', '503-555-0101', 'Work', 'Project manager'),
                (2, 'Bob Jones', 'bob@example.com', '503-555-0102', 'Friend', NULL),
                (3, 'Carol White', 'carol@example.com', '503-555-0103', 'Family', 'Sister');
            SET IDENTITY_INSERT Contacts OFF;
            """;
        command.ExecuteNonQuery();
    }

    private static void ResetTestDatabase()
    {
        using var connection = new SqlConnection(MasterConnectionString);
        connection.Open();

        // Drop the test database if it exists, then create a fresh one.
        // Setting SINGLE_USER forces any lingering connections closed.
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID('{TestDatabaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{TestDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{TestDatabaseName}];
            END
            """;
        command.ExecuteNonQuery();

        // The app's startup will run EF Core Migrate(), which creates the DB and seeds data.
    }

    private static string GetProjectRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static int GetOpenPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private void AppendOutput(string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (_output)
        {
            _output.AppendLine(line);
        }
    }

    private async Task WaitForServerReady()
    {
        using var client = new HttpClient();
        var timeoutAt = DateTime.UtcNow.AddSeconds(20);

        while (DateTime.UtcNow < timeoutAt)
        {
            if (_process.HasExited)
            {
                throw new InvalidOperationException(
                    $"The application exited before it became ready.{Environment.NewLine}{GetOutput()}");
            }

            try
            {
                using var response = await client.GetAsync($"{_baseUrl}/Contact");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(200);
        }

        throw new TimeoutException(
            $"Timed out waiting for the application to start at {_baseUrl}.{Environment.NewLine}{GetOutput()}");
    }

    private string GetOutput()
    {
        lock (_output)
        {
            return _output.ToString();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit();
        }

        _process.Dispose();
    }
}
