namespace NovaBank.Http;

public class AirtimeRequestDto
{
    public string Product { get; set; } = "";

    public string Phone { get; set; } = "";

    public string Network { get; set; } = "";

    public string Plan { get; set; } = "";

    public decimal Amount { get; set; }

    public string CustomerAccount { get; set; } = "";
}
