// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using ASC.Webhooks.Core.EF.Model;

namespace ASC.Web.Files.Utils;

[Scope]
public class WebhookManager(
    IDaoFactory daoFactory,
    IWebhookPublisher webhookPublisher,
    WebhookFileEntryAccessChecker webhookFileEntryAccessChecker)
{
    public async Task<IEnumerable<DbWebhooksConfig>> GetWebhookConfigsAsync<T>(WebhookTrigger trigger, FileEntry<T> fileEntry)
    {
        return await webhookPublisher.GetWebhookConfigsAsync(trigger, webhookFileEntryAccessChecker, fileEntry);
    }

    public async Task PublishAsync<T>(WebhookTrigger trigger, IEnumerable<DbWebhooksConfig> webhookConfigs, FileEntry<T> fileEntry)
    {
        if (fileEntry is File<T>)
        {
            var cleanFileEntry = await daoFactory.GetFileDao<T>().GetFileAsync(fileEntry.Id);
            await webhookPublisher.PublishAsync(trigger, webhookConfigs, cleanFileEntry);
        }

        if (fileEntry is Folder<T>)
        {
            var cleanFolderEntry = await daoFactory.GetFolderDao<T>().GetFolderAsync(fileEntry.Id);
            await webhookPublisher.PublishAsync(trigger, webhookConfigs, cleanFolderEntry);
        }
    }

    public async Task PublishAsync<T>(WebhookTrigger trigger, FileEntry<T> fileEntry)
    {
        if (fileEntry is File<T>)
        {
            var cleanFileEntry = await daoFactory.GetFileDao<T>().GetFileAsync(fileEntry.Id);
            await webhookPublisher.PublishAsync(trigger, webhookFileEntryAccessChecker, cleanFileEntry);
        }

        if (fileEntry is Folder<T>)
        {
            var cleanFolderEntry = await daoFactory.GetFolderDao<T>().GetFolderAsync(fileEntry.Id);
            await webhookPublisher.PublishAsync(trigger, webhookFileEntryAccessChecker, cleanFolderEntry);
        }
    }
}

[Scope]
public class WebhookFileEntryAccessChecker(
    TenantManager tenantManager,
    UserManager userManager,
    FileSecurity fileSecurity) : IWebhookAccessChecker<object>
{
    public async Task<bool> CheckAccessAsync(object fileEntry, Guid userId)
    {
        if (fileEntry is FileEntry<int> fileEntryInt)
        {
            return await fileSecurity.CanReadAsync(fileEntryInt, userId);
        }

        if (fileEntry is FileEntry<string> fileEntryString)
        {
            return await fileSecurity.CanReadAsync(fileEntryString, userId);
        }

        return false;
    }

    private async Task<IEnumerable<Guid>> GetWhoCanRead<T>(FileEntry<T> entry)
    {
        var result = new List<Guid> { entry.CreateBy };

        if (entry.RootFolderType != FolderType.USER && entry.RootFolderType != FolderType.TRASH)
        {
            result.Add(tenantManager.GetCurrentTenant().OwnerId);

            var admins = (await userManager.GetUsersByGroupAsync(Constants.GroupAdmin.ID)).Select(x => x.Id);

            result.AddRange(admins);

            var whoCanRead = await fileSecurity.WhoCanReadAsync(entry);

            result.AddRange(whoCanRead);
        }

        return result.Distinct();
    }
}