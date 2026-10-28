using System;
using System.Collections.Generic;
using System.Text;


namespace NovaBank;

public class Account
{
    public string AccountNumber { get; private set; } = String.Empty;

    public decimal Balance { get; private set; } = 0.0m;

    public string AccountPin { get; private set; } = String.Empty;

    public Customer AccountHolder { get; private set; }

    public bool IsFrozen { get; private set; }

    private readonly List<Transaction> _transactions = new();
    public IReadOnlyList<Transaction> Transactions => _transactions;
    public Account(string accountNumber, Customer holder, decimal openingBalance, string accountPin)
    {
        AccountNumber = accountNumber;
        AccountHolder = holder;
        Balance = openingBalance;
        AccountPin = accountPin;
       
    }

    public void Freeze()
    {
        IsFrozen = true;
    }

    public void Unfreeze()
    {
        IsFrozen = false;
    }

    public bool TryDebit(decimal amount, string description, string type)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount), "Withdrawal amount must be greater than zero.");

        if (amount > Balance)
            return false;

        Balance -= amount;
        AddTransaction(new Transaction(amount, type, description , DateTime.Now));
        return true;
    }

    public void Credit(decimal amount, string description, string type)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        Balance += amount;
        AddTransaction(new Transaction(amount, type, description, DateTime.Now));
    }


    private void AddTransaction(Transaction transaction)
    {
        _transactions.Add(transaction);
    }

}
