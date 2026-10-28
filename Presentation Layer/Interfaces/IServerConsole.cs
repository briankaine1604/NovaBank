namespace NovaBank
{
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
