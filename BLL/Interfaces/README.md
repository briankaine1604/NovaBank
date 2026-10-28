# No domain interfaces here, on purpose

`Account`, `Bank`, `Customer`, `Transaction` and `TransferResult` are concrete, and they stay concrete.

The reason is that an interface earns its place when there is a second implementation or a caller that must be
swapped out. Nothing in this solution needs a different `Account`:

- `Account` holds the invariants (balance cannot go negative, every movement writes its own statement line). An
  `IAccount` interface would not protect anything - a fake implementation could break every rule the real one
  enforces, so the interface would advertise a contract the code does not actually rely on.
- `Bank` is the in-memory store: one dictionary, seeded once at startup and shared by the ATM and the HTTP
  listener through the DI container. Its only would-be second implementation is a database-backed one, which is
  a change this demo does not make. When that arrives, `IBankRepository` is the interface to add - around
  persistence, not around the aggregate.
- `TransferResult` is an enum: a closed set of outcomes, not a contract to be implemented.

Where interfaces *do* earn their place here:

- `Http/Interfaces` - the transport is the boundary the ATM must be able to live without. `IBankHttpClient` lets
  the ATM be exercised with no socket, and `IBankHttpServer` lets the listener be started without the console.
- `Presentation Layer/Interfaces` - the screen. Nobody wants unit tests that type into a terminal, and each
  consumer (a menu, a transfer, an airtime purchase) only needs a slice of it.

So the rule being applied is: interface at a boundary that is genuinely crossed (a socket, a keyboard, a future
store), concrete everywhere the rules live.
