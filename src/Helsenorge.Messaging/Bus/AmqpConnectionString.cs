/* 
 * Copyright (c) 2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Text;

namespace Helsenorge.Messaging.Bus;

/// <summary>
/// An abstraction to build an AMQP 1.0 Connection String.
/// </summary>
public class AmqpConnectionString
{
    /// <summary>
    /// The host name of the broker
    /// </summary>
    public string HostName { get; set; }
    /// <summary>
    /// Set this to override the port to use when connecting to the broker.
    /// If UseTls is true and Port does not have a value we will connect to port 5671, otherwise 5672.
    /// </summary>
    public int? Port { get; set; }
    /// <summary>
    /// A user name. The set accessor expects a raw user name without any URL encoding.
    /// </summary>
    public string UserName { get; set; }
    /// <summary>
    /// The password associated with the user. The set accessor expects a raw password without any URL encoding.
    /// </summary>
    public string Password { get; set; }
    /// <summary>
    /// The exchange messages will be sent to.
    /// </summary>
    public string Exchange { get; set; }
    /// <summary>
    /// If false connection will be unsecured. The default value is true.
    /// </summary>
    public bool UseTls { get; set; } = true;

    /// <summary>
    /// Builds the connection string using the supplied values and returns a fully constructed connection string.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var builder = new StringBuilder();
        var scheme = UseTls ? "amqps" : "amqp";
        var port = Port.HasValue ? Port.Value : UseTls ? 5671 : 5672;

        var encodedUserName = string.IsNullOrEmpty(UserName) ? string.Empty : Uri.EscapeDataString(UserName);
        var encodedPassword = string.IsNullOrEmpty(Password) ? string.Empty : Uri.EscapeDataString(Password);

        builder.Append($"{scheme}://{encodedUserName}:{encodedPassword}@{HostName}:{port}/");

        if (!string.IsNullOrWhiteSpace(Exchange))
        {
            var exchange = Exchange;
            if (!string.IsNullOrEmpty(exchange) && exchange[0] == '/')
                exchange = exchange.Substring(1);
            builder.Append(exchange);
        }

        return builder.ToString();
    }
}