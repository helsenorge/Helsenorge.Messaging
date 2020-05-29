# Helsenorge Messaging Integration Tests

## Running Tests

1. Create HM_IT_CONN_STRING environment variable containing the connection string for running tests.
2. Ensure the following queues are created: `ServiceBusConnectionTests`, `ServiceBusMessageTests`, `ServiceBusReceiverTests`, `ServiceBusSenderTests`.
3. Run integration tests by using either Visual Studio Tst Runner or `dotnet test` command.

