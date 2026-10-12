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

namespace ASC.AI.Tests.Tests.ThreadStorageTests;

[Collection("Test Collection")]
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Threads")]
public class ThreadSearchTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Search_BySubstring_ReturnsOnlyMatching()
    {
        var report = await CreateThreadAsync("Annual report");
        await CreateThreadAsync("Meeting notes");

        var page = await ReadThreadsPageAsync(10, query: "repo");

        page.Items.Should().ContainSingle(t => t.Id == report.Id);
    }

    [Fact]
    public async Task Search_IsCaseInsensitive()
    {
        var report = await CreateThreadAsync("Annual report");

        var page = await ReadThreadsPageAsync(10, query: "REPORT");

        page.Items.Should().ContainSingle(t => t.Id == report.Id);
    }

    [Fact]
    public async Task Search_MultipleTokens_MatchAllInAnyOrder()
    {
        var matching = await CreateThreadAsync("Annual report 2026");
        await CreateThreadAsync("Annual report 2025");

        var page = await ReadThreadsPageAsync(10, query: "2026 report");

        page.Items.Should().ContainSingle(t => t.Id == matching.Id);
    }

    [Fact]
    public async Task Search_EscapesLikeWildcards()
    {
        var withPercent = await CreateThreadAsync("100% complete");
        await CreateThreadAsync("fully complete");

        var page = await ReadThreadsPageAsync(10, query: "%");

        page.Items.Should().ContainSingle(t => t.Id == withPercent.Id);
    }

    [Fact]
    public async Task Search_NoMatches_ReturnsEmpty()
    {
        await CreateThreadAsync("Annual report");

        var page = await ReadThreadsPageAsync(10, query: "nonexistent");

        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_WithEntityId_ReturnsOnlyScopedThreads()
    {
        var roomId = await CreateRoomAsync();
        await CreateThreadAsync("Annual report");
        var scoped = await CreateThreadAsync("Annual report", entityId: roomId.ToString());

        var page = await ReadThreadsPageAsync(10, entityId: roomId.ToString(), query: "report");

        page.Items.Should().ContainSingle(t => t.Id == scoped.Id);
    }
}
