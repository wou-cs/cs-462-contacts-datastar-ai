using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ContactList.Tests;

internal sealed class TestAppHost : IDisposable
{
    private readonly Process _process;
    private readonly string _baseUrl;
    private readonly string _databasePath;
    private readonly StringBuilder _output = new();
    private bool _disposed;

    private TestAppHost()
    {
        var projectRoot = GetProjectRoot();
        var port = GetOpenPort();
        _databasePath = Path.Combine(Path.GetTempPath(), $"contacts-bdd-{Guid.NewGuid():N}.db");
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
        startInfo.ArgumentList.Add("--DatabaseProvider");
        startInfo.ArgumentList.Add("Sqlite");
        startInfo.ArgumentList.Add("--ConnectionStrings:ContactDb");
        startInfo.ArgumentList.Add($"Data Source={_databasePath}");

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

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
