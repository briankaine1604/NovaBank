namespace NovaBank
{
    /// <summary>
    /// Everything the ATM needs from a screen: prompts, reads, menus and outcome
    /// messages. The ATM depends on this, never on a concrete console.
    /// </summary>
    public interface IATMConsole
    {
        void DisplayMainMenu();
        void DisplayAccountMenu(Account account);
        string? ChooseNetwork();
        DataPlan? ChooseDataPlan();
        void DisplayInvalidNetwork();
        bool TryReadAmount(string prompt, out decimal amount);
        string? ReadMenuChoice();
        string ReadAccountNumber();
        string ReadPin();
        string ReadNewPin();
        string ReadRecipientAccountNumber();
        string? ReadPhoneNumber();
        string ReadName(string prompt);
        void DisplayAirtimePurchaseSuccess(decimal amount, string phone, string network, decimal newBalance);
        void DisplayDataPurchaseSuccess(DataPlan plan, string phone, string network, decimal newBalance);
        void DisplayWithdrawalSuccess(decimal amount, decimal newBalance);
        void DisplayBalance(decimal balance);
        void DisplayInvalidLogin();
        void DisplayLogout();
        void DisplayTransferSuccess(decimal newBalance);
        void DisplayInvalidTransferAmount();
        void DisplaySameAccountError();
        void DisplayRecipientNotFound();
        void DisplayInsufficientFunds();
        void DisplayNoTransactions();
        void DisplayTransactionHistory(IReadOnlyList<Transaction> transactions);
        void DisplayInvalidOption();
        void OnAccountCreatedSuccess(Account account);
        void DisplayDepositSuccess(decimal amount, decimal newBalance);
        void DisplayAccountFrozen();
        void DisplayInterBankTransferSuccess(decimal amount, decimal newBalance);
        void DisplayInterBankTransferFailed(string message);
    }
}
