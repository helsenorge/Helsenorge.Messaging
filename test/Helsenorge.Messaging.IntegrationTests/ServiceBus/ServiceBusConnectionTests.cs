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
            var conn = connection.Connection;
            Assert.False(conn.IsClosed);
            Assert.False(connection.IsClosedOrClosing);
            await connection.CloseAsync();
            Assert.True(connection.IsClosedOrClosing);
            Assert.True(conn.IsClosed);
        }

        [Fact, Trait("Category", "IntegrationTest")]
        public void Should_Not_Create_New_Connection_When_Underlying_Object_Is_Not_Closed()
        {
            var connection = _fixture.Connection;
            var conn = connection.Connection;
            Assert.False(conn.IsClosed);
            Assert.False(connection.IsClosedOrClosing);
            Assert.False(connection.EnsureConnection());
            var conn2 = connection.Connection;
            Assert.Same(conn, conn2);
            Assert.False(conn.IsClosed);
        }

        [Fact, Trait("Category", "IntegrationTest")]
        public void Should_Create_New_Connection_When_Underlying_Object_Is_Closed()
        {
            var connection = _fixture.Connection;
            var conn = connection.Connection;
            Assert.False(conn.IsClosed);
            Assert.False(connection.IsClosedOrClosing);
            Assert.False(connection.EnsureConnection());
            conn.Close();
            Assert.True(conn.IsClosed);
            Assert.False(connection.IsClosedOrClosing);
            var conn2 = connection.Connection;
            Assert.NotSame(conn, conn2);
            Assert.False(conn2.IsClosed);
        }

        [Fact, Trait("Category", "IntegrationTest")]
        public async Task Should_Not_Allow_To_Access_Connection_When_Closed()
        {
            var connection = _fixture.Connection;
            var conn = connection.Connection;
            Assert.False(conn.IsClosed);
            Assert.False(connection.IsClosedOrClosing);
            Assert.False(connection.EnsureConnection());
            await connection.CloseAsync();
            Assert.True(conn.IsClosed);
            Assert.True(connection.IsClosedOrClosing);
            Assert.Throws<ObjectDisposedException>(() => connection.EnsureConnection());
            Assert.Throws<ObjectDisposedException>(() => connection.Connection);
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
