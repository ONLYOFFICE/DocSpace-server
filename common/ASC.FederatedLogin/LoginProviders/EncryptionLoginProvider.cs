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

namespace ASC.Web.Studio.Core;

[Scope]
public class EncryptionLoginProvider(
    ILogger<EncryptionLoginProvider> logger,
    AuthContext authContext,
    InstanceCrypto instanceCrypto,
    AccountLinker accountLinker)
{
    public async Task SetKeysAsync(Guid userId, string keys)
    {
        if (string.IsNullOrEmpty(keys))
        {
            return;
        }

        var loginProfile = new LoginProfile
        {
            Id = userId.ToString(),
            Provider = ProviderConstants.Encryption,
            Name = await instanceCrypto.EncryptAsync(keys)
        };

        await accountLinker.AddLinkAsync(userId, loginProfile);
    }

    public async Task<string> GetKeysAsync()
    {
        return await GetKeysAsync(authContext.CurrentAccount.ID);
    }

    public async Task<string> GetKeysAsync(Guid userId)
    {
        var profile = (await accountLinker.GetLinkedProfilesAsync(userId.ToString(), ProviderConstants.Encryption)).FirstOrDefault();
        if (profile == null)
        {
            return null;
        }

        try
        {
            return await instanceCrypto.DecryptAsync(profile.Name);
        }
        catch (Exception ex)
        {
            var message = string.Format("Can not decrypt {0} keys for {1}", ProviderConstants.Encryption, userId.ToString());
            logger.ErrorWithException(message, ex);
            return null;
        }
    }

    public async Task<IDictionary<Guid, string>> GetKeysAsync(IEnumerable<Guid> usrsIds)
    {
        var profiles = await accountLinker.GetLinkedProfilesAsync(usrsIds.Select(id => id.ToString()), ProviderConstants.Encryption);
        var keys = new Dictionary<Guid, string>(profiles.Count);

        foreach (var profilePair in profiles)
        {
            var userId = new Guid(profilePair.Key);

            try
            {
                var key = await instanceCrypto.DecryptAsync(profilePair.Value.Name);
                keys.Add(new Guid(profilePair.Key), key);
            }
            catch (Exception ex)
            {
                var message = string.Format("Can not decrypt {0} keys for {1}", ProviderConstants.Encryption, userId.ToString());
                logger.ErrorWithException(message, ex);
            }
        }

        return keys;
    }
}