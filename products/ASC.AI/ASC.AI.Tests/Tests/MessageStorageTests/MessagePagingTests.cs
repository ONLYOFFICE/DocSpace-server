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

namespace ASC.AI.Tests.Tests.MessageStorageTests;

/// <summary>
/// <c>GET threads/read-messages</c> paged by <c>count</c>, and a cursor in a shape the route does not take. The
/// keyset walk with a proper cursor is in <see cref="MessageReadTests"/>.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Messages")]
public class MessagePagingTests(AspireAppFixture fixture) : MessagePart1TestBase(fixture)
{
    /// <summary>
    /// <c>count</c> used to be ignored: read-messages answered the whole history whatever was asked for. It now
    /// limits the page. A cursor that is a bare message id rather than the JSON sort key degrades to an unpaginated
    /// read (the route's documented latitude, not a bug), so it returns the same first page as no cursor at all.
    /// </summary>
    [Fact]
    [Trait("Bug", "82899")]
    public async Task ReadMessages_Count_LimitsThePage_AndABareIdCursorIsIgnored()
    {
        string[] texts = ["one", "two", "three", "four", "five", "six"];

        var threadId = await CreateMessageThreadAsync();
        foreach (var text in texts)
        {
            await AppendAsync(threadId, text);
        }

        // The unpaged read is the baseline: everything, oldest first.
        var all = await ReadMessagesAsync(threadId);
        all.Select(TextOf).Should().Equal(texts);

        var firstPage = await ReadMessagesAsync(threadId, 2);
        firstPage.Select(TextOf).Should().Equal(texts[..2], "count=2 returns the first two messages");

        var cursored = await ReadMessagesAsync(threadId, 2, all[1].Id);
        cursored.Select(TextOf).Should().Equal(texts[..2], "a bare id is not a cursor: the page is the same as without it");
    }
}
