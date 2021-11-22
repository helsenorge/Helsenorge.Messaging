/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Helsenorge.Messaging.IntegrationTests.ServiceBus
{
    public class ServiceBusConnectionTests : IDisposable
    {
        private readonly ServiceBusFixture _fixture;

        public ServiceBusConnectionTests(ITestOutputHelper output)
        {
            _fixture = new ServiceBusFixture(output);
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact, Trait("Category", "IntegrationTest")]
        public async Task Should_Create_New_Connection_On_First_Access()
        {
            var connection = _fixture.Connection;
            var conn = await connection.GetInternalConnectionAsync();
            Assert.False(conn.IsClosed);
            Assert.False(connection.IsClosedOrClosing);
            await connection.CloseAsync();
            Assert.True(connection.IsClosedOrClosing);
            Assert.True(conn.IsClosed);
        }

        [Fact, Trait("Category", "IntegrationTest")]
        public async Task Should_Not_Create_New_Connection_When_Underlying_Object_Is_Not_Closed()
        {
            var connection = _fixture.Connection;
            var conn = await connection.GetInternalConnectionAsync();
            Assert.False(conn.IsClosed);
            Assert.False(connection.IsClosedOrClosing);
            Assert.False(await connection.EnsureConnectionAsync());
            var conn2 = await connection.GetInternalConnectionAsync();
            Assert.Same(conn, conn2);
            Assert.False(conn.IsClosed);
        }

        [Fact, Trait("Category", "IntegrationTest")]
        public async Task Should_Create_New_Connection_When_Underlying_Object_Is_Closed()
        {
            var connection = _fixture.Connection;
            var conn = await connection.GetInternalConnectionAsync();
            Assert.False(conn.IsClosed);
            Assert.False(connection.IsClosedOrClosing);
            Assert.False(await connection.EnsureConnectionAsync());
            conn.Close();
            Assert.True(conn.IsClosed);
            Assert.False(connection.IsClosedOrClosing);
            var conn2 = await connection.GetInternalConnectionAsync();
            Assert.NotSame(conn, conn2);
            Assert.False(conn2.IsClosed);
        }

        [Fact, Trait("Category", "IntegrationTest")]
        public async Task Should_Not_Allow_To_Access_Connection_When_Closed()
        {
            var connection = _fixture.Connection;
            var conn = await connection.GetInternalConnectionAsync();
            Assert.False(conn.IsClosed);
            Assert.False(connection.IsClosedOrClosing);
            Assert.False(await connection.EnsureConnectionAsync());
            await connection.CloseAsync();
            Assert.True(conn.IsClosed);
            Assert.True(connection.IsClosedOrClosing);
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await connection.EnsureConnectionAsync());
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await connection.GetInternalConnectionAsync());
        }

        [Fact, Trait("Category", "IntegrationTest")]
        public async Task Should_Allow_To_Close_Already_Closed_Connection()
        {
            var connection = _fixture.Connection;
            await connection.CloseAsync();
            Assert.True(connection.IsClosedOrClosing);
            await connection.CloseAsync();
            Assert.True(connection.IsClosedOrClosing);
        }
    }
}
