/*
 * Copyright (c) 2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Helsenorge.Messaging.ServiceBus;
using RabbitMQ.Client;

namespace Helsenorge.Messaging.AdminLib;

public class QueueClient : IDisposable, IAsyncDisposable
{
    private readonly ConnectionString _connectionString;
    private readonly ILogger _logger;
    private IConnectionFactory _connectionFactory;
    private IConnection _connection;
    private IModel _channel;

    private const ushort NoRoute = 312;

    private const string FirstDeathExchangeHeaderName = "x-first-death-exchange";
    private const string FirstDeathQueueHeaderName = "x-first-death-queue";

    public QueueClient(ConnectionString connectionString, ILogger logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IConnectionFactory ConnectionFactory
    {
        get
        {
            return _connectionFactory ??= new ConnectionFactory
            {
                Uri = new Uri($"{(_connectionString.UseTls ? "amqps": "amqp")}://{_connectionString.HostName}"),
                UserName = _connectionString.UserName,
                Password = _connectionString.Password,
                VirtualHost = string.IsNullOrWhiteSpace(_connectionString.VirtualHost) ? "/" : _connectionString.VirtualHost,
                ClientProvidedName = _connectionString.ClientProvidedName,
            };
        }
    }

    private IConnection Connection
    {
        get { return _connection ??= ConnectionFactory.CreateConnection(); }
    }

    private IModel Channel
    {
        get { return _channel ??= Connection.CreateModel(); }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private void PublishMessageAndAckIfSuccessful(BasicGetResult message, string exchange, string destinationQueue, bool mandatory = true, bool waitForConfirm = true)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (exchange == null)
            throw new ArgumentNullException(nameof(exchange));
        if (string.IsNullOrWhiteSpace(destinationQueue))
            throw new ArgumentException("Argument must contain the queue name.", nameof(destinationQueue));

        _logger.LogInformation($"Start-PublishMessageAndAckIfSuccessful - Moving message with delivery tag '{message.DeliveryTag}' to '{destinationQueue}'. MessageId:{message.BasicProperties.MessageId}");

        var confirmed = true;
        if (waitForConfirm)
        {
            // Enable publisher acknowledgements.
            Channel.ConfirmSelect();
            Channel.BasicReturn += (_, args) =>
            {
                if (args.ReplyCode == NoRoute)
                {
                    confirmed = false;
                    _logger.LogError($"PublishMessageAndAckIfSuccessful-BasicReturn - Message is with MessageId: '{args.BasicProperties.MessageId}' un-routable. Exchange: '{args.Exchange}', DestinationQueue: '{args.RoutingKey}', ReplyCode: '{args.ReplyCode}', ReplyText: '{args.ReplyText}'");
                }
            };
        }

        // Publish message to destination queue.
        Channel.BasicPublish(exchange, destinationQueue, mandatory, message.BasicProperties, message.Body);
        if (waitForConfirm && Channel.WaitForConfirms(TimeSpan.FromSeconds(60)) && confirmed)
        {
            // The message was confirmed published to the exchange so positively acknowledge that the message has been processed.
            Channel.BasicAck(message.DeliveryTag, multiple: false);

            _logger.LogInformation($"PublishMessageAndAckIfSuccessful - Message with delivery tag '{message.DeliveryTag}' was ACKed because it was successfully moved to '{destinationQueue}' MessageId:{message.BasicProperties.MessageId}.");
        }
        else
        {
            // The message was not confirmed published to the exchange so negatively acknowledge that the message should be re-queued.
            Channel.BasicNack(message.DeliveryTag, multiple: false, requeue: true);

            _logger.LogInformation($"PublishMessageAndAckIfSuccessful - Message with delivery tag '{message.DeliveryTag}' was NACKed because it was unsuccessfully moved to '{destinationQueue}' MessageId:{message.BasicProperties.MessageId}.");
        }

        _logger.LogInformation($"End-PublishMessageAndAckIfSuccessful - Message with delivery tag '{message.DeliveryTag}' was moved to '{destinationQueue}'. MessageId:{message.BasicProperties.MessageId}");
    }

    /// <summary>
    /// A method to retrieve the number of messages currently on the queue by specifiying the HER-id and the queue type.
    /// </summary>
    /// <param name="herId">The HER-id of the queue.</param>
    /// <param name="queueType">The type of queue.</param>
    /// <returns>The number of messages currently on the queue.</returns>
    public uint GetMessageCount(int herId, QueueType queueType)
    {
        var queue = QueueUtilities.ConstructQueueName(herId, queueType);
        return GetMessageCount(queue);
    }

    /// <summary>
    /// A method to retrieve the number of messages currently on the queue by explicitly naming the queue.
    /// </summary>
    /// <param name="queue">The name of the queue.</param>
    /// <returns>The number of messages currently on the queue.</returns>
    public uint GetMessageCount(string queue)
    {
        return Channel.MessageCount(queue);
    }

        /// <summary>
    /// Republishes messages on the dead letter queue back to the origin queue.
    /// </summary>
    /// <param name="herId">The HER-id of dead letter and source queue.</param>
    /// <param name="numberOfMessagesToMove">The number of message to move, -1 means move all messages</param>
    public void RepublishMessageFromDeadLetterToOriginQueue(int herId, int numberOfMessagesToMove = -1)
    {
        if (herId <= 0)
            throw new ArgumentOutOfRangeException(nameof(herId), herId, "Argument must be a value greater than zero.");

        // Just return if number of messages is set to zero.
        if (numberOfMessagesToMove == 0)
            return;

        var sourceQueue = QueueUtilities.ConstructQueueName(herId, QueueType.DeadLetter);

        var messageCount = 0;
        var result = Channel.BasicGet(sourceQueue, autoAck: false);
        while (result != null)
        {
            var exchange = QueueUtilities.GetByteHeaderAsString(result.BasicProperties.Headers, FirstDeathExchangeHeaderName);
            var destinationQueue = QueueUtilities.GetByteHeaderAsString(result.BasicProperties.Headers, FirstDeathQueueHeaderName);

            PublishMessageAndAckIfSuccessful(result, exchange, destinationQueue);

            messageCount++;
            if (numberOfMessagesToMove < 0 || numberOfMessagesToMove > messageCount)
            {
                result = Channel.BasicGet(sourceQueue, autoAck: false);
            }
            else
            {
                // At this point we have moved the specified number of messages we were asked to so let's break out of the loop.
                break;
            }
        }
    }

    /// <summary>
    /// Publishes messages on the dead letter queue to the specified HER-id and Queue Type.
    /// </summary>
    /// <param name="herId">The HER-id of dead letter and source queue.</param>
    /// <param name="queueType">The queue type we want to move the dead lettered messages to.</param>
    /// <param name="numberOfMessagesToMove">The number of message to move, -1 means move all messages</param>
    public void PublishMessagesFromDeadLetterTo(int herId, QueueType queueType = QueueType.Asynchronous, int numberOfMessagesToMove = -1)
    {
        if (herId <= 0)
            throw new ArgumentOutOfRangeException(nameof(herId), herId, "Argument must be a value greater than zero.");

        var sourceQueue = QueueUtilities.ConstructQueueName(herId, QueueType.DeadLetter);
        var destinationQueue = QueueUtilities.ConstructQueueName(herId, queueType);

        MoveMessages(sourceQueue, destinationQueue, numberOfMessagesToMove);
    }

    /// <summary>
    /// Move message from source to destination queue.
    /// </summary>
    /// <param name="sourceQueue">The source queue messages will be moved from.</param>
    /// <param name="destinationQueue">The destination queue messages will be moved to.</param>
    /// <param name="numberOfMessagesToMove">The number of message to move, -1 means move all messages.</param>
    public void MoveMessages(string sourceQueue, string destinationQueue, int numberOfMessagesToMove = -1)
    {
        if (string.IsNullOrWhiteSpace(sourceQueue))
            throw new ArgumentNullException(nameof(sourceQueue));
        if (string.IsNullOrWhiteSpace(destinationQueue))
            throw new ArgumentNullException(nameof(destinationQueue));
        if (sourceQueue.Equals(destinationQueue, StringComparison.InvariantCultureIgnoreCase))
            throw new SourceAndDestinationIdenticalException(sourceQueue, destinationQueue);

        // Just return if number of messages is set to zero.
        if (numberOfMessagesToMove == 0)
            return;

        _logger.LogInformation($"Start-MoveMessages - Moving messages from '{sourceQueue}' to '{destinationQueue}'. Number of messages to move {(numberOfMessagesToMove == -1 ? int.MaxValue : numberOfMessagesToMove)}.");

        var messageCount = 0;
        var result = Channel.BasicGet(sourceQueue, autoAck: false);
        while (result != null)
        {
            PublishMessageAndAckIfSuccessful(result, _connectionString.Exchange, destinationQueue);

            messageCount++;
            if (numberOfMessagesToMove < 0 || numberOfMessagesToMove > messageCount)
            {
                result = Channel.BasicGet(sourceQueue, autoAck: false);
            }
            else
            {
                // At this point we have moved the specified number of messages we were asked to so let's break out of the loop.
                break;
            }
        }

        _logger.LogInformation($"End-MoveMessages - Successfully moved {messageCount} messages from '{sourceQueue}' to '{destinationQueue}'.");
    }
}
