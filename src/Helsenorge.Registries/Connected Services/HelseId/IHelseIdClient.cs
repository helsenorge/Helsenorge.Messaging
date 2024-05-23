using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Connected_Services.HelseId
{
    public interface IHelseIdClient
    {
        public Task<string> CreateJwtAccessTokenAsync();
    }
}
