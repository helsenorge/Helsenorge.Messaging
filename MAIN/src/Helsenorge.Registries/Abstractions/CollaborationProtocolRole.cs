using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Abstractions
{
	/// <summary>
	/// A role can be thought of as the party providing a specific service, or in practial terms a message type. 
	/// </summary>
	/// <example>
	/// <![CDATA[
	///  <tns:CollaborationRole>
    ///  <tns:ProcessSpecification tns:name="Dialog_Innbygger_Timereservasjon" tns:version="1.1" xlink:type="simple" xlink:href="http://www.helsedirektoratet.no/processes/Dialog_Innbygger_Timereservasjon.xml" tns:uuid="4ab55eaa-a095-4a4f-96e4-48fbf577fe48" />
    ///   <tns:Role tns:name="DIALOG_INNBYGGER_TIMERESERVASJONsender" xlink:type="simple" xlink:href="http://www.helsedirektoratet.no/processes#DIALOG_INNBYGGER_TIMERESERVASJONsender" />
    ///  <tns:ApplicationCertificateRef tns:certId="enc" />
    ///  <tns:ServiceBinding>
    ///		<tns:Service tns:type="string">S-DIALOG_INNBYGGER_TIMERESERVASJON</tns:Service>
    ///		<!-- CanSend and CanReceive content omitted -->
    ///		<tns:CanSend />
    ///		<tns:CanSend />
    ///		<tns:CanReceive />
    ///		<tns:CanReceive />
    ///  </tns:ServiceBinding>
    ///	</tns:CollaborationRole>
	/// ]]>
	/// </example>
	[Serializable]
	public class CollaborationProtocolRole
	{
		Version _version;
		/// <summary>
		/// Name of role
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// String representation of version
		/// </summary>
		public string VersionString { get; set; }
		/// <summary>
		/// Version of role
		/// </summary>
		public Version Version
		{
			get
			{
				if (_version == null)
				{
					_version = new Version(this.VersionString);
				}
				return _version;
			}
		}

		/// <summary>
		/// List of messages this role can send. If messages are bi-directional, the same information will be present in both the SendMessages and ReceiveMessages
		/// </summary>
		public IList<CollaborationProtocolMessage> SendMessages { get; set; }
		/// <summary>
		/// List of messages this role can receive. If messages are bi-directional, the same information will be present in both the SendMessages and ReceiveMessages
		/// </summary>
		public IList<CollaborationProtocolMessage> ReceiveMessages { get; set; }

	}
}
