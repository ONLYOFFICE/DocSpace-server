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

public static class NotifyConstants
{
    #region Events

    public static readonly INotifyAction EventDocuSignComplete = new NotifyAction("DocuSignComplete")
    {
        Patterns = [
            new EmailPattern("DocuSignComplete", () => FilesPatternResource.subject_DocuSignComplete, () => FilesPatternResource.pattern_DocuSignComplete)
        ]
    };
    public static readonly INotifyAction EventDocuSignStatus = new NotifyAction("DocuSignStatus")
    {
        Patterns = [
            new EmailPattern("DocuSignStatus", () => FilesPatternResource.subject_DocuSignStatus, () => FilesPatternResource.pattern_DocuSignStatus)
        ]
    };
    public static readonly INotifyAction EventMailMergeEnd = new NotifyAction("MailMergeEnd")
    {
        Patterns = [
            new EmailPattern("MailMergeEnd", () => FilesPatternResource.subject_MailMergeEnd, () => FilesPatternResource.pattern_MailMergeEnd)
        ]
    };
    public static readonly INotifyAction EventShareDocument = new NotifyAction("ShareDocument")
    {
        Patterns = [
            new EmailPattern("ShareDocument", () => FilesPatternResource.subject_ShareDocument, () => FilesPatternResource.pattern_ShareDocument),
            new TelegramPattern("ShareDocument", () => FilesPatternResource.pattern_ShareDocument),
            new PushPattern("ShareDocument", () => FilesPatternResource.subject_ShareDocument_push)
        ]
    };
    public static readonly INotifyAction EventShareEncryptedDocument = new NotifyAction("ShareEncryptedDocument");
    public static readonly INotifyAction EventShareFolder = new NotifyAction("ShareFolder")
    {
        Patterns = [
            new EmailPattern("ShareFolder", () => FilesPatternResource.subject_ShareFolder, () => FilesPatternResource.pattern_ShareFolder),
            new TelegramPattern("ShareFolder", () => FilesPatternResource.pattern_ShareFolder),
            new PushPattern("ShareFolder", () => FilesPatternResource.subject_ShareFolder_push)
        ]
    };
    public static readonly INotifyAction EventEditorMentions = new NotifyAction("EditorMentions")
    {
        Patterns = [
            new EmailPattern("EditorMentions", () => FilesPatternResource.subject_EditorMentions, () => FilesPatternResource.pattern_EditorMentions),
            new TelegramPattern("EditorMentions", () => FilesPatternResource.pattern_EditorMentions),
            new PushPattern("EditorMentions", () => FilesPatternResource.pattern_EditorMentions_push)
        ]
    };
    public static readonly INotifyAction EventRoomRemoved = new NotifyAction("RoomRemoved")
    {
        Patterns = [
            new EmailPattern("RoomRemoved", () => FilesPatternResource.subject_RoomRemoved, () => FilesPatternResource.pattern_RoomRemoved),
            new TelegramPattern("RoomRemoved", () => FilesPatternResource.pattern_RoomRemoved)
        ]
    };
    public static readonly INotifyAction EventAgentRemoved = new NotifyAction("AgentRemoved")
    {
        Patterns = [
            new EmailPattern("AgentRemoved", () => FilesPatternResource.subject_AgentRemoved, () => FilesPatternResource.pattern_AgentRemoved),
            new TelegramPattern("AgentRemoved", () => FilesPatternResource.pattern_AgentRemoved)
        ]
    };
    public static readonly INotifyAction EventFormSubmitted = new NotifyAction("FormSubmitted")
    {
        Patterns = [
            new EmailPattern("FormSubmitted", () => FilesPatternResource.subject_FormSubmitted, () => FilesPatternResource.pattern_FormSubmitted),
            new TelegramPattern("FormSubmitted", () => FilesPatternResource.pattern_FormSubmitted),
            new PushPattern("FormSubmitted", () => FilesPatternResource.pattern_FormSubmitted_push)
        ]
    };
    public static readonly INotifyAction EventFormReceived = new NotifyAction("FormReceived")
    {
        Patterns = [
            new EmailPattern("FormReceived", () => FilesPatternResource.subject_FormReceived, () => FilesPatternResource.pattern_FormReceived),
            new TelegramPattern("FormReceived", () => FilesPatternResource.pattern_FormReceived)
        ]
    };
    public static readonly INotifyAction EventRoomMovedArchive = new NotifyAction("RoomMovedArchive")
    {
        Patterns = [
            new PushPattern("RoomMovedArchive", () => FilesPatternResource.pattern_RoomMovedArchive_push)
        ]
    };
    public static readonly INotifyAction EventInvitedToRoom = new NotifyAction("InvitedToRoom")
    {
        Patterns = [
            new PushPattern("InvitedToRoom", () => FilesPatternResource.pattern_InvitedToRoom_push)
        ]
    };
    public static readonly INotifyAction EventInvitedToAgent = new NotifyAction("InvitedToAgent")
    {
        Patterns = [
            new PushPattern("InvitedToAgent", () => FilesPatternResource.pattern_InvitedToAgent_push)
        ]
    };
    public static readonly INotifyAction EventRoomUpdateAccessForUser = new NotifyAction("RoomUpdateAccessForUser")
    {
        Patterns = [
            new PushPattern("RoomUpdateAccessForUser", () => FilesPatternResource.pattern_RoomUpdateAccessForUser_push)
        ]
    };
    public static readonly INotifyAction EventAgentUpdateAccessForUser = new NotifyAction("AgentUpdateAccessForUser")
    {
        Patterns = [
            new PushPattern("AgentUpdateAccessForUser", () => FilesPatternResource.pattern_AgentUpdateAccessForUser_push)
        ]
    };
    public static readonly INotifyAction EventDocumentCreatedInRoom = new NotifyAction("DocumentCreatedInRoom")
    {
        Patterns = [
            new PushPattern("DocumentCreatedInRoom", () => FilesPatternResource.pattern_DocumentCreatedInRoom_push)
        ]
    };
    public static readonly INotifyAction EventDocumentUploadedToRoom = new NotifyAction("DocumentUploadedTo")
    {
        Patterns = [
            new PushPattern("DocumentUploadedTo", () => FilesPatternResource.pattern_DocumentUploadedTo_push)
        ]
    };
    public static readonly INotifyAction EventDocumentsUploadedToRoom = new NotifyAction("DocumentsUploadedTo")
    {
        Patterns = [
            new PushPattern("DocumentsUploadedTo", () => FilesPatternResource.pattern_DocumentsUploadedTo_push)
        ]
    };
    public static readonly INotifyAction EventFolderCreatedInRoom = new NotifyAction("FolderCreatedInRoom")
    {
        Patterns = [
            new PushPattern("FolderCreatedInRoom", () => FilesPatternResource.pattern_FolderCreatedInRoom_push)
        ]
    };
    public static readonly INotifyAction EventFormStartedFilling = new NotifyAction("FormStartedFilling")
    {
        Patterns = [
            new EmailPattern("FormStartedFilling", () => FilesPatternResource.subject_FormStartedFilling, () => FilesPatternResource.pattern_FormStartedFilling),
            new TelegramPattern("FormStartedFilling", () => FilesPatternResource.pattern_FormStartedFilling)
        ]
    };
    public static readonly INotifyAction EventYourTurnFormFilling = new NotifyAction("YourTurnFormFilling")
    {
        Patterns = [
            new EmailPattern("YourTurnFormFilling", () => FilesPatternResource.subject_YourTurnFormFilling, () => FilesPatternResource.pattern_YourTurnFormFilling),
            new TelegramPattern("YourTurnFormFilling", () => FilesPatternResource.pattern_YourTurnFormFilling)
        ]
    };
    public static readonly INotifyAction EventFormWasCompletelyFilled = new NotifyAction("FormWasCompletelyFilled")
    {
        Patterns = [
            new EmailPattern("FormWasCompletelyFilled", () => FilesPatternResource.subject_FormWasCompletelyFilled, () => FilesPatternResource.pattern_FormWasCompletelyFilled),
            new TelegramPattern("FormWasCompletelyFilled", () => FilesPatternResource.pattern_FormWasCompletelyFilled)
        ]
    };
    public static readonly INotifyAction EventStoppedFormFilling = new NotifyAction("StoppedFormFilling")
    {
        Patterns = [
            new EmailPattern("StoppedFormFilling", () => FilesPatternResource.subject_StoppedFormFilling, () => FilesPatternResource.pattern_StoppedFormFilling),
            new TelegramPattern("StoppedFormFilling", () => FilesPatternResource.pattern_StoppedFormFilling)
        ]
    };

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