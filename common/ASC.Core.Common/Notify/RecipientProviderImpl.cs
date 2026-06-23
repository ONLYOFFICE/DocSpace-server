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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Core.Notify;

[Scope(typeof(IRecipientProvider))]
public class RecipientProviderImpl(UserManager userManager) : IRecipientProvider
{
    public async Task<IRecipient> GetRecipientAsync(string id)
    {
        if (!TryParseGuid(id, out var recID))
        {
            return null;
        }

        var user = await userManager.GetUsersAsync(recID);
        if (user.Id != Constants.LostUser.Id)
        {
            return new DirectRecipient(user.Id.ToString(), user.ToString());
        }

        var group = await userManager.GetGroupInfoAsync(recID);
        if (group.ID != Constants.LostGroupInfo.ID)
        {
            return new RecipientsGroup(group.ID.ToString(), group.Name);
        }

        return null;
    }

    public async Task<IRecipient[]> GetGroupEntriesAsync(IRecipientsGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var result = new List<IRecipient>();
        if (TryParseGuid(group.ID, out var groupID))
        {
            var coreGroup = await userManager.GetGroupInfoAsync(groupID);
            if (coreGroup.ID != Constants.LostGroupInfo.ID)
            {
                var users = await userManager.GetUsersByGroupAsync(coreGroup.ID);
                Array.ForEach(users, u => result.Add(new DirectRecipient(u.Id.ToString(), u.ToString())));
            }
        }

        return result.ToArray();
    }

    public async Task<IRecipientsGroup[]> GetGroupsAsync(IRecipient recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        var result = new List<IRecipientsGroup>();
        if (TryParseGuid(recipient.ID, out var recID))
        {
            if (recipient is IRecipientsGroup)
            {
                var group = await userManager.GetGroupInfoAsync(recID);
                while (group is { Parent: not null })
                {
                    result.Add(new RecipientsGroup(group.Parent.ID.ToString(), group.Parent.Name));
                    group = group.Parent;
                }
            }
            else if (recipient is IDirectRecipient)
            {
                result.AddRange((await userManager.GetUserGroupsAsync(recID, IncludeType.Distinct)).Select(group => new RecipientsGroup(group.ID.ToString(), group.Name)));
            }
        }

        return result.ToArray();
    }

    public async Task<string[]> GetRecipientAddressesAsync(IDirectRecipient recipient, string senderName)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        if (TryParseGuid(recipient.ID, out var userID))
        {
            var user = await userManager.GetUsersAsync(userID);
            if (user.Id != Constants.LostUser.Id)
            {
                if (senderName == Configuration.Constants.NotifyEMailSenderSysName)
                {
                    return [user.Email];
                }

                if (senderName == Configuration.Constants.NotifyMessengerSenderSysName)
                {
                    return [user.UserName];
                }

                if (senderName == Configuration.Constants.NotifyPushSenderSysName)
                {
                    return [user.UserName];
                }

                if (senderName == Configuration.Constants.NotifyTelegramSenderSysName)
                {
                    return [user.Id.ToString()];
                }
            }
        }

        return [];
    }

    /// <summary>
    /// Check if user with this email is activated
    /// </summary>
    /// <param name="recipient"></param>
    /// <returns></returns>
    public async Task<IDirectRecipient> FilterRecipientAddressesAsync(IDirectRecipient recipient)
    {
        //Check activation
        if (recipient.CheckActivation)
        {
            //It's direct email
            if (recipient.Addresses is { Length: > 0 })
            {
                //Filtering only missing users and users who activated already

                var filteredAddresses = await recipient.Addresses.ToAsyncEnumerable().Where(WhereAsync).ToArrayAsync();

                return new DirectRecipient(recipient.ID, recipient.Name, filteredAddresses.ToArray(), false);
            }
        }

        return recipient;
    }

    private async ValueTask<bool> WhereAsync(string address, CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserByEmailAsync(address);
        return user.Id == Constants.LostUser.Id || (user.IsActive && (user.Status & EmployeeStatus.Default) == user.Status);
    }


    private bool TryParseGuid(string id, out Guid guid)
    {
        guid = Guid.Empty;
        if (!string.IsNullOrEmpty(id))
        {
            try
            {
                guid = new Guid(id);

                return true;
            }
            catch (FormatException) { }
            catch (OverflowException) { }
        }

        return false;
    }
}