namespace NovaBank
{
    /// <summary>
    /// One message, shared by every operation that tries to move money and cannot:
    /// withdrawal, transfer, airtime and data all report it the same way. It sits
    /// apart so no single screen has to own a sentence the others also print.
    /// </summary>
    public interface ICommonOutcomes
    {
        void DisplayInsufficientFunds();
    }
}
