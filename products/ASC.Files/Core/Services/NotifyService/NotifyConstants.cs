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

namespace ASC.Files.Core.Services.NotifyService;

[Scope]
public sealed class DocuSignCompleteNotifyAction(TenantManager tenantManager, BaseCommonLinkUtility baseCommonLinkUtility, FilesLinkUtility filesLinkUtility, FileUtility fileUtility) : NotifyAction(tenantManager)
{
    public override string ID => "DocuSignComplete";

    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_DocuSignComplete, () => FilesPatternResource.pattern_DocuSignComplete)
    ];

    public void Init<T>(File<T> file, string sourceTitle)
    {
        Tags = [ 
            new TagValue(NotifyConstants.TagDocumentUrl, baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebPreviewUrl(fileUtility, file.Title, file.Id))),
            new TagValue(NotifyConstants.TagDocumentTitle, file.Title),
            new TagValue(NotifyConstants.TagMessage, sourceTitle)
        ];
    }
}

[Scope]
public sealed class DocuSignStatusNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "DocuSignStatus";

    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_DocuSignStatus, () => FilesPatternResource.pattern_DocuSignStatus)
    ];

    public void Init(string subject, string status)
    {
        Tags =
        [
            new TagValue(NotifyConstants.TagDocumentTitle, subject),
            new TagValue(NotifyConstants.TagMessage, status)
        ];
    }
}

[Scope]
public sealed class MailMergeEndNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "MailMergeEnd";

    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_MailMergeEnd, () => FilesPatternResource.pattern_MailMergeEnd)
    ];
    
    public void Init(int countMails, int countError)
    {
        Tags =
        [
            new TagValue(NotifyConstants.TagMailsCount, countMails),
            new TagValue(NotifyConstants.TagMessage, countError > 0 ? string.Format(FilesCommonResource.ErrorMessage_MailMergeCount, countError) : string.Empty)
        ];
    }
    
}

[Scope]
public class ShareDocumentNotifyAction(TenantManager tenantManager, BaseCommonLinkUtility baseCommonLinkUtility, StudioNotifyHelper studioNotifyHelper) : NotifyAction(tenantManager)
{
    public override string ID => "ShareDocument";

    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_ShareDocument, () => FilesPatternResource.pattern_ShareDocument),
        new TelegramPattern(() => FilesPatternResource.pattern_ShareDocument),
        new PushPattern(() => FilesPatternResource.subject_ShareDocument_push)
    ];

    public void Init<T>(FileEntry<T> fileEntry, Folder<T> folder, string url, string fileExtension, string aceString, string message, string culture)
    {
        List<ITagValue> tags =
        [
            new TagValue(NotifyConstants.TagDocumentTitle, fileEntry.Title),
            new TagValue(NotifyConstants.TagDocumentUrl, baseCommonLinkUtility.GetFullAbsolutePath(url)),
            new TagValue(NotifyConstants.TagDocumentExtension, fileExtension),
            new TagValue(NotifyConstants.TagAccessRights, aceString),
            new TagValue(NotifyConstants.TagMessage, message == null ? string.Empty : message.HtmlEncode()),
            new TagValue(NotifyConstants.TagFolderID, folder.Id),
            new TagValue(NotifyConstants.TagFolderParentId, folder.RootId),
            new TagValue(NotifyConstants.TagFolderRootFolderType, folder.RootFolderType),
            TagValues.Image(studioNotifyHelper, 0, "privacy.png"),
            new AdditionalSenderTag("push.sender")
        ];
        
        if (!string.IsNullOrEmpty(culture))
        {
            tags.Add(new TagValue(CommonTags.Culture, culture));
        }
        
        Tags = tags;
    }
}

[Scope]
public sealed class ShareEncryptedDocumentNotifyAction(TenantManager tenantManager, BaseCommonLinkUtility baseCommonLinkUtility, StudioNotifyHelper studioNotifyHelper) : ShareDocumentNotifyAction(tenantManager, baseCommonLinkUtility, studioNotifyHelper)
{
    public override string ID => "ShareEncryptedDocument";

    public override List<Pattern> Patterns => [];
}

[Scope]
public sealed class ShareFolderNotifyAction (TenantManager tenantManager, BaseCommonLinkUtility baseCommonLinkUtility, StudioNotifyHelper studioNotifyHelper) : ShareDocumentNotifyAction(tenantManager, baseCommonLinkUtility, studioNotifyHelper)
{
    public override string ID => "ShareFolder";

    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_ShareFolder, () => FilesPatternResource.pattern_ShareFolder),
        new TelegramPattern(() => FilesPatternResource.pattern_ShareFolder),
        new PushPattern(() => FilesPatternResource.subject_ShareFolder_push)
    ];
}

[Scope]
public sealed class EditorMentionsNotifyAction(TenantManager tenantManager, BaseCommonLinkUtility baseCommonLinkUtility, DisplayUserSettingsHelper displayUserSettingsHelper) : NotifyAction(tenantManager)
{
    public override string ID => "EditorMentions";

    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_EditorMentions, () => FilesPatternResource.pattern_EditorMentions),
        new TelegramPattern(() => FilesPatternResource.pattern_EditorMentions),
        new PushPattern(() => FilesPatternResource.pattern_EditorMentions_push)
    ];

    public void Init(FileEntry file, string plainText, UserInfo currentUser, string documentUrl, string folderTitle, string folderUrl)
    {
        Tags =
        [
            new TagValue(NotifyConstants.TagDocumentTitle, file.Title),
            new TagValue(NotifyConstants.TagDocumentUrl, baseCommonLinkUtility.GetFullAbsolutePath(documentUrl)),
            new TagValue(NotifyConstants.TagMessage, plainText),
            new TagValue(CommonTags.ToUserName, currentUser.DisplayUserName(displayUserSettingsHelper)),
            new TagValue(NotifyConstants.RoomTitle, folderTitle),
            new TagValue(NotifyConstants.RoomUrl, folderUrl),
            new AdditionalSenderTag("push.sender")
        ];
    }
}

[Scope]
public class RoomRemovedNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "RoomRemoved";

    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_RoomRemoved, () => FilesPatternResource.pattern_RoomRemoved),
        new TelegramPattern(() => FilesPatternResource.pattern_RoomRemoved)
    ];
    
    public void Init(string roomTitle, string roomUrl)
    {
        Tags =
        [
            new TagValue(NotifyConstants.RoomTitle, roomTitle),
            new TagValue(NotifyConstants.RoomUrl, roomUrl)
        ];
    }
}

[Scope]
public sealed class AgentRemovedNotifyAction(TenantManager tenantManager) : RoomRemovedNotifyAction(tenantManager)
{
    public override string ID => "AgentRemoved";

    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_AgentRemoved, () => FilesPatternResource.pattern_AgentRemoved),
        new TelegramPattern(() => FilesPatternResource.pattern_AgentRemoved)
    ];
}

[Scope]
public sealed class FormSubmittedNotifyAction(TenantManager tenantManager, DisplayUserSettingsHelper displayUserSettingsHelper) : NotifyAction(tenantManager)
{
    public override string ID => "FormSubmitted";

    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_FormSubmitted, () => FilesPatternResource.pattern_FormSubmitted),
        new TelegramPattern(() => FilesPatternResource.pattern_FormSubmitted),
        new PushPattern(() => FilesPatternResource.pattern_FormSubmitted_push)
    ];

    public void Init<T>(Folder<T> room, FileEntry<T> originalForm, FileEntry<T> filledForm, string documentUrl, string documentParentUrl, string roomUrl, UserInfo manager, string managerUrl, CultureInfo userCulture, string userButtonText)
    {
        Tags =
        [
            new TagValue(NotifyConstants.TagMessage, originalForm.Title),
            new TagValue(NotifyConstants.TagDocumentTitle, filledForm.Title),
            new TagValue(NotifyConstants.TagDocumentUrl, documentUrl),
            new TagValue(NotifyConstants.RoomTitle, room.Title),
            new TagValue(NotifyConstants.RoomUrl, roomUrl),
            new TagValue(CommonTags.ToUserName, manager.DisplayUserName(displayUserSettingsHelper)),
            new TagValue(CommonTags.ToUserLink, managerUrl),
            new TagValue(CommonTags.Culture, userCulture.Name),
            TagValues.OrangeButton(userButtonText, documentParentUrl)
        ];
    }

    public void Init<T>(Folder<T> room, FileEntry<T> filledForm, CultureInfo userCulture)
    {
        Tags =
        [
            new TagValue(NotifyConstants.TagDocumentTitle, filledForm.Title),
            new TagValue(NotifyConstants.RoomTitle, room.Title),
            new TagValue(NotifyConstants.TagFolderID, room.Id),
            new TagValue(NotifyConstants.TagFolderParentId, room.ParentId),
            new TagValue(NotifyConstants.TagFolderRootFolderType, room.RootFolderType),
            new TagValue(CommonTags.Culture, userCulture.Name)
        ];
    }
}

[Scope]
public sealed class FormReceivedNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "FormReceived";

    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_FormReceived, () => FilesPatternResource.pattern_FormReceived),
        new TelegramPattern(() => FilesPatternResource.pattern_FormReceived)
    ];

    public void Init<T>(Folder<T> room, FileEntry<T> originalForm, FileEntry<T> filledForm, string documentUrl, string documentParentUrl, string roomUrl, string userName, string userUrl, CultureInfo managerCulture, string managerButtonText)
    {
        Tags =
        [
            new TagValue(NotifyConstants.TagMessage, originalForm.Title),
            new TagValue(NotifyConstants.TagDocumentTitle, filledForm.Title),
            new TagValue(NotifyConstants.TagDocumentUrl, documentUrl),
            new TagValue(NotifyConstants.RoomTitle, room.Title),
            new TagValue(NotifyConstants.RoomUrl, roomUrl),
            new TagValue(CommonTags.FromUserName, userName),
            new TagValue(CommonTags.FromUserLink, userUrl),
            new TagValue(CommonTags.Culture, managerCulture.Name),
            TagValues.OrangeButton(managerButtonText, documentParentUrl)
        ];
    }
}

[Scope]
public sealed class RoomMovedArchiveNotifyAction(TenantManager tenantManager, DisplayUserSettingsHelper displayUserSettingsHelper) : NotifyAction(tenantManager)
{
    public override string ID => "RoomMovedArchive";

    public override List<Pattern> Patterns =>
    [
        new PushPattern(() => FilesPatternResource.pattern_RoomMovedArchive_push)
    ];
    
    public void Init<T>(FileEntry<T> room, UserInfo user)
    {
        Tags =
        [
            new TagValue(NotifyConstants.RoomTitle, room.Title),
            new TagValue(CommonTags.FromUserName, user.DisplayUserName(displayUserSettingsHelper)),
            new TagValue(NotifyConstants.TagFolderID, room.Id),
            new TagValue(NotifyConstants.TagFolderParentId, room.ParentId),
            new TagValue(NotifyConstants.TagFolderRootFolderType, room.RootFolderType)
        ];
    }
}

[Scope]
public class InvitedToRoomNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "InvitedToRoom";

    public override List<Pattern> Patterns =>
    [
        new PushPattern(() => FilesPatternResource.pattern_InvitedToRoom_push)
    ];
    
    public void Init<T>(FileEntry<T> room)
    {
        Tags =
        [
            new TagValue(NotifyConstants.TagFolderID, room.Id),
            new TagValue(NotifyConstants.TagFolderParentId, room.ParentId),
            new TagValue(NotifyConstants.TagFolderRootFolderType, room.RootFolderType),
            new TagValue(NotifyConstants.RoomTitle, room.Title)
        ];
    }
}

[Scope]
public sealed class InvitedToAgentNotifyAction(TenantManager tenantManager) : InvitedToRoomNotifyAction(tenantManager)
{
    public override string ID => "InvitedToAgent";

    public override List<Pattern> Patterns =>
    [
        new PushPattern(() => FilesPatternResource.pattern_InvitedToAgent_push)
    ];
}

[Scope]
public class RoomUpdateAccessForUserNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "RoomUpdateAccessForUser";

    public override List<Pattern> Patterns =>
    [
        new PushPattern(() => FilesPatternResource.pattern_RoomUpdateAccessForUser_push)
    ];

    public void Init<T>(FileEntry<T> room, string accessString)
    {
        Tags =
        [
            new TagValue(NotifyConstants.RoomTitle, room.Title),
            new TagValue(NotifyConstants.TagFolderID, room.Id),
            new TagValue(NotifyConstants.TagFolderParentId, room.ParentId),
            new TagValue(NotifyConstants.TagFolderRootFolderType, room.RootFolderType),
            new TagValue(CommonTags.RoomRole, accessString)
        ];
    }
}

[Scope]
public sealed class AgentUpdateAccessForUserNotifyAction(TenantManager tenantManager) : RoomUpdateAccessForUserNotifyAction(tenantManager)
{
    public override string ID => "AgentUpdateAccessForUser";

    public override List<Pattern> Patterns =>
    [
        new PushPattern(() => FilesPatternResource.pattern_AgentUpdateAccessForUser_push)
    ];
}

[Scope]
public sealed class DocumentCreatedInRoomNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "DocumentCreatedInRoom";

    public override List<Pattern> Patterns =>
    [
        new PushPattern(() => FilesPatternResource.pattern_DocumentCreatedInRoom_push)
    ];

    public void Init<T>(Folder<T> room, FileEntry<T> file)
    {
        Tags =
        [
            new TagValue(NotifyConstants.RoomTitle, room.Title),
            new TagValue(NotifyConstants.TagDocumentTitle, file.Title),
            new TagValue(NotifyConstants.TagDocumentExtension, Path.GetExtension(file.Title)),
            new TagValue(NotifyConstants.TagFolderID, room.Id),
            new TagValue(NotifyConstants.TagFolderParentId, room.ParentId),
            new TagValue(NotifyConstants.TagFolderRootFolderType, room.RootFolderType)
        ];
    }
}

[Scope]
public sealed class DocumentUploadedToRoomNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "DocumentUploadedTo";

    public override List<Pattern> Patterns =>
    [
        new PushPattern(() => FilesPatternResource.pattern_DocumentUploadedTo_push)
    ];

    public void Init<T>(Folder<T> room, FileEntry<T> file)
    {
        Tags =
        [
            new TagValue(NotifyConstants.TagDocumentTitle, file.Title),
            new TagValue(NotifyConstants.TagDocumentExtension, Path.GetExtension(file.Title)),
            new TagValue(NotifyConstants.TagFolderID, room.Id),
            new TagValue(NotifyConstants.RoomTitle, room.Title),
            new TagValue(NotifyConstants.TagFolderParentId, room.ParentId),
            new TagValue(NotifyConstants.TagFolderRootFolderType, room.RootFolderType)
        ];
    }
}

[Scope]
public sealed class DocumentsUploadedToRoomNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "DocumentsUploadedTo";

    public override List<Pattern> Patterns =>
    [
        new PushPattern(() => FilesPatternResource.pattern_DocumentsUploadedTo_push)
    ];

    public void Init<T>(Folder<T> room, int count)
    {
        Tags =
        [
            new TagValue(NotifyConstants.TagFolderID, room.Id),
            new TagValue(NotifyConstants.TagFolderParentId, room.ParentId),
            new TagValue(NotifyConstants.TagFolderRootFolderType, room.RootFolderType),
            new TagValue(NotifyConstants.RoomTitle, room.Title),
            new TagValue(CommonTags.Count, count)
        ];
    }
}

[Scope]
public sealed class FolderCreatedInRoomNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "FolderCreatedInRoom";

    public override List<Pattern> Patterns =>
    [
        new PushPattern(() => FilesPatternResource.pattern_FolderCreatedInRoom_push)
    ];

    public void Init<T>(Folder<T> room, Folder<T> folder)
    {
        Tags =
        [
            new TagValue(NotifyConstants.RoomTitle, room.Title),
            new TagValue(NotifyConstants.FolderTitle, folder.Title),
            new TagValue(NotifyConstants.TagFolderID, folder.Id),
            new TagValue(NotifyConstants.TagFolderParentId, folder.ParentId),
            new TagValue(NotifyConstants.TagFolderRootFolderType, folder.RootFolderType)
        ];
    }
}

[Scope]
public class FormStartedFillingNotifyAction(TenantManager tenantManager, BaseCommonLinkUtility baseCommonLinkUtility, FilesLinkUtility filesLinkUtility, FileUtility fileUtility) : NotifyAction(tenantManager)
{
    public override string ID => "FormStartedFilling";

    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_FormStartedFilling, () => FilesPatternResource.pattern_FormStartedFilling),
        new TelegramPattern(() => FilesPatternResource.pattern_FormStartedFilling)
    ];

    public void Init<T>(FileEntry<T> room, File<T> file, string userName, string userUrl, string roomUrl)
    {
        Tags =
        [
            new TagValue(NotifyConstants.TagDocumentUrl, baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebPreviewUrl(fileUtility, file.Title, file.Id))),
            new TagValue(NotifyConstants.TagDocumentTitle, file.Title),
            new TagValue(NotifyConstants.RoomTitle, room.Title),
            new TagValue(NotifyConstants.RoomUrl, roomUrl),
            new TagValue(CommonTags.FromUserName, userName),
            new TagValue(CommonTags.FromUserLink, userUrl)
        ];
    }
}

[Scope]
public sealed class YourTurnFormFillingNotifyAction(TenantManager tenantManager, BaseCommonLinkUtility baseCommonLinkUtility, FilesLinkUtility filesLinkUtility, FileUtility fileUtility) : FormStartedFillingNotifyAction(tenantManager, baseCommonLinkUtility, filesLinkUtility, fileUtility)
{
    public override string ID => "YourTurnFormFilling";

    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_YourTurnFormFilling, () => FilesPatternResource.pattern_YourTurnFormFilling),
        new TelegramPattern(() => FilesPatternResource.pattern_YourTurnFormFilling)
    ];
}

[Scope]
public sealed class FormWasCompletelyFilledNotifyAction(TenantManager tenantManager, BaseCommonLinkUtility baseCommonLinkUtility, FilesLinkUtility filesLinkUtility, FileUtility fileUtility) : FormStartedFillingNotifyAction(tenantManager, baseCommonLinkUtility, filesLinkUtility, fileUtility)
{
    public override string ID => "FormWasCompletelyFilled";

    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_FormWasCompletelyFilled, () => FilesPatternResource.pattern_FormWasCompletelyFilled),
        new TelegramPattern(() => FilesPatternResource.pattern_FormWasCompletelyFilled)
    ];
}

[Scope]
public sealed class StoppedFormFillingNotifyAction(TenantManager tenantManager, BaseCommonLinkUtility baseCommonLinkUtility, FilesLinkUtility filesLinkUtility, FileUtility fileUtility) : FormStartedFillingNotifyAction(tenantManager, baseCommonLinkUtility, filesLinkUtility, fileUtility)
{
    public override string ID => "StoppedFormFilling";
    
    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_StoppedFormFilling, () => FilesPatternResource.pattern_StoppedFormFilling),
        new TelegramPattern(() => FilesPatternResource.pattern_StoppedFormFilling)
    ];
}

public static class NotifyConstants
{
    public static readonly string TagFolderID = "FolderID";
    public static readonly string TagFolderParentId = "FolderParentId";
    public static readonly string TagFolderRootFolderType = "FolderRootFolderType";
    public static readonly string TagDocumentTitle = "DocumentTitle";
    public static readonly string TagDocumentUrl = "DocumentURL";
    public static readonly string TagDocumentExtension = "DocumentExtension";
    public static readonly string TagAccessRights = "AccessRights";
    public static readonly string TagMessage = "Message";
    public static readonly string TagMailsCount = "MailsCount";
    public static readonly string RoomTitle = "RoomTitle";
    public static readonly string RoomUrl = "RoomURL";
    public static readonly string FolderTitle = "FolderTitle";
}