using NovaBank;
using NovaBank.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NovaBank
{
    public class ATM
    {
        // Accounts starting with this prefix belong to CrestBank, not to this bank.
        private const string CrestBankPrefix = "303";

        // This bank's own name, sent with every transfer so the receiving bank can
        // write a real counterparty line instead of guessing who called it.
        private const string BankName = "NovaBank";

        private readonly Bank _bank;

        private readonly IATMConsole _atmConsole;

        private readonly IAdminConsole _adminConsole;

        private readonly Admin _admin;

        private readonly IBankHttpClient _httpClient;


        public ATM(
            Bank bank,
            IATMConsole atmConsole,
            IAdminConsole adminConsole,
            Admin admin,
            IBankHttpClient httpClient)
        {
            _bank = bank;
            _atmConsole = atmConsole;
            _adminConsole = adminConsole;
            _admin = admin;
            _httpClient = httpClient;
        }

        private void AdminLogin()
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

                string? choice = _atmConsole.ReadMenuChoice();

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
                        _atmConsole.DisplayLogout();
                        break;

                    default:
                        _atmConsole.DisplayInvalidOption();
                        break;
                }
            }
        }

        private void BuyAirtime(Account account)
        {
            string? phone = _atmConsole.ReadPhoneNumber();
            if (phone == null) return;

            string? network = _atmConsole.ChooseNetwork();
            if (network == null)
            {
                _atmConsole.DisplayInvalidNetwork();
                return;
            }

            if (!_atmConsole.TryReadAmount(
                "How much do you want to recharge? ",
                out decimal amount))
                return;

            bool success = account.TryDebit(
                amount,
                $"Airtime for {phone} ({network})",
                "Airtime");

            if (success)
                _atmConsole.DisplayAirtimePurchaseSuccess(amount, phone, network, account.Balance);
            else
                _atmConsole.DisplayInsufficientFunds();
        }

        private void BuyData(Account account)
        {
            string? phone = _atmConsole.ReadPhoneNumber();
            if (phone == null) return;

            string? network = _atmConsole.ChooseNetwork();
            if (network == null)
            {
                _atmConsole.DisplayInvalidNetwork();
                return;
            }

            DataPlan? plan = _atmConsole.ChooseDataPlan();
            if (plan == null) return;

            bool success = account.TryDebit(
                plan.Price,
                $"{plan.Name} data for {phone} ({network})",
                "Data");

            if (success)
            {
                _atmConsole.DisplayDataPurchaseSuccess(
                    plan, phone, network, account.Balance);
            }
            else
            {
                _atmConsole.DisplayInsufficientFunds();
            }
        }      

        private void Withdraw(Account account)
        {
            if (!_atmConsole.TryReadAmount("How much do you want to withdraw? ", out decimal amount))
                return;

            bool success = account.TryDebit(amount, "Cash withdrawal", "Withdrawal");

            if (success)
                _atmConsole.DisplayWithdrawalSuccess(amount, account.Balance);
            else
                _atmConsole.DisplayInsufficientFunds();
        }

        private void Deposit(Account account)
        {
            if (!_atmConsole.TryReadAmount("How much do you want to deposit? ", out decimal amount))
                return;

            account.Credit(amount, "Cash deposit", "Deposit");

            _atmConsole.DisplayDepositSuccess(amount, account.Balance);
        }

        private void ShowTransactionHistory(Account account)
        {
            if (account.Transactions.Count == 0)
            {
                _atmConsole.DisplayNoTransactions();
                return;
            }

            _atmConsole.DisplayTransactionHistory(account.Transactions);
        }
        private void Transfer(Account sender)
        {
            string recipientAccountNumber = _atmConsole.ReadRecipientAccountNumber();

            if (!_atmConsole.TryReadAmount(
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
                    _atmConsole.DisplayTransferSuccess(sender.Balance);
                    break;

                case TransferResult.AmountInvalid:
                    _atmConsole.DisplayInvalidTransferAmount();
                    break;

                case TransferResult.SameAccount:
                    _atmConsole.DisplaySameAccountError();
                    break;

                case TransferResult.RecipientInvalid:
                    _atmConsole.DisplayRecipientNotFound();
                    break;

                case TransferResult.InsufficientFunds:
                    _atmConsole.DisplayInsufficientFunds();
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
                _atmConsole.DisplayInsufficientFunds();

                return;
            }

            // Everything the other bank needs to show this transfer on its own
            // statement is built once, before the debit, and reused on the retry:
            // same reference, same sender, same amount.
            TransferRequest request = new()
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
                _atmConsole.DisplayInterBankTransferSuccess(amount, sender.Balance);

                return;
            }

            // CrestBank did not accept it, so put the money back.
            sender.Credit(
                amount,
                $"Reversal: inter-bank transfer to CrestBank {recipientAccountNumber} failed",
                "Transfer In");

            _atmConsole.DisplayInterBankTransferFailed(message);
        }

        private (bool Success, string Message) SendWithRetry(TransferRequest request)
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

        public void Run()
        {
            bool isRunning = true;

            while (isRunning)
            {
                _atmConsole.DisplayMainMenu();

                string? choice = _atmConsole.ReadMenuChoice();

                switch (choice)
                {
                    case "1":
                        CreateAccount();
                        break;

                    case "2":
                        Login();
                        break;

                    case "3":
                        AdminLogin();
                        break;

                    case "4":
                        isRunning = false;
                        break;

                    default:
                        _atmConsole.DisplayInvalidOption();
                        break;
                }
            }
        }

        private void AccountMenu(Account account)
        {
            bool loggedIn = true;

            while (loggedIn)
            {
                _atmConsole.DisplayAccountMenu(account);

                string? choice = _atmConsole.ReadMenuChoice();

                switch (choice)
                {
                    case "1":
                        _atmConsole.DisplayBalance(account.Balance);
                        break;

                    case "2":
                        if (IsBlockedByFreeze(account)) break;

                        Deposit(account);
                        break;

                    case "3":
                        if (IsBlockedByFreeze(account)) break;

                        Withdraw(account);
                        break;

                    case "4":
                        if (IsBlockedByFreeze(account)) break;

                        Transfer(account);
                        break;

                    case "5":
                        ShowTransactionHistory(account);
                        break;

                    case "6":
                        if (IsBlockedByFreeze(account)) break;

                        BuyAirtime(account);
                        break;

                    case "7":
                        if (IsBlockedByFreeze(account)) break;

                        BuyData(account);
                        break;

                    case "8":
                        loggedIn = false;
                        _atmConsole.DisplayLogout();
                        break;

                    default:
                        _atmConsole.DisplayInvalidOption();
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

            _atmConsole.DisplayAccountFrozen();

            return true;
        }

        private void CreateAccount()
        {
            string name = _atmConsole.ReadName("Enter your name: ");
            string city = _atmConsole.ReadName("Enter your city: ");

            string pin = _atmConsole.ReadNewPin();

            decimal deposit;
            while (!_atmConsole.TryReadAmount("How much do you want to deposit?: ", out deposit))
            {
                // keep asking until the amount passes validation
            }

            Account account = _bank.CreateAccount(name, city, pin, deposit);

            _atmConsole.OnAccountCreatedSuccess(account);
        }


        private void Login()
        {
            string accountNumber = _atmConsole.ReadAccountNumber();
            string pin = _atmConsole.ReadPin();

            Account? account = _bank.Authenticate(accountNumber, pin);

            if (account == null)
            {
                _atmConsole.DisplayInvalidLogin();
                return;
            }

            // A frozen account can still log in: AccountMenu blocks the
            // operations that move money and leaves balance/history available.
            if (account.IsFrozen)
            {
                _atmConsole.DisplayAccountFrozen();
            }

            AccountMenu(account);
        }

        private void ViewAllAccounts()
        {
            var accounts = _bank.GetAccounts();

            _adminConsole.DisplayAccounts(accounts);
        }

        private void FindAccount()
        {
            string accountNumber = _atmConsole.ReadAccountNumber();

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
                _atmConsole.DisplayInvalidOption();
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

                string? choice = _atmConsole.ReadMenuChoice();

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
                        _atmConsole.DisplayInvalidOption();
                        break;
                }
            }
        }

        
    }
 

}
