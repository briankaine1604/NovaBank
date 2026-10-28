namespace NovaBank
{
    /// <summary>
    /// Every prompt that takes typed input. Kept as one interface because a single
    /// implementation serves every screen, and a caller that only reads should not
    /// have to see the messages as well.
    /// </summary>
    public interface IInputReader
    {
        string? ReadMenuChoice();

        bool TryReadAmount(string prompt, out decimal amount);

        string ReadAccountNumber();

        string ReadPin();

        string ReadNewPin();

        string ReadName(string prompt);

        string ReadRecipientAccountNumber();

        string? ReadPhoneNumber();

        string? ChooseNetwork();

        DataPlan? ChooseDataPlan();
    }
}
