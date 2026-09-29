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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The history data.
/// </summary>
[JsonDerivedType(typeof(EntryHistoryDataDto))]
[JsonDerivedType(typeof(EntryOperationHistoryDataDto))]
[JsonDerivedType(typeof(GroupHistoryDataDto))]
[JsonDerivedType(typeof(LinkHistoryDataDto))]
[JsonDerivedType(typeof(RenameEntryHistoryDataDto))]
[JsonDerivedType(typeof(TagHistoryDataDto))]
[JsonDerivedType(typeof(UserHistoryDataDto))]
[JsonDerivedType(typeof(ChangeRoomOwnerHistoryDataDto))]
[JsonDerivedType(typeof(UserFileUpdateHistoryDataDto))]
[JsonDerivedType(typeof(FileHistoryDataDto))]
[JsonDerivedType(typeof(FileOperationHistoryDataDto))]
[JsonDerivedType(typeof(FileRenameHistoryDataDto))]
[JsonDerivedType(typeof(LifeTimeHistoryDataDto))]
[JsonDerivedType(typeof(FolderIndexChangedHistoryDataDto))]
[JsonDerivedType(typeof(FileIndexChangedHistoryDataDto))]
[JsonDerivedType(typeof(FileVersionRemovedHistoryDataDto))]
public abstract record HistoryDataDto
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
public record HistoryActionDto(MessageAction Id, string Key)
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

public abstract record IdentifiedHistoryDataDto : HistoryDataDto
{
    public int? Id { get; internal set; }
}

public record EntryHistoryDataDto : IdentifiedHistoryDataDto
{
    public string Title { get; }
    public string ParentTitle { get; }
    public int? ParentId { get; }
    public int? ParentType { get; }
    public int? Type { get; }

    public EntryHistoryDataDto(string id, string title, int? parentId = null, string parentTitle = null, int? parentType = null, int? currentType = null)
    {
        Id = int.Parse(id);
        Title = title;
        ParentId = parentId;
        ParentTitle = parentTitle;
        ParentType = parentType;
        Type = currentType;
    }

    public override int GetId() => ParentId ?? 0;
}

public record RenameEntryHistoryDataDto : IdentifiedHistoryDataDto
{
    public string OldTitle { get; }
    public string NewTitle { get; }
    public int? ParentId { get; }
    public string ParentTitle { get; }
    public int? ParentType { get; }

    public RenameEntryHistoryDataDto(string id, string oldTitle, string newTitle, int? parentId = null, string parentTitle = null, int? parentType = null)
    {
        Id = string.IsNullOrEmpty(id) ? null : int.Parse(id);
        OldTitle = oldTitle;
        NewTitle = newTitle;
        ParentId = parentId;
        ParentTitle = parentTitle;
        ParentType = parentType;
    }
}

public record LinkHistoryDataDto(string Title, string Id = null, string OldTitle = null, string Access = null) : HistoryDataDto;

public record EntryOperationHistoryDataDto : IdentifiedHistoryDataDto
{
    public string Title { get; }
    public string ToFolderId { get; }
    public string ParentTitle { get; }
    public int? ParentType { get; }
    public string FromParentTitle { get; }
    public int? FromParentType { get; }
    public int? FromFolderId { get; }

    public EntryOperationHistoryDataDto(
        string id,
        string title,
        string toFolderId,
        string parentTitle,
        int? parentType,
        string fromParentTitle,
        int? fromParentType,
        int? fromFolderId)
    {
        Id = int.Parse(id);
        Title = title;
        ToFolderId = toFolderId;
        ParentTitle = parentTitle;
        ParentType = parentType;
        FromParentTitle = fromParentTitle;
        FromParentType = fromParentType;
        FromFolderId = fromFolderId;
    }

    public override int GetId()
    {
        return FromFolderId.HasValue ? HashCode.Combine(ToFolderId, FromFolderId) : ToFolderId.GetHashCode();
    }
}

public record FileHistoryDataDto : EntryHistoryDataDto
{
    public IDictionary<Accessibility, bool> Accessibility { get; }
    public string ViewUrl { get; }

    public FileHistoryDataDto(
        string id,
        string title,
        int? parentId = null,
        string parentTitle = null,
        int? parentType = null,
        int? currentType = null,
        IDictionary<Accessibility, bool> accessibility = null,
        string viewUrl = null)
        : base(id, title, parentId, parentTitle, parentType, currentType)
    {
        Accessibility = accessibility;
        ViewUrl = viewUrl;
    }
}

public record FileOperationHistoryDataDto : EntryOperationHistoryDataDto
{
    public IDictionary<Accessibility, bool> Accessibility { get; }
    public string ViewUrl { get; }

    public FileOperationHistoryDataDto(string id,
        string title,
        string toFolderId,
        string parentTitle,
        int? parentType,
        string fromParentTitle,
        int? fromParentType,
        int? fromFolderId,
        IDictionary<Accessibility, bool> accessibility = null,
        string viewUrl = null)
        : base(id, title, toFolderId, parentTitle, parentType, fromParentTitle, fromParentType, fromFolderId)
    {
        Accessibility = accessibility;
        ViewUrl = viewUrl;
    }
}

public record UserFileUpdateHistoryDataDto : EntryHistoryDataDto
{
    public string UserName { get; }
    public IDictionary<Accessibility, bool> Accessibility { get; }
    public string ViewUrl { get; }
    public override string InitiatorName => UserName;

    public UserFileUpdateHistoryDataDto(string id,
        string title,
        int? parentId = null,
        string parentTitle = null,
        int? parentType = null,
        string userName = null,
        IDictionary<Accessibility, bool> accessibility = null,
        string viewUrl = null) : base(id,
        title,
        parentId,
        parentTitle,
        parentType)
    {
        UserName = userName;
        Accessibility = accessibility;
        ViewUrl = viewUrl;
    }
}

public record FileRenameHistoryDataDto : RenameEntryHistoryDataDto
{
    public IDictionary<Accessibility, bool> Accessibility { get; }
    public string ViewUrl { get; }

    public FileRenameHistoryDataDto(string id,
        string oldTitle,
        string newTitle,
        int? parentId = null,
        string parentTitle = null,
        int? parentType = null,
        IDictionary<Accessibility, bool> accessibility = null,
        string viewUrl = null)
        : base(id, oldTitle, newTitle, parentId, parentTitle, parentType)
    {
        Accessibility = accessibility;
        ViewUrl = viewUrl;
    }
}

public record FileIndexChangedHistoryDataDto : EntryHistoryDataDto
{
    public int OldIndex { get; }
    public int NewIndex { get; }
    public IDictionary<Accessibility, bool> Accessibility { get; }
    public string ViewUrl { get; }
    private readonly string _context;

    public FileIndexChangedHistoryDataDto(
        int oldIndex,
        int newIndex,
        string id,
        string title,
        int? parentId = null,
        string parentTitle = null,
        int? parentType = null,
        IDictionary<Accessibility, bool> accessibility = null,
        string viewUrl = null,
        string context = null) : base(id,
        title,
        parentId,
        parentTitle,
        parentType)
    {
        OldIndex = oldIndex;
        NewIndex = newIndex;
        Accessibility = accessibility;
        ViewUrl = viewUrl;
        _context = context;
    }

    public override int GetId()
    {
        if (!string.IsNullOrEmpty(_context))
        {
            return _context.GetHashCode();
        }

        return ParentId.HasValue ? ParentId.GetHashCode() : 0;
    }
}

public record FileVersionRemovedHistoryDataDto : EntryHistoryDataDto
{
    public int Version { get; }

    public FileVersionRemovedHistoryDataDto(
        string id,
        string title,
        int? parentId = null,
        string parentTitle = null,
        int? parentType = null,
        string version = "") : base(id,
        title,
        parentId,
        parentTitle,
        parentType)
    {
        if (int.TryParse(version, out var versionParsed))
        {
            Version = versionParsed;
        }
    }
}

public record FolderIndexChangedHistoryDataDto : EntryHistoryDataDto
{
    public int OldIndex { get; }
    public int NewIndex { get; }
    private readonly string _context;

    public FolderIndexChangedHistoryDataDto(
        int oldIndex,
        int newIndex,
        string id,
        string title,
        int? parentId = null,
        string parentTitle = null,
        int? parentType = null,
        string context = null) : base(id,
        title,
        parentId,
        parentTitle,
        parentType)
    {
        NewIndex = newIndex;
        OldIndex = oldIndex;
        _context = context;
    }

    public override int GetId()
    {
        if (!string.IsNullOrEmpty(_context))
        {
            return _context.GetHashCode();
        }

        return ParentId.HasValue ? ParentId.GetHashCode() : 0;
    }
}

public record UserHistoryDataDto : HistoryDataDto
{
    public EmployeeDto User { get; set; }
    public string Access { get; set; }
    public string OldAccess { get; set; }
}

public record ChangeRoomOwnerHistoryDataDto : HistoryDataDto
{
    public EmployeeDto Owner { get; set; }
    public EmployeeDto OldOwner { get; set; }
}

public record GroupHistoryDataDto : HistoryDataDto
{
    public GroupSummaryDto Group { get; set; }
    public string Access { get; set; }
    public string OldAccess { get; set; }
}

public record TagHistoryDataDto(string[] Tags) : HistoryDataDto;

public record LifeTimeHistoryDataDto : EntryHistoryDataDto
{
    public LifeTimeHistoryDataDto(RoomDataLifetime lifeTime, string id, string title, int? parentId = null, string parentTitle = null, int? parentType = null)
        : base(id, title, parentId, parentTitle, parentType)
    {
        LifeTime = lifeTime;
    }

    public RoomDataLifetime LifeTime { get; set; }
}
