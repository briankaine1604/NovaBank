namespace NovaBank;

/// <summary>
/// Sells airtime and data on behalf of whoever asks: this bank's own ATM, or the
/// peer bank over HTTP. It holds no customer money - the caller has already
/// debited its own customer - so all it does is check the order and fulfil it.
/// </summary>
public class AirtimeProvider
{
    private static readonly string[] Networks = { "MTN", "Airtel", "Glo", "9mobile" };

    public bool TrySell(
        string product,
        string phone,
        string network,
        string plan,
        decimal amount,
        out string result)
    {
        result = "";

        if (product != "airtime" && product != "data")
        {
            result = $"unknown product '{product}'";

            return false;
        }

        if (!Networks.Contains(network))
        {
            result = $"network '{network}' is not one we sell";

            return false;
        }

        if (phone.Length < 10)
        {
            result = $"'{phone}' is not a phone number";

            return false;
        }

        if (amount <= 0)
        {
            result = "amount must be greater than zero";

            return false;
        }

        if (product == "data")
        {
            DataPlan? selected = DataPlans.All.FirstOrDefault(p => p.Name == plan);

            if (selected == null)
            {
                result = $"no data plan called '{plan}'";

                return false;
            }

            if (selected.Price != amount)
            {
                result = $"{plan} costs {selected.Price:N2}, not {amount:N2}";

                return false;
            }
        }

        string reference = product.Substring(0, 3).ToUpper()
            + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

        result = $"{(product == "data" ? "Data" : "Airtime")} delivered. Reference {reference}.";

        return true;
    }
}
