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

using Action = System.Action;
using Constants = ASC.Core.Users.Constants;

namespace ASC.ActiveDirectory;

public static class LdapUtils
{
    private static readonly Regex _dcRegex = new("dc=([^,]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static string DistinguishedNameToDomain(string distinguishedName)
    {
        if (string.IsNullOrEmpty(distinguishedName))
        {
            return null;
        }

        var matchList = _dcRegex.Matches(distinguishedName);

        var dcList = matchList.Select(match => match.Groups[1].Value).ToList();

        return dcList.Count == 0 ? null : string.Join(".", dcList);
    }

    public static bool IsLoginAccepted(LdapLogin ldapLogin, UserInfo ldapUser, string ldapDomain)
    {
        if (ldapLogin == null
            || string.IsNullOrEmpty(ldapLogin.ToString())
            || string.IsNullOrEmpty(ldapDomain)
            || ldapUser == null
            || ldapUser.Equals(Constants.LostUser)
            || string.IsNullOrEmpty(ldapUser.Email)
            || string.IsNullOrEmpty(ldapUser.UserName))
        {
            return false;
        }

        var hasDomain = !string.IsNullOrEmpty(ldapLogin.Domain);

        if (!hasDomain)
        {
            return ldapLogin.Username.Equals(ldapUser.UserName, StringComparison.InvariantCultureIgnoreCase);
        }

        var fullLogin = ldapLogin.ToString();

        if (fullLogin.Equals(ldapUser.Email, StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        if (!ldapDomain.StartsWith(ldapLogin.Domain))
        {
            return false;
        }

        var alterEmail = ldapUser.UserName.Contains('@')
            ? ldapUser.UserName
            : string.Format("{0}@{1}", ldapUser.UserName, ldapDomain);

        return IsLoginAndEmailSuitable(fullLogin, alterEmail);
    }

    private static string GetLdapAccessableEmail(string email)
    {
        try
        {
            if (string.IsNullOrEmpty(email))
            {
                return null;
            }

            var login = LdapLogin.ParseLogin(email);

            if (string.IsNullOrEmpty(login.Domain))
            {
                return email;
            }

            var dotIndex = login.Domain.LastIndexOf('.');

            var accessableEmail = dotIndex > -1 ? string.Format("{0}@{1}", login.Username, login.Domain.Remove(dotIndex)) : email;

            return accessableEmail;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool IsLoginAndEmailSuitable(string login, string email)
    {
        try
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(email))
            {
                return false;
            }

            var accessableLogin = GetLdapAccessableEmail(login);

            if (string.IsNullOrEmpty(accessableLogin))
            {
                return false;
            }

            var accessableEmail = GetLdapAccessableEmail(email);

            return !string.IsNullOrEmpty(accessableEmail) && accessableLogin.Equals(accessableEmail, StringComparison.InvariantCultureIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static string GeneratePassword()
    {
        return Guid.NewGuid().ToString();
    }

    public static void SkipErrors(Action method, ILogger log = null)
    {
        try
        {
            method();
        }
        catch (Exception ex)
        {
            log?.ErrorSkipErrors(ex);
        }
    }

    extension(UserInfo userInfo)
    {
        public string GetContactsString()
        {
            if (userInfo.ContactsList == null || userInfo.ContactsList.Count == 0)
            {
                return null;
            }

            var sBuilder = new StringBuilder();
            foreach (var contact in userInfo.Contacts)
            {
                sBuilder.Append($"{contact}|");
            }
            return sBuilder.ToString();
        }

        public string GetUserInfoString()
        {
            return string.Format(
                "{{ ID: '{0}' SID: '{1}' Email '{2}' UserName: '{3}' FirstName: '{4}' LastName: '{5}' Title: '{6}' Location: '{7}' Contacts: '{8}' Status: '{9}' }}",
                userInfo.Id,
                userInfo.Sid,
                userInfo.Email,
                userInfo.UserName,
                userInfo.FirstName,
                userInfo.LastName,
                userInfo.Title,
                userInfo.Location,
                userInfo.GetContactsString(),
                Enum.GetName(userInfo.Status));
        }
    }

    public static string UnescapeLdapString(string ldapString)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < ldapString.Length; i++)
        {
            var ch = ldapString[i];
            if (ch == '\\')
            {
                if (i + 1 < ldapString.Length && ldapString[i + 1] == ch)
                {
                    sb.Append(ch);
                    i++;
                }
            }
            else
            {
                sb.Append(ch);
            }
        }
        return sb.ToString();
    }
}