using System;
using System.Security.Cryptography;
using System.Text;
using Helsenorge.Registries.HelseId;
using Microsoft.IdentityModel.Tokens;

namespace Helsenorge.Messaging.Client;

public class SecurityKeyProvider : ISecurityKeyProvider
{
    public SecurityKey GetSecurityKey()
    {
        // Exmple on private key fetch from Base64 encoded Pem key
        var bytes = Convert.FromBase64String("base 64 encoded Pem key");
        var decodedPrivateKeyPem = Encoding.UTF8.GetString(bytes);

        var rsaPrivateKey = RSA.Create();
        rsaPrivateKey.ImportFromPem(decodedPrivateKeyPem);
        var securityKey = new RsaSecurityKey(rsaPrivateKey);
        return securityKey;
    }
}