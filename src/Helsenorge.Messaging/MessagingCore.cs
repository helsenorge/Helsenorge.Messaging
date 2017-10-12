using System;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Security;
using Helsenorge.Messaging.ServiceBus;
using Helsenorge.Registries;
using Helsenorge.Registries.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging
{
	/// <summary>
	/// Provides services that are common to <see cref="MessagingClient"/> and <see cref="MessagingServer"/>
	/// </summary>
	public abstract class MessagingCore
	{
		/// <summary>
		/// Gets the options used for messaging
		/// </summary>
		internal MessagingSettings Settings { get; }
		/// <summary>
		/// Provides access to the collaboration protocol registry
		/// </summary>
		internal ICollaborationProtocolRegistry CollaborationProtocolRegistry { get; }
		/// <summary>
		/// Provides access to the address registry
		/// </summary>
		internal IAddressRegistry AddressRegistry { get; }
		/// <summary>
		/// Gets or sets the default <see cref="ICertificateValidator"/>.The default implementation is <see cref="CertificateValidator"/>
		/// </summary>
		public ICertificateValidator DefaultCertificateValidator { get; set; }
		/// <summary>
		/// Gets or sets the default <see cref="IMessageProtection"/>. The default implementation is <see cref="SignThenEncryptMessageProtection"/>
		/// </summary>
		public IMessageProtection DefaultMessageProtection { get; set; }
		/// <summary>
		/// Provides access to service bus specific functionality
		/// </summary>
		public ServiceBusCore ServiceBus { get; }

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
		{
			if (settings == null) throw new ArgumentNullException(nameof(settings));
			if (collaborationProtocolRegistry == null) throw new ArgumentNullException(nameof(collaborationProtocolRegistry));
			if (addressRegistry == null) throw new ArgumentNullException(nameof(addressRegistry));

			Settings = settings;
			CollaborationProtocolRegistry = collaborationProtocolRegistry;
			AddressRegistry = addressRegistry;

			DefaultCertificateValidator = new CertificateValidator(settings.UseOnlineRevocationCheck);
			DefaultMessageProtection = new SignThenEncryptMessageProtection();
			ServiceBus = new ServiceBusCore(this);

			Settings.Validate();
		}
	}
}
