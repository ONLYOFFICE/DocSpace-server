// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Common.Utils;

/// <summary>
/// Reads a tuning value from the configuration without letting a bad value stop the host or switch a protection off
/// unnoticed. A missing or empty key gives the default. A value that does not parse gives the default and a warning.
/// A value outside the bounds is moved to the nearest bound, with a warning. None of the readers throws for what the
/// configuration holds.
/// </summary>
/// <remarks>
/// Read once, when the service is created, and keep the result: every call that meets a bad value writes its warning
/// again. Values are read in the invariant culture. A time span is <c>[-][d.]hh:mm[:ss[.fffffff]]</c>. Two spellings
/// read as days, which may not be what was meant: a bare number (<c>"60"</c> is 60 days, not 60 minutes), and three
/// parts whose first is 24 or more, read as days:hours:minutes (<c>"24:00:00"</c> is 24 days, not 24 hours;
/// <c>"36:15:00"</c> is 36 days and 15 hours; <c>"24:30:00"</c> and <c>"24:00"</c> do not parse). They are kept as
/// read, since days may be what was meant, and get a warning that shows the reading in the unambiguous form
/// (<c>"24.00:00:00"</c>; one day is <c>"1.00:00:00"</c>). The bounds are code, not configuration, so a default outside
/// them, or a minimum above the maximum, throws <see cref="ArgumentException"/>.
/// </remarks>
public static class ConfigurationValueExtensions
{
    private delegate bool TryParser<T>(string value, out T result);

    /// <summary>
    /// The time span at <paramref name="key"/>, within <paramref name="min"/>..<paramref name="max"/>.
    /// </summary>
    public static TimeSpan ReadInterval(this IConfiguration configuration, string key, TimeSpan @default, TimeSpan min, TimeSpan max, ILogger logger)
    {
        return Read(configuration, key, @default, min, max, logger, TryParseInterval, IsAmbiguousInterval);
    }

    /// <summary>
    /// The 32-bit integer at <paramref name="key"/>, within <paramref name="min"/>..<paramref name="max"/>.
    /// </summary>
    public static int ReadInt(this IConfiguration configuration, string key, int @default, int min, int max, ILogger logger)
    {
        return Read(configuration, key, @default, min, max, logger, TryParseInt);
    }

    /// <summary>
    /// The 64-bit integer at <paramref name="key"/>, within <paramref name="min"/>..<paramref name="max"/>.
    /// </summary>
    public static long ReadLong(this IConfiguration configuration, string key, long @default, long min, long max, ILogger logger)
    {
        return Read(configuration, key, @default, min, max, logger, TryParseLong);
    }

    /// <summary>
    /// The floating-point number at <paramref name="key"/>, within <paramref name="min"/>..<paramref name="max"/>, with
    /// a dot as the decimal separator. <c>NaN</c> and the infinities are unreadable: they would bound nothing.
    /// </summary>
    public static double ReadDouble(this IConfiguration configuration, string key, double @default, double min, double max, ILogger logger)
    {
        return Read(configuration, key, @default, min, max, logger, TryParseDouble);
    }

    /// <summary>
    /// The flag at <paramref name="key"/>: <c>true</c>, <c>1</c>, <c>yes</c> or <c>on</c>, and <c>false</c>, <c>0</c>,
    /// <c>no</c> or <c>off</c>, in any case. Anything else gives the default and a warning, so that a flag written as
    /// <c>enabled=1</c> neither fails the host nor leaves the feature off without a word in the log.
    /// </summary>
    public static bool ReadBool(this IConfiguration configuration, string key, bool @default, ILogger logger)
    {
        var value = configuration[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            return @default;
        }

        if (TryParseBool(value.Trim(), out var result))
        {
            return result;
        }

        logger.WarningConfigurationValueUnreadable(key, value, Format(@default));

        return @default;
    }

    private static T Read<T>(IConfiguration configuration, string key, T @default, T min, T max, ILogger logger, TryParser<T> tryParse, Func<string, bool> isAmbiguous = null)
        where T : struct, IComparable<T>
    {
        if (min.CompareTo(max) > 0)
        {
            throw new ArgumentException($"The minimum of {key} is above its maximum.", nameof(min));
        }

        if (@default.CompareTo(min) < 0 || @default.CompareTo(max) > 0)
        {
            throw new ArgumentException($"The default of {key} is outside its bounds.", nameof(@default));
        }

        var value = configuration[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            return @default;
        }

        var text = value.Trim();

        if (!tryParse(text, out var parsed))
        {
            logger.WarningConfigurationValueUnreadable(key, value, Format(@default));

            return @default;
        }

        var effective = parsed.CompareTo(min) < 0 ? min : parsed.CompareTo(max) > 0 ? max : parsed;

        if (effective.CompareTo(parsed) != 0)
        {
            logger.WarningConfigurationValueOutOfRange(key, value, Format(min), Format(max), Format(effective));
        }
        else if (isAmbiguous is not null && isAmbiguous(text))
        {
            // One warning per value: a value moved to a bound already says what is used.
            logger.WarningConfigurationValueAmbiguous(key, value, Format(effective));
        }

        return effective;
    }

    private static bool TryParseInterval(string value, out TimeSpan result)
    {
        return TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>
    /// A spelling that <see cref="TimeSpan"/> reads as days although it may have been meant otherwise: a bare number
    /// (<c>"60"</c>), or three parts whose first is 24 or more (<c>"24:00:00"</c>, read as days:hours:minutes).
    /// </summary>
    private static bool IsAmbiguousInterval(string text)
    {
        if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _))
        {
            return true;
        }

        var parts = text.TrimStart('-').Split(':');

        return parts.Length == 3
            && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var first)
            && first >= 24;
    }

    private static bool TryParseInt(string value, out int result)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    private static bool TryParseLong(string value, out long result)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    private static bool TryParseDouble(string value, out double result)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) && double.IsFinite(result);
    }

    private static bool TryParseBool(string value, out bool result)
    {
        switch (value.ToLowerInvariant())
        {
            case "true" or "1" or "yes" or "on":
                result = true;
                return true;
            case "false" or "0" or "no" or "off":
                result = false;
                return true;
            default:
                result = false;
                return false;
        }
    }

    private static string Format<T>(T value)
    {
        return value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value.ToString();
    }
}
