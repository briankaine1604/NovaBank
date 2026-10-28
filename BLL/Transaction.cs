using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBank;

public class Transaction
{
    public decimal Amount { get; }
    public string Type { get; }
    public string Description { get; }
    public DateTime Date { get; }

    public Transaction(decimal amount, string type, string description, DateTime dateTime)
    {
        Amount = amount;
        Type = type;
        Description = description;
        Date= dateTime;
    }
}
