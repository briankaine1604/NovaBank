# NovaBank

A console banking app in C# (.NET 10). This is Bank A. Running it also starts
Bank B, CrestBank, in a second console window.

## Layout

    BLL/                 customers, accounts, transactions, data plans
    BLL/AirtimeProvider.cs   airtime and data sales (the telco side)
    Http/                the small HTTP server CrestBank calls, and the client it uses
    Presentation Layer/  console screens and the services behind them
    Configuration/       loading bank.config.json, starting the peer

## Run

    dotnet run

Two windows open: NovaBank on 5001 and CrestBank on 5002. Option 4 in the
NovaBank menu closes both.

Admin login is `admin` / `password`.

## bank.config.json

Both ports, the shared secret, and the path to CrestBank's project live here.
Changing a port in this file changes it for both banks: the values are handed to
CrestBank on the command line when it starts.

## What it answers over HTTP

    POST /transfers   credit one of this bank's accounts
    POST /airtime     sell airtime
    POST /data        sell a data bundle

All three need the shared secret in `X-Bank-Timestamp` and `X-Bank-Signature`.

## Accounts to log into

    0000000998 / 1234   50,000
    0000000999 / 1234   500,000

## Notes

- The shared secret is a demo value sitting in a file. Fine for coursework, not
  for anything real.
- Server events (incoming transfers, rejections) are written to `bank.log`, not
  to the ATM screen, so a logged-in customer never sees someone else's transfer.
- This repo was split out of an older solution that held both banks, so the
  earliest commits are not here.
