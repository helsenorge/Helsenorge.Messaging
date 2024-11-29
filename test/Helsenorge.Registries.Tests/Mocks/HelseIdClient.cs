using Helsenorge.Registries.HelseId;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Tests.Mocks
{
    public class HelseIdClientMock : IHelseIdClient
    {
        public async Task<string> CreateJwtAccessTokenAsync()
        {
            return await Task.FromResult("accesstokenCppa");
        }
    }
}
