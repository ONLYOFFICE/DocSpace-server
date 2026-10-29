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

using ASC.Webhooks.Core.EF.Model;

namespace ASC.Web.Files.Utils;

[Scope]
public class WebhookManager(
    IDaoFactory daoFactory,
    IWebhookPublisher webhookPublisher,
    IServiceProvider serviceProvider,
    FileEntryWebhookDtoHelper fileEntryWebhookDtoHelper,
    ILogger<WebhookManager> logger)
{
    /// <summary>
    /// Finds the subscriptions an event about <paramref name="fileEntry"/> goes to while the caller can still read it,
    /// for an action that moves it where they no longer can (the trash); the payload is built later, by
    /// <see cref="PublishAsync{T}(WebhookTrigger, IEnumerable{DbWebhooksConfig}, FileEntry{T})"/>, from the entry as it
    /// is after the action.
    /// </summary>
    public async Task<IEnumerable<DbWebhooksConfig>> GetWebhookConfigsAsync<T>(WebhookTrigger trigger, FileEntry<T> fileEntry)
    {
        if (!await webhookPublisher.HasWebhookConfigsAsync(trigger))
        {
            return [];
        }

        var checker = serviceProvider.GetService<WebhookFileEntryAccessChecker<T>>();
        var pureFileEntry = await GetPureFileEntry(fileEntry);

        return await webhookPublisher.GetWebhookConfigsAsync(trigger, checker, pureFileEntry);
    }

    public async Task PublishAsync<T>(WebhookTrigger trigger, IEnumerable<DbWebhooksConfig> webhookConfigs, FileEntry<T> fileEntry)
    {
        var publish = await TryBuildAsync(trigger, fileEntry, () => PrepareAfterMoveAsync(trigger, webhookConfigs, fileEntry));

        await publish();
    }

    private async Task<Func<Task>> PrepareAfterMoveAsync<T>(WebhookTrigger trigger, IEnumerable<DbWebhooksConfig> webhookConfigs, FileEntry<T> fileEntry)
    {
        if (!webhookConfigs.Any())
        {
            return () => Task.CompletedTask;
        }

        // the caller's object still describes the entry where it was before the action (moving it updates the
        // database only), so it is read again, and in the trash it gets the origins the trash listing shows
        var entry = await GetPureFileEntry(fileEntry) ?? fileEntry;
        await SetTrashOriginsAsync(entry);

        var payload = await fileEntryWebhookDtoHelper.GetAsync(entry);

        return await PrepareForSubscribersAsync(trigger, webhookConfigs, payload, entry.Id,
            (entryPayload, subscriberId) => fileEntryWebhookDtoHelper.RestrictForSubscriberAsync(entryPayload, entry, subscriberId));
    }

    /// <summary>
    /// Builds the event for every subscriber now and returns the call that publishes it, for an action that removes
    /// <paramref name="fileEntry"/>: who receives the event and what each of them may see in it (the quota of a room
    /// goes only to those who may edit the room) is decided while the entry and its access records still exist.
    /// </summary>
    public async Task<Func<Task>> PrepareAsync<T>(WebhookTrigger trigger, FileEntry<T> fileEntry)
    {
        if (!await webhookPublisher.HasWebhookConfigsAsync(trigger))
        {
            return () => Task.CompletedTask;
        }

        var checker = serviceProvider.GetService<WebhookFileEntryAccessChecker<T>>();
        var pureFileEntry = await GetPureFileEntry(fileEntry) ?? fileEntry;

        var webhookConfigs = await webhookPublisher.GetWebhookConfigsAsync(trigger, checker, pureFileEntry);

        if (webhookConfigs.Count == 0)
        {
            return () => Task.CompletedTask;
        }

        var payload = await fileEntryWebhookDtoHelper.GetAsync(pureFileEntry);

        return await PrepareForSubscribersAsync(trigger, webhookConfigs, payload, pureFileEntry.Id,
            (entryPayload, subscriberId) => fileEntryWebhookDtoHelper.RestrictForSubscriberAsync(entryPayload, pureFileEntry, subscriberId));
    }

    /// <summary>
    /// <see cref="PrepareAsync{T}"/> for an action that must go ahead whatever happens to its webhook: the payload is
    /// built before the action removes what it describes, and a failure to build it must not stop the action itself,
    /// so the webhook is dropped (and logged) instead.
    /// </summary>
    public Task<Func<Task>> TryPrepareAsync<T>(WebhookTrigger trigger, FileEntry<T> fileEntry)
    {
        return TryBuildAsync(trigger, fileEntry, () => PrepareAsync(trigger, fileEntry));
    }

    /// <summary>
    /// Publishes an event about an action that has taken effect. A failure to build it must not fail a request whose
    /// change is already made, so the webhook is dropped (and logged) instead, as for <see cref="TryPrepareAsync{T}"/>.
    /// </summary>
    public async Task PublishAsync<T>(WebhookTrigger trigger, FileEntry<T> fileEntry)
    {
        var publish = await TryBuildAsync(trigger, fileEntry, () => PrepareAfterAsync(trigger, fileEntry));

        await publish();
    }

    private async Task<Func<Task>> TryBuildAsync<T>(WebhookTrigger trigger, FileEntry<T> fileEntry, Func<Task<Func<Task>>> prepare)
    {
        try
        {
            return await prepare();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.ErrorPrepareWebhook(trigger, fileEntry.Id.ToString(), e);

            return () => Task.CompletedTask;
        }
    }

    private async Task<Func<Task>> PrepareAfterAsync<T>(WebhookTrigger trigger, FileEntry<T> fileEntry)
    {
        // the cached subscription list answers before the entry is read again, so a portal without a subscription to
        // this trigger pays no query for an event nobody receives
        if (!await webhookPublisher.HasWebhookConfigsAsync(trigger))
        {
            return () => Task.CompletedTask;
        }

        if (trigger is WebhookTrigger.FormSubmit)
        {
            var formChecker =  serviceProvider.GetService<WebhookSubmittedFormAccessChecker<T>>();
            var formData = await GetSubmittedFormData(fileEntry);

            if (formData.OriginalForm is null)
            {
                return () => Task.CompletedTask;
            }

            var formConfigs = await webhookPublisher.GetWebhookConfigsAsync(trigger, formChecker, formData);

            if (formConfigs.Count == 0)
            {
                return () => Task.CompletedTask;
            }

            var formPayload = await GetFormSubmitPayloadAsync(formData);

            return await PrepareForSubscribersAsync(trigger, formConfigs, formPayload, formData.OriginalForm.Id, fileEntryWebhookDtoHelper.RestrictForSubscriberAsync);
        }

        var checker =  serviceProvider.GetService<WebhookFileEntryAccessChecker<T>>();
        var pureFileEntry = await GetPureFileEntry(fileEntry) ?? fileEntry;

        var webhookConfigs = await webhookPublisher.GetWebhookConfigsAsync(trigger, checker, pureFileEntry);

        if (webhookConfigs.Count == 0)
        {
            return () => Task.CompletedTask;
        }

        var payload = await fileEntryWebhookDtoHelper.GetAsync(pureFileEntry);

        return await PrepareForSubscribersAsync(trigger, webhookConfigs, payload, pureFileEntry.Id,
            (entryPayload, subscriberId) => fileEntryWebhookDtoHelper.RestrictForSubscriberAsync(entryPayload, pureFileEntry, subscriberId));
    }

    /// <summary>
    /// The payload is built once per event, but what it may show depends on the subscription owner (the profile
    /// links of the users in it, the quota of a room), so each owner's subscriptions get the payload cut for that owner.
    /// </summary>
    private async Task<Func<Task>> PrepareForSubscribersAsync<TPayload, TId>(
        WebhookTrigger trigger,
        IEnumerable<DbWebhooksConfig> webhookConfigs,
        TPayload payload,
        TId dataId,
        Func<TPayload, Guid?, Task<TPayload>> restrictForSubscriber)
    {
        var publications = new List<Func<Task>>();

        foreach (var subscriberConfigs in webhookConfigs.GroupBy(r => r.CreatedBy))
        {
            var subscriberPayload = await restrictForSubscriber(payload, subscriberConfigs.Key);

            publications.Add(() => webhookPublisher.PublishAsync(trigger, subscriberConfigs, subscriberPayload, dataId));
        }

        return async () =>
        {
            foreach (var publish in publications)
            {
                await publish();
            }
        };
    }

    private async Task<FormSubmitWebhookDto<T>> GetFormSubmitPayloadAsync<T>(SubmittedFormData<T> formData)
    {
        return new FormSubmitWebhookDto<T>
        {
            OriginalForm = await fileEntryWebhookDtoHelper.GetFileAsync(formData.OriginalForm),
            SubmittedForm = await fileEntryWebhookDtoHelper.GetFileAsync(formData.SubmittedForm)
        };
    }

    /// <summary>
    /// Fills the origins of an entry in the trash, the folder and the room it was moved from, as the trash listing
    /// does; only the entries of the portal's own storage keep them.
    /// </summary>
    private async Task SetTrashOriginsAsync<T>(FileEntry<T> entry)
    {
        if (entry.RootFolderType != FolderType.TRASH || entry is not FileEntry<int> internalEntry)
        {
            return;
        }

        var data = await daoFactory.GetFolderDao<int>().GetOriginsDataAsync([internalEntry.Id])
            .FirstOrDefaultAsync(d => d.Entries.Contains(new KeyValuePair<string, FileEntryType>(internalEntry.Id.ToString(), internalEntry.FileEntryType)));

        if (data?.OriginRoom != null && data.OriginRoom.FolderType.IsRoom())
        {
            internalEntry.OriginRoomId = data.OriginRoom.Id;
            internalEntry.OriginRoomTitle = data.OriginRoom.Title;
        }

        if (data?.OriginFolder != null)
        {
            internalEntry.OriginId = data.OriginFolder.Id;
            internalEntry.OriginTitle = data.OriginFolder.FolderType == FolderType.USER ? FilesUCResource.Files : data.OriginFolder.Title;
        }
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
    public File<T> OriginalForm { get; set; }
    public File<T> SubmittedForm { get; set; }
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
