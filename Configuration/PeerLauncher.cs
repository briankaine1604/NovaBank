using System.Diagnostics;

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
        if (Process.GetProcessesByName(_config.PeerName).Length > 0)
        {
            Console.WriteLine($"[{_config.Name}] {_config.PeerName} is already running.");

            return;
        }

        string project = _config.ResolvePeerProject();

        if (!File.Exists(project))
        {
            Console.WriteLine($"[{_config.Name}] no peer project at {project}");

            return;
        }

        // dotnet run builds the peer first; everything after "--" is passed to it.
        _started = Process.Start(new ProcessStartInfo("dotnet",
            $"run --project \"{project}\" -- " +
            $"--own {_config.PeerAddress} --peer {_config.Address} --secret {_config.Secret}")
        {
            WorkingDirectory = Path.GetDirectoryName(project)!,
            UseShellExecute = true
        });

        Console.WriteLine($"[{_config.Name}] starting {_config.PeerName} at {_config.PeerAddress}.");
    }

    public void Dispose()
    {
        // dotnet run is the parent process: kill the tree or the bank outlives us.
        if (_started is { HasExited: false })
        {
            _started.Kill(entireProcessTree: true);
        }
    }
}
