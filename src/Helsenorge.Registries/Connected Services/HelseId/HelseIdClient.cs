/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Threading.Tasks;
using Helsenorge.Registries.Configuration;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;

namespace Helsenorge.Registries.HelseId;

public class HelseIdClient : IHelseIdClient
{
    private readonly HelseIdConfiguration _configuration;
    private readonly ISecurityKeyProvider _securityKeyProvider;

    public HelseIdClient(HelseIdConfiguration configuration, ISecurityKeyProvider securityKeyProvider)
    {
        _configuration = configuration;
        _securityKeyProvider = securityKeyProvider;
    }

    /// <summary>
    /// Creates a Jwt access token for authentication with HelseId for the Cppa service
    /// </summary>
    /// <returns>Jwt access token</returns>
    public async Task<string> CreateJwtAccessTokenAsync()
    {
        var scopeName = _configuration.ScopeName;
        string tokenKey = $"HelseIdJwtAccessToken_{scopeName}";

        if (TokenCache.TryGetToken(tokenKey, out var cachedToken))
        {
            return cachedToken;
        }

        var request = CreateClientAssertionsRequest(scopeName);
        var response = await new HttpClient().RequestClientCredentialsTokenAsync(request);
        if (response.IsError)
        {
            throw new ApplicationException("Error fetching JWT token: " + response.Error);
        }

        TokenCache.SetToken(tokenKey, response.AccessToken, 45); // Adjust TTL here
        return response.AccessToken;
    }

    private ClientCredentialsTokenRequest CreateClientAssertionsRequest(string scopeName)
    {
        var request = new ClientCredentialsTokenRequest
        {
            ClientId = _configuration.ClientId,
            Address = _configuration.TokenEndpoint,
            Scope = scopeName,
            GrantType = OidcConstants.GrantTypes.ClientCredentials,
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
            ClientAssertion = new ClientAssertion
            {
                Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                Value = CreateJwtToken()
            }
        };
        return request;
    }

    private string CreateJwtToken()
    {
        var tokenIssuedAtEpochTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var securityKey = _securityKeyProvider.GetSecurityKey();
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha512);
        var header = new JwtHeader(signingCredentials);
        var endpointUri = new Uri(_configuration.TokenEndpoint);
        var payload = new JwtPayload
        {
            [JwtRegisteredClaimNames.Iss] = _configuration.ClientId,
            [JwtRegisteredClaimNames.Sub] = _configuration.ClientId,
            [JwtRegisteredClaimNames.Aud] = $"{endpointUri.Scheme}://{endpointUri.Host}",
            [JwtRegisteredClaimNames.Exp] = tokenIssuedAtEpochTimeSeconds + 60,
            [JwtRegisteredClaimNames.Nbf] = tokenIssuedAtEpochTimeSeconds,
            [JwtRegisteredClaimNames.Iat] = tokenIssuedAtEpochTimeSeconds,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N")
        };

        var token = new JwtSecurityToken(header, payload);
        var serializedToken = new JwtSecurityTokenHandler().WriteToken(token);
        return serializedToken;
    }
}