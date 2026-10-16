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

namespace ASC.Files.Core.Core.History;

public abstract class ActionInterpreter
{
    private static readonly FrozenDictionary<MessageAction, MessageAction> _aliases = new Dictionary<MessageAction, MessageAction>
    {
        { MessageAction.FileMovedWithOverwriting, MessageAction.FileMoved },
        { MessageAction.FileCopiedWithOverwriting, MessageAction.FileCopied },
        { MessageAction.FolderMovedWithOverwriting, MessageAction.FolderMoved },
        { MessageAction.FolderCopiedWithOverwriting, MessageAction.FolderCopied },
        { MessageAction.FileRestoreVersion, MessageAction.UserFileUpdated },
        { MessageAction.FileUploadedWithOverwriting, MessageAction.UserFileUpdated }
    }.ToFrozenDictionary();

    public async ValueTask<HistoryEntry> InterpretAsync(DbAuditEvent @event, DbFilesAuditReference reference, IServiceProvider serviceProvider)
    {
        var messageAction = @event.Action.HasValue ? (MessageAction)@event.Action.Value : MessageAction.None;
        var processedAction = _aliases.GetValueOrDefault(messageAction, messageAction);
        var key = processedAction != MessageAction.None ? processedAction.ToStringFast() : null;

        var description = JsonSerializer.Deserialize<List<string>>(@event.DescriptionRaw);
        var data = await GetDataAsync(serviceProvider, @event.Target, description);

        if (reference.Corrupted && data is IdentifiedHistoryDataDto identifiedData)
        {
            identifiedData.Id = 0;
        }

        var initiatorId = @event.UserId ?? ASC.Core.Configuration.Constants.Guest.ID;
        string initiatorName = null;

        if (!string.IsNullOrEmpty(data?.InitiatorName) && initiatorId == ASC.Core.Configuration.Constants.Guest.ID)
        {
            initiatorName = data.InitiatorName != AuditReportResource.GuestAccount
                ? $"{data.InitiatorName} ({FilesCommonResource.ExternalUser})"
                : data.InitiatorName;
        }

        var historyEntry = new HistoryEntry
        {
            Id = @event.Id,
            Action = new HistoryActionDto(processedAction, key),
            InitiatorId = initiatorId,
            InitiatorName = initiatorName,
            Date = @event.Date,
            Data = data
        };

        return historyEntry;
    }

    protected static EventDescription<int> GetAdditionalDescription(List<string> description)
    {
        return JsonSerializer.Deserialize<EventDescription<int>>(description.Last());
    }

    protected abstract ValueTask<HistoryDataDto> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description);
}
