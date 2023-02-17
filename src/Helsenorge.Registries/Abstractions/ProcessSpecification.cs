/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

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
        private Version _version;
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
                if (_version == null && !string.IsNullOrWhiteSpace(VersionString))
                {
                    _version = new Version(VersionString);
                }
                return _version;
            }
        }
    }
}
