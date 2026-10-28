namespace NovaBank
{
    /// <summary>
    /// Airtime and data. Both are a debit plus a receipt; neither leaves this bank.
    /// </summary>
    public class AirtimeService
    {
        private readonly IAirtimeScreen _screen;

        private readonly IInputReader _input;

        private readonly ICommonOutcomes _outcomes;

        public AirtimeService(
            IAirtimeScreen screen,
            IInputReader input,
            ICommonOutcomes outcomes)
        {
            _screen = screen;
            _input = input;
            _outcomes = outcomes;
        }

        public void BuyAirtime(Account account)
        {
            string? phone = _input.ReadPhoneNumber();
            if (phone == null) return;

            string? network = _input.ChooseNetwork();
            if (network == null)
            {
                _screen.DisplayInvalidNetwork();
                return;
            }

            if (!_input.TryReadAmount(
                "How much do you want to recharge? ",
                out decimal amount))
                return;

            bool success = account.TryDebit(
                amount,
                $"Airtime for {phone} ({network})",
                "Airtime");

            if (success)
                _screen.DisplayAirtimePurchaseSuccess(amount, phone, network, account.Balance);
            else
                _outcomes.DisplayInsufficientFunds();
        }

        public void BuyData(Account account)
        {
            string? phone = _input.ReadPhoneNumber();
            if (phone == null) return;

            string? network = _input.ChooseNetwork();
            if (network == null)
            {
                _screen.DisplayInvalidNetwork();
                return;
            }

            DataPlan? plan = _input.ChooseDataPlan();
            if (plan == null) return;

            bool success = account.TryDebit(
                plan.Price,
                $"{plan.Name} data for {phone} ({network})",
                "Data");

            if (success)
            {
                _screen.DisplayDataPurchaseSuccess(
                    plan, phone, network, account.Balance);
            }
            else
            {
                _outcomes.DisplayInsufficientFunds();
            }
        }
    }
}
