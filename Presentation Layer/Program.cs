using Microsoft.Extensions.DependencyInjection;
using NovaBank;
using NovaBank.Http;

Console.Title = "NovaBank ATM  (Bank A)";

// Shared secret for the inter-bank channel; both banks must hold the same value.
// A real deployment keeps one key per peer in configuration, never in source.
const string InterBankSecret = "nova-crest-demo-secret";

const string PeerBankAddress = "http://localhost:5002/";
const string OwnAddress = "http://localhost:5001/";

var services = new ServiceCollection();

// One Bank for the life of the process, shared by the ATM and the HTTP listener.
services.AddSingleton(_ =>
{
    var bank = new Bank();
    BankSeeder.Seed(bank);
    return bank;
});

services.AddSingleton(_ => new Admin("admin", "password"));

// ATMConsole is ONE object registered under each role, so each service gets only the slice it asks for.
services.AddSingleton<ATMConsole>();
services.AddSingleton<IInputReader>(p => p.GetRequiredService<ATMConsole>());
services.AddSingleton<IMenuScreen>(p => p.GetRequiredService<ATMConsole>());
services.AddSingleton<IAccountScreen>(p => p.GetRequiredService<ATMConsole>());
services.AddSingleton<ITransferScreen>(p => p.GetRequiredService<ATMConsole>());
services.AddSingleton<IAirtimeScreen>(p => p.GetRequiredService<ATMConsole>());
services.AddSingleton<IAccountOpeningScreen>(p => p.GetRequiredService<ATMConsole>());
services.AddSingleton<ICommonOutcomes>(p => p.GetRequiredService<ATMConsole>());

services.AddSingleton<IAdminConsole, AdminConsole>();
services.AddSingleton<IServerConsole, ServerConsole>();

services.AddSingleton<AccountOpeningService>();
services.AddSingleton<AccountService>();
services.AddSingleton<TransferService>();
services.AddSingleton<AirtimeService>();
services.AddSingleton<AdminService>();

services.AddSingleton<IBankHttpClient>(
    _ => new BankHttpClient(PeerBankAddress, InterBankSecret));

// Which account numbers this bank owns. Everything else is routed to the peer over HTTP.
const string OwnAccountPrefix = "000";
const string BankName = "NovaBank";
const string PeerBankName = "CrestBank";

services.AddSingleton(_ => new LocalAccountNumbers(OwnAccountPrefix));

// Routes in priority order: the first one that handles the recipient takes the transfer.
services.AddSingleton<ITransferRoute>(provider => new LocalTransferRoute(
    provider.GetRequiredService<Bank>(),
    provider.GetRequiredService<LocalAccountNumbers>(),
    provider.GetRequiredService<ITransferScreen>(),
    provider.GetRequiredService<ICommonOutcomes>()));

services.AddSingleton<ITransferRoute>(provider => new InterBankTransferRoute(
    provider.GetRequiredService<IBankHttpClient>(),
    provider.GetRequiredService<LocalAccountNumbers>(),
    provider.GetRequiredService<ITransferScreen>(),
    provider.GetRequiredService<ICommonOutcomes>(),
    BankName,
    PeerBankName));

services.AddSingleton<IBankHttpServer>(
    provider => new BankHttpServer(
        provider.GetRequiredService<Bank>(),
        provider.GetRequiredService<IServerConsole>(),
        OwnAddress,
        InterBankSecret));

services.AddSingleton<ATM>();

await using ServiceProvider provider = services.BuildServiceProvider();

// Talks to CrestBank, and listens for CrestBank talking to us.
_ = provider.GetRequiredService<IBankHttpServer>().Start();

ATM atm = provider.GetRequiredService<ATM>();

atm.Run();
