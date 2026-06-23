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

namespace ASC.ActiveDirectory.Base;


public abstract class LdapHelper(ILogger<LdapHelper> logger, InstanceCrypto instanceCrypto) : IDisposable
{
    public LdapSettings Settings { get; private set; }
    public abstract bool IsConnected { get; }

    protected readonly ILogger<LdapHelper> _logger = logger;
    protected readonly InstanceCrypto _instanceCrypto = instanceCrypto;

    public void Init(LdapSettings settings)
    {
        Settings = settings;
    }

    public abstract void Connect();

    public abstract Dictionary<string, string[]> GetCapabilities();

    public abstract string SearchDomain();

    public abstract void CheckCredentials(string login, string password, string server, int portNumber,
        bool startTls, bool ssl, bool acceptCertificate, string acceptCertificateHash);

    public abstract bool CheckUserDn(string userDn);

    public abstract List<LdapObject> GetUsers(string filter = null, int limit = -1);

    public abstract LdapObject GetUserBySid(string sid);

    public abstract bool CheckGroupDn(string groupDn);

    public abstract List<LdapObject> GetGroups(Criteria criteria = null);

    public bool UserExistsInGroup(LdapObject domainGroup, LdapObject domainUser, LdapSettings settings) // string memberString, string groupAttribute, string primaryGroupId)
    {
        try
        {
            if (domainGroup == null || domainUser == null)
            {
                return false;
            }

            var memberString = domainUser.GetValue(Settings.UserAttribute) as string;
            if (string.IsNullOrEmpty(memberString))
            {
                return false;
            }

            var groupAttribute = settings.GroupAttribute;
            if (string.IsNullOrEmpty(groupAttribute))
            {
                return false;
            }

            var userPrimaryGroupId = domainUser.GetValue(LdapConstants.ADSchemaAttributes.PRIMARY_GROUP_ID) as string;

            if (!string.IsNullOrEmpty(userPrimaryGroupId) && domainGroup.Sid.EndsWith("-" + userPrimaryGroupId))
            {
                // Domain Users found
                return true;
            }

            var members = domainGroup.GetValues(groupAttribute);

            if (members.Count == 0)
            {
                return false;
            }

            if (members.Any(member => memberString.Equals(member, StringComparison.InvariantCultureIgnoreCase)
                                      || member.Equals(domainUser.DistinguishedName, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }
        }
        catch (Exception e)
        {
            _logger.ErrorUserExistsInGroupFailed(e);
        }

        return false;
    }

    public string GetPassword(byte[] passwordBytes)
    {
        if (passwordBytes == null || passwordBytes.Length == 0)
        {
            return string.Empty;
        }

        string password;
        try
        {
            password = _instanceCrypto.Decrypt(passwordBytes, new UnicodeEncoding());
        }
        catch (Exception)
        {
            password = string.Empty;
        }
        return password;
    }

    public async Task<byte[]> GetPasswordBytesAsync(string password)
    {
        byte[] passwordBytes;

        try
        {
            passwordBytes = await _instanceCrypto.EncryptAsync(new UnicodeEncoding().GetBytes(password));
        }
        catch (Exception)
        {
            passwordBytes = [];
        }

        return passwordBytes;
    }

    public abstract void Dispose();
}