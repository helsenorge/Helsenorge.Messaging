using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Abstractions
{
    /// <summary>
    /// A ProcessSpecification can be thought of in practial terms as a message type. 
    /// </summary>
    /// <example>
    /// <![CDATA[
    ///  <tns:ProcessSpecification tns:name="Dialog_Innbygger_Timereservasjon" tns:version="1.1" xlink:type="simple" xlink:href="http://www.helsedirektoratet.no/processes/Dialog_Innbygger_Timereservasjon.xml" tns:uuid="4ab55eaa-a095-4a4f-96e4-48fbf577fe48" />
    /// ]]>
    /// </example>
    [Serializable]
    public class ProcessSpecification
    {
        Version _version;
        /// <summary>
        /// Name of process specification
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
    }
}
