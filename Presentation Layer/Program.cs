using Microsoft.Extensions.DependencyInjection;
using NovaBank;
using NovaBank.Configuration;
using NovaBank.Http;

BankConfig config = BankConfig.Load();

Console.Title = $"{config.Name} ATM  (Bank A)";

var peerLauncher = new PeerLauncher(config);

peerLauncher.Start();

AppDomain.CurrentDomain.ProcessExit += (_, _) => peerLauncher.Dispose();
Console.CancelKeyPress += (_, _) => peerLauncher.Dispose();

var services = new ServiceCollection();

services.AddSingleton(_ =>
{
    var bank = new Bank();
    BankSeeder.Seed(bank);
    return bank;
});

services.AddSingleton(_ => new Admin("admin", "password"));

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

const string OwnAccountPrefix = "000";

services.AddSingleton(_ => new LocalAccountNumbers(OwnAccountPrefix));

// Order matters: the first route that handles the number takes the transfer.
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

_ = provider.GetRequiredService<IBankHttpServer>().Start();

ATM atm = provider.GetRequiredService<ATM>();

atm.Run();

peerLauncher.Dispose();
