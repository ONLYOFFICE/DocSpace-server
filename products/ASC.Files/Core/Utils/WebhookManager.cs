// (c) Copyright Ascensio System SIA 2009-2026
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
    IServiceProvider serviceProvider)
{
    public async Task<IEnumerable<DbWebhooksConfig>> GetWebhookConfigsAsync<T>(WebhookTrigger trigger, FileEntry<T> fileEntry)
    {
        var checker = serviceProvider.GetService<WebhookFileEntryAccessChecker<T>>();
        var pureFileEntry = await GetPureFileEntry(fileEntry);

        return await webhookPublisher.GetWebhookConfigsAsync(trigger, checker, pureFileEntry);
    }

    public async Task PublishAsync<T>(WebhookTrigger trigger, IEnumerable<DbWebhooksConfig> webhookConfigs, FileEntry<T> fileEntry)
    {
        await webhookPublisher.PublishAsync(trigger, webhookConfigs, fileEntry, fileEntry.Id);
    }

    public async Task PublishAsync<T>(WebhookTrigger trigger, FileEntry<T> fileEntry)
    {
        if (trigger is WebhookTrigger.FormSubmit)
        {
            var formChecker =  serviceProvider.GetService<WebhookSubmittedFormAccessChecker<T>>();
            var formData = await GetSubmittedFormData(fileEntry);

            if (formData.OriginalForm is null)
            {
                return;
            }

            await webhookPublisher.PublishAsync(trigger, formChecker, formData, formData.OriginalForm.Id);
            return;
        }

        var checker =  serviceProvider.GetService<WebhookFileEntryAccessChecker<T>>();
        var pureFileEntry = await GetPureFileEntry(fileEntry);

        await webhookPublisher.PublishAsync(trigger, checker, pureFileEntry, pureFileEntry.Id);
    }

    private async Task<FileEntry<T>> GetPureFileEntry<T>(FileEntry<T> fileEntry)
    {
        return fileEntry.FileEntryType == FileEntryType.File
            ? await daoFactory.GetFileDao<T>().GetFileAsync(fileEntry.Id)
            : await daoFactory.GetFolderDao<T>().GetFolderAsync(fileEntry.Id);
    }

    private async Task<SubmittedFormData<T>> GetSubmittedFormData<T>(FileEntry<T> fileEntry)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var submittedForm = await fileDao.GetFileAsync(fileEntry.Id);
        var data = new SubmittedFormData<T>
        {
            SubmittedForm = submittedForm
        };

        var properties = await fileDao.GetProperties(fileEntry.Id);
        if (properties?.FormFilling == null || Equals(properties.FormFilling.OriginalFormId, default(T)))
        {
            data.OriginalForm = submittedForm;
            return data;
        }

        data.OriginalForm = await fileDao.GetFileAsync(properties.FormFilling.OriginalFormId);
        return data;
    }
}

[Scope(GenericArguments = [typeof(int)])]
[Scope(GenericArguments = [typeof(string)])]
public class WebhookFileEntryAccessChecker<T>(FileSecurity fileSecurity) : IWebhookAccessChecker<FileEntry<T>>
{
    public bool CheckIsTarget(FileEntry<T> fileEntry, string targetId)
    {
        return fileEntry.Id.ToString() == targetId;
    }

    public async Task<bool> CheckAccessAsync(FileEntry<T> fileEntry, Guid userId)
    {
        return await fileSecurity.CanReadAsync(fileEntry, userId);
    }
}

public class SubmittedFormData<T>
{
    public FileEntry<T> OriginalForm { get; set; }
    public FileEntry<T> SubmittedForm { get; set; }
}

[Scope(GenericArguments = [typeof(int)])]
public class WebhookSubmittedFormAccessChecker<T>(FileSecurity fileSecurity) : IWebhookAccessChecker<SubmittedFormData<T>>
{
    public bool CheckIsTarget(SubmittedFormData<T> completedFormEntry, string targetId)
    {
        return completedFormEntry.OriginalForm.Id.ToString() == targetId;
    }

    public async Task<bool> CheckAccessAsync(SubmittedFormData<T> completedFormEntry, Guid userId)
    {
        return await fileSecurity.CanReadAsync(completedFormEntry.SubmittedForm, userId);
    }
}
