using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Abstractions
{
    /// <summary>
    /// Represents information about a part of a message
    /// </summary>
    /// <example>
    ///		<![CDATA[
    ///		<tns:SimplePart tns:id="message_dialogmld_v1p1" tns:mimetype="application/xml">
    ///			<tns:NamespaceSupported tns:location="MsgHead-v1_2.xsd" tns:version="1.2">http://www.kith.no/xmlstds/msghead/2006-05-24</tns:NamespaceSupported>
    ///			<tns:NamespaceSupported tns:location="dialogmelding-1.1.xsd" tns:version="1.1">http://www.kith.no/xmlstds/dialog/2013-01-23</tns:NamespaceSupported>
    ///		</tns:SimplePart>
    /// ]]>
    /// </example>
    [Serializable]
    public class CollaborationProtocolMessagePart
    {
        /// <summary>
        /// The XML namespace that defines content provided by the part
        /// </summary>
        public string XmlNamespace { get; set; }
        /// <summary>
        /// The xsd file provided by the part
        /// </summary>
        public string XmlSchema { get; set; }
        /// <summary>
        /// Minimum number of occurencies this part can be used
        /// </summary>
        public int MinOccurrence { get; set; }
        /// <summary>
        /// Maximum number of occurencies this part can be used
        /// </summary>
        public int MaxOccurrence { get; set; }
    }
}
