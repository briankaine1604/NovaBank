namespace NovaBank
{
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
