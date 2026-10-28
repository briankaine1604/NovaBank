using System.Collections.Generic;

namespace NovaBank
{
    /// <summary>
    /// The logged-in customer's screens: their menu, their balance, their statement,
    /// and the outcomes of the operations that do not cross a bank boundary.
    /// </summary>
    public interface IAccountScreen
    {
        void DisplayAccountMenu(Account account);

        void DisplayBalance(decimal balance);

        void DisplayDepositSuccess(decimal amount, decimal newBalance);

        void DisplayWithdrawalSuccess(decimal amount, decimal newBalance);

        void DisplayNoTransactions();

        void DisplayTransactionHistory(IReadOnlyList<Transaction> transactions);

        void DisplayAccountFrozen();

        void DisplayInvalidLogin();
    }
}
