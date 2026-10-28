namespace NovaBank
{
    /// <summary>The only thing the account-opening flow says: the account exists now.</summary>
    public interface IAccountOpeningScreen
    {
        void DisplayAccountCreated(Account account);
    }
}
