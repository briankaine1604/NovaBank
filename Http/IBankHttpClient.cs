namespace NovaBank.Http
{
    /// <summary>
    /// Sending money to the other bank. Behind an interface so the ATM can be
    /// exercised without a live socket.
    /// </summary>
    public interface IBankHttpClient
    {
        Task<(bool Success, string Message)> TransferAsync(TransferRequestDto transfer);
    }
}
