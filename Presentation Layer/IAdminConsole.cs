namespace NovaBank
{
    /// <summary>Everything the ATM needs from the admin screens.</summary>
    public interface IAdminConsole
    {
        void DisplayAdminMenu();
        string ReadUsername();
        string ReadPassword();
        void DisplayInvalidAdminLogin();
        void DisplayAdminLoginSuccess();
        void DisplayAccounts(IEnumerable<Account> accounts);
        void DisplayAccount(Account account);
        void DisplayAccountNotFound();
        void DisplayNoAccounts();
        void DisplayAccountSelection(IEnumerable<Account> accounts);
        int? ChooseAccountSelection(int accountCount);
        void DisplayManageAccountMenu(Account account);
        void DisplayFreezeSuccess();
        void DisplayUnfreezeSuccess();
    }
}
