namespace NovaBank
{
    /// <summary>
    /// Which account numbers this bank owns. The rule is configuration, not a constant buried in
    /// the transfer code, and both routes ask this one object - so the two copies of the rule
    /// cannot drift apart.
    /// </summary>
    public class LocalAccountNumbers
    {
        private readonly string[] _prefixes;

        public LocalAccountNumbers(params string[] prefixes)
        {
            if (prefixes.Length == 0)
            {
                throw new ArgumentException(
                    "At least one own-account prefix is required.",
                    nameof(prefixes));
            }

            _prefixes = prefixes;
        }

        public bool IsOwn(string accountNumber) =>
            _prefixes.Any(prefix =>
                accountNumber.StartsWith(prefix, StringComparison.Ordinal));
    }
}
