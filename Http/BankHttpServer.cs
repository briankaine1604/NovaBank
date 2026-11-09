using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace NovaBank.Http;

internal class BankHttpServer : IBankHttpServer
{
    private const string TransferPath = "/transfers";

    private const string AirtimePath = "/airtime";

    private const string DataPath = "/data";

    private const string SuccessMessage = "Transfer successful. Recipient account credited.";

    private readonly HttpListener _listener = new();

    private readonly Bank _bank;

    private readonly AirtimeProvider _airtime;

    private readonly IServerConsole _serverConsole;

    private readonly string _sharedSecret;

    // References already applied. A retry under the same reference must not credit again.
    private readonly Dictionary<string, string> _processedTransfers = new();

    public BankHttpServer(
        Bank bank,
        IServerConsole serverConsole,
        string prefix,
        string sharedSecret,
        AirtimeProvider airtime)
    {
        _bank = bank;
        _airtime = airtime;
        _serverConsole = serverConsole;
        _sharedSecret = sharedSecret;
        _listener.Prefixes.Add(prefix);
    }


    public async Task Start()
    {
        try
        {
            _listener.Start();
        }
        catch (HttpListenerException ex)
        {
            _serverConsole.DisplayServerStartFailed(ex.Message, ex.ErrorCode);

            return;
        }

        _serverConsole.DisplayServerStarted(_listener.Prefixes.First());

        while (true)
        {
            HttpListenerContext context = await _listener.GetContextAsync();

            // One bad request must not take the whole listener down with it.
            try
            {
                await Route(context);
            }
            catch (Exception ex)
            {
                _serverConsole.DisplayTransferRejected($"unexpected error: {ex.Message}");

                // Last resort: never leave a caller waiting on a silent socket.
                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch (Exception)
                {
                    // the connection is already gone - nothing left to report
                }
            }
        }
    }

    private Task Route(HttpListenerContext context)
    {
        string path = context.Request.Url?.AbsolutePath ?? "";

        return path switch
        {
            AirtimePath => HandleAirtime(context, "airtime"),
            DataPath => HandleAirtime(context, "data"),
            _ => HandleTransfer(context)
        };
    }

    private async Task HandleTransfer(HttpListenerContext context)
    {
        // Only POST /transfers moves money; everything else is refused before the body is read.
        if (context.Request.HttpMethod != "POST")
        {
            context.Response.AddHeader("Allow", "POST");

            await Reject(
                context,
                405,
                "Method not allowed.",
                $"method {context.Request.HttpMethod} {context.Request.Url?.AbsolutePath}");

            return;
        }

        if (context.Request.Url?.AbsolutePath != TransferPath)
        {
            await Reject(
                context,
                404,
                "Unknown endpoint.",
                $"endpoint {context.Request.HttpMethod} {context.Request.Url?.AbsolutePath}");

            return;
        }

        using var reader = new StreamReader(context.Request.InputStream);
        string body = await reader.ReadToEndAsync();

        if (!TryAuthenticate(context, body, out string authFailure))
        {
            await Reject(context, 401, "Unauthorized.", authFailure);

            return;
        }

        TransferRequestDto? transfer;

        try
        {
            transfer = JsonSerializer.Deserialize<TransferRequestDto>(body);
        }
        catch (JsonException ex)
        {
            // Malformed or empty body: answer the caller instead of leaving it waiting.
            await Reject(
                context,
                400,
                "Malformed transfer request.",
                $"unreadable JSON body: {ex.Message}");

            return;
        }

        if (transfer == null)
        {
            await Reject(context, "request body could not be read");

            return;
        }

        // No reference means a retry cannot be told from a new transfer: refuse it.
        if (string.IsNullOrWhiteSpace(transfer.TransferId))
        {
            await Reject(context, "missing transfer reference");

            return;
        }

        // Already applied: return the original outcome and credit nothing.
        if (_processedTransfers.TryGetValue(transfer.TransferId, out string? applied))
        {
            _serverConsole.DisplayDuplicateTransfer(transfer.TransferId);

            await WriteResponse(context, 200, applied);

            return;
        }

        // The statement line needs these, so a transfer with no sender is refused.
        if (string.IsNullOrWhiteSpace(transfer.SenderName)
            || string.IsNullOrWhiteSpace(transfer.SenderAccountNumber)
            || string.IsNullOrWhiteSpace(transfer.SenderBank))
        {
            await Reject(context, "missing sender details");

            return;
        }

        if (transfer.Amount <= 0)
        {
            await Reject(context, "amount must be greater than zero");

            return;
        }

        Account? recipient = _bank.GetAccount(transfer.AccountNumber);

        if (recipient == null)
        {
            await Reject(context, $"account {transfer.AccountNumber} not found");

            return;
        }

        recipient.Credit(
            transfer.Amount,
            $"Inter-bank transfer from {transfer.SenderName} " +
            $"{transfer.SenderAccountNumber} ({transfer.SenderBank})",
            "Transfer In");

        // Record the outcome BEFORE answering, so a concurrent retry sees it as applied.
        _processedTransfers[transfer.TransferId] = SuccessMessage;

        _serverConsole.DisplayIncomingTransfer(
            transfer.SenderName,
            transfer.SenderAccountNumber,
            transfer.SenderBank,
            transfer.AccountNumber,
            transfer.Amount,
            recipient.Balance);

        await WriteResponse(context, 200, SuccessMessage);
    }

    private async Task HandleAirtime(HttpListenerContext context, string product)
    {
        if (context.Request.HttpMethod != "POST")
        {
            context.Response.AddHeader("Allow", "POST");

            await Reject(
                context,
                405,
                "Method not allowed.",
                $"method {context.Request.HttpMethod} {context.Request.Url?.AbsolutePath}");

            return;
        }

        using var reader = new StreamReader(context.Request.InputStream);
        string body = await reader.ReadToEndAsync();

        if (!TryAuthenticate(context, body, out string authFailure))
        {
            await Reject(context, 401, "Unauthorized.", authFailure);

            return;
        }

        AirtimeRequestDto? order;

        try
        {
            order = JsonSerializer.Deserialize<AirtimeRequestDto>(body);
        }
        catch (JsonException ex)
        {
            await Reject(context, 400, "Malformed request.", $"unreadable JSON body: {ex.Message}");

            return;
        }

        if (order == null)
        {
            await Reject(context, 400, "Malformed request.", "empty body");

            return;
        }

        if (!_airtime.TrySell(product, order.Phone, order.Network, order.Plan, order.Amount, out string result))
        {
            await Reject(context, 400, result, $"{product} for {order.Phone} refused: {result}");

            return;
        }

        _serverConsole.DisplayAirtimeSale(
            product, order.Phone, order.Network, order.Amount, order.CustomerAccount, result);

        await WriteResponse(context, 200, result);
    }

    private bool TryAuthenticate(HttpListenerContext context, string body, out string failure)
    {
        // Authenticate the RAW BYTES before parsing: nothing in the body is trusted yet.
        string timestamp = context.Request.Headers["X-Bank-Timestamp"] ?? "";
        string signature = context.Request.Headers["X-Bank-Signature"] ?? "";

        if (!RequestSignature.IsFresh(timestamp))
        {
            failure = $"missing, malformed or stale timestamp '{timestamp}'";

            return false;
        }

        if (!RequestSignature.Verify(_sharedSecret, timestamp, body, signature))
        {
            failure = "signature did not match the body or the shared secret";

            return false;
        }

        failure = "";

        return true;
    }

    private async Task Reject(HttpListenerContext context, string reason)
    {
        await Reject(context, 400, "Invalid transfer request.", reason);
    }

    private async Task Reject(
        HttpListenerContext context,
        int statusCode,
        string message,
        string reason)
    {
        _serverConsole.DisplayTransferRejected(reason);

        await WriteResponse(context, statusCode, message);
    }

    private static async Task WriteResponse(
        HttpListenerContext context,
        int statusCode,
        string message)
    {
        // ContentLength64 has to be the BYTE count, not the character count.
        byte[] buffer = Encoding.UTF8.GetBytes(message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/plain; charset=utf-8";
        context.Response.ContentLength64 = buffer.Length;

        await context.Response.OutputStream.WriteAsync(buffer);
        context.Response.Close();
    }
}
