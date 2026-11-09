using System.Diagnostics;
using System.Net.Sockets;

namespace NovaBank.Configuration;

/// <summary>
/// Starts the peer bank next to this one, and stops it again on the way out.
/// Only a process this class started is ever killed.
/// </summary>
public sealed class PeerLauncher : IDisposable
{
    private readonly BankConfig _config;

    private Process? _started;

    public PeerLauncher(BankConfig config)
    {
        _config = config;
    }

    public void Start()
    {
        if (IsListening(_config.PeerAddress))
        {
            Console.WriteLine($"[{_config.Name}] {_config.PeerName} is already running.");

            return;
        }

        string project = _config.ResolvePeerProject();

        if (!File.Exists(project))
        {
            Console.WriteLine(
                $"[{_config.Name}] no peer project at {project}; start it yourself.");

            return;
        }

        // dotnet run builds the peer if it is stale, then starts it. The -- separates
        // dotnet's arguments from ours. UseShellExecute gives it its own console
        // window, so both ATMs stay usable.
        _started = Process.Start(new ProcessStartInfo("dotnet",
            $"run --project \"{project}\" -- " +
            $"--own {_config.PeerAddress} --peer {_config.Address} --secret {_config.Secret}")
        {
            WorkingDirectory = Path.GetDirectoryName(project)!,
            UseShellExecute = true
        });

        Console.WriteLine($"[{_config.Name}] starting {_config.PeerName} at {_config.PeerAddress} ...");

        // dotnet may need a few seconds to build it before the port answers.
        for (int attempt = 0; attempt < 120 && !IsListening(_config.PeerAddress); attempt++)
        {
            Thread.Sleep(250);
        }

        Console.WriteLine(IsListening(_config.PeerAddress)
            ? $"[{_config.Name}] {_config.PeerName} is up."
            : $"[{_config.Name}] {_config.PeerName} is not answering yet.");
    }

    public void Dispose()
    {
        // dotnet run is the parent process: kill the tree or the bank outlives us.
        if (_started is { HasExited: false })
        {
            _started.Kill(entireProcessTree: true);
        }
    }

    private static bool IsListening(string address)
    {
        Uri uri = new(address);

        try
        {
            using var client = new TcpClient();

            return client.ConnectAsync(uri.Host, uri.Port).Wait(TimeSpan.FromMilliseconds(400))
                && client.Connected;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
