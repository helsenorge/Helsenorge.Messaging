/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

namespace Helsenorge.Registries.Configuration
{
    public class WcfConfiguration
    {
        /// <summary>
        /// Username used for connecting
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Password used for connecting
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Endpoint address.
        /// </summary>
        public string Address { get; set; }

        public WcfHttpBinding HttpBinding { get; set; } = WcfHttpBinding.Basic;

        public int MaxBufferSize { get; set; }

        public int MaxBufferPoolSize { get; set; }

        public int MaxReceivedMessageSize { get; set; }
    }

    public enum WcfHttpBinding
    {
        Basic = 1,
        WsHttp
    }
}
