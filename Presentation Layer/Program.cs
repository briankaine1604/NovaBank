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
var httpClient = new BankHttpClient("http://localhost:5002/");
var httpServer = new BankHttpServer(bank, serverConsole, "http://localhost:5001/");
_ = httpServer.Start();

ATM atm = new(
    bank,
    aTMConsole,
    adminConsole,
    admin,
    httpClient);

atm.Run();
