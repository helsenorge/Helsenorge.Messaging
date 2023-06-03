/*
 * Copyright (c) 2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Collections.Generic;

namespace Helsenorge.Registries.Abstractions;

/// <summary>
/// Detailed representation of an organization.
/// </summary>
public class OrganizationDetails : CommunicationPartyDetails
{
    /// <summary>
    /// The enterprise's organizational number.
    /// </summary>
    public int OrganizationNumber { get; set; }
    /// <summary>
    /// Type of enterprise. Valid values: OID: 9040.
    /// Norwegian description:
    /// "Virksomhetstype. Gyldige verdier: OID 9040"
    /// </summary>
    public Code BusinessType { get; set; }
    /// <summary>
    /// The enterprise's industry code. Valid valuesØ SN2007.
    /// Norwegian description:
    /// "Virksomhetens næringskode. Gyldige verdier: SN2007"
    /// </summary>
    public IEnumerable<Code> IndustryCodes { get; set; }
    /// <summary>
    /// The enterprise's published services
    /// Norwegian description:
    /// "Virksomhetens publiserte tjenester."
    /// </summary>
    public IEnumerable<ServiceDetails> Services { get; set; }
}
