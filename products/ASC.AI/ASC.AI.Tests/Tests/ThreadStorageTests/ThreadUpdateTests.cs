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

[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Threads")]
public class ThreadUpdateTests(AspireAppFixture fixture) : ThreadTestBase(fixture)
{
    [Fact]
    public async Task Update_Existing_PersistsNewTitle()
    {
        var created = await CreateOwnThreadAsync("original");

        var result = await RenameAsync(created.ThreadId, "renamed");

        result.Success.Should().BeTrue();
        (await GetThreadAsync(created.ThreadId)).Title.Should().Be("renamed");
    }

    [Fact]
    public async Task Update_Existing_MovesLastEditDate()
    {
        var created = await CreateOwnThreadAsync("original");

        await Task.Delay(1100, TestContext.Current.CancellationToken);

        await RenameAsync(created.ThreadId, "renamed");

        (await GetThreadAsync(created.ThreadId)).LastEditDate.Should().BeGreaterThan(created.LastEditDate);
    }

    [Fact]
    public async Task Update_NonExisting_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await RenameAsync(Guid.NewGuid().ToString(), "renamed"));

        exception.ErrorCode.Should().Be(404);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Update_BlankTitle_Returns400(string title)
    {
        var created = await CreateOwnThreadAsync("original");

        // Same title rules as create: a blank title is refused rather than blanking the stored name (Bug 83094).
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await RenameAsync(created.ThreadId, title));

        exception.ErrorCode.Should().Be(400);
        (await GetThreadAsync(created.ThreadId)).Title.Should().Be("original");
    }

    // Internal contract: a null title as a no-op update has no public route (rename refuses a missing or blank title
    // with 400, Bug 83094 - see Update_BlankTitle_Returns400).
    [Fact]
    public async Task Update_NullTitle_Succeeds()
    {
        var created = await CreateThreadInternalAsync("original");

        using var response = await _ai.PutAsync(
            $"{ThreadsPath}/{created.Id}",
            new { title = (string?)null },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private Task<AiSuccessResponse> RenameAsync(string threadId, string title) =>
        _aiThreads.AiThreadsRenameAsync(new AiThreadsRenameRequest(threadId, title), TestContext.Current.CancellationToken);
}
