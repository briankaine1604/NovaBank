using NovaBank.Http;

namespace NovaBank
{
    /// <summary>
    /// A transfer to the peer bank over HTTP. The credit happens in another process, so this
    /// cannot debit and credit in one pass: it debits first, asks, and reverses if no answer
    /// arrives. Handles every account number this bank does not own.
    /// </summary>
    public class InterBankTransferRoute : ITransferRoute
    {
        private readonly IBankHttpClient _httpClient;

        private readonly LocalAccountNumbers _ownAccounts;

        private readonly ITransferScreen _screen;

        private readonly ICommonOutcomes _outcomes;

        // This bank's own name, so the receiving bank can write a real counterparty line.
        private readonly string _bankName;

        // The peer's name, so statements say where the money went.
        private readonly string _peerBankName;

        public InterBankTransferRoute(
            IBankHttpClient httpClient,
            LocalAccountNumbers ownAccounts,
            ITransferScreen screen,
            ICommonOutcomes outcomes,
            string bankName,
            string peerBankName)
        {
            _httpClient = httpClient;
            _ownAccounts = ownAccounts;
            _screen = screen;
            _outcomes = outcomes;
            _bankName = bankName;
            _peerBankName = peerBankName;
        }

        public bool Handles(string recipientAccountNumber) =>
            !_ownAccounts.IsOwn(recipientAccountNumber);

        public void Transfer(Account sender, string recipientAccountNumber, decimal amount)
        {
            // Debit first, so a slow or dead link can never send funds this account does not have.
            bool debited = sender.TryDebit(
                amount,
                $"Inter-bank transfer to {_peerBankName} {recipientAccountNumber}",
                "Transfer Out");

            if (!debited)
            {
                _outcomes.DisplayInsufficientFunds();

                return;
            }

            // Built once before the debit and reused on the retry: same reference, sender and amount.
            TransferRequestDto request = new()
            {
                AccountNumber = recipientAccountNumber,
                Amount = amount,
                TransferId = Guid.NewGuid().ToString("N"),
                SenderName = sender.AccountHolder.Name,
                SenderAccountNumber = sender.AccountNumber,
                SenderBank = _bankName
            };

            (bool success, string message) = SendWithRetry(request);

            if (success)
            {
                _screen.DisplayInterBankTransferSuccess(amount, sender.Balance);

                return;
            }

            // The peer did not accept it, so put the money back.
            sender.Credit(
                amount,
                $"Reversal: inter-bank transfer to {_peerBankName} {recipientAccountNumber} failed",
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

            // Retry ONCE with the SAME request object. If the first attempt landed and only the
            // answer was lost, this one is recognised as a duplicate and returns the original
            // outcome instead of crediting twice - so the caller can report success.
            (success, message) = _httpClient
                .TransferAsync(request)
                .GetAwaiter().GetResult();

            return (success, message);
        }
    }
}
