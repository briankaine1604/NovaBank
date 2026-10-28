namespace NovaBank
{
    /// <summary>
    /// The console the HTTP listener writes to. Kept behind an interface so the
    /// listener can run without one (and so it never touches Console directly).
    /// </summary>
    public interface IServerConsole
    {
        void DisplayServerStarted(string address);

        void DisplayServerStartFailed(string message, int errorCode);

        void DisplayIncomingTransfer(
            string senderName,
            string senderAccountNumber,
            string senderBank,
            string accountNumber,
            decimal amount,
            decimal newBalance);

        void DisplayTransferRejected(string reason);

        void DisplayDuplicateTransfer(string transferId);
    }
}
