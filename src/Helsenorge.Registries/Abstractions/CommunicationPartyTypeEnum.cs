/*
 * Copyright (c) 2023-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

namespace Helsenorge.Registries.Abstractions;

/// <summary>
/// Type of Communication Party.
/// </summary>
[Flags]
public enum CommunicationPartyTypeEnum
{
    /// <summary>
    /// Communication Party Type not defined.
    /// </summary>
    None = 0,
    /// <summary>
    /// The communication party is a person.
    /// </summary>
    Person = 1,
    /// <summary>
    /// An organization from the Norwegian enhetsregisteret.
    /// </summary>
    Organization = 2,
    /// <summary>
    /// A department, can also be a company from the Norwegian bedrifts- og foretaksregisteret (BoF).
    /// </summary>
    Department = 8,
    /// <summary>
    /// A service the organization provides.
    /// </summary>
    Service = 4,
    /// <summary>
    /// All of the above.
    /// </summary>
    All = 15,

}
