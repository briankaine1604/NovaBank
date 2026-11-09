# NovaBank

A console banking application (.NET 10). NovaBank is "Bank A", and the entry
point for the pair: it holds the shared configuration and starts CrestBank,
"Bank B", alongside itself. The two move money between each other over HTTP.

## Layers

    BLL/                  Customers, accounts, transactions, seeding, transfers
    BLL/Interfaces/       Contracts the business layer exposes
    Configuration/        bank.config.json model and peer process launcher
    Http/                 Inter-bank transfer server and client, request signing
    Http/Interfaces/      Contracts for the server and client
    Presentation Layer/   ATM console, ATM session, admin console, services, DI wiring

## Run

    dotnet run

That starts NovaBank **and** CrestBank, in two console windows. Quitting the
NovaBank ATM (option 4) shuts CrestBank down too, so both ports are free for the
next run. Admin login is `admin` / `password`.

`NovaBank.slnx` opens the project on its own in Visual Studio; F5 behaves the
same as `dotnet run`.

## Configuration - `bank.config.json`

The only place the pair's addresses and shared secret are written down.

| Key                | Meaning                                                       |
| ------------------ | ------------------------------------------------------------- |
| `name`             | This bank's name, used on the peer's statement lines.          |
| `address`          | Where this bank listens, e.g. `http://localhost:5001/`.        |
| `secret`           | HMAC key for `/transfers`, handed to the peer at startup.      |
| `peerName`         | The other bank's name, used on this bank's statement lines.    |
| `peerAddress`      | Where the peer listens, e.g. `http://localhost:5002/`.         |
| `peerProjectPath`  | The peer's `.csproj`, relative to this repository.             |

The file is copied next to the executable at build time and read from there, so
it is found however the app is started.

On startup NovaBank reads the file, starts the peer with
`dotnet run --project <peerProjectPath> -- --own <peerAddress>
--peer <address> --secret <secret>`, waits for the peer's port, then serves its
own ATM. If the peer is already listening it is left alone - and it is only
killed on exit when this process started it.

Two things are deliberately *not* settings: the account prefix (`000`, baked
into every number the seeder and `Bank` issue) and the ATM's admin credentials.

Killing the NovaBank window instead of choosing Exit can leave CrestBank
running; it holds port 5002 and the next start will report the port is taken.
Stop it with `taskkill /F /IM CrestBank.exe`.

## Inter-bank contract

NovaBank is one half of a two-bank system. Its peer, CrestBank, lives in its own
repository. Both processes must be running for a cross-bank transfer to work.

| Setting            | NovaBank        | CrestBank (peer) |
| ------------------ | --------------- | ---------------- |
| HTTP listener      | localhost:5001  | localhost:5002   |
| Account prefix     | `000`           | `303`            |
| Shared secret      | `nova-crest-demo-secret` | same    |

CrestBank keeps these same values as defaults, so it still runs on its own from
its own repository — but when NovaBank starts it, `bank.config.json` here wins:
NovaBank passes the peer its address, this bank's address, and the secret.

Requests to `/transfers` are signed with the shared secret and a timestamp;
unsigned, stale, or wrongly signed requests are rejected by both sides.

## History

This repository was split out of the `ConsoleBank` solution, which held both
banks. Its commit history was extracted from that repository with
`git subtree split`, so commits that touched only the other bank are not here.
