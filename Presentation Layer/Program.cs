using Microsoft.Extensions.DependencyInjection;
using NovaBank;
using NovaBank.Configuration;
using NovaBank.Http;

// NovaBank is Bank A and the entry point of the pair. bank.config.json holds every
// address and the shared secret, and names the project to start alongside this one.
BankConfig config = BankConfig.Load();

Console.Title = $"{config.Name} ATM  (Bank A)";

var peerLauncher = new PeerLauncher(config);

peerLauncher.Start();

// The peer shares a fixed port with nothing else, so it must not outlive us.
AppDomain.CurrentDomain.ProcessExit += (_, _) => peerLauncher.Dispose();
Console.CancelKeyPress += (_, _) => peerLauncher.Dispose();

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
    _ => new BankHttpClient(config.PeerAddress, config.Secret));

// Which account numbers this bank owns. Everything else is routed to the peer over HTTP.
// Not in the config: the prefix is baked into every account number this bank issues
// (the seeder and Bank's ten-digit generator), so changing it is a data change, not a setting.
const string OwnAccountPrefix = "000";

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
    config.Name,
    config.PeerName));

services.AddSingleton<IBankHttpServer>(
    provider => new BankHttpServer(
        provider.GetRequiredService<Bank>(),
        provider.GetRequiredService<IServerConsole>(),
        config.Address,
        config.Secret));

services.AddSingleton<ATM>();

await using ServiceProvider provider = services.BuildServiceProvider();

// Talks to the peer, and listens for the peer talking to us.
_ = provider.GetRequiredService<IBankHttpServer>().Start();

ATM atm = provider.GetRequiredService<ATM>();

atm.Run();

// Reached when the user picks Exit: take the peer down with us and free both ports.
peerLauncher.Dispose();
