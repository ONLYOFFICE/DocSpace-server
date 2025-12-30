// (c) Copyright Ascensio System SIA 2009-2025
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

using HtmlAgilityPack;
using ConfigurationConstants = ASC.Core.Configuration.Constants;

namespace ASC.Files.Core.Services.NotifyService;

[Scope]
public class NotifyClient(
    WorkContext notifyContext,
    NotifySource notifySource,
    SecurityContext securityContext,
    FilesLinkUtility filesLinkUtility,
    FileUtility fileUtility,
    BaseCommonLinkUtility baseCommonLinkUtility,
    CommonLinkUtility commonLinkUtility,
    IDaoFactory daoFactory,
    PathProvider pathProvider,
    UserManager userManager,
    TenantManager tenantManager,
    StudioNotifyHelper studioNotifyHelper,
    RoomsNotificationSettingsHelper roomsNotificationSettingsHelper,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    FileSecurity fileSecurity,
    GlobalFolder globalFolder,
    IServiceProvider serviceProvider)
{
    public async Task SendDocuSignCompleteAsync<T>(File<T> file, string sourceTitle)
    {
        var client = notifyContext.RegisterClient(serviceProvider, notifySource);
        var recipient = await notifySource.GetRecipientsProvider().GetRecipientAsync(securityContext.CurrentAccount.ID.ToString());
        var action = serviceProvider.GetService<DocuSignCompleteNotifyAction>();
        action.Init(file, sourceTitle);
            
        await client.SendNoticeAsync(action, file.UniqID, recipient, true);
    }

    public async Task SendDocuSignStatusAsync(string subject, string status)
    {
        var client = notifyContext.RegisterClient(serviceProvider, notifySource);

        var recipient = await notifySource.GetRecipientsProvider().GetRecipientAsync(securityContext.CurrentAccount.ID.ToString());
        
        var action = serviceProvider.GetService<DocuSignStatusNotifyAction>();
        action.Init(subject, status);
        
        await client.SendNoticeAsync(action, null, recipient, true);
    }

    public async Task SendMailMergeEndAsync(Guid userId, int countMails, int countError)
    {
        var client = notifyContext.RegisterClient(serviceProvider, notifySource);

        var recipient = await notifySource.GetRecipientsProvider().GetRecipientAsync(userId.ToString());
        
        var action = serviceProvider.GetService<MailMergeEndNotifyAction>();
        action.Init(countMails, countError);
        
        await client.SendNoticeAsync(action, null, recipient, true);
    }

    public async Task SendShareNoticeAsync<T>(FileEntry<T> fileEntry, Dictionary<Guid, FileShare> recipients, string message, string culture = null)
    {
        if (fileEntry == null || recipients.Count == 0)
        {
            return;
        }

        var client = notifyContext.RegisterClient(serviceProvider, notifySource);

        var folderDao = daoFactory.GetFolderDao<T>();
        if (fileEntry.FileEntryType == FileEntryType.File && await folderDao.GetFolderAsync(((File<T>)fileEntry).ParentId) == null)
        {
            return;
        }

        var url = fileEntry.FileEntryType == FileEntryType.File
                      ? filesLinkUtility.GetFileWebPreviewUrl(fileUtility, fileEntry.Title, fileEntry.Id)
                      : pathProvider.GetFolderUrl((Folder<T>)fileEntry);

        Folder<T> folder;

        var fileExtension = "";

        if (fileEntry.FileEntryType == FileEntryType.File)
        {
            var file = (File<T>)fileEntry;
            fileExtension = file.ConvertedExtension;
            folder = await folderDao.GetFolderAsync(file.ParentId);
        }
        else
        {
            folder = (Folder<T>)fileEntry;
        }

        var recipientsProvider = notifySource.GetRecipientsProvider();
        
        var action = fileEntry.FileEntryType == FileEntryType.File
        ? ((File<T>)fileEntry).Encrypted
            ? serviceProvider.GetService<ShareEncryptedDocumentNotifyAction>()
            : serviceProvider.GetService<ShareDocumentNotifyAction>() 
        :  serviceProvider.GetService<ShareFolderNotifyAction>();


        foreach (var recipientPair in recipients)
        {
            if (!await studioNotifyHelper.IsSubscribedToNotifyAsync(recipientPair.Key, serviceProvider.GetService<RoomsActivityNotifyAction>()))
            {
                continue;
            }

            var u = await userManager.GetUsersAsync(recipientPair.Key);
            CultureInfo userCulture;

            if (!string.IsNullOrEmpty(culture))
            {
                userCulture = CultureInfo.GetCultureInfo(culture);
            }
            else
            {
                userCulture = string.IsNullOrEmpty(u.CultureName)
                    ? tenantManager.GetCurrentTenant().GetCulture()
                    : CultureInfo.GetCultureInfo(u.CultureName);
            }


            var aceString = GetAccessString(recipientPair.Value, userCulture);
            var recipient = await recipientsProvider.GetRecipientAsync(u.Id.ToString());
            
            action.Init(fileEntry, folder, url, fileExtension, aceString, message, culture);

            await client.SendNoticeAsync(
                action,
                fileEntry.UniqID,
                recipient,
                true
                );
        }
    }

    public async Task SendEditorMentions<T>(FileEntry<T> file, string documentUrl, List<Guid> recipientIds, string message)
    {
        if (file == null || recipientIds.Count == 0)
        {
            return;
        }

        var client = notifyContext.RegisterClient(serviceProvider, notifySource);

        var recipientsProvider = notifySource.GetRecipientsProvider();

        var folderDao = daoFactory.GetFolderDao<T>();

        T roomId = default;
        string folderTitle;
        string folderUrl;

        switch (file.RootFolderType)
        {
            case FolderType.VirtualRooms:
                (roomId, folderTitle, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(file);
                if (!int.TryParse(roomId.ToString(), out var roomIdInt))
                {
                    return;
                }
                folderUrl = pathProvider.GetRoomsUrl(roomIdInt, false);
                break;
            case FolderType.USER:
                var shareFolderId = await globalFolder.GetFolderShareAsync<T>(daoFactory);
                var shareFolder = await folderDao.GetFolderAsync(shareFolderId);
                folderTitle = shareFolder.Title;
                folderUrl = pathProvider.GetFolderUrl(shareFolder);
                break;
            default:
                return;
        }

        var currentUser = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);

        foreach (var recipientId in recipientIds)
        {
            if (!await fileSecurity.CanReadAsync(file, recipientId))
            {
                continue;
            }

            var recipient = await recipientsProvider.GetRecipientAsync(recipientId.ToString());

            if (!await studioNotifyHelper.IsSubscribedToNotifyAsync(recipientId, serviceProvider.GetService<RoomsActivityNotifyAction>()))
            {
                continue;
            }

            if (file.RootFolderType is FolderType.VirtualRooms && await roomsNotificationSettingsHelper.CheckMuteForRoomAsync(roomId, recipientId))
            {
                continue;
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(message);
            var plainText = htmlDoc.DocumentNode.InnerText;

            var action = serviceProvider.GetService<EditorMentionsNotifyAction>();
            action.Init(file, plainText, currentUser, documentUrl, folderTitle, folderUrl);
            
            await client.SendNoticeAsync(action, file.UniqID, recipient);
        }
    }

    public async Task SendFormSubmittedAsync<T>(Folder<T> room, FileEntry<T> originalForm, FileEntry<T> filledForm)
    {
        if (filledForm.CreateBy.Equals(originalForm.CreateBy))
        {
            return;
        }

        var client = notifyContext.RegisterClient(serviceProvider, notifySource);

        var tenant = tenantManager.GetCurrentTenant();

        var user = await userManager.GetUsersAsync(filledForm.CreateBy);

        var userCulture = CultureInfo.GetCultureInfo(user.CultureName ?? tenant.Language);

        var userUrl = baseCommonLinkUtility.GetFullAbsolutePath(await commonLinkUtility.GetUserProfileAsync(filledForm.CreateBy));

        var manager = await userManager.GetUsersAsync(originalForm.CreateBy);

        var managerCulture = CultureInfo.GetCultureInfo(manager.CultureName ?? tenant.Language);

        var managerUrl = baseCommonLinkUtility.GetFullAbsolutePath(await commonLinkUtility.GetUserProfileAsync(originalForm.CreateBy));

        var roomUrl = pathProvider.GetRoomsUrl(room.Id.ToString(), false);

        var documentParentUrl = pathProvider.GetRoomsUrl(filledForm.ParentId.ToString(), false);

        var documentUrl = baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebPreviewUrl(fileUtility, filledForm.Title, filledForm.Id));

        var userName = filledForm.CreateBy == ConfigurationConstants.Guest.ID
            ? AuditReportResource.ResourceManager.GetString("GuestAccount", managerCulture)
            : user.DisplayUserName(displayUserSettingsHelper);

        var userButtonText = FilesPatternResource.ResourceManager.GetString("button_CheckReadyForms", userCulture);

        var managerButtonText = FilesPatternResource.ResourceManager.GetString("button_CheckReadyForms", managerCulture);
        
        var formSubmittedNotifyAction = serviceProvider.GetService<FormSubmittedNotifyAction>();
        formSubmittedNotifyAction.Init(room, originalForm, filledForm, documentUrl, documentParentUrl, roomUrl, manager, managerUrl, userCulture, userButtonText);
        
        await client.SendNoticeToAsync(
            formSubmittedNotifyAction,
            filledForm.UniqID,
            [user],
            [ConfigurationConstants.NotifyEMailSenderSysName, ConfigurationConstants.NotifyTelegramSenderSysName],
            true
            );
        
        var formReceivedNotifyAction = serviceProvider.GetService<FormReceivedNotifyAction>();
        formReceivedNotifyAction.Init(room, originalForm, filledForm, documentUrl, documentParentUrl, roomUrl,  userName, userUrl, managerCulture, managerButtonText);
        
        await client.SendNoticeToAsync(
            formReceivedNotifyAction,
            filledForm.UniqID,
            [manager],
            [ConfigurationConstants.NotifyEMailSenderSysName, ConfigurationConstants.NotifyTelegramSenderSysName],
            true
            );

        var userFormSubmittedNotifyAction = serviceProvider.GetService<FormSubmittedNotifyAction>();
        userFormSubmittedNotifyAction.Init(room, filledForm, userCulture);
        
        await client.SendNoticeAsync(
            userFormSubmittedNotifyAction,
            filledForm.UniqID,
            [new DirectRecipient(manager.Id.ToString(), manager.ToString()), new DirectRecipient(user.Id.ToString(), user.ToString())],
            ConfigurationConstants.NotifyPushSenderSysName);
    }

    public async Task SendRoomRemovedAsync<T>(Folder<T> folder, List<AceWrapper> aces, Guid userId)
    {
        if (aces.Count == 0)
        {
            return;
        }

        var client = notifyContext.RegisterClient(serviceProvider, notifySource);
        var recipientsProvider = notifySource.GetRecipientsProvider();

        var folderId = folder.Id.ToString();
        var roomUrl = pathProvider.GetRoomsUrl(folderId, false);

        foreach (var ace in aces)
        {
            var recepientId = ace.Id;

            if (ace.SubjectGroup
                || recepientId == userId
                || ace.Access != FileShare.RoomManager && !ace.Owner)
            {
                continue;
            }

            if (!await studioNotifyHelper.IsSubscribedToNotifyAsync(userId, serviceProvider.GetService<RoomsActivityNotifyAction>()))
            {
                continue;
            }

            if (await roomsNotificationSettingsHelper.CheckMuteForRoomAsync(folderId, userId))
            {
                continue;
            }

            var recipient = await recipientsProvider.GetRecipientAsync(recepientId.ToString());

            if (recipient == null)
            {
                continue;
            }
            
            var action = folder.FolderType == FolderType.AiRoom ? serviceProvider.GetService<AgentRemovedNotifyAction>()  : serviceProvider.GetService<RoomRemovedNotifyAction>();
            action.Init(folder.Title, roomUrl);
            
            await client.SendNoticeToAsync(
                action,
                folder.UniqID,
                [recipient],
                [ConfigurationConstants.NotifyEMailSenderSysName, ConfigurationConstants.NotifyTelegramSenderSysName],
                true
                );

        }
    }
    public async Task SendRoomMovedArchiveAsync<T>(FileEntry<T> room, IEnumerable<Guid> aces, Guid userId)
    {
        if (room is not { FileEntryType: FileEntryType.Folder } || aces.Count() == 0)
        {
            return;
        }
        var recipients = await GetNotifiableUsersAsync(aces, room, userId);

        if (recipients.Length == 0)
        {
            return;
        }

        var client = notifyContext.RegisterClient(serviceProvider, notifySource);
        var user = await userManager.GetUsersAsync(userId);
        
        var action = serviceProvider.GetService<RoomMovedArchiveNotifyAction>();
        action.Init(room, user);
        
        await client.SendNoticeAsync(action, room.UniqID, recipients, ConfigurationConstants.NotifyPushSenderSysName);
    }
    public async Task SendInvitedToRoom<T>(Folder<T> room, UserInfo user)
    {
        if (!await CanNotifyRoom(room, user))
        {
            return;
        }

        var client = notifyContext.RegisterClient(serviceProvider, notifySource);
        
        var action = room.FolderType == FolderType.AiRoom ? serviceProvider.GetService<InvitedToAgentNotifyAction>() : serviceProvider.GetService<InvitedToRoomNotifyAction>();
        action.Init(room);
        
        await client.SendNoticeAsync(action, room.UniqID, new DirectRecipient(user.Id.ToString(), user.ToString()), ConfigurationConstants.NotifyPushSenderSysName);
    }
    public async Task SendRoomUpdateAccessForUser<T>(Folder<T> room, UserInfo user, FileShare currentRole)
    {
        if (!await CanNotifyRoom(room, user))
        {
            return;
        }

        var client = notifyContext.RegisterClient(serviceProvider, notifySource);
        var isAgent = room.FolderType == FolderType.AiRoom;
        var accessString = FileShareExtensions.GetAccessString(currentRole, isAgent: isAgent);

        var action = isAgent ? serviceProvider.GetService<AgentUpdateAccessForUserNotifyAction>() : serviceProvider.GetService<RoomUpdateAccessForUserNotifyAction>();
        action.Init(room, accessString);
        
        await client.SendNoticeAsync(
            action,
            room.UniqID,
            new DirectRecipient(user.Id.ToString(), user.ToString()),
            ConfigurationConstants.NotifyPushSenderSysName
            );
    }
    public async Task SendDocumentCreatedInRoom<T>(Folder<T> room, IEnumerable<Guid> aces, FileEntry<T> file, Guid userId)
    {
        var recipients = await GetNotifiableUsersAsync(aces, room, userId);

        if (recipients.Length == 0)
        {
            return;
        }
        
        var client = notifyContext.RegisterClient(serviceProvider, notifySource);
        var action = serviceProvider.GetService<DocumentCreatedInRoomNotifyAction>();
        action.Init(room, file);
        
        await client.SendNoticeAsync(
            action,
                file.UniqID,
                recipients,
                ConfigurationConstants.NotifyPushSenderSysName
            );

    }
    public async Task SendDocumentUploadedToRoom<T>(IEnumerable<Guid> aces, File<T> file, Folder<T> room, Guid userId)
    {
        var recipients = await GetNotifiableUsersAsync(aces, room, userId);

        if (recipients.Length == 0)
        {
            return;
        }

        var client = notifyContext.RegisterClient(serviceProvider, notifySource);
        var action = serviceProvider.GetService<DocumentUploadedToRoomNotifyAction>();
        action.Init(room, file);
        
        await client.SendNoticeAsync(action, file.UniqID, recipients, ConfigurationConstants.NotifyPushSenderSysName);
    }
    
    public async Task SendDocumentsUploadedToRoom<T>(IEnumerable<Guid> aces, int count, Folder<T> room, Guid userId)
    {
        var recipients = await GetNotifiableUsersAsync(aces, room, userId);

        if (recipients.Length == 0)
        {
            return;
        }

        var client = notifyContext.RegisterClient(serviceProvider, notifySource);
        var action = serviceProvider.GetService<DocumentsUploadedToRoomNotifyAction>();
        action.Init(room, count);
        
        await client.SendNoticeAsync(action, room.UniqID, recipients, ConfigurationConstants.NotifyPushSenderSysName);
    }
    
    public async Task SendFolderCreatedInRoom<T>(Folder<T> room, IEnumerable<Guid> aces, Folder<T> folder, Guid userId)
    {
        var recipients = await GetNotifiableUsersAsync(aces, room, userId);

        if (recipients.Length == 0)
        {
            return;
        }

        var client = notifyContext.RegisterClient(serviceProvider, notifySource);
        var action = serviceProvider.GetService<FolderCreatedInRoomNotifyAction>();
        action.Init(room, folder);
        
        await client.SendNoticeAsync(action, folder.UniqID, recipients, ConfigurationConstants.NotifyPushSenderSysName);
    }

    public async Task<IRecipient[]> GetNotifiableUsersAsync<T>(IEnumerable<Guid> aces, FileEntry<T> room, Guid userId)
    {
        var notifiableUsers = new List<IRecipient>();

        foreach (var aceId in aces)
        {
            if (aceId == userId)
            {
                continue;
            }
            var user = await userManager.GetUsersAsync(aceId);

            if (user == null)
            {
                continue;
            }
            if (!await CanNotifyRoom(room, user))
            {
                continue;
            }
            notifiableUsers.Add(new DirectRecipient(user.Id.ToString(), user.ToString()));
        }
        return notifiableUsers.ToArray();
    }
    public async Task SendFormFillingEvent<T>(FileEntry<T> room, File<T> file, List<Guid> aces, Type actionType, Guid? userId = null)
    {
        if (aces.Count == 0)
        {
            return;
        }

        var client = notifyContext.RegisterClient(serviceProvider, notifySource);
        var recipientsProvider = notifySource.GetRecipientsProvider();

        var folderId = room.Id.ToString();
        var roomUrl = pathProvider.GetRoomsUrl(folderId, false);

        var userUrl = "";
        var userName = "";
        var user = await userManager.GetUsersAsync(userId ?? Guid.Empty);
        if (user != null)
        {
            userUrl = baseCommonLinkUtility.GetFullAbsolutePath(await commonLinkUtility.GetUserProfileAsync(user.Id));
            userName = user.DisplayUserName(displayUserSettingsHelper);
        }

        foreach (var ace in aces)
        {
            var recipient = await recipientsProvider.GetRecipientAsync(ace.ToString());

            var action = (FormStartedFillingNotifyAction)serviceProvider.GetService(actionType);
            action?.Init(room, file, userName, userUrl, roomUrl);

            await client.SendNoticeToAsync(
                action,
                room.UniqID,
                [recipient],
                [ConfigurationConstants.NotifyEMailSenderSysName, ConfigurationConstants.NotifyTelegramSenderSysName],
                true);
        }
    }
    private async Task<bool> CanNotifyRoom<T>(FileEntry<T> room, UserInfo user)
    {
        if (room is not { FileEntryType: FileEntryType.Folder })
        {
            return false;
        }

        if (!await studioNotifyHelper.IsSubscribedToNotifyAsync(user, serviceProvider.GetService<RoomsActivityNotifyAction>()))
        {
            return false;
        }
        if (await roomsNotificationSettingsHelper.CheckMuteForRoomAsync(room.Id, user.Id))
        {
            return false;
        }

        return true;
    }
    private static string GetAccessString(FileShare fileShare, CultureInfo cultureInfo)
    {
        return fileShare switch
        {
            FileShare.Read => FilesCommonResource.ResourceManager.GetString("AceStatusEnum_Read", cultureInfo),
            FileShare.ReadWrite => FilesCommonResource.ResourceManager.GetString("AceStatusEnum_ReadWrite", cultureInfo),
            FileShare.Editing => FilesCommonResource.ResourceManager.GetString("AceStatusEnum_Editing", cultureInfo),
            FileShare.CustomFilter => FilesCommonResource.ResourceManager.GetString("AceStatusEnum_CustomFilter", cultureInfo),
            FileShare.Review => FilesCommonResource.ResourceManager.GetString("AceStatusEnum_Review", cultureInfo),
            FileShare.FillForms => FilesCommonResource.ResourceManager.GetString("AceStatusEnum_FillForms", cultureInfo),
            FileShare.Comment => FilesCommonResource.ResourceManager.GetString("AceStatusEnum_Comment", cultureInfo),
            _ => string.Empty
        };
    }
}