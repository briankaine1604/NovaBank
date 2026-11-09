namespace NovaBank
{
    public interface ITransferRoute
    {
        bool Handles(string recipientAccountNumber);

        void Transfer(Account sender, string recipientAccountNumber, decimal amount);
    }
}
