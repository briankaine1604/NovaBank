using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace NovaBank.Http
{
    public class BankHttpClient
    {
        private readonly HttpClient _client;

        private readonly string _sharedSecret;

        public BankHttpClient(string baseAddress, string sharedSecret)
        {
            _sharedSecret = sharedSecret;

            _client = new HttpClient
            {
                BaseAddress = new Uri(baseAddress),
                Timeout = TimeSpan.FromSeconds(3)
            };
        }

        public async Task<(bool Success, string Message)> TransferAsync(TransferRequest transfer)
        {
            string json = JsonSerializer.Serialize(transfer);

            // Signed per attempt with a fresh timestamp, so the retry carries a new
            // valid signature over the same body instead of a stale replay.
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            using var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, "transfers")
            {
                Content = content
            };

            request.Headers.Add("X-Bank-Timestamp", timestamp);
            request.Headers.Add(
                "X-Bank-Signature",
                RequestSignature.Sign(_sharedSecret, timestamp, json));

            try
            {
                using HttpResponseMessage response = await _client.SendAsync(request);

                string message = await response.Content.ReadAsStringAsync();

                // 401 is a credentials problem, not a transfer problem: say so
                // instead of passing the bare word "Unauthorized." to the customer.
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return (false, "Recipient bank rejected the request signature.");
                }

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
