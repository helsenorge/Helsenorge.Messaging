/*
 * Copyright (c) 2022-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Collections.Generic;

namespace Helsenorge.Messaging.AdminLib;

public class ConnectionString
{
    public string HostName { get; set; }
    public int Port { get; set; }
    public string VirtualHost { get; set; } = "/";
    public string UserName { get; set; }
    public string Password { get; set; }
    public string Exchange { get; set; }
    public string ClientProvidedName { get; set; }
    public bool UseTls { get; set; } = true;
}
