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

namespace ASC.AI.Core.Search;

[Transient]
[OpenSearchType(RelationName = Index)]
public class ThreadSearchItem : ISearchItem
{
    public const string Index = "ai_threads";

    /// <summary>
    /// Value used when the thread has no entry (a personal chat): EntryId of 0 never matches a real entry.
    /// </summary>
    public const int NoEntry = 0;

    public Guid ThreadId { get; set; }
    public int TenantId { get; set; }
    public Guid CreatedBy { get; set; }
    public int EntryId { get; set; }

    [Text(Analyzer = "whitespacecustom")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Snapshot of the thread's LastEditDate taken when the document was written to the index.
    /// Only feeds the incremental crawl watermark; not a search or sort field.
    /// </summary>
    public DateTime LastEditDate { get; set; }

    [Ignore]
    public string IndexName => Index;

    /// <summary>
    /// Not used: threads are keyed by <see cref="ThreadId"/>, see BaseIndexerThread.GetDocumentId.
    /// </summary>
    int ISearchItem.Id { get => 0; set { } }

    public Expression<Func<ISearchItem, object[]>> GetSearchContentFields(SearchSettingsHelper searchSettings)
    {
        return _ => new object[] { Title };
    }

    public static ThreadSearchItem FromThread(DbThread thread)
    {
        return new ThreadSearchItem
        {
            ThreadId = thread.Id,
            TenantId = thread.TenantId,
            CreatedBy = thread.CreatedBy,
            EntryId = thread.EntryId ?? NoEntry,
            Title = thread.Title,
            LastEditDate = thread.LastEditDate
        };
    }
}
