using System.Diagnostics;
using System.Net.Sockets;

namespace NovaBank.Configuration;

/// <summary>
/// Starts the peer bank when NovaBank starts, and stops it again on the way out so
/// the fixed ports are free for the next run. Only a process this class started is
/// ever killed - if the peer was already up, it is left alone.
/// </summary>
public sealed class PeerLauncher : IDisposable
{
    private readonly BankConfig _config;

    private readonly string _configDirectory;

    private Process? _started;

    private bool _disposed;

    public PeerLauncher(BankConfig config, string configPath)
    {
        _config = config;
        _configDirectory = Path.GetDirectoryName(Path.GetFullPath(configPath))!;
    }

    public void Start()
    {
        if (!_config.Process.Enabled)
        {
            Console.WriteLine($"[{_config.Own.Name}] peer auto-start is off (process.enabled = false).");

            return;
        }

        if (IsListening(_config.Peer.Address))
        {
            Console.WriteLine(
                $"[{_config.Own.Name}] {_config.Peer.Name} is already listening at {_config.Peer.Address}; not starting it.");

            return;
        }

        string projectPath = ResolveProjectPath();

        if (!File.Exists(projectPath))
        {
            Console.WriteLine(
                $"[{_config.Own.Name}] cannot start {_config.Peer.Name}: no project at {projectPath}. " +
                "Fix process.projectPath in bank.config.json, or start it yourself.");

            return;
        }

        // Hand the peer the addresses and secret from this file, so the pair cannot disagree.
        string arguments =
            $"run --project \"{projectPath}\" -- " +
            $"--own {_config.Peer.Address} " +
            $"--peer {_config.Own.Address} " +
            $"--secret {_config.Own.InterBankSecret}";

        var startInfo = new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = Path.GetDirectoryName(projectPath)!,

            // Its own console window: two ATMs, two windows, both usable.
            UseShellExecute = true
        };

        try
        {
            _started = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{_config.Own.Name}] could not start {_config.Peer.Name}: {ex.Message}");

            return;
        }

        Console.WriteLine($"[{_config.Own.Name}] starting {_config.Peer.Name} at {_config.Peer.Address} ...");

        if (_started is not null && WaitForPeer(_config.Process.StartTimeoutSeconds))
        {
            Console.WriteLine($"[{_config.Own.Name}] {_config.Peer.Name} is listening.");
        }
        else
        {
            Console.WriteLine(
                $"[{_config.Own.Name}] {_config.Peer.Name} did not answer within " +
                $"{_config.Process.StartTimeoutSeconds}s; starting anyway, transfers to it will be reversed.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_started is null || _started.HasExited)
        {
            return;
        }

        try
        {
            // dotnet run is the parent: kill the tree so the app host goes with it.
            _started.Kill(entireProcessTree: true);
            _started.WaitForExit(5000);
        }
        catch (Exception)
        {
            // Already gone, or not ours to kill - nothing left to clean up.
        }
    }

    private string ResolveProjectPath()
    {
        string configured = _config.Process.ProjectPath;

        if (Path.IsPathRooted(configured))
        {
            return configured;
        }

        // The config is copied next to the executable as well as living in the repo, so
        // try the config's own directory, then its parents, then the same from the output
        // directory - a relative path should work whichever way the app was started.
        foreach (string start in new[] { _configDirectory, AppContext.BaseDirectory })
        {
            for (DirectoryInfo? directory = new(start); directory is not null; directory = directory.Parent)
            {
                string candidate = Path.Combine(directory.FullName, configured);

                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }
        }

        return Path.GetFullPath(configured, _configDirectory);
    }

    private bool WaitForPeer(int seconds)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(seconds);

        while (DateTime.UtcNow < deadline)
        {
            if (IsListening(_config.Peer.Address))
            {
                return true;
            }

            if (_started?.HasExited == true)
            {
                return false;
            }

            Thread.Sleep(250);
        }

        return false;
    }

    private static bool IsListening(string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        try
        {
            using var client = new TcpClient();

            return client.ConnectAsync(uri.Host, uri.Port)
                .Wait(TimeSpan.FromMilliseconds(400)) && client.Connected;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
