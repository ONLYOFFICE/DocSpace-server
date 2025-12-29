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

using ASC.Core.Common.Notify.Model;

namespace ASC.Files.Core.Services.NotifyService;

[Scope]
public sealed class DocuSignCompleteNotifyAction : INotifyAction
{
    public string ID => "DocuSignComplete";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_DocuSignComplete, () => FilesPatternResource.pattern_DocuSignComplete)
    ];
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
}

[Scope]
public sealed class ShareDocumentNotifyAction : INotifyAction
{
    public string ID => "ShareDocument";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_ShareDocument, () => FilesPatternResource.pattern_ShareDocument),
        new TelegramPattern(() => FilesPatternResource.pattern_ShareDocument),
        new PushPattern(() => FilesPatternResource.subject_ShareDocument_push)
    ];
}

[Scope]
public sealed class ShareEncryptedDocumentNotifyAction : INotifyAction
{
    public string ID => "ShareEncryptedDocument";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns => [];
}

[Scope]
public sealed class ShareFolderNotifyAction : INotifyAction
{
    public string ID => "ShareFolder";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_ShareFolder, () => FilesPatternResource.pattern_ShareFolder),
        new TelegramPattern(() => FilesPatternResource.pattern_ShareFolder),
        new PushPattern(() => FilesPatternResource.subject_ShareFolder_push)
    ];
}

[Scope]
public sealed class EditorMentionsNotifyAction : INotifyAction
{
    public string ID => "EditorMentions";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_EditorMentions, () => FilesPatternResource.pattern_EditorMentions),
        new TelegramPattern(() => FilesPatternResource.pattern_EditorMentions),
        new PushPattern(() => FilesPatternResource.pattern_EditorMentions_push)
    ];
}

[Scope]
public sealed class RoomRemovedNotifyAction : INotifyAction
{
    public string ID => "RoomRemoved";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_RoomRemoved, () => FilesPatternResource.pattern_RoomRemoved),
        new TelegramPattern(() => FilesPatternResource.pattern_RoomRemoved)
    ];
}

[Scope]
public sealed class AgentRemovedNotifyAction : INotifyAction
{
    public string ID => "AgentRemoved";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_AgentRemoved, () => FilesPatternResource.pattern_AgentRemoved),
        new TelegramPattern(() => FilesPatternResource.pattern_AgentRemoved)
    ];
}

[Scope]
public sealed class FormSubmittedNotifyAction : INotifyAction
{
    public string ID => "FormSubmitted";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_FormSubmitted, () => FilesPatternResource.pattern_FormSubmitted),
        new TelegramPattern(() => FilesPatternResource.pattern_FormSubmitted),
        new PushPattern(() => FilesPatternResource.pattern_FormSubmitted_push)
    ];
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
}

[Scope]
public sealed class RoomMovedArchiveNotifyAction : INotifyAction
{
    public string ID => "RoomMovedArchive";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
    [
        new PushPattern(() => FilesPatternResource.pattern_RoomMovedArchive_push)
    ];
}

[Scope]
public sealed class InvitedToRoomNotifyAction : INotifyAction
{
    public string ID => "InvitedToRoom";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
    [
        new PushPattern(() => FilesPatternResource.pattern_InvitedToRoom_push)
    ];
}

[Scope]
public sealed class InvitedToAgentNotifyAction : INotifyAction
{
    public string ID => "InvitedToAgent";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
    [
        new PushPattern(() => FilesPatternResource.pattern_InvitedToAgent_push)
    ];
}

[Scope]
public sealed class RoomUpdateAccessForUserNotifyAction : INotifyAction
{
    public string ID => "RoomUpdateAccessForUser";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
    [
        new PushPattern(() => FilesPatternResource.pattern_RoomUpdateAccessForUser_push)
    ];
}

[Scope]
public sealed class AgentUpdateAccessForUserNotifyAction : INotifyAction
{
    public string ID => "AgentUpdateAccessForUser";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
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
}

[Scope]
public sealed class FormStartedFillingNotifyAction : INotifyAction
{
    public string ID => "FormStartedFilling";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_FormStartedFilling, () => FilesPatternResource.pattern_FormStartedFilling),
        new TelegramPattern(() => FilesPatternResource.pattern_FormStartedFilling)
    ];
}

[Scope]
public sealed class YourTurnFormFillingNotifyAction : INotifyAction
{
    public string ID => "YourTurnFormFilling";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_YourTurnFormFilling, () => FilesPatternResource.pattern_YourTurnFormFilling),
        new TelegramPattern(() => FilesPatternResource.pattern_YourTurnFormFilling)
    ];
}

[Scope]
public sealed class FormWasCompletelyFilledNotifyAction : INotifyAction
{
    public string ID => "FormWasCompletelyFilled";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_FormWasCompletelyFilled, () => FilesPatternResource.pattern_FormWasCompletelyFilled),
        new TelegramPattern(() => FilesPatternResource.pattern_FormWasCompletelyFilled)
    ];
}

[Scope]
public sealed class StoppedFormFillingNotifyAction : INotifyAction
{
    public string ID => "StoppedFormFilling";

    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns =>
    [
        new EmailPattern(() => FilesPatternResource.subject_StoppedFormFilling, () => FilesPatternResource.pattern_StoppedFormFilling),
        new TelegramPattern(() => FilesPatternResource.pattern_StoppedFormFilling)
    ];
}

[Singleton]
public class NotifyConstants(
    DocuSignCompleteNotifyAction docuSignComplete,
    DocuSignStatusNotifyAction docuSignStatus,
    MailMergeEndNotifyAction mailMergeEnd,
    ShareDocumentNotifyAction shareDocument,
    ShareEncryptedDocumentNotifyAction shareEncryptedDocument,
    ShareFolderNotifyAction shareFolder,
    EditorMentionsNotifyAction editorMentions,
    RoomRemovedNotifyAction roomRemoved,
    AgentRemovedNotifyAction agentRemoved,
    FormSubmittedNotifyAction formSubmitted,
    FormReceivedNotifyAction formReceived,
    RoomMovedArchiveNotifyAction roomMovedArchive,
    InvitedToRoomNotifyAction invitedToRoom,
    InvitedToAgentNotifyAction invitedToAgent,
    RoomUpdateAccessForUserNotifyAction roomUpdateAccessForUser,
    AgentUpdateAccessForUserNotifyAction agentUpdateAccessForUser,
    DocumentCreatedInRoomNotifyAction documentCreatedInRoom,
    DocumentUploadedToRoomNotifyAction documentUploadedToRoom,
    DocumentsUploadedToRoomNotifyAction documentsUploadedToRoom,
    FolderCreatedInRoomNotifyAction folderCreatedInRoom,
    FormStartedFillingNotifyAction formStartedFilling,
    YourTurnFormFillingNotifyAction yourTurnFormFilling,
    FormWasCompletelyFilledNotifyAction formWasCompletelyFilled,
    StoppedFormFillingNotifyAction stoppedFormFilling)
{
    #region Events

    public readonly INotifyAction EventDocuSignComplete = docuSignComplete;
    public readonly INotifyAction EventDocuSignStatus = docuSignStatus;
    public readonly INotifyAction EventMailMergeEnd = mailMergeEnd;
    public readonly INotifyAction EventShareDocument = shareDocument;
    public readonly INotifyAction EventShareEncryptedDocument = shareEncryptedDocument;
    public readonly INotifyAction EventShareFolder = shareFolder;
    public readonly INotifyAction EventEditorMentions = editorMentions;
    public readonly INotifyAction EventRoomRemoved = roomRemoved;
    public readonly INotifyAction EventAgentRemoved = agentRemoved;
    public readonly INotifyAction EventFormSubmitted = formSubmitted;
    public readonly INotifyAction EventFormReceived = formReceived;
    public readonly INotifyAction EventRoomMovedArchive = roomMovedArchive;
    public readonly INotifyAction EventInvitedToRoom = invitedToRoom;
    public readonly INotifyAction EventInvitedToAgent = invitedToAgent;
    public readonly INotifyAction EventRoomUpdateAccessForUser = roomUpdateAccessForUser;
    public readonly INotifyAction EventAgentUpdateAccessForUser = agentUpdateAccessForUser;
    public readonly INotifyAction EventDocumentCreatedInRoom = documentCreatedInRoom;
    public readonly INotifyAction EventDocumentUploadedToRoom = documentUploadedToRoom;
    public readonly INotifyAction EventDocumentsUploadedToRoom = documentsUploadedToRoom;
    public readonly INotifyAction EventFolderCreatedInRoom = folderCreatedInRoom;
    public readonly INotifyAction EventFormStartedFilling = formStartedFilling;
    public readonly INotifyAction EventYourTurnFormFilling = yourTurnFormFilling;
    public readonly INotifyAction EventFormWasCompletelyFilled = formWasCompletelyFilled;
    public readonly INotifyAction EventStoppedFormFilling = stoppedFormFilling;

    #endregion

    #region  Tags

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

    #endregion
}