using Helsenorge.Registries.HelseId;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Tests.Mocks
{
    //TODO can be discotinued ?
    public class HelseIdClientMock : IHelseIdClient
    {
        public async Task<string> CreateJwtAccessTokenAsync()
        {
            return await Task.FromResult("accesstokenCppa");
        }
    }
}
