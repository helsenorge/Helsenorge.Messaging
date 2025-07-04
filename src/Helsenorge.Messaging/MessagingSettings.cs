﻿/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Amqp;
using Helsenorge.Messaging.Amqp;

namespace Helsenorge.Messaging
{
    /// <summary>
    /// Specifies settings for the messaging system
    /// </summary>
    public class MessagingSettings
    {
        /// <summary>
        /// The HER-ids we are representing.
        /// </summary>
        public List<int> MyHerIds { get; set; } = new List<int>();
        /// <summary>
        /// The certificate used for signing messages
        /// </summary>
        public CertificateSettings SigningCertificate { get; set; }
        /// <summary>
        /// The certificate used to decrypt messages
        /// </summary>
        public CertificateSettings DecryptionCertificate { get; set; }
        /// <summary>
        /// The old certificate used for decryption when we have moved over to a new one.
        /// </summary>
        public CertificateSettings LegacyDecryptionCertificate { get; set; }
        /// <summary>
        /// Indicates if we should ignore certificate errors when sending
        /// </summary>
        public bool IgnoreCertificateErrorOnSend { get; set; }
        /// <summary>
        /// Use online certificate revocation list (CRL) check. Default true.
        /// </summary>
        public bool UseOnlineRevocationCheck { get; set; } = true;
        /// <summary>
        /// Provides access to service bus settings
        /// </summary>
        public AmqpSettings AmqpSettings { get; }
        /// <summary>
        /// Indicates if the payload should be logged. This is false by default since the payload can contain sensitive information
        /// </summary>
        public bool LogPayload { get; set; }

        /// <summary>
        /// A Dictionary with additional application properties which will be added to <see cref="Message"/>.
        /// </summary>
        public IDictionary<string, object> ApplicationProperties { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Indicates if we should NOT add metadata from payload into application properties. default: true
        /// </summary>
        public bool SkipAddingPayloadMetadataIntoApplicationProperties  { get; set; }

        /// <summary>
        /// Indicates what encryption types get rejected in messaging
        /// </summary>
        public MessagingEncryptionType RejectionMessagingEncryptionType { get; set; } = MessagingEncryptionType.None;

        /// <summary>
        /// Skip CPA lookup for message functions in this list. Instead use an internal dummy CPA.
        /// </summary>
        public List<string> MessageFunctionsExcludedFromCpaResolve { get; set; } = new List<string>();

        /// <summary>
        /// Specifies the encryption type used in messaging
        /// </summary>
        [Obsolete("SHOULD NOT BE USED BY ANYONE EXCEPT HELSENORGE. Temporary config to allow for TripleDES encryption")]
        public MessagingEncryptionType MessagingEncryptionType { get; set; } = MessagingEncryptionType.AES256;

        /// <summary>
        /// Default constructor
        /// </summary>
        public MessagingSettings()
        {
            IgnoreCertificateErrorOnSend = false;
            LogPayload = false;

            AmqpSettings = new AmqpSettings(this);
        }

        internal void Validate()
        {
            if (MyHerIds.Count == 0 || MyHerIds.Any(herId => herId <= 0)) throw new ArgumentOutOfRangeException(nameof(MyHerIds));
            if (DecryptionCertificate == null) throw new ArgumentNullException(nameof(DecryptionCertificate));
            DecryptionCertificate.Validate();
            if (SigningCertificate == null) throw new ArgumentNullException(nameof(SigningCertificate));
            SigningCertificate.Validate();
            AmqpSettings.Validate();
        }

    }
    /// <summary>
    /// Defines settings for service bus
    /// </summary>
    public class AmqpSettings
    {
        internal const int DefaultMaxLinksPerSession = 64;
        internal const ushort DefaultMaxSessions = 256;
        private const int DefaultLinkCredits = 25;
        private const ushort DefaultCacheEntryTimeToLive = 120;
        private const ushort DefaultMaxCacheEntryTrimCount = 24;
        internal const int DefaultTimeoutInMilliseconds = 60000;

        private readonly MessagingSettings _settings;

        /// <summary>Initializes a new instance of the <see cref="AmqpSettings" /> class.</summary>
        public AmqpSettings()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="AmqpSettings" /> class with a <see cref="MessagingSettings"/>.</summary>
        /// <param name="settings">A <see cref="MessagingSettings"/> instance that contains additional settings.</param>
        public AmqpSettings(MessagingSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Gets or set the Type of Message Broker we are using
        /// </summary>
        public MessageBrokerDialect MessageBrokerDialect { get; set; } = MessageBrokerDialect.RabbitMQ;
        /// <summary>
        /// Gets or sets the connection string
        /// </summary>
        public string ConnectionString { get; set; }
        /// <summary>
        /// Provides access to settings related to asynchronous queues
        /// </summary>
        public AsynchronousSettings Asynchronous { get; } = new AsynchronousSettings();
        /// <summary>
        /// Provides access to settings related to synchronous queues
        /// </summary>
        public SynchronousSettings Synchronous { get; } = new SynchronousSettings();
        /// <summary>
        /// Provides access to settings related to error queues
        /// </summary>
        public ErrorSettings Error { get; } = new ErrorSettings();
        /// <summary>
        /// The maximum number of receivers to keep open at any time
        /// </summary>
        public uint MaxReceivers { get; set; } = 5;
        /// <summary>
        /// The maximum number of senders to keep open at any time
        /// </summary>
        public uint MaxSenders { get; set; } = 5;
        /// <summary>
        /// The maximum number of messaging factories to use
        /// </summary>
        public uint MaxFactories { get; set; } = 1;
        /// <summary>
        /// Get or set the begin-handle-max field (less by one)
        /// </summary>
        public int MaxLinksPerSession { get; set; } = DefaultMaxLinksPerSession;
        /// <summary>
        /// Get or set the open.channel-max field (less by one)
        /// </summary>
        public ushort MaxSessionsPerConnection { get; set; } = DefaultMaxSessions;
        /// <summary>
        /// Get or set the link-credit being used by the receiver link. Setting this overrides the default value of 1 credit.
        /// </summary>
        public int LinkCredits { get; set; } = DefaultLinkCredits;
        /// <summary>
        /// The time in seconds since the last time a cache entry was used and when we consider it prime for recycling.
        /// </summary>
        public ushort CacheEntryTimeToLive { get; set; } = DefaultCacheEntryTimeToLive;
        /// <summary>
        /// The max cache entries our recycling process will handle each time it is triggered.
        /// </summary>
        public ushort MaxCacheEntryTrimCount { get; set; } = DefaultMaxCacheEntryTrimCount;
        /// <summary>
        /// Log messages not encrypted and enveloped as Warning
        /// </summary>
        public bool LogMessagesNotSignedAndEnvelopedAsWarning { get; set; } = true;
        /// <summary>
        /// If true logs every the MessageListener has finished a read call to the broker.
        /// </summary>
        public bool LogReadTime { get; set; } = false;
        /// <summary>
        /// The HER-ids we are representing.
        /// </summary>
        public List<int> MyHerIds => _settings.MyHerIds;
        /// <summary>
        /// The certificate used for signing messages
        /// </summary>
        public CertificateSettings SigningCertificate => _settings.SigningCertificate;
        /// <summary>
        /// The certificate used to decrypt messages
        /// </summary>
        public CertificateSettings DecryptionCertificate => _settings.DecryptionCertificate;
        /// <summary>
        /// The old certificate used for decryption when we have moved over to a new one.
        /// </summary>
        public CertificateSettings LegacyDecryptionCertificate => _settings.LegacyDecryptionCertificate;

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString)) throw new ArgumentNullException(nameof(ConnectionString));
            if (Asynchronous.ProcessingTasks > 0)
                Asynchronous.Validate();
            if (Synchronous.ProcessingTasks > 0)
                Synchronous.Validate();
            if (Error.ProcessingTasks > 0)
                Error.Validate();
        }
    }
    /// <summary>
    /// Defines settings for synchronous queues
    /// </summary>
    public class SynchronousSettings
    {
        /// <summary>
        /// Number of processing tasks
        /// </summary>
        public int ProcessingTasks { get; set; } = 2;
        /// <summary>
        /// Time to live for messages sent
        /// </summary>
        public TimeSpan TimeToLive { get; set; } = TimeSpan.FromSeconds(15);
        /// <summary>
        /// Timeout for read operations
        /// </summary>
        public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(1);
        /// <summary>
        /// Sets up a map between reply to queue and server name
        /// </summary>
        public Dictionary<string, string> ReplyQueueMapping { get; set; } = new Dictionary<string, string>();
        /// <summary>
        /// The timout for synchronous calls
        /// </summary>
        public TimeSpan CallTimeout { get; set; } = TimeSpan.FromSeconds(15);
        /// <summary>
        /// Specify the reply queue you are listening on if you have implemented <see cref="Helsenorge.Messaging.Amqp.Receivers.SynchronousReplySingleQueueListener"/>
        /// </summary>
        public string StaticReplyQueue { get; set; }
        /// <summary>
        /// Enable if you want to skip validation of the <see cref="ReplyQueueMapping"/>. Default false.
        /// </summary>
        public bool SkipReplyQueueMappingValidation { get; set; } = false;
        /// <summary>
        /// Enable if you want to set up SynchronousReplySingleQueueListener for handling the  <see cref="StaticReplyQueue"/>. Default false.
        /// </summary>
        public bool HandleSyncreplyListenerIndependently { get; set; } = false;
        
        internal SynchronousSettings() { }

        internal void Validate()
        {
            if (TimeToLive == TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(TimeToLive));
            if (ReadTimeout == TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(ReadTimeout));
            if (CallTimeout == TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(CallTimeout));

            if (SkipReplyQueueMappingValidation)
            {
                return;
            }

            if (FindReplyQueueForMe() == null)
            {
                throw new InvalidOperationException("Could not determine reply to queue for this server");
            }
        }

        internal string FindReplyQueueForMe()
        {
            if (ReplyQueueMapping == null) return null;
            if (ReplyQueueMapping.Count == 0) return null;

            var server = (from s in ReplyQueueMapping.Keys
                where s.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)
                select s).FirstOrDefault();

            return server == null ? null : ReplyQueueMapping[server];
        }
    }
    /// <summary>
    /// Defines settings for asynchronous queues
    /// </summary>
    public class AsynchronousSettings
    {
        /// <summary>
        /// Number of processing tasks
        /// </summary>
        public int ProcessingTasks { get; set; } = 5;
        /// <summary>
        /// Time to live for messages sent
        /// </summary>
        public TimeSpan TimeToLive { get; set; } = TimeSpan.Zero;
        /// <summary>
        /// Timeout for read operations
        /// </summary>
        public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(60);
        internal AsynchronousSettings() {}

        internal void Validate()
        {
            if (ReadTimeout == TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(ReadTimeout));
        }
    }
    /// <summary>
    /// Defines settings for error queues
    /// </summary>
    public class ErrorSettings
    {
        /// <summary>
        /// Number of processing tasks
        /// </summary>
        public int ProcessingTasks { get; set; } = 1;
        /// <summary>
        /// Time to live for messages sent
        /// </summary>
        public TimeSpan TimeToLive { get; set; } = TimeSpan.Zero;
        /// <summary>
        /// Timeout for read operations
        /// </summary>
        public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(60);

        internal ErrorSettings() {}

        internal void Validate()
        {
            if (ReadTimeout == TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(ReadTimeout));
        }
    }
    /// <summary>
    /// Represents information about a certificate and where to get it
    /// </summary>
    public class CertificateSettings
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public CertificateSettings()
        {
            StoreLocation = StoreLocation.LocalMachine;
            StoreName = StoreName.My;
        }
        /// <summary>
        /// The thumbprint of the certificate we should use
        /// </summary>
        public string Thumbprint { get; set; }
        /// <summary>
        /// The name of the certificate store to use
        /// </summary>
        public StoreName StoreName { get; set; }
        /// <summary>
        /// The location of the certificate store to use
        /// </summary>
        public StoreLocation StoreLocation { get; set; }
        /// <summary>
        /// Validates the CertificateSettings, ensuring required values has been set
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrEmpty(Thumbprint)) throw new ArgumentNullException(nameof(Thumbprint));
        }
    }
}
