using HelseId.Library.Interfaces.JwtTokens;
using HelseId.Library.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Tests.Mocks
{
    public class DPoPProofCreatorMock : IDPoPProofCreatorForApiRequests
    {
        public string Url { get; private set; }
        public HttpMethod HttpMethod { get; private set; }
        public string AccessToken { get; private set; }

        private readonly string _dPoPProof;

        public DPoPProofCreatorMock(string dPoPProof)
        {
            _dPoPProof = dPoPProof;
        }

        public Task<string> CreateDPoPProofForApiRequest(HttpMethod httpMethod, string url, string accessToken)
        {
            Url = url;
            HttpMethod = httpMethod;
            AccessToken = accessToken;
            return Task.FromResult(_dPoPProof);
        }

        public Task<string> CreateDPoPProofForApiRequest(HttpMethod httpMethod, string url, AccessTokenResponse accessTokenResponse)
        {
            Url = url;
            HttpMethod = httpMethod;
            AccessToken = accessTokenResponse.AccessToken;
            return Task.FromResult(_dPoPProof);
        }
    }
}
