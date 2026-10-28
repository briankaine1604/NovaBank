using NovaBank;
using NovaBank.Http;
using System.Globalization;
using System.Linq;


Console.Title = "NovaBank ATM  (Bank A)";

var bank = new Bank();
ATMConsole aTMConsole = new();
AdminConsole adminConsole = new();
ServerConsole serverConsole = new();


Admin admin = new("admin", "password");
BankSeeder.Seed(bank);

// 1st Talks to CrestBank,
// 2nd listens for CrestBank talking to us.
// Shared secret for the inter-bank channel; both banks must hold the same value.
// A real deployment keeps one key per peer in configuration, never in source.
const string InterBankSecret = "nova-crest-demo-secret-change-me";

var httpClient = new BankHttpClient("http://localhost:5002/", InterBankSecret);
var httpServer = new BankHttpServer(bank, serverConsole, "http://localhost:5001/", InterBankSecret);
_ = httpServer.Start();

ATM atm = new(
    bank,
    aTMConsole,
    adminConsole,
    admin,
    httpClient);

atm.Run();
