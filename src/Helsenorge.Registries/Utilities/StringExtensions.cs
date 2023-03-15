/*
 * Copyright (c) 2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Globalization;

namespace Helsenorge.Registries.Utilities;

/// <summary>
/// Contains extension methods for String.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Returns an integer parsed from the string, otherwise the value set in defaultValue.
    /// </summary>
    /// <param name="value">The string to be converted to an integer.</param>
    /// <param name="defaultValue">The default value to be returned if the integer value cannot be parsed.</param>
    /// <returns></returns>
    public static int ToInt(this string value, int defaultValue = 0)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
            ? i
            : defaultValue;
    }
}
