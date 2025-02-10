// (c) Copyright Ascensio System SIA 2009-2024
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
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description, FileEntry<int> entry)
    {
        var desc = GetAdditionalDescription(description);

        return new ValueTask<HistoryData>(new EntryData(target, description[0], desc.ParentId, desc.ParentTitle, desc.ParentType, desc.Type));
    }
}

public class FolderMovedInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description, FileEntry<int> entry)
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
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description, FileEntry<int> entry)
    {
        var desc = GetAdditionalDescription(description);
        
        return new ValueTask<HistoryData>(new RenameEntryData(target, description[1], description[0], desc.ParentId, 
            desc.ParentTitle, desc.ParentType));
    }
}

public class FolderCopiedInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description, FileEntry<int> entry)
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
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description, FileEntry<int> entry)
    {
        return new ValueTask<HistoryData>(new EntryData(target, description[0]));
    }
}

public class FolderIndexReorderedInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description, FileEntry<int> entry)
    {
        var desc = GetAdditionalDescription(description);
        var title = description[0];
        
        var isRoom = desc.ParentType is (int)FolderType.VirtualRooms or (int)FolderType.Archive;
        var parentId = isRoom ? int.Parse(target) : desc.ParentId;
        var parentTitle = isRoom ? title : desc.ParentTitle;
        var parentType = isRoom ? (int)FolderType.VirtualDataRoom : desc.ParentType;
        
        return new ValueTask<HistoryData>(new EntryData(target, title, parentId, parentTitle, parentType));
    }
}

public class FolderIndexChangedInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description, FileEntry<int> entry)
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