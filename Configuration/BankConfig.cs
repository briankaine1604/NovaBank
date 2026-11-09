using System.Text.Json;

namespace NovaBank.Configuration;

/// <summary>
/// NovaBank is the entry point of the pair, so this file decides where both banks
/// listen, which secret they sign with, and whether the peer is started with us.
/// Read once at startup; nothing reloads it.
/// </summary>
public sealed class BankConfig
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public BankIdentity Own { get; set; } = new();

    public PeerBank Peer { get; set; } = new();

    public PeerProcess Process { get; set; } = new();

    /// <summary>
    /// Loads the config, and reports where it came from so the startup log can show it.
    /// Throws rather than falling back to guesses: a wrong address is a network bug
    /// that looks like a transfer bug.
    /// </summary>
    public static BankConfig Load(out string sourcePath)
    {
        sourcePath = Find()
            ?? throw new FileNotFoundException(
                "bank.config.json not found beside the working directory or the executable. " +
                "Set BANK_CONFIG to an explicit path.");

        string json = File.ReadAllText(sourcePath);

        BankConfig config = JsonSerializer.Deserialize<BankConfig>(json, Options)
            ?? throw new InvalidDataException($"bank.config.json at {sourcePath} is empty.");

        config.Validate(sourcePath);

        return config;
    }

    private static string? Find()
    {
        string? fromEnvironment = Environment.GetEnvironmentVariable("BANK_CONFIG");

        if (!string.IsNullOrWhiteSpace(fromEnvironment) && File.Exists(fromEnvironment))
        {
            return Path.GetFullPath(fromEnvironment);
        }

        foreach (string directory in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            string candidate = Path.Combine(directory, "bank.config.json");

            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        return null;
    }

    private void Validate(string sourcePath)
    {
        if (!Uri.TryCreate(Own.Address, UriKind.Absolute, out _))
        {
            throw new InvalidDataException($"own.address '{Own.Address}' in {sourcePath} is not a URL.");
        }

        if (!Uri.TryCreate(Peer.Address, UriKind.Absolute, out _))
        {
            throw new InvalidDataException($"peer.address '{Peer.Address}' in {sourcePath} is not a URL.");
        }

        if (Own.Address == Peer.Address)
        {
            throw new InvalidDataException($"own.address and peer.address in {sourcePath} are the same: {Own.Address}");
        }

        if (string.IsNullOrWhiteSpace(Own.InterBankSecret))
        {
            throw new InvalidDataException($"own.interBankSecret in {sourcePath} is empty; both banks need it.");
        }
    }
}

public sealed class BankIdentity
{
    public string Name { get; set; } = "NovaBank";

    /// <summary>Where this bank listens. This bank's own prefix, e.g. http://localhost:5001/.</summary>
    public string Address { get; set; } = "http://localhost:5001/";

    /// <summary>Shared with the peer, and passed to it when we start it. Never committed for real.</summary>
    public string InterBankSecret { get; set; } = "";
}

public sealed class PeerBank
{
    public string Name { get; set; } = "CrestBank";

    /// <summary>Where the peer listens. Handed to the peer as --own when we start it.</summary>
    public string Address { get; set; } = "http://localhost:5002/";
}

public sealed class PeerProcess
{
    /// <summary>False: the peer is started by hand, and this bank only talks to it.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Path to the peer's .csproj, absolute or relative to this config file.</summary>
    public string ProjectPath { get; set; } = "";

    /// <summary>Seconds to wait for the peer's port after starting it. It builds first, so allow for that.</summary>
    public int StartTimeoutSeconds { get; set; } = 30;
}
