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

[Singleton]
public class NotifyConstants : INotifyActionList
{
    public NotifyConstants()
    {
        EventDocuSignComplete = new NotifyAction("DocuSignComplete", this)
        {
            Patterns =
            [
                new EmailPattern(() => FilesPatternResource.subject_DocuSignComplete, () => FilesPatternResource.pattern_DocuSignComplete)
            ]
        };
        
        EventDocuSignStatus = new NotifyAction("DocuSignStatus", this)
        {
            Patterns = [
                new EmailPattern(() => FilesPatternResource.subject_DocuSignStatus, () => FilesPatternResource.pattern_DocuSignStatus)
            ]
        };
        
        EventMailMergeEnd = new NotifyAction("MailMergeEnd", this)
        {
            Patterns = [
                new EmailPattern(() => FilesPatternResource.subject_MailMergeEnd, () => FilesPatternResource.pattern_MailMergeEnd)
            ]
        };
        
        EventShareDocument = new NotifyAction("ShareDocument", this)
        {
            Patterns = [
                new EmailPattern(() => FilesPatternResource.subject_ShareDocument, () => FilesPatternResource.pattern_ShareDocument),
                new TelegramPattern(() => FilesPatternResource.pattern_ShareDocument),
                new PushPattern(() => FilesPatternResource.subject_ShareDocument_push)
            ]
        };
        
        EventShareEncryptedDocument = new NotifyAction("ShareEncryptedDocument", this);
        
        EventShareFolder = new NotifyAction("ShareFolder", this)
        {
            Patterns = [
                new EmailPattern(() => FilesPatternResource.subject_ShareFolder, () => FilesPatternResource.pattern_ShareFolder),
                new TelegramPattern(() => FilesPatternResource.pattern_ShareFolder),
                new PushPattern(() => FilesPatternResource.subject_ShareFolder_push)
            ]
        };
        
        EventEditorMentions = new NotifyAction("EditorMentions", this)
        {
            Patterns = [
                new EmailPattern(() => FilesPatternResource.subject_EditorMentions, () => FilesPatternResource.pattern_EditorMentions),
                new TelegramPattern(() => FilesPatternResource.pattern_EditorMentions),
                new PushPattern(() => FilesPatternResource.pattern_EditorMentions_push)
            ]
        };
        
        EventRoomRemoved = new NotifyAction("RoomRemoved", this)
        {
            Patterns = [
                new EmailPattern(() => FilesPatternResource.subject_RoomRemoved, () => FilesPatternResource.pattern_RoomRemoved),
                new TelegramPattern(() => FilesPatternResource.pattern_RoomRemoved)
            ]
        };
        
        EventAgentRemoved = new NotifyAction("AgentRemoved", this)
        {
            Patterns = [
                new EmailPattern(() => FilesPatternResource.subject_AgentRemoved, () => FilesPatternResource.pattern_AgentRemoved),
                new TelegramPattern(() => FilesPatternResource.pattern_AgentRemoved)
            ]
        };
        
        EventFormSubmitted = new NotifyAction("FormSubmitted", this)
        {
            Patterns = [
                new EmailPattern(() => FilesPatternResource.subject_FormSubmitted, () => FilesPatternResource.pattern_FormSubmitted),
                new TelegramPattern(() => FilesPatternResource.pattern_FormSubmitted),
                new PushPattern(() => FilesPatternResource.pattern_FormSubmitted_push)
            ]
        };
        
        EventFormReceived = new NotifyAction("FormReceived", this)
        {
            Patterns = [
                new EmailPattern(() => FilesPatternResource.subject_FormReceived, () => FilesPatternResource.pattern_FormReceived),
                new TelegramPattern(() => FilesPatternResource.pattern_FormReceived)
            ]
        };
        
        EventRoomMovedArchive = new NotifyAction("RoomMovedArchive", this)
        {
            Patterns = [
                new PushPattern(() => FilesPatternResource.pattern_RoomMovedArchive_push)
            ]
        };
        
        EventInvitedToRoom = new NotifyAction("InvitedToRoom", this)
        {
            Patterns = [
                new PushPattern(() => FilesPatternResource.pattern_InvitedToRoom_push)
            ]
        };
        
        EventInvitedToAgent = new NotifyAction("InvitedToAgent", this)
        {
            Patterns = [
                new PushPattern(() => FilesPatternResource.pattern_InvitedToAgent_push)
            ]
        };
        
        EventRoomUpdateAccessForUser = new NotifyAction("RoomUpdateAccessForUser", this)
        {
            Patterns = [
                new PushPattern(() => FilesPatternResource.pattern_RoomUpdateAccessForUser_push)
            ]
        };
        
        EventAgentUpdateAccessForUser = new NotifyAction("AgentUpdateAccessForUser", this)
        {
            Patterns = [
                new PushPattern(() => FilesPatternResource.pattern_AgentUpdateAccessForUser_push)
            ]
        };
        
        EventDocumentCreatedInRoom = new NotifyAction("DocumentCreatedInRoom", this)
        {
            Patterns = [
                new PushPattern(() => FilesPatternResource.pattern_DocumentCreatedInRoom_push)
            ]
        };
        
        EventDocumentUploadedToRoom = new NotifyAction("DocumentUploadedTo", this)
        {
            Patterns = [
                new PushPattern(() => FilesPatternResource.pattern_DocumentUploadedTo_push)
            ]
        };
        
        EventDocumentsUploadedToRoom = new NotifyAction("DocumentsUploadedTo", this)
        {
            Patterns = [
                new PushPattern(() => FilesPatternResource.pattern_DocumentsUploadedTo_push)
            ]
        };
        
        EventFolderCreatedInRoom = new NotifyAction("FolderCreatedInRoom", this)
        {
            Patterns = [
                new PushPattern(() => FilesPatternResource.pattern_FolderCreatedInRoom_push)
            ]
        };
        
        EventFormStartedFilling = new NotifyAction("FormStartedFilling", this)
        {
            Patterns = [
                new EmailPattern(() => FilesPatternResource.subject_FormStartedFilling, () => FilesPatternResource.pattern_FormStartedFilling),
                new TelegramPattern(() => FilesPatternResource.pattern_FormStartedFilling)
            ]
        };
        
        EventYourTurnFormFilling = new NotifyAction("YourTurnFormFilling", this)
        {
            Patterns = [
                new EmailPattern(() => FilesPatternResource.subject_YourTurnFormFilling, () => FilesPatternResource.pattern_YourTurnFormFilling),
                new TelegramPattern(() => FilesPatternResource.pattern_YourTurnFormFilling)
            ]
        };
        
        EventFormWasCompletelyFilled = new NotifyAction("FormWasCompletelyFilled", this)
        {
            Patterns = [
                new EmailPattern(() => FilesPatternResource.subject_FormWasCompletelyFilled, () => FilesPatternResource.pattern_FormWasCompletelyFilled),
                new TelegramPattern(() => FilesPatternResource.pattern_FormWasCompletelyFilled)
            ]
        };
        
        EventStoppedFormFilling = new NotifyAction("StoppedFormFilling", this)
        {
            Patterns = [
                new EmailPattern(() => FilesPatternResource.subject_StoppedFormFilling, () => FilesPatternResource.pattern_StoppedFormFilling),
                new TelegramPattern(() => FilesPatternResource.pattern_StoppedFormFilling)
            ]
        };
    }
    
    #region Events
    
    public readonly INotifyAction EventDocuSignComplete;
    public readonly INotifyAction EventDocuSignStatus;
    public readonly INotifyAction EventMailMergeEnd;
    public readonly INotifyAction EventShareDocument;
    public readonly INotifyAction EventShareEncryptedDocument;
    public readonly INotifyAction EventShareFolder;
    public readonly INotifyAction EventEditorMentions;
    public readonly INotifyAction EventRoomRemoved;
    public readonly INotifyAction EventAgentRemoved;
    public readonly INotifyAction EventFormSubmitted;
    public readonly INotifyAction EventFormReceived;
    public readonly INotifyAction EventRoomMovedArchive;
    public readonly INotifyAction EventInvitedToRoom;
    public readonly INotifyAction EventInvitedToAgent;
    public readonly INotifyAction EventRoomUpdateAccessForUser;
    public readonly INotifyAction EventAgentUpdateAccessForUser;
    public readonly INotifyAction EventDocumentCreatedInRoom;
    public readonly INotifyAction EventDocumentUploadedToRoom;
    public readonly INotifyAction EventDocumentsUploadedToRoom;
    public readonly INotifyAction EventFolderCreatedInRoom;
    public readonly INotifyAction EventFormStartedFilling;
    public readonly INotifyAction EventYourTurnFormFilling;
    public readonly INotifyAction EventFormWasCompletelyFilled;
    public readonly INotifyAction EventStoppedFormFilling;

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