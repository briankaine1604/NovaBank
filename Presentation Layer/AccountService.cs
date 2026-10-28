namespace NovaBank
{
    /// <summary>
    /// The operations that stay inside this bank: balance, deposit, withdrawal and
    /// the statement. No network, so every one of these completes or refuses on the
    /// spot.
    /// </summary>
    public class AccountService
    {
        private readonly IAccountScreen _screen;

        private readonly IInputReader _input;

        private readonly ICommonOutcomes _outcomes;

        public AccountService(
            IAccountScreen screen,
            IInputReader input,
            ICommonOutcomes outcomes)
        {
            _screen = screen;
            _input = input;
            _outcomes = outcomes;
        }

        public void ShowBalance(Account account)
        {
            _screen.DisplayBalance(account.Balance);
        }

        public void Deposit(Account account)
        {
            if (!_input.TryReadAmount("How much do you want to deposit? ", out decimal amount))
                return;

            account.Credit(amount, "Cash deposit", "Deposit");

            _screen.DisplayDepositSuccess(amount, account.Balance);
        }

        public void Withdraw(Account account)
        {
            if (!_input.TryReadAmount("How much do you want to withdraw? ", out decimal amount))
                return;

            bool success = account.TryDebit(amount, "Cash withdrawal", "Withdrawal");

            if (success)
                _screen.DisplayWithdrawalSuccess(amount, account.Balance);
            else
                _outcomes.DisplayInsufficientFunds();
        }

        public void ShowTransactionHistory(Account account)
        {
            if (account.Transactions.Count == 0)
            {
                _screen.DisplayNoTransactions();
                return;
            }

            _screen.DisplayTransactionHistory(account.Transactions);
        }
    }
}
