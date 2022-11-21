/*
 * Copyright (c) 2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

namespace Helsenorge.Messaging.AdminLib;

public record MessageMetadata
{
    public string MessageId { get; set; }
    public string CorrelationId { get; set; }
    public ulong DeliveryTag { get; set; }
    public string Exchange { get; set; }
    public string RoutingKey { get; set; }
    public bool Redelivered { get; set; }
    public string FirstDeathExchangeHeaderName { get; set; }
    public string FirstDeathQueueHeaderName { get; set; }
}
