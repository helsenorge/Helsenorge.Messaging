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
/// Detailed description of a service.
/// </summary>
public class ServiceDetails : CommunicationPartyDetails
{
    /// <summary>
    /// Service code defining what type of service this is. Valid values: OID 8655. 8652. 8653. 8654. 8663.
    /// Norwegian description:
    /// "Tjenestekoden som definerer hva slags tjeneste dette er. Gyldige verdier: OID 8655 , 8652, 8653, 8654, 8663"
    /// </summary>
    public Code Code { get; set;  }

    /// <summary>
    /// Remarks.
    /// Maps to the field "Merknader" in AR's User Interface.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Place/Function
    /// Maps to the field "Sted/funksjon" in AR's User Interface.
    /// </summary>
    public string LocationDescription { get; set; }
}
