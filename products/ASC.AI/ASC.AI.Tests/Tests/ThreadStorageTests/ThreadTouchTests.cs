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
public class ThreadTouchTests(AspireAppFixture fixture) : ThreadTestBase(fixture)
{
    [Fact]
    public async Task Touch_NonExisting_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiThreads.AiThreadsTouchAsync(
                new AiThreadsTouchRequest(Guid.NewGuid().ToString()),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task Touch_Existing_MovesLastEditDate()
    {
        var created = await CreateOwnThreadAsync();

        await Task.Delay(1100, TestContext.Current.CancellationToken);

        var result = await _aiThreads.AiThreadsTouchAsync(
            new AiThreadsTouchRequest(created.ThreadId),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        (await GetThreadAsync(created.ThreadId)).LastEditDate.Should().BeGreaterThan(created.LastEditDate);
    }

    [Fact]
    public async Task Touch_WithProfileId_AssignsProfile()
    {
        var created = await CreateOwnThreadAsync();
        var other = await CreateProfileAsync();

        var result = await _aiThreads.AiThreadsTouchAsync(
            new AiThreadsTouchRequest(created.ThreadId, other.Id),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        (await GetThreadAsync(created.ThreadId)).ProfileId.Should().Be(other.Id);
    }

    [Fact]
    public async Task Touch_NullProfileId_RemovesProfile()
    {
        var created = await CreateOwnThreadAsync();
        created.ProfileId.Should().NotBeNull();

        // Raw on purpose: an explicit "profileId": null is what unbinds the profile (the chat service turns it into
        // clearProfile), but AiThreadsTouchRequest declares profileId with EmitDefaultValue = false, so the SDK
        // drops a null and only ever sends a plain touch.
        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(
            $"{PublicThreadsPath}/touch",
            $$"""{ "threadId": "{{created.ThreadId}}", "profileId": null }""",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetThreadAsync(created.ThreadId)).ProfileId.Should().BeNull();
    }

    [Fact]
    public async Task Touch_NonExistentProfileId_Returns404()
    {
        var created = await CreateOwnThreadAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiThreads.AiThreadsTouchAsync(
                new AiThreadsTouchRequest(created.ThreadId, Guid.NewGuid().ToString()),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
        (await GetThreadAsync(created.ThreadId)).ProfileId.Should().Be(created.ProfileId);
    }
}
