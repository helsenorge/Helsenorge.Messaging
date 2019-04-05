namespace Helsenorge.Registries
{
    /// <summary>
    /// SOAP configuration class.
    /// </summary>
    public class SoapConfiguration
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
        /// The endpoint address for the SOAP Service.
        /// </summary>
        public string EndpointAddress { get; set; }
        /// <summary>
        /// Gets or sets the maximum size in bytes, for a buffer that receives messages from the channel.
        /// </summary>
        public int MaxBufferSize { get; set; }
        /// <summary>
        /// Gets or sets the maximum amount of memory, in bytes, that is allocated for use by the manager of the message burffers that receive messages from the channel.
        /// </summary>
        public long MaxBufferPoolSize { get; set; }
        /// <summary>
        /// Gets or set the maximum size, in bytes, for a message that can be received on a channel configured with this binding.
        /// </summary>
        public int MaxRecievedMessageSize { get; set; }
    }
}