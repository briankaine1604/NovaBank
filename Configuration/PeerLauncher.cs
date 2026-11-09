using System.Diagnostics;
using System.Net.Sockets;

namespace NovaBank.Configuration;

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

        // "--" separates dotnet's arguments from ours; UseShellExecute gives the peer its own console window.
        _started = Process.Start(new ProcessStartInfo("dotnet",
            $"run --project \"{project}\" -- " +
            $"--own {_config.PeerAddress} --peer {_config.Address} --secret {_config.Secret}")
        {
            WorkingDirectory = Path.GetDirectoryName(project)!,
            UseShellExecute = true
        });

        Console.WriteLine($"[{_config.Name}] starting {_config.PeerName} at {_config.PeerAddress} ...");

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
