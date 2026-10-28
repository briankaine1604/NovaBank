using NovaBank;
using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBank;
public static class BankSeeder
{
    public static void Seed(Bank bank)
    {
        Customer ada = new("Ada", "Benin");
        Account account = new("0000000998", ada, 0m, "1234");
        account.Credit(50000m, "Opening deposit", "Deposit");
        bank.AddAccount(account);

        Customer john = new("John", "Bayelsa");
        Account account2 = new("0000000999", john, 0m, "1234");
        account2.Credit(500000m, "Opening deposit", "Deposit");
        bank.AddAccount(account2);
    }
}
