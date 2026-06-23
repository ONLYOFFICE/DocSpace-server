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

namespace ASC.Files.Core.Core.History.Interpreters;

public record FolderIndexChangedData : EntryData
{
    public int OldIndex { get; }
    public int NewIndex { get; }
    private readonly string _context;

    public FolderIndexChangedData(
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

public class FolderCreatedInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var desc = GetAdditionalDescription(description);

        return new ValueTask<HistoryData>(new EntryData(target, description[0], desc.ParentId, desc.ParentTitle, desc.ParentType, desc.Type));
    }
}

public class FolderMovedInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var splitTarget = target.Split(',');
        var desc = GetAdditionalDescription(description);

        return new ValueTask<HistoryData>(
            new EntryOperationData(
                splitTarget[0],
                description[0],
                splitTarget[1],
                desc.ParentTitle,
                desc.ParentType,
                desc.FromParentTitle,
                desc.FromParentType,
                desc.FromFolderId));
    }
}

public class FolderRenamedInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var desc = GetAdditionalDescription(description);

        return new ValueTask<HistoryData>(new RenameEntryData(target, description[1], description[0], desc.ParentId,
            desc.ParentTitle, desc.ParentType));
    }
}

public class FolderCopiedInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var splitTarget = target.Split(',');
        var desc = GetAdditionalDescription(description);

        return new ValueTask<HistoryData>(
            new EntryOperationData(
                splitTarget[0],
                description[0],
                splitTarget[1],
                desc.ParentTitle,
                desc.ParentType,
                desc.FromParentTitle,
                desc.FromParentType,
                desc.FromFolderId));
    }
}

public class FolderDeletedInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        return new ValueTask<HistoryData>(new EntryData(target, description[0]));
    }
}

public class FolderIndexReorderedInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var desc = GetAdditionalDescription(description);
        var title = description[0];

        var isRoom = desc.ParentType is (int)FolderType.VirtualRooms or (int)FolderType.RoomTemplates or (int)FolderType.Archive;
        var parentId = isRoom ? int.Parse(target) : desc.ParentId;
        var parentTitle = isRoom ? title : desc.ParentTitle;
        var parentType = isRoom ? (int)FolderType.VirtualDataRoom : desc.ParentType;

        return new ValueTask<HistoryData>(new EntryData(target, title, parentId, parentTitle, parentType));
    }
}

public class FolderIndexChangedInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var oldIndex = int.Parse(description[1]);
        var newIndex = int.Parse(description[2]);

        string context = null;
        if (description.Count >= 4)
        {
            context = description[3];
        }

        var desc = GetAdditionalDescription(description);

        return new ValueTask<HistoryData>(new FolderIndexChangedData(
            oldIndex,
            newIndex,
            target,
            description[0],
            desc.ParentId,
            desc.ParentTitle,
            desc.ParentType,
            context));
    }
}