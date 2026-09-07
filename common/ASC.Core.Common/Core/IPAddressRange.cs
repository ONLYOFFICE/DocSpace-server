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

namespace ASC.Core;

public class IPAddressRange(IPAddress lower, IPAddress upper)
{
    private readonly AddressFamily _addressFamily = lower.AddressFamily;
    private readonly byte[] _lowerBytes = lower.GetAddressBytes();
    private readonly byte[] _upperBytes = upper.GetAddressBytes();

    /// <summary>
    /// Tells whether a request address falls under a restriction entry: a plain address, an inclusive
    /// <c>from-to</c> range or a CIDR block. Anything that cannot be parsed is not a match — this runs on the
    /// login path, where an exception would lock every user out of the portal.
    /// </summary>
    public static bool MatchIPs(string requestIp, string restrictionIp)
    {
        if (string.IsNullOrWhiteSpace(restrictionIp))
        {
            return false;
        }

        var ipWithoutPort = GetIpWithoutPort(requestIp);
        var dividerIdx = restrictionIp.IndexOf('-');
        if (dividerIdx > 0)
        {
            if (!IPAddress.TryParse(restrictionIp[..dividerIdx].Trim(), out var lower) ||
                !IPAddress.TryParse(restrictionIp[(dividerIdx + 1)..].Trim(), out var upper) ||
                !IPAddress.TryParse(ipWithoutPort, out var address))
            {
                return false;
            }

            var range = new IPAddressRange(lower, upper);

            return range.IsInRange(address);
        }

        if (restrictionIp.IndexOf('/') > 0)
        {
            return IsInRange(ipWithoutPort, restrictionIp);
        }

        return ipWithoutPort == restrictionIp;
    }

    /// <summary>
    /// Tells whether a restriction entry is one <see cref="MatchIPs"/> can ever match a request against, so a
    /// value that would silently never apply can be rejected where it is written instead of stored.
    /// </summary>
    public static bool IsValidRestriction(string restrictionIp)
    {
        if (string.IsNullOrWhiteSpace(restrictionIp))
        {
            return false;
        }

        var dividerIdx = restrictionIp.IndexOf('-');
        if (dividerIdx > 0)
        {
            return IPAddress.TryParse(restrictionIp[..dividerIdx].Trim(), out _) &&
                   IPAddress.TryParse(restrictionIp[(dividerIdx + 1)..].Trim(), out _);
        }

        if (restrictionIp.IndexOf('/') > 0)
        {
            return IPNetwork.TryParse(restrictionIp, out _);
        }

        return IPAddress.TryParse(restrictionIp, out _);
    }

    private bool IsInRange(IPAddress address)
    {
        if (address.AddressFamily != _addressFamily)
        {
            return false;
        }

        var addressBytes = address.GetAddressBytes();

        bool lowerBoundary = true, upperBoundary = true;

        for (var i = 0; i < _lowerBytes.Length && (lowerBoundary || upperBoundary); i++)
        {
            var addressByte = addressBytes[i];
            var upperByte = _upperBytes[i];
            var lowerByte = _lowerBytes[i];

            if ((lowerBoundary && addressByte < lowerByte) || (upperBoundary && addressByte > upperByte))
            {
                return false;
            }

            lowerBoundary &= addressByte == lowerByte;
            upperBoundary &= addressByte == upperByte;
        }

        return true;
    }

    private static bool IsInRange(string ipAddress, string cidrMask)
    {
        if (!IPNetwork.TryParse(cidrMask, out var network) || !IPAddress.TryParse(ipAddress, out var requestIP))
        {
            return false;
        }

        var restrictionIP = network.BaseAddress;

        if (requestIP.AddressFamily != restrictionIP.AddressFamily)
        {
            return false;
        }

        var requestAddr = BitConverter.ToInt32(requestIP.GetAddressBytes(), 0);
        var cidrAddr = BitConverter.ToInt32(restrictionIP.GetAddressBytes(), 0);
        var cidrMaskBits = IPAddress.HostToNetworkOrder(-1 << (32 - network.PrefixLength));

        return (requestAddr & cidrMaskBits) == (cidrAddr & cidrMaskBits);
    }

    private static string GetIpWithoutPort(string ip)
    {
        if (ip.StartsWith("["))
        {
            // [IPv6]:port
            var end = ip.IndexOf(']');
            if (end > 0)
            {
                ip = ip[1..end];
            }
        }
        else if (ip.Count(c => c == ':') == 1)
        {
            // IPv4:port
            ip = ip.Split(':')[0];
        }

        return ip;
    }
}

/// <summary>
/// Requires every entry of the annotated value — a single string or a collection of them — to be a restriction
/// <see cref="IPAddressRange.MatchIPs"/> can match a request against: a plain IP address, an inclusive
/// <c>from-to</c> range or a CIDR block.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class IpAddressOrRangeAttribute() : ValidationAttribute("The {0} field must contain a valid IP address, IP range or CIDR block.")
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var valid = value switch
        {
            null => true,
            string single => IPAddressRange.IsValidRestriction(single),
            IEnumerable<string> many => many.All(IPAddressRange.IsValidRestriction),
            _ => IPAddressRange.IsValidRestriction(value.ToString())
        };

        return valid
            ? ValidationResult.Success
            : new ValidationResult(FormatErrorMessage(validationContext.DisplayName), [validationContext.MemberName]);
    }
}
