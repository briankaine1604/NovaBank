namespace NovaBank
{
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
