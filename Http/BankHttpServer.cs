using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace NovaBank.Http;

internal class BankHttpServer
{
    private const string TransferPath = "/transfers";

    private const string SuccessMessage = "Transfer successful. Recipient account credited.";

    private readonly HttpListener _listener = new();

    private readonly Bank _bank;

    private readonly ServerConsole _serverConsole;

    private readonly string _sharedSecret;

    // One entry per transfer reference this bank has already applied. A retry
    // that arrives with the same reference must not credit the account again.
    private readonly Dictionary<string, string> _processedTransfers = new();

    public BankHttpServer(
        Bank bank,
        ServerConsole serverConsole,
        string prefix,
        string sharedSecret)
    {
        _bank = bank;
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
                await HandleTransfer(context);
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

    private async Task HandleTransfer(HttpListenerContext context)
    {
        // Only POST /transfers is a transfer; everything else is refused before
        // the body is read, so no other request can move money.
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

        // Authenticate the raw bytes BEFORE parsing them: nothing in the body is
        // trusted until the signature over that exact body checks out.
        string timestamp = context.Request.Headers["X-Bank-Timestamp"] ?? "";
        string signature = context.Request.Headers["X-Bank-Signature"] ?? "";

        if (!RequestSignature.IsFresh(timestamp))
        {
            await Reject(
                context,
                401,
                "Unauthorized.",
                $"missing, malformed or stale timestamp '{timestamp}'");

            return;
        }

        if (!RequestSignature.Verify(_sharedSecret, timestamp, body, signature))
        {
            await Reject(
                context,
                401,
                "Unauthorized.",
                "signature did not match the body or the shared secret");

            return;
        }

        TransferRequest? transfer;

        try
        {
            transfer = JsonSerializer.Deserialize<TransferRequest>(body);
        }
        catch (JsonException ex)
        {
            // Malformed, empty or wrongly-typed body: answer the caller instead
            // of letting the exception escape and leave it waiting.
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

        // Without a reference there is no way to tell a retry from a new
        // transfer, so refuse it rather than risk crediting twice.
        if (string.IsNullOrWhiteSpace(transfer.TransferId))
        {
            await Reject(context, "missing transfer reference");

            return;
        }

        // Already applied under this reference: answer with the original
        // outcome and credit nothing. This is what makes a retry safe, and it is
        // checked first so a retry resolves on its reference alone.
        if (_processedTransfers.TryGetValue(transfer.TransferId, out string? applied))
        {
            _serverConsole.DisplayDuplicateTransfer(transfer.TransferId);

            await WriteResponse(context, 200, applied);

            return;
        }

        // The statement line is built from these, so a transfer with no sender
        // is refused rather than credited as "unknown".
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

        // Record the outcome before answering, so a retry that lands while the
        // first answer is still in flight sees it as applied.
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
