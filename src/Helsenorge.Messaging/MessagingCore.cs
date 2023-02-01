/* 
 * Copyright (c) 2020-2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Versioning;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Security;
using Helsenorge.Messaging.Bus;
using Helsenorge.Registries;
using Helsenorge.Registries.Abstractions;

namespace Helsenorge.Messaging
{
    /// <summary>
    /// Provides services that are common to <see cref="MessagingClient"/> and <see cref="MessagingServer"/>
    /// </summary>
    public abstract class MessagingCore
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings">Set of options to use</param>
        /// <param name="collaborationProtocolRegistry">Reference to the collaboration protocol registry</param>
        /// <param name="addressRegistry">Reference to the address registry</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        protected MessagingCore(
            MessagingSettings settings,
            ICollaborationProtocolRegistry collaborationProtocolRegistry,
            IAddressRegistry addressRegistry)
            : this(settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            CollaborationProtocolRegistry = collaborationProtocolRegistry ?? throw new ArgumentNullException(nameof(collaborationProtocolRegistry));
            AddressRegistry = addressRegistry ?? throw new ArgumentNullException(nameof(addressRegistry));
            BusCore = new BusCore(this);

            Settings.Validate();

            CertificateStore = GetDefaultCertificateStore();
            CertificateValidator = GetDefaultCertificateValidator();
            MessageProtection = GetDefaultMessageProtection();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings">Set of options to use</param>
        /// <param name="collaborationProtocolRegistry">Reference to the collaboration protocol registry</param>
        /// <param name="addressRegistry">Reference to the address registry</param>
        /// <param name="certificateStore">
        /// Reference to a custom implementation of <see cref="ICertificateStore"/>, if not set the library will default to Windows Certificate Store.
        /// Setting this argument to null will throw an <see cref="ArgumentNullException"/>.
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        protected MessagingCore(
            MessagingSettings settings,
            ICollaborationProtocolRegistry collaborationProtocolRegistry,
            IAddressRegistry addressRegistry,
            ICertificateStore certificateStore)
            : this(settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            CollaborationProtocolRegistry = collaborationProtocolRegistry ?? throw new ArgumentNullException(nameof(collaborationProtocolRegistry));
            AddressRegistry = addressRegistry ?? throw new ArgumentNullException(nameof(addressRegistry));
            BusCore = new BusCore(this);

            Settings.Validate();

            CertificateStore = certificateStore ?? throw new ArgumentNullException(nameof(certificateStore));
            CertificateValidator = GetDefaultCertificateValidator();
            MessageProtection = GetDefaultMessageProtection();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings">Set of options to use</param>
        /// <param name="collaborationProtocolRegistry">Reference to the collaboration protocol registry</param>
        /// <param name="addressRegistry">Reference to the address registry</param>
        /// <param name="certificateStore">
        /// Reference to a custom implementation of <see cref="ICertificateStore"/>, if not set the library will default to Windows Certificate Store. 
        /// Setting this argument to null must be done cautiously as the default implementation of <see cref="IMessageProtection"/> 
        /// <see cref="SignThenEncryptMessageProtection"/> relies on an <see cref="ICertificateStore"/> implementation.
        /// </param>
        /// <param name="certificateValidator">
        /// Reference to a custom implementation of <see cref="ICertificateValidator"/>, if not set the library will default to the standard implementation 
        /// of <see cref="ICertificateValidator"/>. By setting this parameter to null you effectively disable certificate validation.
        /// </param>
        /// <param name="messageProtection">
        /// Reference to custom implemenation of <see cref="IMessageProtection"/>, if not set the library will default to standard behavior which relies on 
        /// certificates retrieved from <see cref="ICertificateStore"/>. Setting this parameter to null will throw an <see cref="ArgumentNullException"/>.
        /// </param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        protected MessagingCore(
            MessagingSettings settings,
            ICollaborationProtocolRegistry collaborationProtocolRegistry,
            IAddressRegistry addressRegistry,
            ICertificateStore certificateStore,
            ICertificateValidator certificateValidator,
            IMessageProtection messageProtection)
            : this(settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            CollaborationProtocolRegistry = collaborationProtocolRegistry ?? throw new ArgumentNullException(nameof(collaborationProtocolRegistry));
            AddressRegistry = addressRegistry ?? throw new ArgumentNullException(nameof(addressRegistry));
            BusCore = new BusCore(this);

            Settings.Validate();

            CertificateStore = certificateStore;
            CertificateValidator = certificateValidator;
            MessageProtection = messageProtection ?? throw new ArgumentNullException(nameof(messageProtection));
        }

        private MessagingCore(MessagingSettings settings)
        {
            if(settings?.ApplicationProperties == null) return;

            // Populate MessagingSettings.ApplicationProperties with additional System Information.
            var systemInformationProperties = GetSystemInformation();
            foreach (var property in systemInformationProperties)
            {
                if (!settings.ApplicationProperties.ContainsKey(property.Key))
                    settings.ApplicationProperties.Add(property);
            }
        }

        /// <summary>
        /// Gets the options used for messaging
        /// </summary>
        public MessagingSettings Settings { get; }
        /// <summary>
        /// Provides access to the collaboration protocol registry
        /// </summary>
        internal ICollaborationProtocolRegistry CollaborationProtocolRegistry { get; }
        /// <summary>
        /// Provides access to the address registry
        /// </summary>
        internal IAddressRegistry AddressRegistry { get; }

        /// <summary>
        /// Returns the current instance of <see cref="ICertificateStore"/>.
        /// </summary>
        internal ICertificateStore CertificateStore { get;  }

        /// <summary>
        /// Returns the current instance of <see cref="IMessageProtection"/>.
        /// </summary>
        internal IMessageProtection MessageProtection { get; set;  }

        /// <summary>
        /// Returns the current instance of <see cref="ICertificateValidator"/>.
        /// </summary>
        internal ICertificateValidator CertificateValidator { get; }

        /// <summary>
        /// Provides access to bus specific functionality
        /// </summary>
        public BusCore BusCore { get; }

        internal ICertificateStore GetDefaultCertificateStore()
        {
            return new WindowsCertificateStore(Settings.SigningCertificate?.StoreName, Settings.SigningCertificate?.StoreLocation);
        }

        internal IMessageProtection GetDefaultMessageProtection()
        {
            var signingCertificate = CertificateStore.GetCertificate(Settings.SigningCertificate?.Thumbprint);
            var encryptionCertificate = CertificateStore.GetCertificate(Settings.DecryptionCertificate?.Thumbprint);
            var legacyEncryptionCertificate = Settings.LegacyDecryptionCertificate == null ? null : CertificateStore.GetCertificate(Settings.LegacyDecryptionCertificate.Thumbprint);

#pragma warning disable CS0618
            return new SignThenEncryptMessageProtection(signingCertificate, encryptionCertificate, legacyEncryptionCertificate, messagingEncryptionType: Settings.MessagingEncryptionType);
#pragma warning restore CS0618
        }

        internal ICertificateValidator GetDefaultCertificateValidator()
        {
            return new CertificateValidator(Settings.UseOnlineRevocationCheck);
        }

        private static IDictionary<string, object> GetSystemInformation()
        {
            var systemInformation = new Dictionary<string, object>();
            try
            {
                var assemblyName = Assembly.GetExecutingAssembly().GetName();
                systemInformation.Add("X-ExecutingAssembly", assemblyName.FullName);
                systemInformation.Add("X-ExecutingAssemblyVersion", assemblyName.Version.ToString());

                var targetFramework = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
                if(!string.IsNullOrWhiteSpace(targetFramework))
                    systemInformation.Add("X-TargetFramework", targetFramework);
            }
            catch
            {
                // Ignore any errors, we don't want to fail on this step.
            }

            return systemInformation;
        }
    }
}
