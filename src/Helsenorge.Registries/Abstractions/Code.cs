/*
 * Copyright (c) 2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

namespace Helsenorge.Registries.Abstractions;

/// <summary>
/// Code from a code system.
/// </summary>
public class Code
{
    /// <summary>
    /// The code system's OID (Object Identifier).
    /// </summary>
    public int OID { get; set; }
    /// <summary>
    /// The code's value.
    /// </summary>
    public string Value { get; set; }
    /// <summary>
    /// A textual description appropriate for a human.
    /// </summary>
    public string Text { get; set; }
}
