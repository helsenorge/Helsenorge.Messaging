using Helsenorge.Registries.HelseId;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Tests.Mocks
{
    public class HelseIdClientMock : IHelseIdClient
    {
        public async Task<string> CreateJwtAccessTokenAsyncCpe()
        {
            return await Task.FromResult("accesstokenCpe");
        }

        public async Task<string> CreateJwtAccessTokenAsyncCppa()
        {
            return await Task.FromResult("accesstokenCppa");
        }
    }
}
