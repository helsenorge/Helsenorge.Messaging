/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.Amqp;
using Xunit;

namespace Helsenorge.Messaging.Tests.Amqp;

public class AmqpConnectionStringTests
{
    [Fact]
    public void Construct_ConnectionString_Using_Default_Values_And_Password_Unencoded()
    {
        var expectedConnectionString = "amqps://a_user_name:a_password@host_name:5671/";
        var connectionString = new AmqpConnectionString
        {
            HostName = "host_name",
            UserName = "a_user_name",
            Password = "a_password",
        };

        Assert.Equal(expectedConnectionString, connectionString.ToString());
    }

    [Fact]
    public void Construct_ConnectionString_Using_Default_Values_And_Password_Encoded()
    {
        var expectedConnectionString = "amqps://a_user_name:a_password_%24%25-%2A-%2F%3A%21%23%2B%2C67@host_name:5671/";
        var connectionString = new AmqpConnectionString
        {
            HostName = "host_name",
            UserName = "a_user_name",
            Password = "a_password_$%-*-/:!#+,67",
        };

        Assert.Equal(expectedConnectionString, connectionString.ToString());
    }

    [Fact]
    public void Construct_ConnectionString_With_Exchange_Specified()
    {
        var expectedConnectionString = "amqps://a_user_name:a_password@host_name:5671/an_exchange";
        var connectionString = new AmqpConnectionString
        {
            HostName = "host_name",
            Exchange = "an_exchange",
            UserName = "a_user_name",
            Password = "a_password",
        };

        Assert.Equal(expectedConnectionString, connectionString.ToString());
    }

    [Fact]
    public void Construct_ConnectionString_Setting_UseTls_To_False()
    {
        var expectedConnectionString = "amqp://a_user_name:a_password@host_name:5672/";
        var connectionString = new AmqpConnectionString
        {
            HostName = "host_name",
            UserName = "a_user_name",
            Password = "a_password",
            UseTls = false
        };

        Assert.Equal(expectedConnectionString, connectionString.ToString());
    }

    [Fact]
    public void Construct_ConnectionString_Override_Port_Setting()
    {
        var expectedConnectionString = "amqps://a_user_name:a_password@host_name:56721/";
        var connectionString = new AmqpConnectionString
        {
            HostName = "host_name",
            UserName = "a_user_name",
            Password = "a_password",
            Port = 56721
        };

        Assert.Equal(expectedConnectionString, connectionString.ToString());
    }
}
