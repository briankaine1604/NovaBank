using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace NovaBank.Http;

internal class BankHttpServer
{
    private const string TransferPath = "/transfers";

    private readonly HttpListener _listener = new();

    private readonly Bank _bank;

    private readonly ServerConsole _serverConsole;

    public BankHttpServer(Bank bank, ServerConsole serverConsole, string prefix)
    {
        _bank = bank;
        _serverConsole = serverConsole;
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
            "Inter-bank transfer received from CrestBank",
            "Transfer In");

        _serverConsole.DisplayIncomingTransfer(
            transfer.AccountNumber,
            transfer.Amount,
            recipient.Balance);

        byte[] buffer = Encoding.UTF8.GetBytes(
            "Transfer successful. Recipient account credited.");

        context.Response.ContentType = "text/plain; charset=utf-8";
        context.Response.ContentLength64 = buffer.Length;

        await context.Response.OutputStream.WriteAsync(buffer);
        context.Response.Close();
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

        byte[] buffer = Encoding.UTF8.GetBytes(message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/plain; charset=utf-8";
        context.Response.ContentLength64 = buffer.Length;

        await context.Response.OutputStream.WriteAsync(buffer);
        context.Response.Close();
    }
}
