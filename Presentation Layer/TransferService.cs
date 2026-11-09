namespace NovaBank
{
    /// <summary>
    /// Reads the recipient and the amount, then hands the transfer to the first route that
    /// answers for that account number. Which bank owns a number is the routes' business, not
    /// this class's: adding a third bank is one more registration, not an edit here.
    /// </summary>
    public class TransferService
    {
        private readonly IReadOnlyList<ITransferRoute> _routes;

        private readonly IInputReader _input;

        private readonly ITransferScreen _screen;

        public TransferService(
            IEnumerable<ITransferRoute> routes,
            IInputReader input,
            ITransferScreen screen)
        {
            _routes = routes.ToList();
            _input = input;
            _screen = screen;
        }

        public void Transfer(Account sender)
        {
            string recipientAccountNumber = _input.ReadRecipientAccountNumber();

            if (!_input.TryReadAmount(
                "How much do you want to transfer? ",
                out decimal amount))
            {
                return;
            }

            ITransferRoute? route = _routes.FirstOrDefault(
                candidate => candidate.Handles(recipientAccountNumber));

            if (route == null)
            {
                // The registered routes cover own and non-own numbers between them, so this
                // only fires when the container is missing a route.
                _screen.DisplayRecipientNotFound();

                return;
            }

            route.Transfer(sender, recipientAccountNumber, amount);
        }
    }
}
