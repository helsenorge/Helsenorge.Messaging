/* 
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

namespace Helsenorge.Registries.Configuration
{
    /// <summary>
    /// Representing the WCF configuration which was previously located in app.config
    /// </summary>
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
        /// <summary>
        /// Get or set the WCF binding, defaults to BasicHttpBinding
        /// </summary>
        public WcfHttpBinding HttpBinding { get; set; } = WcfHttpBinding.Basic;
        /// <summary>
        /// Gets or sets the maximum size, in bytes, for a buffer that receives messages from the channel.
        /// </summary>
        public int MaxBufferSize { get; set; }
        /// <summary>
        /// Gets or sets the maximum amount of memory, in bytes, that is allocated for use by the manager of the message buffers that receive messages from the channel.
        /// </summary>
        public int MaxBufferPoolSize { get; set; }
        /// <summary>
        /// Gets or sets the maximum size, in bytes, for a message that can be received on a channel configured with this binding.
        /// </summary>
        public int MaxReceivedMessageSize { get; set; }
        /// <summary>
        /// Gets or sets a value that indicates whether the auto-configured HTTP proxy of the system should be used, if available.
        /// </summary>
        public bool UseDefaultWebProxy { get; set; }
        /// <summary>
        /// Gets or sets a value that indicates whether to bypass the proxy server for local addresses.
        /// </summary>
        public bool BypassProxyOnLocal { get; set; }
        /// <summary>
        /// Gets or sets the URI address of the HTTP proxy.
        /// </summary>
        public Uri ProxyAddress { get; set; }
    }

    /// <summary>
    /// An enumeration with the allowed WCF Http Bindings
    /// </summary>
    public enum WcfHttpBinding
    {
        /// <summary>
        /// Represents a binding that a Windows Communication Foundation (WCF) service can use to configure and expose endpoints that are able to communicate with ASMX-based Web services and clients and other services that conform to the WS-I Basic Profile 1.1.
        /// </summary>
        Basic = 1,
        /// <summary>
        /// Represents an interoperable binding that supports distributed transactions and secure, reliable sessions.
        /// </summary>
        WsHttp
    }
}
