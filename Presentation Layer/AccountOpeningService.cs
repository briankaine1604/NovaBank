namespace NovaBank
{
    /// <summary>
    /// Opening an account: ask for the details, then hand them to the domain.
    /// Knows nothing about menus or balances.
    /// </summary>
    public class AccountOpeningService
    {
        private readonly Bank _bank;

        private readonly IAccountOpeningScreen _screen;

        private readonly IInputReader _input;

        public AccountOpeningService(
            Bank bank,
            IAccountOpeningScreen screen,
            IInputReader input)
        {
            _bank = bank;
            _screen = screen;
            _input = input;
        }

        public void CreateAccount()
        {
            string name = _input.ReadName("Enter your name: ");
            string city = _input.ReadName("Enter your city: ");

            string pin = _input.ReadNewPin();

            decimal deposit;
            while (!_input.TryReadAmount("How much do you want to deposit?: ", out deposit))
            {
            }

            Account account = _bank.CreateAccount(name, city, pin, deposit);

            _screen.DisplayAccountCreated(account);
        }
    }
}
