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

using LinkData = ASC.Files.Core.Core.History.LinkData;

namespace ASC.Files.Core.Core.Entries;

/// <summary>
/// The file history parameters.
/// </summary>
public record HistoryEntry
{
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
        
        return _groupId = HashCode.Combine(Action.Id, InitiatorId, Date, Data?.GetId() ?? 0, Random.Shared.Next(Int32.MaxValue));
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
    public virtual int GetId() => 0;

    /// <summary>
    /// The name of the action initiator.
    /// </summary>
    public virtual string InitiatorName => null;
}

/// <summary>
/// The action performed on the file.
/// </summary>
public record HistoryAction(MessageAction Id, string Key);