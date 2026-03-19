using HelseId.Library.ClientCredentials.Interfaces;
using HelseId.Library.Models;
using HelseId.Library.Models.DetailsFromClient;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Tests.Mocks
{
    public class HelseIdClientCredentialsFlowMock : IHelseIdClientCredentialsFlow
    {
        public bool SetTokenErrorResponse { get; set; }

        public string ErrorResponse { get; set; } = "ErrorResponse";

        public Task<TokenResponse> GetTokenResponseAsync()
        {
            var accessToken = AccessTokenResponse("");
            return Task.FromResult(accessToken);
        }

        public Task<TokenResponse> GetTokenResponseAsync(OrganizationNumbers organizationNumbers)
        {
            var accessToken = AccessTokenResponse("");
            return Task.FromResult(accessToken);
        }

        public Task<TokenResponse> GetTokenResponseAsync(string scope)
        {
            var accessToken = AccessTokenResponse(scope);
            return Task.FromResult(accessToken);
        }

        public Task<TokenResponse> GetTokenResponseAsync(string scope, OrganizationNumbers organizationNumbers)
        {
            var accessToken = AccessTokenResponse(scope);
            return Task.FromResult(accessToken);
        }

        private TokenResponse AccessTokenResponse(string scope)
        {
            if (SetTokenErrorResponse)
            {
                return new TokenErrorResponse
                {
                    Error = "Error",
                    ErrorDescription = ErrorResponse,
                };
            }

            return new AccessTokenResponse
            {
                AccessToken = "AccessToken",
                ExpiresIn = 60,
                Scope = scope,
            };
        }
    }
}
