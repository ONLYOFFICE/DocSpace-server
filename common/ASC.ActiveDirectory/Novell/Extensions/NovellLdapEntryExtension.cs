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

namespace ASC.ActiveDirectory.Novell.Extensions;

[Singleton]
public class NovellLdapEntryExtension(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("ASC.ActiveDirectory");
    public object GetAttributeValue(LdapEntry ldapEntry, string attributeName, bool getBytes = false)
    {
        try
        {
            var attribute = ldapEntry.GetAttribute(attributeName);

            if (attribute == null)
            {
                return null;
            }

            if (!(string.Equals(attributeName, LdapConstants.ADSchemaAttributes.OBJECT_SID,
                StringComparison.OrdinalIgnoreCase) || getBytes))
            {
                return attribute.StringValue;
            }

            if (attribute.ByteValue == null)
            {
                return null;
            }

            var value = new byte[attribute.ByteValue.Length];

            Buffer.BlockCopy(attribute.ByteValue, 0, value, 0, attribute.ByteValue.Length);

            if (getBytes)
            {
                return value;
            }

            return DecodeSid(value);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public string[] GetAttributeArrayValue(LdapEntry ldapEntry, string attributeName)
    {
        var attribute = ldapEntry.GetAttribute(attributeName);
        return attribute?.StringValueArray;
    }

    private static string DecodeSid(byte[] sid)
    {
        var strSid = new StringBuilder("S-");

        // get version
        int revision = sid[0];
        strSid.Append(revision.ToString(CultureInfo.InvariantCulture));

        //next byte is the count of sub-authorities
        var countSubAuths = sid[1] & 0xFF;

        //get the authority
        long authority = 0;

        //String rid = "";
        for (var i = 2; i <= 7; i++)
        {
            authority |= (long)sid[i] << (8 * (5 - (i - 2)));
        }

        strSid.Append('-');
        strSid.Append(authority);

        //iterate all the sub-auths
        var offset = 8;
        const int size = 4; //4 bytes for each sub auth

        for (var j = 0; j < countSubAuths; j++)
        {
            long subAuthority = 0;
            for (var k = 0; k < size; k++)
            {
                subAuthority |= (long)(sid[offset + k] & 0xFF) << (8 * k);
            }

            strSid.Append('-');
            strSid.Append(subAuthority);

            offset += size;
        }

        return strSid.ToString();
    }

    /// <summary>
    /// Create LDAPObject by LdapEntry
    /// </summary>
    /// <param name="ldapEntry">init ldapEntry</param>
    /// <param name="ldapUniqueIdAttribute"></param>
    /// <returns>LDAPObject</returns>
    public LdapObject ToLdapObject(LdapEntry ldapEntry, string ldapUniqueIdAttribute = null)
    {
        ArgumentNullException.ThrowIfNull(ldapEntry);

        var novellLdapObject = new NovellLdapObject(_logger, this);
        novellLdapObject.Init(ldapEntry, ldapUniqueIdAttribute);

        return novellLdapObject;
    }

    /// <summary>
    /// Create lis of LDAPObject by LdapEntry list
    /// </summary>
    /// <param name="entries">list of LdapEntry</param>
    /// <param name="ldapUniqueIdAttribute"></param>
    /// <returns>list of LDAPObjects</returns>
    public List<LdapObject> ToLdapObjects(IEnumerable<LdapEntry> entries, string ldapUniqueIdAttribute = null)
    {
        return entries.Select(e => ToLdapObject(e, ldapUniqueIdAttribute)).ToList();
    }
}
