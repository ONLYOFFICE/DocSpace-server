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

using LinkData = ASC.Files.Core.Core.History.LinkData;

namespace ASC.Files.Core.Core.Entries;

/// <summary>
/// The file history parameters.
/// </summary>
public record HistoryEntry
{
    /// <summary>
    /// The unique identifier for the history entry.
    /// </summary>
    public required int Id { get; init; }
    
    /// <summary>
    /// The action performed on the file.
    /// </summary>
    public HistoryAction Action { get; init; }

    /// <summary>
    /// The ID of the action initiator.
    /// </summary>
    public Guid InitiatorId { get; init; }

    /// <summary>
    /// The name of the action initiator.
    /// </summary>
    public string InitiatorName { get; init; }

    /// <summary>
    /// The date and time when the action was performed.
    /// </summary>
    public DateTime Date { get; init; }

    /// <summary>
    /// The history data.
    /// </summary>
    public HistoryData Data { get; init; }

    private static readonly HashSet<MessageAction> _gropedActions =
    [
        MessageAction.FileUploaded,
        MessageAction.FileMoved,
        MessageAction.FileCopied,
        MessageAction.FolderMoved,
        MessageAction.FolderCopied,
        MessageAction.FileDeleted,
        MessageAction.FileConverted,
        MessageAction.FileIndexChanged,
        MessageAction.FolderDeleted,
        MessageAction.FolderIndexChanged,
        MessageAction.RoomCreateUser,
        MessageAction.RoomGroupAdded,
        MessageAction.RoomInviteResend
    ];

    private static readonly HashSet<MessageAction> _mergedActions =
    [
        MessageAction.FileIndexChanged,
        MessageAction.FolderIndexChanged
    ];

    private int _groupId;

    public int GetGroupId()
    {
        if (_groupId != 0)
        {
            return _groupId;
        }

        if (_mergedActions.Contains(Action.Id))
        {
            return _groupId = Data?.GetId() ?? 0;
        }

        if (_gropedActions.Contains(Action.Id))
        {
            return _groupId = HashCode.Combine(Action.Id, InitiatorId, new DateTime(Date.Year, Date.Month, Date.Day, Date.Hour, Date.Minute, 0), Data?.GetId() ?? 0);
        }

        return _groupId = HashCode.Combine(Action.Id, InitiatorId, Date, Data?.GetId() ?? 0, Random.Shared.Next(int.MaxValue));
    }
}

/// <summary>
/// The history data.
/// </summary>
[JsonDerivedType(typeof(EntryData))]
[JsonDerivedType(typeof(EntryOperationData))]
[JsonDerivedType(typeof(GroupHistoryData))]
[JsonDerivedType(typeof(LinkData))]
[JsonDerivedType(typeof(RenameEntryData))]
[JsonDerivedType(typeof(TagData))]
[JsonDerivedType(typeof(UserHistoryData))]
[JsonDerivedType(typeof(ChangeRoomOwnerHistoryData))]
[JsonDerivedType(typeof(UserFileUpdateData))]
[JsonDerivedType(typeof(FileData))]
[JsonDerivedType(typeof(FileOperationData))]
[JsonDerivedType(typeof(FileRenameData))]
[JsonDerivedType(typeof(LifeTimeHistoryData))]
[JsonDerivedType(typeof(FolderIndexChangedData))]
[JsonDerivedType(typeof(FileIndexChangedData))]
[JsonDerivedType(typeof(FileVersionRemovedData))]
public abstract record HistoryData
{
    /// <summary>
    /// The history data ID.
    /// </summary>
    /// <example>0</example>
    public virtual int GetId() => 0;

    /// <summary>
    /// The name of the action initiator.
    /// </summary>
    /// <example>John Doe</example>
    public virtual string InitiatorName => null;
}

/// <summary>
/// The action performed on the file.
/// </summary>
public record HistoryAction(MessageAction Id, string Key)
{
    /// <summary>
    /// The action performed on the file.
    /// </summary>
    /// <example>FileUploaded</example>
    public MessageAction Id { get; init; } = Id;
    
    
    /// <summary>
    /// The action performed on the file.
    /// </summary>
    /// <example>fileUploaded</example>   
    public string Key { get; init; } = Key;
}