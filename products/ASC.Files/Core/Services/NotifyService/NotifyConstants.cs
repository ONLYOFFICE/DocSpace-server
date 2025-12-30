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

namespace ASC.Files.Core.Services.NotifyService;

[Scope]
public sealed class DocuSignCompleteNotifyAction(BaseCommonLinkUtility baseCommonLinkUtility, FilesLinkUtility filesLinkUtility, FileUtility fileUtility) : INotifyAction
{
    public string ID => "DocuSignComplete";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
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
public sealed class DocuSignStatusNotifyAction : INotifyAction
{
    public string ID => "DocuSignStatus";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
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
public sealed class MailMergeEndNotifyAction : INotifyAction
{
    public string ID => "MailMergeEnd";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
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
public class ShareDocumentNotifyAction(BaseCommonLinkUtility baseCommonLinkUtility, StudioNotifyHelper studioNotifyHelper) : INotifyAction
{
    public virtual string ID => "ShareDocument";

    public List<ITagValue> Tags { get; set; }

    public virtual List<Pattern> Patterns =>
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
public sealed class ShareEncryptedDocumentNotifyAction(BaseCommonLinkUtility baseCommonLinkUtility, StudioNotifyHelper studioNotifyHelper) : ShareDocumentNotifyAction(baseCommonLinkUtility, studioNotifyHelper)
{
    public override string ID => "ShareEncryptedDocument";

    public override List<Pattern> Patterns => [];
}

[Scope]
public sealed class ShareFolderNotifyAction (BaseCommonLinkUtility baseCommonLinkUtility, StudioNotifyHelper studioNotifyHelper) : ShareDocumentNotifyAction(baseCommonLinkUtility, studioNotifyHelper)
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
public sealed class EditorMentionsNotifyAction(BaseCommonLinkUtility baseCommonLinkUtility, DisplayUserSettingsHelper displayUserSettingsHelper) : INotifyAction
{
    public string ID => "EditorMentions";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
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
public class RoomRemovedNotifyAction : INotifyAction
{
    public virtual string ID => "RoomRemoved";

    public List<ITagValue> Tags { get; set; }

    public virtual List<Pattern> Patterns =>
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
public sealed class AgentRemovedNotifyAction : RoomRemovedNotifyAction
{
    public override string ID => "AgentRemoved";

    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_AgentRemoved, () => FilesPatternResource.pattern_AgentRemoved),
        new TelegramPattern(() => FilesPatternResource.pattern_AgentRemoved)
    ];
}

[Scope]
public sealed class FormSubmittedNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper) : INotifyAction
{
    public string ID => "FormSubmitted";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
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
public sealed class FormReceivedNotifyAction : INotifyAction
{
    public string ID => "FormReceived";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
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
public sealed class RoomMovedArchiveNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper) : INotifyAction
{
    public string ID => "RoomMovedArchive";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
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
public class InvitedToRoomNotifyAction : INotifyAction
{
    public virtual string ID => "InvitedToRoom";

    public List<ITagValue> Tags { get; set; }

    public virtual List<Pattern> Patterns =>
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
public sealed class InvitedToAgentNotifyAction : InvitedToRoomNotifyAction
{
    public override string ID => "InvitedToAgent";

    public override List<Pattern> Patterns =>
    [
        new PushPattern(() => FilesPatternResource.pattern_InvitedToAgent_push)
    ];
}

[Scope]
public class RoomUpdateAccessForUserNotifyAction : INotifyAction
{
    public virtual string ID => "RoomUpdateAccessForUser";

    public List<ITagValue> Tags { get; set; }

    public virtual List<Pattern> Patterns =>
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
public sealed class AgentUpdateAccessForUserNotifyAction : RoomUpdateAccessForUserNotifyAction
{
    public override string ID => "AgentUpdateAccessForUser";

    public override List<Pattern> Patterns =>
    [
        new PushPattern(() => FilesPatternResource.pattern_AgentUpdateAccessForUser_push)
    ];
}

[Scope]
public sealed class DocumentCreatedInRoomNotifyAction : INotifyAction
{
    public string ID => "DocumentCreatedInRoom";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
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
public sealed class DocumentUploadedToRoomNotifyAction : INotifyAction
{
    public string ID => "DocumentUploadedTo";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
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
public sealed class DocumentsUploadedToRoomNotifyAction : INotifyAction
{
    public string ID => "DocumentsUploadedTo";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
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
public sealed class FolderCreatedInRoomNotifyAction : INotifyAction
{
    public string ID => "FolderCreatedInRoom";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
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
public class FormStartedFillingNotifyAction(BaseCommonLinkUtility baseCommonLinkUtility, FilesLinkUtility filesLinkUtility, FileUtility fileUtility) : INotifyAction
{
    public virtual string ID => "FormStartedFilling";

    public List<ITagValue> Tags { get; set; }

    public virtual List<Pattern> Patterns =>
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
public sealed class YourTurnFormFillingNotifyAction(BaseCommonLinkUtility baseCommonLinkUtility, FilesLinkUtility filesLinkUtility, FileUtility fileUtility) : FormStartedFillingNotifyAction(baseCommonLinkUtility, filesLinkUtility,fileUtility)
{
    public override string ID => "YourTurnFormFilling";

    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_YourTurnFormFilling, () => FilesPatternResource.pattern_YourTurnFormFilling),
        new TelegramPattern(() => FilesPatternResource.pattern_YourTurnFormFilling)
    ];
}

[Scope]
public sealed class FormWasCompletelyFilledNotifyAction(BaseCommonLinkUtility baseCommonLinkUtility, FilesLinkUtility filesLinkUtility, FileUtility fileUtility) : FormStartedFillingNotifyAction(baseCommonLinkUtility, filesLinkUtility,fileUtility)
{
    public override string ID => "FormWasCompletelyFilled";

    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_FormWasCompletelyFilled, () => FilesPatternResource.pattern_FormWasCompletelyFilled),
        new TelegramPattern(() => FilesPatternResource.pattern_FormWasCompletelyFilled)
    ];
}

[Scope]
public sealed class StoppedFormFillingNotifyAction(BaseCommonLinkUtility baseCommonLinkUtility, FilesLinkUtility filesLinkUtility, FileUtility fileUtility) : FormStartedFillingNotifyAction(baseCommonLinkUtility, filesLinkUtility,fileUtility)
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