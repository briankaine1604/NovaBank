using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace NovaBank.Http
{
    public class BankHttpClient
    {
        private readonly HttpClient _client;

        public BankHttpClient(string baseAddress)
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(baseAddress),
                Timeout = TimeSpan.FromSeconds(3)
            };
        }

        public async Task<(bool Success, string Message)> TransferAsync(
            string accountNumber,
            decimal amount)
        {
            var transfer = new TransferRequest
            {
                AccountNumber = accountNumber,
                Amount = amount
            };

            string json = JsonSerializer.Serialize(transfer);

            using var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            try
            {
                using HttpResponseMessage response =
                    await _client.PostAsync("transfers", content);

                string message = await response.Content.ReadAsStringAsync();

                return (response.IsSuccessStatusCode, message);
            }
            catch (HttpRequestException)
            {
                // The other bank is not running, or refused the connection.
                return (false, "Recipient bank is unreachable. Nothing was sent.");
            }
            catch (TaskCanceledException)
            {
                // The other bank accepted the connection but never answered.
                return (false, "Recipient bank did not answer, so the outcome is unknown. "
                             + "Check with the bank before retrying.");
            }
        }
    }
}
