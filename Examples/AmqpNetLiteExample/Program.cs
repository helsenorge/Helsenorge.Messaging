// See https://aka.ms/new-console-template for more information

using Amqp;

namespace AmqpNetLiteExample;

class Program
{
    private static readonly string _connectionString = "amqp://guest:guest@127.0.0.1:5672";
    private static readonly string _queue = "/amq/queue/123_async";

    static async Task Main(string[] args)
    {
        var connectionFactory = new ConnectionFactory();
        var connection = await connectionFactory.CreateAsync(new Address(_connectionString));
        var session = new Session(connection);
        var link = new ReceiverLink(session, "test-receiver", _queue);
        link.SetCredit(25);
        int i = 0;
        while (true)
        {
            var message = await link.ReceiveAsync();
            if (message == null) break;

            Console.WriteLine($"Message Id: {message.Properties.GetMessageId()}");

            if (i == 1)
                await Task.Delay(60 * 1000);
            else if (i <= 25)
                await Task.Delay(5 * 1000);
        }
    }
}
