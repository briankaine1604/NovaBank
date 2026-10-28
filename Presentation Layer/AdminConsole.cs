namespace NovaBank;

public class AdminConsole : IAdminConsole
{
    public void DisplayAdminMenu()
    {
        Console.WriteLine("\n=== ADMIN MENU ===");
        Console.WriteLine("1. View all accounts");
        Console.WriteLine("2. Find an account");
        Console.WriteLine("3. Manage an account");
        Console.WriteLine("4. Logout");
        Console.Write("Choose an option: ");
    }

    public string ReadUsername()
    {
        Console.Write("Enter admin username: ");

        return Console.ReadLine()?.Trim() ?? "";
    }

    public string ReadPassword()
    {
        Console.Write("Enter admin password: ");

        return Console.ReadLine()?.Trim() ?? "";
    }

    public void DisplayInvalidAdminLogin()
    {
        Console.WriteLine("Invalid username or password.");
    }

    public void DisplayAdminLoginSuccess()
    {
        Console.WriteLine("Admin login successful!");
    }

    public void DisplayAccounts(IEnumerable<Account> accounts)
    {
        Console.WriteLine("\n=== ALL ACCOUNTS ===");

        foreach (Account account in accounts)
        {
            string status = account.IsFrozen ? "Frozen" : "Active";

            Console.WriteLine($"Account Number: {account.AccountNumber}");
            Console.WriteLine($"Name: {account.AccountHolder.Name}");
            Console.WriteLine($"Balance: {account.Balance:N2}");
            Console.WriteLine($"Status: {status}");
            Console.WriteLine("--------------------");
        }
    }

    public void DisplayAccount(Account account)
    {
        Console.WriteLine("\n=== ACCOUNT DETAILS ===");
        Console.WriteLine($"Account Number: {account.AccountNumber}");
        Console.WriteLine($"Name: {account.AccountHolder.Name}");
        Console.WriteLine($"Balance: {account.Balance:N2}");
    }

    public void DisplayAccountNotFound()
    {
        Console.WriteLine("Account not found.");
    }

    public void DisplayNoAccounts()
    {
        Console.WriteLine("No accounts available.");
    }

    public void DisplayAccountSelection(IEnumerable<Account> accounts)
    {
        Console.WriteLine("\n=== SELECT AN ACCOUNT ===");

        int option = 1;

        foreach (Account account in accounts)
        {
            string status = account.IsFrozen ? "Frozen" : "Active";

            Console.WriteLine(
                $"{option}. {account.AccountHolder.Name} | " +
                $"{account.AccountNumber} | {status}");

            option++;
        }

        Console.WriteLine("0. Back");
        Console.Write("Choose an account: ");
    }

    public int? ChooseAccountSelection(int accountCount)
    {
        if (!int.TryParse(Console.ReadLine(), out int selection)
            || selection < 0 || selection > accountCount)
        {
            return null;
        }

        return selection;
    }

    public void DisplayManageAccountMenu(Account account)
    {
        Console.WriteLine("\n=== MANAGE ACCOUNT ===");
        Console.WriteLine($"Account Number: {account.AccountNumber}");
        Console.WriteLine($"Name: {account.AccountHolder.Name}");
        Console.WriteLine($"Balance: {account.Balance:N2}");
        Console.WriteLine($"Status: {(account.IsFrozen ? "Frozen" : "Active")}");

        if (account.IsFrozen)
        {
            Console.WriteLine("1. Unfreeze account");
        }
        else
        {
            Console.WriteLine("1. Freeze account");
        }

        Console.WriteLine("2. Back");
        Console.Write("Choose an option: ");
    }

    public void DisplayFreezeSuccess()
    {
        Console.WriteLine("Account frozen successfully.");
    }

    public void DisplayUnfreezeSuccess()
    {
        Console.WriteLine("Account unfrozen successfully.");
    }

    
}
