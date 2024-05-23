using Helsenorge.Registries.Connected_Services.HelseId;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Tests.Mocks
{
    public class HelseIdClientMock : IHelseIdClient
    {
        public async Task<string> CreateJwtAccessTokenAsync()
        {
            return await Task.FromResult("accesstoken");
        }
    }
}
