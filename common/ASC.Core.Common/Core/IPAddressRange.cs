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

using System.Net.Sockets;

namespace ASC.Core;

public class IPAddressRange(IPAddress lower, IPAddress upper)
{
    private readonly AddressFamily _addressFamily = lower.AddressFamily;
    private readonly byte[] _lowerBytes = lower.GetAddressBytes();
    private readonly byte[] _upperBytes = upper.GetAddressBytes();

    public static bool MatchIPs(string requestIp, string restrictionIp)
    {
        var ipWithoutPort = GetIpWithoutPort(requestIp);
        var dividerIdx = restrictionIp.IndexOf('-');
        if (dividerIdx > 0)
        {
            var lower = IPAddress.Parse(restrictionIp.Substring(0, dividerIdx).Trim());
            var upper = IPAddress.Parse(restrictionIp.Substring(dividerIdx + 1).Trim());

            var range = new IPAddressRange(lower, upper);

            return range.IsInRange(IPAddress.Parse(ipWithoutPort));
        }

        if (restrictionIp.IndexOf('/') > 0)
        {
            return IsInRange(ipWithoutPort, restrictionIp);
        }

        return ipWithoutPort == restrictionIp;
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

    private static bool IsInRange(string ipAddress, string CIDRmask)
    {
        var network = IPNetwork.Parse(CIDRmask);

        var requestIP = IPAddress.Parse(ipAddress);
        var restrictionIP = network.BaseAddress;

        if (requestIP.AddressFamily != restrictionIP.AddressFamily)
        {
            return false;
        }

        var IP_addr = BitConverter.ToInt32(requestIP.GetAddressBytes(), 0);
        var CIDR_addr = BitConverter.ToInt32(restrictionIP.GetAddressBytes(), 0);
        var CIDR_mask = IPAddress.HostToNetworkOrder(-1 << (32 - network.PrefixLength));

        return (IP_addr & CIDR_mask) == (CIDR_addr & CIDR_mask);
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
