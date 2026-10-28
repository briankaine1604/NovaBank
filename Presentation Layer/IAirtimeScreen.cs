namespace NovaBank
{
    /// <summary>Airtime and data purchases: network choice failures and receipts.</summary>
    public interface IAirtimeScreen
    {
        void DisplayInvalidNetwork();

        void DisplayAirtimePurchaseSuccess(decimal amount, string phone, string network, decimal newBalance);

        void DisplayDataPurchaseSuccess(DataPlan plan, string phone, string network, decimal newBalance);
    }
}
