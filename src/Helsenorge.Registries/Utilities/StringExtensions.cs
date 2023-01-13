/*
 * Copyright (c) 2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Globalization;

namespace Helsenorge.Registries.Utilities;

public static class StringExtensions
{
    public static int ToInt(this string value, int defaultValue = 0)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
            ? i
            : defaultValue;
    }
}
