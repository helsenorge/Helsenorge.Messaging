/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Microsoft.IdentityModel.Tokens;

namespace Helsenorge.Registries.HelseId;

/// <summary>
/// Interface for providing the security key for creating AccessToken from HelseId
/// A security key can be JsonWebKey, RsaSecurityKey(PEM), etc. so user can implement this interface to provide the security key they use
/// </summary>
public interface ISecurityKeyProvider
{
    /// <summary>
    /// Get the security key (JsonWebKey, RsaSecurityKey, etc.)
    /// </summary>
    /// <returns>Security key</returns>
    SecurityKey GetSecurityKey();
}