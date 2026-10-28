namespace NovaBank
{
    /// <summary>
    /// The shell. Its only jobs are: show the right menu, read the choice, and hand
    /// the work to the service that owns it. It holds no rules, no formatting and no
    /// transfer logic - each service talks to the screen interfaces it needs.
    /// </summary>
    public class ATM
    {
        private readonly Bank _bank;

        private readonly IInputReader _input;

        private readonly IMenuScreen _menu;

        private readonly IAccountScreen _accountScreen;

        private readonly AccountOpeningService _accountOpening;

        private readonly AccountService _accounts;

        private readonly TransferService _transfers;

        private readonly AirtimeService _airtime;

        private readonly AdminService _admin;

        public ATM(
            Bank bank,
            IInputReader input,
            IMenuScreen menu,
            IAccountScreen accountScreen,
            AccountOpeningService accountOpening,
            AccountService accounts,
            TransferService transfers,
            AirtimeService airtime,
            AdminService admin)
        {
            _bank = bank;
            _input = input;
            _menu = menu;
            _accountScreen = accountScreen;
            _accountOpening = accountOpening;
            _accounts = accounts;
            _transfers = transfers;
            _airtime = airtime;
            _admin = admin;
        }

        public void Run()
        {
            bool isRunning = true;

            while (isRunning)
            {
                _menu.DisplayMainMenu();

                string? choice = _input.ReadMenuChoice();

                switch (choice)
                {
                    case "1":
                        _accountOpening.CreateAccount();
                        break;

                    case "2":
                        Login();
                        break;

                    case "3":
                        _admin.AdminLogin();
                        break;

                    case "4":
                        isRunning = false;
                        break;

                    default:
                        _menu.DisplayInvalidOption();
                        break;
                }
            }
        }

        private void Login()
        {
            string accountNumber = _input.ReadAccountNumber();
            string pin = _input.ReadPin();

            Account? account = _bank.Authenticate(accountNumber, pin);

            if (account == null)
            {
                _accountScreen.DisplayInvalidLogin();
                return;
            }

            // A frozen account can still log in: AccountMenu blocks the
            // operations that move money and leaves balance/history available.
            if (account.IsFrozen)
            {
                _accountScreen.DisplayAccountFrozen();
            }

            AccountMenu(account);
        }

        private void AccountMenu(Account account)
        {
            bool loggedIn = true;

            while (loggedIn)
            {
                _accountScreen.DisplayAccountMenu(account);

                string? choice = _input.ReadMenuChoice();

                switch (choice)
                {
                    case "1":
                        _accounts.ShowBalance(account);
                        break;

                    case "2":
                        if (IsBlockedByFreeze(account)) break;

                        _accounts.Deposit(account);
                        break;

                    case "3":
                        if (IsBlockedByFreeze(account)) break;

                        _accounts.Withdraw(account);
                        break;

                    case "4":
                        if (IsBlockedByFreeze(account)) break;

                        _transfers.Transfer(account);
                        break;

                    case "5":
                        _accounts.ShowTransactionHistory(account);
                        break;

                    case "6":
                        if (IsBlockedByFreeze(account)) break;

                        _airtime.BuyAirtime(account);
                        break;

                    case "7":
                        if (IsBlockedByFreeze(account)) break;

                        _airtime.BuyData(account);
                        break;

                    case "8":
                        loggedIn = false;
                        _menu.DisplayLogout();
                        break;

                    default:
                        _menu.DisplayInvalidOption();
                        break;
                }
            }
        }

        /// Money-moving operations are blocked on a frozen account; balance and
        /// transaction history stay available.
        private bool IsBlockedByFreeze(Account account)
        {
            if (!account.IsFrozen)
                return false;

            _accountScreen.DisplayAccountFrozen();

            return true;
        }
    }
}
