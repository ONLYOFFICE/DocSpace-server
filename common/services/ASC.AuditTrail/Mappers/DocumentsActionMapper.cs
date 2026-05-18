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

namespace ASC.AuditTrail.Mappers;

internal class DocumentsActionMapper : IProductActionMapper
{
    public List<ILocationActionMapper> Mappers { get; } =
    [
        new FilesActionMapper(),
        new FoldersActionMapper(),
        new RoomsActionMapper(),
        new SettingsActionMapper(),
        new AgentsActionMapper()
    ];

    public ProductType Product => ProductType.Documents;
}
internal class FilesActionMapper : ILocationActionMapper
{
    public LocationType Location { get; }
    public IDictionary<MessageAction, MessageMaps> Actions { get; }

    public FilesActionMapper()
    {
        Location = LocationType.Files;
        Actions = new MessageMapsDictionary(ProductType.Documents, Location)
        {
            {
                EntryType.File, new Dictionary<ActionType, MessageAction[]>
                {
                    { ActionType.Create, [MessageAction.FileCreated, MessageAction.FileCreatedVersion, MessageAction.FileRestoreVersion, MessageAction.FileConverted, MessageAction.FileExternalLinkCreated]
                    },
                    {
                        ActionType.Update, [
                            MessageAction.FileRenamed, MessageAction.FileUpdated, MessageAction.UserFileUpdated, MessageAction.FileUpdatedRevisionComment,
                            MessageAction.FileLocked, MessageAction.FileUnlocked, MessageAction.FileOpenedForChange, MessageAction.FileMarkedAsFavorite,
                            MessageAction.FormStartedToFill, MessageAction.FormPartiallyFilled, MessageAction.FormCompletelyFilled, MessageAction.FormStopped,
                            MessageAction.FileSavedButUserQuotaExceeded, MessageAction.FileNotSavedDueToUserQuota,
                            MessageAction.FileSavedButRoomQuotaExceeded, MessageAction.FileNotSavedDueToRoomQuota,
                            MessageAction.FileRemovedFromFavorite, MessageAction.FileMarkedAsRead, MessageAction.FileReaded, MessageAction.FormSubmit, MessageAction.FormOpenedForFilling,
                            MessageAction.FileIndexChanged, MessageAction.FolderIndexReordered, MessageAction.FileCustomFilterEnabled, MessageAction.FileCustomFilterDisabled,
                            MessageAction.FileExternalLinkUpdated
                        ]
                    },
                    { ActionType.Delete, [MessageAction.FileDeletedVersion, MessageAction.FileDeleted, MessageAction.TrashEmptied, MessageAction.FileVersionRemoved, MessageAction.FileExternalLinkDeleted]
                    },
                    { ActionType.UpdateAccess, [MessageAction.FileUpdatedAccess, MessageAction.FileUpdatedAccessFor, MessageAction.FileRemovedFromList, MessageAction.FileExternalLinkAccessUpdated
                        ]
                    },
                    { ActionType.Download, [MessageAction.FileDownloaded, MessageAction.FileDownloadedAs, MessageAction.FileRevisionDownloaded
                        ]
                    },
                    { ActionType.Send, [MessageAction.FileSendAccessLink, MessageAction.FileChangeOwner] },
                    { ActionType.Upload, [MessageAction.FileUploaded, MessageAction.FileUploadedWithOverwriting]}
                },
                new Dictionary<ActionType, MessageAction>
                {
                    { ActionType.Import, MessageAction.FileImported },
                    { ActionType.Move, MessageAction.FileMovedToTrash }
                }
            },
            {
                EntryType.File, EntryType.Folder, new Dictionary<ActionType, MessageAction[]>
                {
                    { ActionType.Copy, [MessageAction.FileCopied, MessageAction.FileCopiedWithOverwriting] },
                    { ActionType.Move, [MessageAction.FileMoved, MessageAction.FileMovedWithOverwriting] }
                }
            }
        };

        Actions.Add(MessageAction.DocumentSignComplete, new MessageMaps(nameof(AuditReportResource.FilesDocumentSigned), ActionType.Send, ProductType.Documents, Location, EntryType.File));
        Actions.Add(MessageAction.DocumentSendToSign, new MessageMaps(nameof(AuditReportResource.FilesRequestSign), ActionType.Send, ProductType.Documents, Location, EntryType.File));
    }
}

internal class FoldersActionMapper : ILocationActionMapper
{
    public LocationType Location { get; }
    public IDictionary<MessageAction, MessageMaps> Actions { get; }

    public FoldersActionMapper()
    {
        Location = LocationType.Folders;
        Actions = new MessageMapsDictionary(ProductType.Documents, Location)
        {
            {
                EntryType.Folder, new Dictionary<ActionType, MessageAction[]>
                {
                    { ActionType.Create, [MessageAction.FolderExternalLinkCreated] },
                    { ActionType.Update, [MessageAction.FolderRenamed, MessageAction.FolderMarkedAsRead, MessageAction.FolderIndexChanged, MessageAction.FolderExternalLinkUpdated] },
                    { ActionType.UpdateAccess, [MessageAction.FolderUpdatedAccess, MessageAction.FolderUpdatedAccessFor, MessageAction.FolderRemovedFromList] },
                    { ActionType.Delete, [MessageAction.FolderExternalLinkDeleted] }
                },
                new Dictionary<ActionType, MessageAction>
                {
                    { ActionType.Create, MessageAction.FolderCreated },
                    { ActionType.Move, MessageAction.FolderMovedToTrash },
                    { ActionType.Delete, MessageAction.FolderDeleted },
                    { ActionType.Download, MessageAction.FolderDownloaded }
                }
            },
            {
                EntryType.Folder, EntryType.Folder, new Dictionary<ActionType, MessageAction[]>
                {
                    { ActionType.Copy, [MessageAction.FolderCopied, MessageAction.FolderCopiedWithOverwriting] },
                    { ActionType.Move, [MessageAction.FolderMoved, MessageAction.FolderMovedWithOverwriting] }
                }
            }
        };
    }
}

internal class RoomsActionMapper : ILocationActionMapper
{
    public LocationType Location { get; }
    public IDictionary<MessageAction, MessageMaps> Actions { get; }

    public RoomsActionMapper()
    {
        Location = LocationType.Rooms;
        Actions = new MessageMapsDictionary(ProductType.Documents, Location)
        {
            {
                EntryType.Room, new Dictionary<ActionType, MessageAction[]>
                {
                    { ActionType.Create, [MessageAction.RoomCreated, MessageAction.AgentCreated] },
                    { ActionType.Copy, [MessageAction.RoomCopied] },
                    {
                        ActionType.Update, [
                            MessageAction.RoomArchived,
                            MessageAction.RoomUnarchived,
                            MessageAction.RoomRenamed,
                            MessageAction.AddedRoomTags,
                            MessageAction.DeletedRoomTags,
                            MessageAction.RoomLogoCreated,
                            MessageAction.RoomLogoDeleted,
                            MessageAction.RoomCreateUser,
                            MessageAction.RoomChangeOwner,
                            MessageAction.RoomUpdateAccessForUser,
                            MessageAction.RoomRemoveUser,
                            MessageAction.RoomInvitationLinkCreated,
                            MessageAction.RoomInvitationLinkUpdated,
                            MessageAction.RoomInvitationLinkDeleted,
                            MessageAction.RoomExternalLinkCreated,
                            MessageAction.RoomExternalLinkUpdated,
                            MessageAction.RoomExternalLinkDeleted,
                            MessageAction.RoomExternalLinkRevoked,
                            MessageAction.RoomGroupAdded,
                            MessageAction.RoomUpdateAccessForGroup,
                            MessageAction.RoomGroupRemove,
                            MessageAction.RoomIndexingEnabled,
                            MessageAction.RoomIndexingDisabled,
                            MessageAction.RoomLifeTimeSet,
                            MessageAction.RoomLifeTimeDisabled,
                            MessageAction.RoomDenyDownloadEnabled,
                            MessageAction.RoomDenyDownloadDisabled,
                            MessageAction.RoomWatermarkSet,
                            MessageAction.RoomWatermarkDisabled,
                            MessageAction.RoomInviteResend
                        ]
                    },
                    { ActionType.Delete, [MessageAction.RoomDeleted] },
                    { ActionType.Export, [MessageAction.RoomIndexExportSaved] }
                }
            },
            {
                EntryType.Tag, new Dictionary<ActionType, MessageAction>
                {
                    { ActionType.Create, MessageAction.TagCreated },
                    { ActionType.Delete, MessageAction.TagsDeleted }
                }
            }
        };
    }
}

internal class AgentsActionMapper : ILocationActionMapper
{
    public LocationType Location { get; }
    public IDictionary<MessageAction, MessageMaps> Actions { get; }

    public AgentsActionMapper()
    {
        Location = LocationType.Agents;
        Actions = new MessageMapsDictionary(ProductType.Documents, Location)
        {
            {
                EntryType.Room, new Dictionary<ActionType, MessageAction[]>
                {
                    { ActionType.Create, [MessageAction.AgentCreated] },
                    { ActionType.Update, [
                        MessageAction.AgentRenamed, 
                        MessageAction.AddedServerToAgent, 
                        MessageAction.DeletedServerFromAgent
                    ] },
                    { ActionType.Delete, [MessageAction.AgentDeleted] }
                }
            },
            {
                EntryType.Tag, new Dictionary<ActionType, MessageAction>
                {
                    { ActionType.Create, MessageAction.TagCreated },
                    { ActionType.Delete, MessageAction.TagsDeleted }
                }
            }
        };
    }
}

internal class SettingsActionMapper : ILocationActionMapper
{
    public LocationType Location { get; }
    public IDictionary<MessageAction, MessageMaps> Actions { get; }

    public SettingsActionMapper()
    {
        Location = LocationType.DocumentsSettings;
        Actions = new MessageMapsDictionary(ProductType.Documents, Location)
        {
            {
                EntryType.Folder, new Dictionary<ActionType, MessageAction>
                {
                    { ActionType.Create,  MessageAction.ThirdPartyCreated  },
                    { ActionType.Update, MessageAction.ThirdPartyUpdated },
                    { ActionType.Delete, MessageAction.ThirdPartyDeleted }
                }
            },
            {
                ActionType.Update, [
                    MessageAction.DocumentsThirdPartySettingsUpdated, MessageAction.DocumentsOverwritingSettingsUpdated,
                    MessageAction.DocumentsForcesave, MessageAction.DocumentsStoreForcesave, MessageAction.DocumentsUploadingFormatsSettingsUpdated,
                    MessageAction.DocumentsExternalShareSettingsUpdated, MessageAction.DocumentsDefaultTemplatesSettingsUpdated
                ]
            }
        };
    }
}