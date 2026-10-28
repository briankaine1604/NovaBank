namespace NovaBank
{
    /// <summary>
    /// How a transfer is reported to the customer - the local ones and the ones
    /// that leave this bank.
    /// </summary>
    public interface ITransferScreen
    {
        void DisplayTransferSuccess(decimal newBalance);

        void DisplayInvalidTransferAmount();

        void DisplaySameAccountError();

        void DisplayRecipientNotFound();

        void DisplayInterBankTransferSuccess(decimal amount, decimal newBalance);

        void DisplayInterBankTransferFailed(string message);
    }
}
