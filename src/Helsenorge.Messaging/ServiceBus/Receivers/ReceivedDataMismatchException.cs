using System;

namespace Helsenorge.Messaging.ServiceBus.Receivers
{
    /// <summary>
    /// Exception that is used to indicate that received data does not match what one expects.
    /// This can be used by application code to signal error to sender
    /// </summary>
    [Serializable]
    public class ReceivedDataMismatchException : Exception
    {
        /// <summary>
        /// The received value
        /// </summary>
        public string ReceivedValue
        {
            get
            {
                return Data.Contains("ReceivedValue") ? Data["ReceivedValue"].ToString() : string.Empty;
            }
            set
            {
                if (Data.Contains("ReceivedValue") == false)
                {
                    Data.Add("ReceivedValue", value);
                }
            }
        }
        /// <summary>
        /// The expected value
        /// </summary>
        public string ExpectedValue
        {
            get
            {
                return Data.Contains("ExpectedValue") ? Data["ExpectedValue"].ToString() : string.Empty;
            }
            set
            {
                if (Data.Contains("ExpectedValue") == false)
                {
                    Data.Add("ExpectedValue", value);
                }
            }
        }
        /// <summary>
        /// Constructor
        /// </summary>
        public ReceivedDataMismatchException() { }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        public ReceivedDataMismatchException(string message) : base(message) { }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public ReceivedDataMismatchException(string message, Exception inner) : base(message, inner) { }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected ReceivedDataMismatchException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
