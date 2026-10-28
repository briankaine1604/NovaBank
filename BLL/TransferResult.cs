using System;
using System.Collections.Generic;
using System.Text;

namespace NovaBank;

public enum TransferResult
{
    Success,
    AmountInvalid,
    SameAccount,
    RecipientInvalid,
    InsufficientFunds
}
