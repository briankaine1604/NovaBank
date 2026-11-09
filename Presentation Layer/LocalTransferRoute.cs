namespace NovaBank
{
    /// <summary>
    /// A transfer to another account in this bank. Both sides are here, so it can validate,
    /// debit and credit in one pass - no network, so it either completes or refuses on the spot.
    /// </summary>
    public class LocalTransferRoute : ITransferRoute
    {
        private readonly Bank _bank;

        private readonly LocalAccountNumbers _ownAccounts;

        private readonly ITransferScreen _screen;

        private readonly ICommonOutcomes _outcomes;

        public LocalTransferRoute(
            Bank bank,
            LocalAccountNumbers ownAccounts,
            ITransferScreen screen,
            ICommonOutcomes outcomes)
        {
            _bank = bank;
            _ownAccounts = ownAccounts;
            _screen = screen;
            _outcomes = outcomes;
        }

        public bool Handles(string recipientAccountNumber) =>
            _ownAccounts.IsOwn(recipientAccountNumber);

        public void Transfer(Account sender, string recipientAccountNumber, decimal amount)
        {
            TransferResult result = _bank.Transfer(
                sender, recipientAccountNumber, amount);

            // Outcomes of one validation, not a choice between kinds of transfer.
            switch (result)
            {
                case TransferResult.Success:
                    _screen.DisplayTransferSuccess(sender.Balance);
                    break;

                case TransferResult.AmountInvalid:
                    _screen.DisplayInvalidTransferAmount();
                    break;

                case TransferResult.SameAccount:
                    _screen.DisplaySameAccountError();
                    break;

                case TransferResult.RecipientInvalid:
                    _screen.DisplayRecipientNotFound();
                    break;

                case TransferResult.InsufficientFunds:
                    _outcomes.DisplayInsufficientFunds();
                    break;
            }
        }
    }
}
