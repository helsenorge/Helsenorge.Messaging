using System.Collections.Generic;
using System.Security;
using Helsenorge.Registries.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    ///     CertificateException used for issues with the certificate
    /// </summary>
    public class CertificateException : SecurityException
    {
        /// <summary>
        ///     Initiates a new instance of CertificateException
        /// </summary>
        /// <param name="error">The issue of the certificate</param>
        /// <param name="additionalInformation">Additional information to send back</param>
        public CertificateException(CertificateErrors error, string additionalInformation)
        {
            Error = error;
            AdditionalInformation = new[] {additionalInformation};
        }

        /// <summary>
        ///     Initiates a new instance of CertificateException
        /// </summary>
        /// <param name="error">The issue of the certificate</param>
        /// <param name="errorCode">Error code reported back</param>
        /// <param name="description">Description of issue reported back</param>
        /// <param name="eventId">EventId used for logging</param>
        /// <param name="additionalInformation">Additional information to send back</param>
        public CertificateException(CertificateErrors error, string errorCode, string description, EventId eventId,
            IEnumerable<string> additionalInformation)
        {
            Error = error;
            ErrorCode = errorCode;
            Description = description;
            EventId = eventId;
            AdditionalInformation = additionalInformation;
        }

        /// <summary>
        /// The issue of the certificate
        /// </summary>
        public CertificateErrors Error { get; }

        /// <summary>
        /// ErrorCode, a short name that can be used to identify the problem
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// A description of the certification issue
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// EventId used for categorizing when logging
        /// </summary>
        public EventId EventId { get; }

        /// <summary>
        /// Additional information for the issue
        /// </summary>
        public IEnumerable<string> AdditionalInformation { get; }
    }
}