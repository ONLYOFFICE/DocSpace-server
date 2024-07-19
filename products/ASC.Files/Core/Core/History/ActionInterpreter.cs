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

namespace ASC.Files.Core.Core.History;

public abstract class ActionInterpreter
{
    private static readonly FrozenDictionary<MessageAction, MessageAction> _aliases = new Dictionary<MessageAction, MessageAction>
    {
        { MessageAction.FileMovedWithOverwriting, MessageAction.FileMoved },
        { MessageAction.FileCopiedWithOverwriting, MessageAction.FileCopied },
        { MessageAction.FileMovedToTrash, MessageAction.FileDeleted },
        { MessageAction.FolderMovedWithOverwriting, MessageAction.FolderMoved },
        { MessageAction.FolderCopiedWithOverwriting, MessageAction.FolderCopied },
        { MessageAction.FolderMovedToTrash, MessageAction.FolderDeleted },
        { MessageAction.FileRestoreVersion, MessageAction.UserFileUpdated },
        { MessageAction.FileUploadedWithOverwriting, MessageAction.UserFileUpdated }
    }.ToFrozenDictionary();
    
    public async ValueTask<HistoryEntry> InterpretAsync(DbAuditEvent @event, IServiceProvider serviceProvider)
    {
        var messageAction = @event.Action.HasValue ? (MessageAction)@event.Action.Value : MessageAction.None;
        var processedAction = _aliases.GetValueOrDefault(messageAction, messageAction);
        var key = processedAction != MessageAction.None ? processedAction.ToStringFast() : null;
        
        var description = JsonSerializer.Deserialize<List<string>>(@event.DescriptionRaw);
        
        var entry = new HistoryEntry
        {
            Action = new HistoryAction(processedAction, key),
            InitiatorId = @event.UserId ?? ASC.Core.Configuration.Constants.Guest.ID,
            Date = @event.Date,
            Data = await GetDataAsync(serviceProvider, @event.Target, description)
        };

        return entry;
    }
    
    protected static EventDescription<int> GetAdditionalDescription(List<string> description)
    {
        return JsonSerializer.Deserialize<EventDescription<int>>(description.Last());
    }

    protected abstract ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description);
}

public record EntryData : HistoryData
{
    public int Id { get; }
    public string Title { get; }
    public string ParentTitle { get; }
    public int? ParentId { get; }
    public int? ParentType { get; }
    
    public EntryData(string id, string title, int? parentId = null, string parentTitle = null, int? parentType = null)
    {
        Id = int.Parse(id);
        Title = title;
        ParentId = parentId;
        ParentTitle = parentTitle;
        ParentType = parentType;
    }
    
    public override int GetId() => ParentId ?? 0;
}

public record RenameEntryData : HistoryData
{
    public int? Id { get; }
    public string OldTitle { get; }
    public string NewTitle { get; }
    public int? ParentId { get; }
    public string ParentTitle { get; }
    public int? ParentType { get; }
    
    public RenameEntryData(string id, string oldTitle, string newTitle, int? parentId = null, string parentTitle = null, int? parentType = null)
    {
        Id = string.IsNullOrEmpty(id) ? null : int.Parse(id);
        OldTitle = oldTitle;
        NewTitle = newTitle;
        ParentId = parentId;
        ParentTitle = parentTitle;
        ParentType = parentType;
    }
}

public record LinkData(string Title, string Id = null, string OldTitle = null, string Access = null) : HistoryData;

public record EntryOperationData : HistoryData
{
    public int Id { get; }
    public string Title { get; }
    public int ToFolderId { get; }
    public string ToFolderTitle { get; }
    public int ToParentType { get; }
    
    public EntryOperationData(string id, string title, string toFolderId, string toFolderTitle, int toParentType)
    {
        Id = int.Parse(id);
        Title = title;
        ToFolderId = int.Parse(toFolderId);
        ToFolderTitle = toFolderTitle;
        ToParentType = toParentType;
    }
    
    public override int GetId() => ToFolderId;
}