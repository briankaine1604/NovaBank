namespace NovaBank;

public class ServerConsole
{
    public void DisplayServerStarted(string address)
    {
        Console.WriteLine($"[NovaBank] HTTP server listening on {address}");
    }

    public void DisplayServerStartFailed(string message, int errorCode)
    {
        Console.WriteLine($"[NovaBank] could not start the HTTP server: {message} (error {errorCode})");
    }

    public void DisplayIncomingTransfer(string accountNumber, decimal amount, decimal newBalance)
    {
        Console.WriteLine($"[NovaBank] credited {amount:N2} to {accountNumber} (balance now {newBalance:N2})");
    }

    public void DisplayTransferRejected(string reason)
    {
        Console.WriteLine($"[NovaBank] transfer rejected: {reason}");
    }

    public void DisplayDuplicateTransfer(string transferId)
    {
        Console.WriteLine($"[NovaBank] duplicate transfer {transferId} replayed - nothing credited");
    }
}
