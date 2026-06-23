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

public static class MailAddressUtils
{
    public static MailAddress Create(string address)
    {
        if (!string.IsNullOrEmpty(address))
        {
            var firstPos = address.IndexOf('"');
            var lastPos = address.LastIndexOf('"');

            if (firstPos != -1 && firstPos < lastPos && address.IndexOf('"', firstPos + 1, lastPos - firstPos - 1) != -1)
            {
                address = new StringBuilder(address).Replace("\"", string.Empty, firstPos + 1, lastPos - firstPos - 1).ToString();
            }
        }

        return new MailAddress(address);
    }

    public static MailAddress Create(string address, string displayName)
    {
        if (!string.IsNullOrEmpty(displayName))
        {
            displayName = displayName.Replace("\"", string.Empty);

            if (125 < displayName.Length)
            {
                displayName = displayName[..125];
            }
        }

        return Create(ToSmtpAddress(address, displayName));
    }

    public static string ToEncodedString(this MailAddress m)
    {
        return ToSmtpAddress(m.Address, MimeHeaderUtils.EncodeMime(m.DisplayName));
    }

    private static string ToSmtpAddress(string address, string displayName)
    {
        return $"\"{displayName}\" <{address}>";
    }
}