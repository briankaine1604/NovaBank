using System.Text.Json;

namespace NovaBank.Configuration;

public sealed class BankConfig
{
    public string Name { get; set; } = "NovaBank";

    public string Address { get; set; } = "http://localhost:5001/";

    public string Secret { get; set; } = "";

    public string PeerName { get; set; } = "CrestBank";

    public string PeerAddress { get; set; } = "http://localhost:5002/";

    public string PeerProjectPath { get; set; } = "";

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
