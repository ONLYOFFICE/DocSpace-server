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

using net.openstack.Core;

namespace ASC.Web.Files.Utils;

[Scope]
public class WebhookManager(
    TenantManager tenantManager,
    UserManager userManager,
    FileSecurity fileSecurity,
    IServiceProvider serviceProvider,
    IWebhookPublisher webhookPublisher)
{

    public async Task<IEnumerable<DbWebhooksConfig>> GetWebhookConfigsAsync<T>(WebhookTrigger trigger, FileEntry<T> entry)
    {
        var whoCanRead = await GetWhoCanRead(entry);

        var result = await webhookPublisher.GetWebhookConfigsAsync(trigger, CanRead);

        //async Task<bool> CanRead(Guid userId)
        //{
        //    return await fileSecurity.CanReadAsync(entry, userId);
        //}

        Task<bool> CanRead(Guid userId)
        {
            return Task.FromResult(whoCanRead.Contains(userId));
        }

        return result;
    }

    public async Task PublishAsync<T>(WebhookTrigger trigger, IEnumerable<DbWebhooksConfig> webhookConfigs, FileEntry<T> entry)
    {
        var data = await Convert(entry);

        await webhookPublisher.PublishAsync(trigger, webhookConfigs, data);
    }

    public async Task PublishAsync<T>(WebhookTrigger trigger, FileEntry<T> entry)
    {
        var whoCanRead = await GetWhoCanRead(entry);

        var data = await Convert(entry);

        await webhookPublisher.PublishAsync(trigger, CanRead, data);

        //async Task<bool> CanRead(Guid userId)
        //{
        //    return await fileSecurity.CanReadAsync(entry, userId);
        //}

        Task<bool> CanRead(Guid userId)
        {
            return Task.FromResult(whoCanRead.Contains(userId));
        }
    }

    private async Task<FileEntryDto<T>> Convert<T>(FileEntry<T> entry)
    {
        return entry switch
        {
            File<T> file => await serviceProvider.GetRequiredService<FileDtoHelper>().GetAsync(file),
            Folder<T> folder => await serviceProvider.GetRequiredService<FolderDtoHelper>().GetAsync(folder),
            _ => null
        };
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
