
namespace NovaBank.Http;

public class TransferRequestDto
{
    public string AccountNumber { get; set; } = "";
    public decimal Amount { get; set; }

    // Same reference on a retry means "the same transfer": do not apply it twice.
    public string TransferId { get; set; } = "";

    // Who sent it, so the receiving bank writes a real counterparty line instead of guessing.
    public string SenderName { get; set; } = "";

    public string SenderAccountNumber { get; set; } = "";

    public string SenderBank { get; set; } = "";
}
