/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Net.Http;

namespace Helsenorge.Registries.Utilities;

/// <summary>
/// Request parameters used to build http request to rest service
/// </summary>
internal class RequestParameters
{
    public HttpMethod Method { get; set; }

    public string Path { get; set; }

    public string BearerToken { get; set; }
}