using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helsenorge.Messaging.ServiceBus;
using Helsenorge.Registries.Abstractions;

namespace Helsenorge.Messaging.Abstractions
{
	public class OutgointMessageDetails
	{
		public MemoryStream Payload { get; set; }
		public string MessageFunction { get; set; }
		public int ToHerId { get; set; }
		public int FromHerId { get; set; }
		public string MessageId { get; set; }
		public CollaborationProtocolProfile Profile { get; set; }
		public DateTime ScheduledSendTimeUtc { get; set; }
		public QueueType QueueType { get; set; }
		public TimeSpan TimeToLive { get; set; }
		public string ContentType { get; set; }
	}
}
