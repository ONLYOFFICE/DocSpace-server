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
// design elements, icons, logos, and text, are the property of
// Ascensio System SIA and are protected by copyright and trademark laws.
// These elements may not be used in derivative works or modified in any way
// without prior written permission from Ascensio System SIA.
//
// Pursuant to Section 7 § 3(b) of the License you must retain the original
// Product logo when distributing the program. Pursuant to Section 7 § 3(e) we
// decline to grant you any rights under trademark law for use of our trademarks.
//
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Files.Core;

/// <summary>
/// Tells the clients that the metadata of entries changed behind their back: a cascade pass, the deletion of a
/// template or of a field touch entries nobody has open in a request. Every entry gets the same update event a
/// direct write sends, so a client viewing its folder re-reads the row. The recipients are resolved once per parent
/// folder rather than per entry: the event is a nudge to re-read, and the readers of a folder are the readers of its
/// content, so a per-entry resolution of the rights would cost several queries per entry for the same answer.
/// </summary>
[Scope]
public class MetadataEntryNotifier(IDaoFactory daoFactory, SocketManager socketManager)
{
    private const int BatchSize = 1000;

    public async Task NotifyUpdatedAsync(IEnumerable<(int EntryId, FileEntryType EntryType)> entries)
    {
        foreach (var group in entries.GroupBy(e => e.EntryType))
        {
            foreach (var batch in group.Select(e => e.EntryId).Distinct().Chunk(BatchSize))
            {
                await NotifyUpdatedAsync(group.Key, batch);
            }
        }
    }

    public async Task NotifyUpdatedAsync(FileEntryType entryType, IReadOnlyCollection<int> entryIds)
    {
        if (entryIds.Count == 0)
        {
            return;
        }

        var recipientsByParent = new Dictionary<int, IEnumerable<Guid>>();

        if (entryType == FileEntryType.File)
        {
            await foreach (var file in daoFactory.GetFileDao<int>().GetFilesAsync(entryIds))
            {
                await socketManager.UpdateFileAsync(file, await RecipientsAsync(recipientsByParent, file.ParentId));
            }
        }
        else
        {
            await foreach (var folder in daoFactory.GetFolderDao<int>().GetFoldersAsync(entryIds))
            {
                await socketManager.UpdateFolderAsync(folder, await RecipientsAsync(recipientsByParent, folder.ParentId));
            }
        }
    }

    /// <summary>
    /// The readers of the parent folder. Null when the parent cannot be loaded: the socket manager then resolves the
    /// recipients of that entry on its own.
    /// </summary>
    private async Task<IEnumerable<Guid>> RecipientsAsync(Dictionary<int, IEnumerable<Guid>> recipientsByParent, int parentId)
    {
        if (recipientsByParent.TryGetValue(parentId, out var recipients))
        {
            return recipients;
        }

        var parent = await daoFactory.GetFolderDao<int>().GetFolderAsync(parentId);

        recipients = parent == null ? null : (await socketManager.GetDeleteRecipientsAsync(parent)).users;

        recipientsByParent[parentId] = recipients;

        return recipients;
    }
}
