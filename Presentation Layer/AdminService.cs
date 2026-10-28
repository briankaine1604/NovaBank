namespace NovaBank
{
    /// <summary>
    /// The staff side: log in, list accounts, look one up, freeze or unfreeze it.
    /// Split from the customer flows because it reads the whole bank, not one
    /// customer's account.
    /// </summary>
    public class AdminService
    {
        private readonly Bank _bank;

        private readonly Admin _admin;

        private readonly IAdminConsole _adminConsole;

        private readonly IInputReader _input;

        private readonly IMenuScreen _menu;

        public AdminService(
            Bank bank,
            Admin admin,
            IAdminConsole adminConsole,
            IInputReader input,
            IMenuScreen menu)
        {
            _bank = bank;
            _admin = admin;
            _adminConsole = adminConsole;
            _input = input;
            _menu = menu;
        }

        public void AdminLogin()
        {
            string username = _adminConsole.ReadUsername();
            string password = _adminConsole.ReadPassword();

            bool isAuthenticated = _admin.Authenticate(username, password);

            if (!isAuthenticated)
            {
                _adminConsole.DisplayInvalidAdminLogin();
                return;
            }

            _adminConsole.DisplayAdminLoginSuccess();
            AdminMenu();
        }

        private void AdminMenu()
        {
            bool loggedIn = true;

            while (loggedIn)
            {
                _adminConsole.DisplayAdminMenu();

                string? choice = _input.ReadMenuChoice();

                switch (choice)
                {
                    case "1":
                        ViewAllAccounts();
                        break;

                    case "2":
                        FindAccount();
                        break;

                    case "3":
                        ManageAccount();
                        break;

                    case "4":
                        loggedIn = false;
                        _menu.DisplayLogout();
                        break;

                    default:
                        _menu.DisplayInvalidOption();
                        break;
                }
            }
        }

        private void ViewAllAccounts()
        {
            var accounts = _bank.GetAccounts();

            _adminConsole.DisplayAccounts(accounts);
        }

        private void FindAccount()
        {
            string accountNumber = _input.ReadAccountNumber();

            Account? account = _bank.GetAccount(accountNumber);

            if (account == null)
            {
                _adminConsole.DisplayAccountNotFound();
                return;
            }

            _adminConsole.DisplayAccount(account);
        }

        private void ManageAccount()
        {
            var accounts = _bank.GetAccounts().ToList();

            if (accounts.Count == 0)
            {
                _adminConsole.DisplayNoAccounts();
                return;
            }

            _adminConsole.DisplayAccountSelection(accounts);

            int? selection = _adminConsole.ChooseAccountSelection(accounts.Count);

            if (selection == null)
            {
                _menu.DisplayInvalidOption();
                return;
            }

            if (selection == 0)
            {
                return;
            }

            ManageAccountMenu(accounts[selection.Value - 1]);
        }

        private void ManageAccountMenu(Account account)
        {
            bool isManaging = true;

            while (isManaging)
            {
                _adminConsole.DisplayManageAccountMenu(account);

                string? choice = _input.ReadMenuChoice();

                switch (choice)
                {
                    case "1":
                        if (account.IsFrozen)
                        {
                            account.Unfreeze();
                            _adminConsole.DisplayUnfreezeSuccess();
                        }
                        else
                        {
                            account.Freeze();
                            _adminConsole.DisplayFreezeSuccess();
                        }

                        break;

                    case "2":
                        isManaging = false;
                        break;

                    default:
                        _menu.DisplayInvalidOption();
                        break;
                }
            }
        }
    }
}
