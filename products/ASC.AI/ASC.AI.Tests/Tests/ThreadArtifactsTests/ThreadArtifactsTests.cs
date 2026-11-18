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

namespace ASC.AI.Tests.Tests.ThreadArtifactsTests;

[Trait("Category", "Storage")]
[Trait("Feature", "AI/ThreadArtifacts")]
public class ThreadArtifactsTests(AspireAppFixture fixture) : ThreadFilesTestBase(fixture)
{
    [Fact]
    public async Task Upload_ByAuthor_AppearsInTheThreadList()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());

        var uploaded = await UploadArtifactAsync(thread.Id, "report.txt");

        var page = await ListArtifactsAsync(projectId, thread.Id);

        uploaded.Title.Should().Be("report.txt");
        page.Total.Should().Be(1);
        page.Files.Should().ContainSingle().Which.Id.Should().Be(uploaded.Id);
    }

    [Fact]
    public async Task Upload_Twice_LandsInTheSameFolder()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());

        var first = await UploadArtifactAsync(thread.Id, "first.txt");
        var second = await UploadArtifactAsync(thread.Id, "second.txt");

        var page = await ListArtifactsAsync(projectId, thread.Id);

        second.FolderId.Should().Be(first.FolderId);
        page.Total.Should().Be(2);
        page.Files.Select(f => f.Id).Should().BeEquivalentTo([first.Id, second.Id]);
    }

    [Fact]
    public async Task Upload_SameTitleTwice_KeepsBothFiles()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());

        var first = await UploadArtifactAsync(thread.Id, "report.txt");
        var second = await UploadArtifactAsync(thread.Id, "report.txt");

        var page = await ListArtifactsAsync(projectId, thread.Id);

        second.Id.Should().NotBe(first.Id);
        second.Title.Should().NotBe(first.Title);
        page.Total.Should().Be(2);
    }

    [Fact]
    public async Task Upload_InParallelToANewThread_CreatesOneFolder()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());

        var uploaded = await Task.WhenAll(
            UploadArtifactAsync(thread.Id, "first.txt"),
            UploadArtifactAsync(thread.Id, "second.txt"),
            UploadArtifactAsync(thread.Id, "third.txt"));

        var page = await ListArtifactsAsync(projectId, thread.Id);

        uploaded.Select(f => f.FolderId).Distinct().Should().ContainSingle();
        page.Total.Should().Be(3);
    }

    [Fact]
    public async Task Upload_ToThreadsOfTheSameProject_UsesAFolderPerThread()
    {
        var projectId = await CreateProjectAsync();
        var firstThread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var secondThread = await CreateThreadInternalAsync(entityId: projectId.ToString());

        var first = await UploadArtifactAsync(firstThread.Id, "first.txt");
        var second = await UploadArtifactAsync(secondThread.Id, "second.txt");

        var firstPage = await ListArtifactsAsync(projectId, firstThread.Id);
        var secondPage = await ListArtifactsAsync(projectId, secondThread.Id);

        second.FolderId.Should().NotBe(first.FolderId);
        firstPage.Files.Should().ContainSingle().Which.Id.Should().Be(first.Id);
        secondPage.Files.Should().ContainSingle().Which.Id.Should().Be(second.Id);
    }

    [Fact]
    public async Task Upload_ToAThreadOutsideAProject_Returns400()
    {
        var thread = await CreateThreadInternalAsync();

        using var response = await PostArtifactAsync(thread.Id, "report.txt");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_ToAnUnknownThread_Returns404()
    {
        using var response = await PostArtifactAsync(Guid.CreateVersion7(), "report.txt");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Upload_ToSomeoneElsesThread_Returns403AndAddsNothing()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var member = await InviteMemberAsync(projectId);

        await _aiClient.Authenticate(member);
        using var response = await PostArtifactAsync(thread.Id, "report.txt");
        await _aiClient.Authenticate(Owner);

        var page = await ListArtifactsAsync(projectId, thread.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        page.Total.Should().Be(0);
    }

    [Fact]
    public async Task Upload_AnEmptyFile_Returns400AndAddsNothing()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());

        using var response = await PostArtifactAsync(thread.Id, "empty.txt", []);

        var page = await ListArtifactsAsync(projectId, thread.Id);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        page.Total.Should().Be(0);
    }

    [Fact]
    public async Task Upload_AFileOverTheLimit_Returns400AndAddsNothing()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());

        using var response = await PostArtifactAsync(thread.Id, "large.txt", new byte[(100 * 1024 * 1024) + 1]);

        var page = await ListArtifactsAsync(projectId, thread.Id);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        page.Total.Should().Be(0);
    }

    [Fact]
    public async Task Upload_DoesNotExposeFolderIdsInTheThread()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        await UploadArtifactAsync(thread.Id, "report.txt");

        using var response = await _ai.GetAsync($"{ThreadsPath}/{thread.Id}", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotContainEquivalentOf("folderId");
    }

    [Fact]
    public async Task List_ThreadWithoutArtifacts_ReturnsAnEmptyPage()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());

        var page = await ListArtifactsAsync(projectId, thread.Id);

        page.Files.Should().BeEmpty();
        page.Total.Should().Be(0);
    }

    [Fact]
    public async Task List_WithCountAndStartIndex_ReturnsThatPage()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        await UploadArtifactAsync(thread.Id, "first.txt");
        await UploadArtifactAsync(thread.Id, "second.txt");

        var page = await ListArtifactsAsync(projectId, thread.Id, "?count=1&startIndex=1");

        page.Files.Should().ContainSingle();
        page.Total.Should().Be(2);
    }

    [Fact]
    public async Task List_SomeoneElsesThread_Returns404EvenToTheProjectOwner()
    {
        var projectId = await CreateProjectAsync();
        var member = await InviteMemberAsync(projectId);

        await _aiClient.Authenticate(member);
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var uploaded = await UploadArtifactAsync(thread.Id, "report.txt");
        await _aiClient.Authenticate(Owner);

        await FilesHttpClient.Authenticate(member);
        var memberPage = await ListArtifactsAsync(projectId, thread.Id);

        await FilesHttpClient.Authenticate(Owner);
        using var ownerResponse = await GetArtifactsAsync(projectId, thread.Id);

        memberPage.Files.Should().ContainSingle().Which.Id.Should().Be(uploaded.Id);
        ownerResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_ThreadOfAnotherProject_Returns404()
    {
        var projectId = await CreateProjectAsync();
        var otherProjectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        await UploadArtifactAsync(thread.Id, "report.txt");

        using var response = await GetArtifactsAsync(otherProjectId, thread.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_ByAMemberWithoutAccessToTheProject_Returns403()
    {
        var projectId = await CreateProjectAsync();
        var thread = await CreateThreadInternalAsync(entityId: projectId.ToString());
        var outsider = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);

        await FilesHttpClient.Authenticate(outsider);
        using var response = await GetArtifactsAsync(projectId, thread.Id);
        await FilesHttpClient.Authenticate(Owner);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
