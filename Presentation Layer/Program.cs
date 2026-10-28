using Microsoft.Extensions.DependencyInjection;
using NovaBank;
using NovaBank.Http;

Console.Title = "NovaBank ATM  (Bank A)";

// Shared secret for the inter-bank channel; both banks must hold the same value.
// A real deployment keeps one key per peer in configuration, never in source.
const string InterBankSecret = "nova-crest-demo-secret-change-me";

const string PeerBankAddress = "http://localhost:5002/";
const string OwnAddress = "http://localhost:5001/";

var services = new ServiceCollection();

// One Bank for the life of the process: seeded once here and shared by the ATM
// and the HTTP listener, so both sides see the same accounts.
services.AddSingleton(_ =>
{
    var bank = new Bank();
    BankSeeder.Seed(bank);
    return bank;
});

services.AddSingleton(_ => new Admin("admin", "password"));

// Screens behind interfaces: nothing in the app names a concrete console.
// ATMConsole is ONE object registered under each role it plays, so every service
// gets the narrow slice it asked for instead of the whole 32-method surface.
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

// Use cases, one per job the menus can start.
services.AddSingleton<AccountOpeningService>();
services.AddSingleton<AccountService>();
services.AddSingleton<TransferService>();
services.AddSingleton<AirtimeService>();
services.AddSingleton<AdminService>();

// Transport. Addresses and the shared secret are wired in one place.
services.AddSingleton<IBankHttpClient>(
    _ => new BankHttpClient(PeerBankAddress, InterBankSecret));

services.AddSingleton<IBankHttpServer>(
    provider => new BankHttpServer(
        provider.GetRequiredService<Bank>(),
        provider.GetRequiredService<IServerConsole>(),
        OwnAddress,
        InterBankSecret));

services.AddSingleton<ATM>();

await using ServiceProvider provider = services.BuildServiceProvider();

// 1st Talks to CrestBank,
// 2nd listens for CrestBank talking to us.
_ = provider.GetRequiredService<IBankHttpServer>().Start();

ATM atm = provider.GetRequiredService<ATM>();

atm.Run();
