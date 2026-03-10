using HelseId.Library.ClientCredentials.Interfaces;
using HelseId.Library.Models;
using HelseId.Library.Models.DetailsFromClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Tests.Mocks
{
    //TODO
    public class HelseIdClientCredentialsFlowMock : IHelseIdClientCredentialsFlow
    {
        public HelseIdClientCredentialsFlowMock()
        {

        }

        public Task<TokenResponse> GetTokenResponseAsync()
        {
            throw new NotImplementedException();
        }

        public Task<TokenResponse> GetTokenResponseAsync(OrganizationNumbers organizationNumbers)
        {
            throw new NotImplementedException();
        }

        public Task<TokenResponse> GetTokenResponseAsync(string scope)
        {
            throw new NotImplementedException();
        }

        public Task<TokenResponse> GetTokenResponseAsync(string scope, OrganizationNumbers organizationNumbers)
        {
            throw new NotImplementedException();
        }
    }
}
