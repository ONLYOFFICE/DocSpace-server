// (c) Copyright Ascensio System SIA 2010-2023
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

using Actions = ASC.Web.Studio.Core.Notify.Actions;
using ConfigurationConstants = ASC.Core.Configuration.Constants;

namespace ASC.Files.Core.Services.NotifyService;

[Scope]
public class NotifyClient(WorkContext notifyContext,
    NotifySource notifySource,
    SecurityContext securityContext,
    FilesLinkUtility filesLinkUtility,
    FileUtility fileUtility,
    BaseCommonLinkUtility baseCommonLinkUtility,
    IDaoFactory daoFactory,
    PathProvider pathProvider,
    UserManager userManager,
    TenantManager tenantManager,
    StudioNotifyHelper studioNotifyHelper,
    RoomsNotificationSettingsHelper roomsNotificationSettingsHelper,
    FileSecurity fileSecurity,
    IServiceProvider serviceProvider)
{
    public async Task SendDocuSignCompleteAsync<T>(File<T> file, string sourceTitle)
    {
        var client = notifyContext.RegisterClient(serviceProvider, notifySource);
        var recipient = await notifySource.GetRecipientsProvider().GetRecipientAsync(securityContext.CurrentAccount.ID.ToString());

        await client.SendNoticeAsync(
            NotifyConstants.EventDocuSignComplete,
            file.UniqID,
            recipient,
            true,
            new TagValue(NotifyConstants.TagDocumentUrl, baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebPreviewUrl(fileUtility, file.Title, file.Id))),
            new TagValue(NotifyConstants.TagDocumentTitle, file.Title),
            new TagValue(NotifyConstants.TagMessage, sourceTitle)
            );
    }

    public async Task SendDocuSignStatusAsync(string subject, string status)
    {
        var client = notifyContext.RegisterClient(serviceProvider, notifySource);

        var recipient = await notifySource.GetRecipientsProvider().GetRecipientAsync(securityContext.CurrentAccount.ID.ToString());

        await client.SendNoticeAsync(
            NotifyConstants.EventDocuSignStatus,
            null,
            recipient,
            true,
            new TagValue(NotifyConstants.TagDocumentTitle, subject),
            new TagValue(NotifyConstants.TagMessage, status)
            );
    }

    public async Task SendMailMergeEndAsync(Guid userId, int countMails, int countError)
    {
        var client = notifyContext.RegisterClient(serviceProvider, notifySource);

        var recipient = await notifySource.GetRecipientsProvider().GetRecipientAsync(userId.ToString());

        await client.SendNoticeAsync(
            NotifyConstants.EventMailMergeEnd,
            null,
            recipient,
            true,
            new TagValue(NotifyConstants.TagMailsCount, countMails),
            new TagValue(NotifyConstants.TagMessage, countError > 0 ? string.Format(FilesCommonResource.ErrorMassage_MailMergeCount, countError) : string.Empty)
            );
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
                      : await pathProvider.GetFolderUrlAsync((Folder<T>)fileEntry);

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
            ? NotifyConstants.EventShareEncryptedDocument
            : NotifyConstants.EventShareDocument
        : NotifyConstants.EventShareFolder;


        foreach (var recipientPair in recipients)
        {
            var u = await userManager.GetUsersAsync(recipientPair.Key);
            CultureInfo userCulture;

            if (!string.IsNullOrEmpty(culture))
            {
                userCulture = CultureInfo.GetCultureInfo(culture);
            }
            else
            {
                userCulture = string.IsNullOrEmpty(u.CultureName)
                    ? (await tenantManager.GetCurrentTenantAsync()).GetCulture()
                    : CultureInfo.GetCultureInfo(u.CultureName);
            }
            

            var aceString = GetAccessString(recipientPair.Value, userCulture);
            var recipient = await recipientsProvider.GetRecipientAsync(u.Id.ToString());

            var tags = new List<ITagValue>
            {
                new TagValue(NotifyConstants.TagDocumentTitle, fileEntry.Title),
                new TagValue(NotifyConstants.TagDocumentUrl, baseCommonLinkUtility.GetFullAbsolutePath(url)),
                new TagValue(NotifyConstants.TagDocumentExtension, fileExtension),
                new TagValue(NotifyConstants.TagAccessRights, aceString),
                new TagValue(NotifyConstants.TagMessage, message.HtmlEncode()),
                new TagValue(NotifyConstants.TagFolderID, folder.Id),
                new TagValue(NotifyConstants.TagFolderParentId, folder.RootId),
                new TagValue(NotifyConstants.TagFolderRootFolderType, folder.RootFolderType),
                TagValues.Image(studioNotifyHelper, 0, "privacy.png"),
                new AdditionalSenderTag("push.sender")
            };
            
            if (!string.IsNullOrEmpty(culture))
            {
                tags.Add(new TagValue(CommonTags.Culture, culture));
            }
            
            await client.SendNoticeAsync(
                action,
                fileEntry.UniqID,
                recipient,
                true,
                tags.ToArray()
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

        var folderDao = daoFactory.GetFolderDao<int>();

        var (roomId, roomTitle) = await folderDao.GetParentRoomInfoFromFileEntryAsync(file);
        var roomUrl = pathProvider.GetRoomsUrl(roomId);

        var room = await folderDao.GetFolderAsync(roomId);

        foreach (var recipientId in recipientIds)
        {
            if (!await fileSecurity.CanReadAsync(room, recipientId))
            {
                continue;
            }

            var u = await userManager.GetUsersAsync(recipientId);

            if (!await studioNotifyHelper.IsSubscribedToNotifyAsync(u, Actions.RoomsActivity))
            {
                continue;
            }

            var recipient = await recipientsProvider.GetRecipientAsync(recipientId.ToString());

            var disabledRooms = await roomsNotificationSettingsHelper.GetDisabledRoomsForUserAsync(recipientId);

            if (disabledRooms.Contains(roomId))
            {
                continue;
            }

            await client.SendNoticeAsync(
                NotifyConstants.EventEditorMentions,
                file.UniqID,
                recipient,
                new TagValue(NotifyConstants.TagDocumentTitle, file.Title),
                new TagValue(NotifyConstants.TagDocumentUrl, baseCommonLinkUtility.GetFullAbsolutePath(documentUrl)),
                new TagValue(NotifyConstants.TagMessage, message.HtmlEncode()),
                new TagValue(NotifyConstants.RoomTitle, roomTitle),
                new TagValue(NotifyConstants.RoomUrl, roomUrl),
                new AdditionalSenderTag("push.sender")
                );
        }
    }

    public async Task SendRoomRemovedAsync<T>(FileEntry<T> folder, List<AceWrapper> aces, Guid userId)
    {
        if (folder == null || folder.FileEntryType != FileEntryType.Folder || aces.Count == 0)
        {
            return;
        }

        var client = notifyContext.RegisterClient(serviceProvider, notifySource);
        var recipientsProvider = notifySource.GetRecipientsProvider();

        var folderId = folder.Id.ToString();
        var roomUrl = pathProvider.GetRoomsUrl(folderId);

        foreach (var ace in aces)
        {
            var recepientId = ace.Id;

            if (ace.SubjectGroup
                || recepientId == userId
                || ace.Access != FileShare.RoomAdmin && ace.Owner != true)
            {
                continue;
            }

            if (!await studioNotifyHelper.IsSubscribedToNotifyAsync(userId, Actions.RoomsActivity))
            {
                continue;
            }

            var disabledRooms = (await roomsNotificationSettingsHelper.GetDisabledRoomsForUserAsync(userId)).Select(d => d.ToString());

            if (disabledRooms.Contains(folderId))
            {
                continue;
            }

            var recipient = await recipientsProvider.GetRecipientAsync(recepientId.ToString());

            await client.SendNoticeAsync(
                NotifyConstants.EventRoomRemoved,
                folder.UniqID,
                recipient,
                ConfigurationConstants.NotifyEMailSenderSysName,
                new TagValue(NotifyConstants.RoomTitle, folder.Title),
                new TagValue(NotifyConstants.RoomUrl, roomUrl)
                );

        }
    }

    private static string GetAccessString(FileShare fileShare, CultureInfo cultureInfo)
    {
        return fileShare switch
        {
            FileShare.Read => FilesCommonResource.ResourceManager.GetString("AceStatusEnum_Read", cultureInfo),
            FileShare.ReadWrite => FilesCommonResource.ResourceManager.GetString("AceStatusEnum_ReadWrite", cultureInfo),
            FileShare.CustomFilter => FilesCommonResource.ResourceManager.GetString("AceStatusEnum_CustomFilter", cultureInfo),
            FileShare.Review => FilesCommonResource.ResourceManager.GetString("AceStatusEnum_Review", cultureInfo),
            FileShare.FillForms => FilesCommonResource.ResourceManager.GetString("AceStatusEnum_FillForms", cultureInfo),
            FileShare.Comment => FilesCommonResource.ResourceManager.GetString("AceStatusEnum_Comment", cultureInfo),
            _ => string.Empty,
        };
    }
}
