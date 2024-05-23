using System.Threading.Tasks;

namespace Helsenorge.Registries.Connected_Services.HelseId
{
    public interface IHelseIdClient
    {
        public Task<string> CreateJwtAccessTokenAsync();
    }
}
