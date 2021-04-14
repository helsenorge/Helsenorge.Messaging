using System;
using System.Collections.Generic;
using System.Security;
using Helsenorge.Registries.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.Abstractions
{
    public class CertificateException : SecurityException
    {
        public CertificateException(CertificateErrors error, string additionalInformation) : base()
        {
            Error = error;
            AdditionalInformation = new[] {additionalInformation};
        }

        public CertificateException(CertificateErrors error, string errorCode, string description, EventId eventId,
            IEnumerable<string> additionalInformation) : base()
        {
            Error = error;
            ErrorCode = errorCode;
            Description = description;
            EventId = eventId;
            AdditionalInformation = additionalInformation;
        }

        public CertificateErrors Error { get; }
        public string ErrorCode { get; }
        public string Description { get; }
        public EventId EventId { get; }
        public IEnumerable<string> AdditionalInformation { get; }
    }
}