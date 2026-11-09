using System.Text.Json;

namespace NovaBank.Configuration;

/// <summary>
/// What bank.config.json holds: this bank's name and address, the secret both
/// banks sign with, and where the peer's project lives.
/// </summary>
public sealed class BankConfig
{
    public string Name { get; set; } = "NovaBank";

    public string Address { get; set; } = "http://localhost:5001/";

    public string Secret { get; set; } = "";

    public string PeerName { get; set; } = "CrestBank";

    public string PeerAddress { get; set; } = "http://localhost:5002/";

    /// <summary>The peer's .csproj, relative to the repository root.</summary>
    public string PeerProjectPath { get; set; } = "";

    /// <summary>
    /// Read from beside the executable - the csproj copies it there - so this is
    /// the one path that is right however the app was started.
    /// </summary>
    public static BankConfig Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "bank.config.json");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"bank.config.json should be next to the executable: {path}");
        }

        return JsonSerializer.Deserialize<BankConfig>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException($"{path} is empty.");
    }

    /// <summary>
    /// The app runs from bin/Debug/net10.0, so a repo-relative path only resolves
    /// after climbing a few levels. Climb until the peer's project turns up.
    /// </summary>
    public string ResolvePeerProject()
    {
        if (Path.IsPathRooted(PeerProjectPath))
        {
            return PeerProjectPath;
        }

        for (DirectoryInfo? directory = new(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            string candidate = Path.GetFullPath(Path.Combine(directory.FullName, PeerProjectPath));

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.GetFullPath(PeerProjectPath, AppContext.BaseDirectory);
    }
}
