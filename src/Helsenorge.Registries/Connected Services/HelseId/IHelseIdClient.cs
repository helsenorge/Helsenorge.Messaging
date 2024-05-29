using System.Threading.Tasks;

namespace Helsenorge.Registries.HelseId
{
    public interface IHelseIdClient
    {
        public Task<string> CreateJwtAccessTokenAsync();
    }
}
