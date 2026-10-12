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

namespace ASC.AI.Tests.Tests.AssignmentsStorageTests;

[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Assignments")]
public class AssignmentsDeleteTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Delete_Existing_Removes()
    {
        var profile = await CreateProfileAsync();
        await CreateAssignmentAsync("Chat", profile.Id);

        using var response = await _ai.DeleteAsync($"{AssignmentsPath}/Chat", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ReadAssignmentAsync("Chat")).Should().BeNull();
    }

    [Fact]
    public async Task Delete_Existing_NotReturnedByReadAll()
    {
        var profile = await CreateProfileAsync();
        await CreateAssignmentAsync("Chat", profile.Id);

        using var response = await _ai.DeleteAsync($"{AssignmentsPath}/Chat", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ReadAllAssignmentsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_Twice_KeepsUnassigned()
    {
        var profile = await CreateProfileAsync();
        await CreateAssignmentAsync("Chat", profile.Id);

        using var first = await _ai.DeleteAsync($"{AssignmentsPath}/Chat", TestContext.Current.CancellationToken);
        using var second = await _ai.DeleteAsync($"{AssignmentsPath}/Chat", TestContext.Current.CancellationToken);

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ReadAssignmentAsync("Chat")).Should().BeNull();
    }

    [Fact]
    public async Task Delete_ThenAssignAgain_Persists()
    {
        var profile = await CreateProfileAsync();
        var anotherProfile = await CreateProfileAsync();
        await CreateAssignmentAsync("Chat", profile.Id);

        using var response = await _ai.DeleteAsync($"{AssignmentsPath}/Chat", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await CreateAssignmentAsync("Chat", anotherProfile.Id);

        (await ReadAssignmentAsync("Chat")).Should().Be(anotherProfile.Id);
    }

    [Fact]
    public async Task Delete_TypeWithDefault_NotReturnedByReadAll()
    {
        var profile = await CreateProfileAsync();
        await CreateAssignmentAsync("ImageGeneration", profile.Id);

        using var response = await _ai.DeleteAsync($"{AssignmentsPath}/ImageGeneration", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ReadAssignmentAsync("ImageGeneration")).Should().BeNull();
        (await ReadAllAssignmentsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_TypeWithDefault_ThenAssignAgain_Persists()
    {
        var profile = await CreateProfileAsync();
        var anotherProfile = await CreateProfileAsync();
        await CreateAssignmentAsync("ImageGeneration", profile.Id);

        using var response = await _ai.DeleteAsync($"{AssignmentsPath}/ImageGeneration", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await CreateAssignmentAsync("ImageGeneration", anotherProfile.Id);

        (await ReadAssignmentAsync("ImageGeneration")).Should().Be(anotherProfile.Id);
    }

    [Fact]
    public async Task Delete_NonExisting_Succeeds()
    {
        using var response = await _ai.DeleteAsync($"{AssignmentsPath}/Chat", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteMany_RemovesAllListedTypes()
    {
        var chatProfile = await CreateProfileAsync();
        var codeProfile = await CreateProfileAsync();
        var summarizationProfile = await CreateProfileAsync();

        await CreateAssignmentAsync("Chat", chatProfile.Id);
        await CreateAssignmentAsync("Code", codeProfile.Id);
        await CreateAssignmentAsync("Summarization", summarizationProfile.Id);

        using var response = await _ai.DeleteAsync(
            AssignmentsPath,
            new { actionTypes = new[] { "Chat", "Code" } },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var all = await ReadAllAssignmentsAsync();
        all.Should().HaveCount(1).And.ContainKey("Summarization");
    }

    [Fact]
    public async Task DeleteMany_ThenAssignAgain_Persists()
    {
        var chatProfile = await CreateProfileAsync();
        var codeProfile = await CreateProfileAsync();

        await CreateAssignmentAsync("Chat", chatProfile.Id);

        using var response = await _ai.DeleteAsync(
            AssignmentsPath,
            new { actionTypes = new[] { "Chat" } },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await CreateAssignmentAsync("Chat", codeProfile.Id);

        (await ReadAssignmentAsync("Chat")).Should().Be(codeProfile.Id);
    }

    [Fact]
    public async Task DeleteMany_EmptyList_Succeeds()
    {
        using var response = await _ai.DeleteAsync(
            AssignmentsPath,
            new { actionTypes = Array.Empty<string>() },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

}
