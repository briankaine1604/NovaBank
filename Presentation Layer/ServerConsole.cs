namespace NovaBank;

public class ServerConsole : IServerConsole
{
    // The ATM screen belongs to whoever is logged in, so server events go to a
    // file instead of writing over another customer's screen.
    private readonly string _logPath = Path.Combine(Directory.GetCurrentDirectory(), "bank.log");

    public void DisplayServerStarted(string address)
    {
        Console.WriteLine($"[NovaBank] HTTP server listening on {address}");
    }

    public void DisplayServerStartFailed(string message, int errorCode)
    {
        Console.WriteLine($"[NovaBank] could not start the HTTP server: {message} (error {errorCode})");
    }

    public void DisplayIncomingTransfer(
        string senderName,
        string senderAccountNumber,
        string senderBank,
        string accountNumber,
        decimal amount,
        decimal newBalance)
    {
        Write(
            $"credited {amount:N2} to {accountNumber} " +
            $"from {senderName} {senderAccountNumber} ({senderBank}) - balance now {newBalance:N2}");
    }

    public void DisplayTransferRejected(string reason)
    {
        Write($"transfer rejected: {reason}");
    }

    public void DisplayDuplicateTransfer(string transferId)
    {
        Write($"duplicate transfer {transferId} replayed - nothing credited");
    }

    private void Write(string line)
    {
        File.AppendAllText(_logPath, $"{DateTime.Now:HH:mm:ss} {line}{Environment.NewLine}");
    }

    public IReadOnlyList<string> RecentLines(int count)
    {
        if (!File.Exists(_logPath))
        {
            return Array.Empty<string>();
        }

        return File.ReadAllLines(_logPath).TakeLast(count).ToList();
    }
}
