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
        { MessageAction.FileConverted, MessageAction.UserFileUpdated },
        { MessageAction.FileRestoreVersion, MessageAction.UserFileUpdated }
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

public record EntryData(int Id, string Title, int? ParentId = null, string ParentTitle = null) : HistoryData
{
    public override int GetId() => ParentId ?? 0;
}

public record RenameEntryData(int? Id, string OldTitle, string NewTitle, int? ParentId = null, string ParentTitle = null) : HistoryData;
public record LinkData(string Title, string Access, string Id = null, string OldTitle = null, string OldAccess = null) : HistoryData;

public record EntryOperationData(int Id, string Title, int ToFolderId, string ToFolderTitle) : HistoryData
{
    public override int GetId() => ToFolderId;
}