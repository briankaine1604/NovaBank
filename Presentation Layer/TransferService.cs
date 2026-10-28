using NovaBank.Http;

namespace NovaBank
{
    /// <summary>
    /// Every way money leaves this account: a local transfer, or a transfer to the
    /// other bank over HTTP.
    ///
    /// The local path validates, debits and credits in one pass. The inter-bank path
    /// cannot do that - the credit happens in another process - so it debits first,
    /// asks, and reverses if no answer arrives. That asymmetry is the whole reason
    /// this class exists separately from AccountService.
    /// </summary>
    public class TransferService
    {
        // Accounts starting with this prefix belong to CrestBank, not to this bank.
        private const string CrestBankPrefix = "303";

        // This bank's own name, sent with every transfer so the receiving bank can
        // write a real counterparty line instead of guessing who called it.
        private const string BankName = "NovaBank";

        private readonly Bank _bank;

        private readonly ITransferScreen _screen;

        private readonly IInputReader _input;

        private readonly ICommonOutcomes _outcomes;

        private readonly IBankHttpClient _httpClient;

        public TransferService(
            Bank bank,
            ITransferScreen screen,
            IInputReader input,
            ICommonOutcomes outcomes,
            IBankHttpClient httpClient)
        {
            _bank = bank;
            _screen = screen;
            _input = input;
            _outcomes = outcomes;
            _httpClient = httpClient;
        }

        public void Transfer(Account sender)
        {
            string recipientAccountNumber = _input.ReadRecipientAccountNumber();

            if (!_input.TryReadAmount(
                "How much do you want to transfer? ",
                out decimal amount))
            {
                return;
            }

            if (recipientAccountNumber.StartsWith(CrestBankPrefix))
            {
                TransferToCrestBank(sender, recipientAccountNumber, amount);

                return;
            }

            TransferResult result = _bank.Transfer(
                sender, recipientAccountNumber, amount);

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

        private void TransferToCrestBank(
            Account sender,
            string recipientAccountNumber,
            decimal amount)
        {
            // Take the money out first, so a slow or dead link can never send
            // funds this account does not have.
            bool debited = sender.TryDebit(
                amount,
                $"Inter-bank transfer to CrestBank {recipientAccountNumber}",
                "Transfer Out");

            if (!debited)
            {
                _outcomes.DisplayInsufficientFunds();

                return;
            }

            // Everything the other bank needs to show this transfer on its own
            // statement is built once, before the debit, and reused on the retry:
            // same reference, same sender, same amount.
            TransferRequestDto request = new()
            {
                AccountNumber = recipientAccountNumber,
                Amount = amount,
                TransferId = Guid.NewGuid().ToString("N"),
                SenderName = sender.AccountHolder.Name,
                SenderAccountNumber = sender.AccountNumber,
                SenderBank = BankName
            };

            // Blocking on purpose: the console menu has to stay in step with the
            // request, and there is no UI thread here to deadlock on.
            (bool success, string message) = SendWithRetry(request);

            if (success)
            {
                _screen.DisplayInterBankTransferSuccess(amount, sender.Balance);

                return;
            }

            // CrestBank did not accept it, so put the money back.
            sender.Credit(
                amount,
                $"Reversal: inter-bank transfer to CrestBank {recipientAccountNumber} failed",
                "Transfer In");

            _screen.DisplayInterBankTransferFailed(message);
        }

        private (bool Success, string Message) SendWithRetry(TransferRequestDto request)
        {
            (bool success, string message) = _httpClient
                .TransferAsync(request)
                .GetAwaiter().GetResult();

            if (success)
            {
                return (true, message);
            }

            // Retry ONCE with the SAME request object. If the first attempt reached
            // the other bank and only the answer was lost, this one is recognised as
            // a duplicate and returns the original outcome instead of crediting twice.
            // That is what lets the caller report success instead of reversing.
            (success, message) = _httpClient
                .TransferAsync(request)
                .GetAwaiter().GetResult();

            return (success, message);
        }
    }
}
