using System.Security.Principal;

namespace NovaBank;

public class ATMConsole
{
    private const decimal MaxAmount = 1_000_000_000m;

    public void DisplayMainMenu()
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("                P I G G Y   B A N K   A T M");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("1. Create account");
        Console.WriteLine("2. Customer login");
        Console.WriteLine("3. Admin login");
        Console.WriteLine("4. Exit");
        Console.Write("Choose an option: ");
    }

    public void DisplayAccountMenu(Account account)
    {
        Console.WriteLine($"\nWelcome, {account.AccountHolder.Name}");
        Console.WriteLine("1. Check balance");
        Console.WriteLine("2. Deposit");
        Console.WriteLine("3. Withdraw");
        Console.WriteLine("4. Transfer");
        Console.WriteLine("5. Transaction history");
        Console.WriteLine("6. Buy Airtime");
        Console.WriteLine("7. Buy Data");
        Console.WriteLine("8. Logout");
        Console.Write("Choose an option: ");
    }

    public string? ChooseNetwork()
    {
        Console.WriteLine("Select network:");
        Console.WriteLine("1. MTN");
        Console.WriteLine("2. Airtel");
        Console.WriteLine("3. Glo");
        Console.WriteLine("4. 9mobile");
        Console.Write("Choose network: ");

        return Console.ReadLine()?.Trim() switch
        {
            "1" => "MTN",
            "2" => "Airtel",
            "3" => "Glo",
            "4" => "9mobile",
            _ => null
        };
    }

    public DataPlan? ChooseDataPlan()
    {
        Console.WriteLine("\nSelect data plan:");

        for (int i = 0; i < DataPlans.All.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {DataPlans.All[i].Name} - {DataPlans.All[i].Price:N2}");
        }

        Console.Write("Choose plan: ");

        if (!int.TryParse(Console.ReadLine(), out int planChoice)
            || planChoice < 1 || planChoice > DataPlans.All.Count)
        {
            Console.WriteLine("Invalid plan.");
            return null;
        }

        return DataPlans.All[planChoice - 1];
    }

    public void DisplayInvalidNetwork()
    {
        Console.WriteLine("Invalid network.");
    }

    public bool TryReadAmount(string prompt, out decimal amount)
    {
        Console.Write(prompt);

        return TryParseAmount(Console.ReadLine(), out amount);
    }

    private bool TryParseAmount(string? input, out decimal amount)
    {
        if (!decimal.TryParse(input, out amount))
        {
            Console.WriteLine("Invalid amount. Please enter a number.");
            return false;
        }

        if (amount <= 0)
        {
            Console.WriteLine("Amount must be greater than zero.");
            return false;
        }

        if (amount != Math.Round(amount, 2))
        {
            Console.WriteLine("Amount can have at most 2 decimal places.");
            return false;
        }

        if (amount > MaxAmount)
        {
            Console.WriteLine($"Amount cannot exceed {MaxAmount:N2}.");
            return false;
        }

        return true;
    }

    public string? ReadMenuChoice()
    {
        return Console.ReadLine();
    }


    public string ReadAccountNumber()
    {
        Console.Write("Enter your account number: ");

        return Console.ReadLine()?.Trim() ?? "";
    }

    public string ReadPin()
    {
        Console.Write("Enter your PIN: ");

        return Console.ReadLine()?.Trim() ?? "";
    }

    public string ReadNewPin()
    {
        while (true)
        {
            Console.Write("Choose a PIN (4 digits): ");
            string pin = Console.ReadLine()?.Trim() ?? "";

            if (pin.Length == 4 && pin.All(char.IsDigit))
                return pin;

            Console.WriteLine("PIN must contain exactly four digits.");
        }
    }

    public string ReadRecipientAccountNumber()
    {
        Console.Write("Enter recipient account number: ");

        return Console.ReadLine()?.Trim() ?? "";
    }

    public string? ReadPhoneNumber()
    {
        Console.Write("Enter phone number: ");
        string phone = Console.ReadLine()?.Trim().Replace(" ", "").Replace("-", "") ?? "";

        if (phone.StartsWith("+234"))
            phone = "0" + phone.Substring(4);
        else if (phone.StartsWith("234"))
            phone = "0" + phone.Substring(3);
        else if (phone.Length == 10 && !phone.StartsWith("0"))
            phone = "0" + phone;

        if (phone.Length != 11 || !phone.All(char.IsDigit) || !phone.StartsWith("0"))
        {
            Console.WriteLine("Enter a valid Nigerian number, e.g. 08012345678, 8012345678 or +2348012345678.");
            return null;
        }

        return phone;
    }

    public string ReadName(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = Console.ReadLine()?.Trim() ?? "";

            if (input.Length >= 2 && input.All(c => char.IsLetter(c) || c == ' ' || c == '-' || c == '\''))
                return input;

            Console.WriteLine("Use letters only (spaces, hyphens, apostrophes allowed), at least 2 characters.");
        }
    }

    public void DisplayAirtimePurchaseSuccess(decimal amount, string phone, string network, decimal newBalance)
    {
        Console.WriteLine($"Airtime purchase successful. Amount: {amount:N2} for {phone} ({network})");
        Console.WriteLine($"Remaining balance: {newBalance:N2}");
    }

    public void DisplayDataPurchaseSuccess(DataPlan plan, string phone, string network, decimal newBalance)
    {
        Console.WriteLine($"Data purchase successful: {plan.Name} for {phone} ({network})");
        Console.WriteLine($"Remaining balance: {newBalance:N2}");
    }

    public void DisplayWithdrawalSuccess(decimal amount, decimal newBalance)
    {
        Console.WriteLine($"Withdrawal successful. Amount: {amount:N2}");
        Console.WriteLine($"Remaining balance: {newBalance:N2}");
    }

    public void DisplayBalance(decimal balance)
    {
        Console.WriteLine($"Balance: {balance:N2}");
    }

    public void DisplayInvalidLogin()
    {
        Console.WriteLine("Invalid account number or PIN.");
    }

    public void DisplayLogout()
    {
        Console.WriteLine("Logged out successfully.");
    }

    public void DisplayTransferSuccess(decimal newBalance)
    {
        Console.WriteLine("Transfer successful.");
        Console.WriteLine($"Remaining balance: {newBalance:N2}");
    }

    public void DisplayInvalidTransferAmount()
    {
        Console.WriteLine("Amount must be greater than zero.");
    }

    public void DisplaySameAccountError()
    {
        Console.WriteLine("You can't transfer money to your own account.");
    }

    public void DisplayRecipientNotFound()
    {
        Console.WriteLine("Recipient account not found. Check the account number and try again.");
    }

    public void DisplayInsufficientFunds()
    {
        Console.WriteLine("Insufficient funds.");
    }

    public void DisplayNoTransactions()
    {
        Console.WriteLine("You don't have any transactions yet.");
    }

    public void DisplayTransactionHistory(IReadOnlyList<Transaction> transactions)
    {
        Console.WriteLine($"\n{"Date",-18}{"Type",-12}{"Amount",14}   Description");
        Console.WriteLine(new string('-', 95));

        foreach (Transaction transaction in transactions)
        {
            Console.WriteLine($"{transaction.Date,-18:dd MMM yyyy HH:mm}{transaction.Type,-12}{transaction.Amount,14:N2}   {transaction.Description}");
        }
    }

    public void DisplayInvalidOption()
    {
        Console.WriteLine("Invalid option.");
    }

    public void OnAccountCreatedSuccess(Account account)
    {
        Console.WriteLine("\nAccount created successfully!");
        Console.WriteLine($"Account number: {account.AccountNumber}");
        Console.WriteLine($"Account holder: {account.AccountHolder.Name}");
        Console.WriteLine($"Opening balance: {account.Balance:N2}");
    }

    public void DisplayDepositSuccess(decimal amount, decimal newBalance)
    {
        Console.WriteLine($"Deposit successful. Amount: {amount:N2}");
        Console.WriteLine($"New balance: {newBalance:N2}");
    }

    public void DisplayAccountFrozen()
    {
        Console.WriteLine("This account is frozen. Only balance and transaction history are available.");
    }

    public void DisplayInterBankTransferSuccess(decimal amount, decimal newBalance)
    {
        Console.WriteLine($"Inter-bank transfer successful. Amount: {amount:N2}");
        Console.WriteLine($"Remaining balance: {newBalance:N2}");
    }

    public void DisplayInterBankTransferFailed(string message)
    {
        Console.WriteLine($"Inter-bank transfer failed: {message}");
        Console.WriteLine("The amount has been reversed on this account.");
    }

}