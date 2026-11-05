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

[Trait("Category", "Validation")]
[Trait("Feature", "AI/Threads")]
public class ThreadTitleLengthTests(AspireAppFixture fixture) : ThreadTestBase(fixture)
{
    private const int MaxTitleLength = 255;

    [Fact]
    public async Task Create_TitleAtLimit_Succeeds()
    {
        var title = new string('a', MaxTitleLength);

        var created = await CreateOwnThreadAsync(title);

        created.Title.Should().Be(title);
        (await GetThreadAsync(created.ThreadId)).Title.Should().Be(title);
    }

    [Theory]
    [InlineData(MaxTitleLength + 1)]
    [InlineData(1024)]
    [InlineData(5000)]
    public async Task Create_TitleOverLimit_Returns400(int length)
    {
        var profile = await GetProfileAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateThreadAsync(new string('a', length), profile.Id));

        exception.ErrorCode.Should().Be(400);
        (await ListThreadsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Update_TitleAtLimit_PersistsFullTitle()
    {
        var created = await CreateOwnThreadAsync("original");
        var title = new string('a', MaxTitleLength);

        var result = await _aiThreads.AiThreadsRenameAsync(
            new AiThreadsRenameRequest(created.ThreadId, title),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        (await GetThreadAsync(created.ThreadId)).Title.Should().Be(title);
    }

    [Theory]
    [InlineData(MaxTitleLength + 1)]
    [InlineData(1024)]
    [InlineData(5000)]
    public async Task Update_TitleOverLimit_Returns400(int length)
    {
        var created = await CreateOwnThreadAsync("original");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiThreads.AiThreadsRenameAsync(
                new AiThreadsRenameRequest(created.ThreadId, new string('a', length)),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        (await GetThreadAsync(created.ThreadId)).Title.Should().Be("original");
    }
}
