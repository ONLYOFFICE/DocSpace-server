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

namespace ASC.Files.Tests.Tests._10_Metadata;

/// <summary>
/// Covers the metadata filter on the sections that gather their entries from tags and shares rather than from a parent
/// folder: "Recent", "Favorites" and "Shared with me". The filter used to be refused there with 400; now it is pushed
/// into the tag and share queries, on the numeric section id as well as on the <c>@recent</c> / <c>@favorites</c>
/// aliases. The "Templates" section still refuses it.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataSectionFiltersTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string ClientField = "Client";

    #region Recent

    [Fact]
    public async Task Recent_FilteredByTemplate_ReturnsOnlyTheFilesCarryingIt()
    {
        var data = await ArrangeRecentAsync();

        var content = await PollAsync(
            () => data.Api.GetFolderContentAsync(data.SectionId, data.TemplateId, cancellationToken: TestContext.Current.CancellationToken),
            c => c.FileIds().Contains(data.MatchingFileId) && c.FileIds().Contains(data.OtherFileId));

        content.FileIds().Should().BeEquivalentTo(new[] { data.MatchingFileId, data.OtherFileId }, "the bare file is in Recent too but carries no template");
        content.Total.Should().Be(2);
    }

    [Fact]
    public async Task Recent_FilteredByCondition_ReturnsTheMatchingFile()
    {
        var data = await ArrangeRecentAsync();

        var content = await PollAsync(
            () => data.Api.GetFolderContentAsync(data.SectionId, data.TemplateId, [Eq(data.FieldId, "ACME")], cancellationToken: TestContext.Current.CancellationToken),
            c => c.FileIds().Contains(data.MatchingFileId));

        content.FileIds().Should().Equal(new[] { data.MatchingFileId }, "the other file carries the template with another value");
    }

    [Fact]
    public async Task Recent_FilteredByConditionWithNoMatches_ReturnsNothing()
    {
        var data = await ArrangeRecentAsync();

        // wait for the tags first: the filtered listing must be empty because of the filter, not because the tags are not there yet
        await PollAsync(
            () => data.Api.GetFolderContentAsync(data.SectionId, cancellationToken: TestContext.Current.CancellationToken),
            c => c.FileIds().Contains(data.MatchingFileId) && c.FileIds().Contains(data.OtherFileId));

        var content = await data.Api.GetFolderContentAsync(data.SectionId, data.TemplateId, [Eq(data.FieldId, "NoSuchClient")], cancellationToken: TestContext.Current.CancellationToken);

        content.Files.Should().BeEmpty();
        content.Total.Should().Be(0);
    }

    [Fact]
    public async Task RecentAlias_FilteredByTemplate_ReturnsOnlyTheFilesCarryingIt()
    {
        var data = await ArrangeRecentAsync();

        var content = await PollAsync(
            () => data.Api.GetSectionContentAsync("@recent", data.TemplateId, cancellationToken: TestContext.Current.CancellationToken),
            c => c.FileIds().Contains(data.MatchingFileId) && c.FileIds().Contains(data.OtherFileId));

        content.FileIds().Should().BeEquivalentTo(new[] { data.MatchingFileId, data.OtherFileId }, "the bare file carries no template");
    }

    [Fact]
    public async Task RecentAlias_WithAnUnknownTemplate_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var api = new MetadataApiClient(_filesClient);

        using var response = await api.GetSectionContentResponseAsync("@recent", int.MaxValue, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "the alias validates the filter the same way the folder listing does");
    }

    #endregion

    #region Favorites

    [Fact]
    public async Task Favorites_FilteredByTemplate_ReturnsTheFolderAndTheFileCarryingIt()
    {
        var data = await ArrangeFavoritesAsync();

        var content = await PollAsync(
            () => data.Api.GetFolderContentAsync(data.SectionId, data.TemplateId, cancellationToken: TestContext.Current.CancellationToken),
            c => c.FolderIds().Contains(data.FolderId) && c.FileIds().Contains(data.MatchingFileId));

        content.FolderIds().Should().Equal(data.FolderId);
        content.FileIds().Should().Equal(new[] { data.MatchingFileId }, "the bare favorite file carries no template");
        content.Total.Should().Be(2);
    }

    [Fact]
    public async Task Favorites_FilteredByCondition_ReturnsOnlyTheMatchingEntry()
    {
        var data = await ArrangeFavoritesAsync();

        var content = await PollAsync(
            () => data.Api.GetFolderContentAsync(data.SectionId, data.TemplateId, [Eq(data.FieldId, "ACME")], cancellationToken: TestContext.Current.CancellationToken),
            c => c.FolderIds().Contains(data.FolderId));

        content.FolderIds().Should().Equal(data.FolderId);
        content.Files.Should().BeEmpty("the favorite file holds another value");
        content.Total.Should().Be(1);
    }

    [Fact]
    public async Task FavoritesAlias_FilteredByCondition_ReturnsOnlyTheMatchingEntry()
    {
        var data = await ArrangeFavoritesAsync();

        var content = await PollAsync(
            () => data.Api.GetSectionContentAsync("@favorites", data.TemplateId, [Eq(data.FieldId, "Globex")], cancellationToken: TestContext.Current.CancellationToken),
            c => c.FileIds().Contains(data.MatchingFileId));

        content.FileIds().Should().Equal(data.MatchingFileId);
        content.Folders.Should().BeEmpty("the favorite folder holds another value");
    }

    #endregion

    #region Shared with me

    [Fact]
    public async Task Share_FilteredByTemplate_ReturnsOnlyTheSharedEntriesCarryingIt()
    {
        var data = await ArrangeShareAsync();

        var content = await PollAsync(
            () => data.Api.GetFolderContentAsync(data.SectionId, data.TemplateId, cancellationToken: TestContext.Current.CancellationToken),
            c => c.FolderIds().Contains(data.FolderId) && c.FileIds().Contains(data.MatchingFileId));

        content.FolderIds().Should().Equal(data.FolderId);
        content.FileIds().Should().Equal(new[] { data.MatchingFileId }, "the bare shared file carries no template");
        content.Total.Should().Be(2);
    }

    [Fact]
    public async Task Share_FilteredByCondition_ReturnsTheMatchingFile()
    {
        var data = await ArrangeShareAsync();

        var content = await PollAsync(
            () => data.Api.GetFolderContentAsync(data.SectionId, data.TemplateId, [Eq(data.FieldId, "ACME")], cancellationToken: TestContext.Current.CancellationToken),
            c => c.FileIds().Contains(data.MatchingFileId));

        content.FileIds().Should().Equal(data.MatchingFileId);
        content.Folders.Should().BeEmpty("the shared folder holds another value");
        content.Total.Should().Be(1);
    }

    [Fact]
    public async Task Share_WithoutMetadataFilter_ListsEverySharedEntry()
    {
        var data = await ArrangeShareAsync();

        // guards the tests above: the filter is what narrows the listing, not the share itself
        var content = await data.Api.GetFolderContentAsync(data.SectionId, cancellationToken: TestContext.Current.CancellationToken);

        content.FolderIds().Should().Equal(data.FolderId);
        content.FileIds().Should().BeEquivalentTo(new[] { data.MatchingFileId, data.OtherFileId });
    }

    #endregion

    #region Sections that still refuse the filter

    [Fact]
    public async Task Templates_WithMetadataTemplateFilter_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var api = new MetadataApiClient(_filesClient);
        var template = await CreateTemplateAsync(api);
        var templatesId = await GetSectionRootIdAsync("api/2.0/files/@templates");

        using var response = await api.GetFolderContentResponseAsync(templatesId, metadataTemplateId: template.Id, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "the section cannot apply the filter and must not pretend it did");
    }

    #endregion

    #region Arrange

    /// <summary>
    /// Two files of the owner in Recent: one carries the template with the value "ACME", the other the template with
    /// the value "Globex"; a third, bare, file has no metadata at all.
    /// </summary>
    private async Task<SectionData> ArrangeRecentAsync()
    {
        await _filesClient.Authenticate(Owner);

        var api = new MetadataApiClient(_filesClient);
        var suffix = Suffix();
        var template = await CreateTemplateAsync(api);
        var fieldId = template.Field(ClientField).Id;

        var matching = await CreateFileInMy($"matching-{suffix}.docx", Owner);
        var other = await CreateFileInMy($"other-{suffix}.docx", Owner);
        var bare = await CreateFileInMy($"bare-{suffix}.docx", Owner);

        await SetFileValueAsync(api, matching.Id, template.Id, fieldId, "ACME");
        await SetFileValueAsync(api, other.Id, template.Id, fieldId, "Globex");

        foreach (var fileId in new[] { matching.Id, other.Id, bare.Id })
        {
            await _filesApi.AddFileToRecentAsync(fileId, TestContext.Current.CancellationToken);
        }

        var sectionId = await GetFolderIdAsync(FolderType.Recent, Owner);

        return new SectionData(api, template.Id, fieldId, sectionId, 0, matching.Id, other.Id);
    }

    /// <summary>
    /// A favorite folder with the value "ACME", a favorite file with the value "Globex" and a bare favorite file.
    /// </summary>
    private async Task<SectionData> ArrangeFavoritesAsync()
    {
        await _filesClient.Authenticate(Owner);

        var api = new MetadataApiClient(_filesClient);
        var suffix = Suffix();
        var template = await CreateTemplateAsync(api);
        var fieldId = template.Field(ClientField).Id;

        var folder = await CreateFolderInMy($"Favorite {suffix}", Owner);
        var matching = await CreateFileInMy($"matching-{suffix}.docx", Owner);
        var bare = await CreateFileInMy($"bare-{suffix}.docx", Owner);

        await api.AssignFolderTemplatesAsync(folder.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(folder.Id, [new MetadataValuePayload { FieldId = fieldId, StringValue = "ACME" }], TestContext.Current.CancellationToken);
        await SetFileValueAsync(api, matching.Id, template.Id, fieldId, "Globex");

        await _filesOperationsApi.AddFavoritesAsync(new BaseBatchRequestDto
        {
            FolderIds = [new BaseBatchRequestDtoAllOfFolderIds(folder.Id)],
            FileIds = [new BaseBatchRequestDtoAllOfFileIds(matching.Id), new BaseBatchRequestDtoAllOfFileIds(bare.Id)]
        }, TestContext.Current.CancellationToken);

        var sectionId = await GetFolderIdAsync(FolderType.Favorites, Owner);

        return new SectionData(api, template.Id, fieldId, sectionId, folder.Id, matching.Id, bare.Id);
    }

    /// <summary>
    /// The owner shares a folder holding "Globex", a file holding "ACME" and a bare file with a user; the listing is
    /// read as that user, whose "Shared with me" section is the one under test.
    /// </summary>
    private async Task<SectionData> ArrangeShareAsync()
    {
        await _filesClient.Authenticate(Owner);

        var api = new MetadataApiClient(_filesClient);
        var suffix = Suffix();
        var template = await CreateTemplateAsync(api);
        var fieldId = template.Field(ClientField).Id;
        var user = await InviteContact(EmployeeType.User);

        var folder = await CreateFolderInMy($"Shared {suffix}", Owner);
        var matching = await CreateFileInMy($"matching-{suffix}.docx", Owner);
        var bare = await CreateFileInMy($"bare-{suffix}.docx", Owner);

        await api.AssignFolderTemplatesAsync(folder.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(folder.Id, [new MetadataValuePayload { FieldId = fieldId, StringValue = "Globex" }], TestContext.Current.CancellationToken);
        await SetFileValueAsync(api, matching.Id, template.Id, fieldId, "ACME");

        var share = new SecurityInfoSimpleRequestDto { Share = [new FileShareParams { ShareTo = user.Id, Access = FileShare.Read }] };

        await _sharingApi.SetFolderSecurityInfoAsync(folder.Id, share, TestContext.Current.CancellationToken);
        await _sharingApi.SetFileSecurityInfoAsync(matching.Id, share, TestContext.Current.CancellationToken);
        await _sharingApi.SetFileSecurityInfoAsync(bare.Id, share, TestContext.Current.CancellationToken);

        // GetShareFolderIdAsync authenticates the client as the user; the api client shares that HttpClient
        var sectionId = await GetShareFolderIdAsync(user);

        return new SectionData(api, template.Id, fieldId, sectionId, folder.Id, matching.Id, bare.Id);
    }

    private static async Task<MetadataTemplateResponse> CreateTemplateAsync(MetadataApiClient api)
    {
        return await api.CreateTemplateAsync("Sections " + Suffix(), [new MetadataFieldPayload { Name = ClientField, Type = 0 }], TestContext.Current.CancellationToken);
    }

    private static async Task SetFileValueAsync(MetadataApiClient api, int fileId, int templateId, int fieldId, string value)
    {
        await api.AssignFileTemplatesAsync(fileId, [templateId], TestContext.Current.CancellationToken);
        await api.SetFileValuesAsync(fileId, [new MetadataValuePayload { FieldId = fieldId, StringValue = value }], TestContext.Current.CancellationToken);
    }

    private static object Eq(int fieldId, string value)
    {
        return new { fieldId, op = "eq", value };
    }

    /// <summary>
    /// The Recent and Favorites tags are written after the request that adds them returns, and the metadata documents
    /// become visible a moment after the values are written, so the listing is polled on a deadline and the last observed
    /// state is returned for the assertion to show what was actually there.
    /// </summary>
    private static async Task<FolderContentResponse> PollAsync(Func<Task<FolderContentResponse>> read, Func<FolderContentResponse, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var content = await read();

            if (until(content) || DateTime.UtcNow >= deadline)
            {
                return content;
            }

            await Task.Delay(300, TestContext.Current.CancellationToken);
        }
    }

    private static string Suffix()
    {
        return Guid.NewGuid().ToString()[..8];
    }

    /// <param name="FolderId">the folder under test, 0 for Recent which lists files only</param>
    /// <param name="OtherFileId">the second file: the one with another value in Recent, the bare one in Favorites and Share</param>
    private sealed record SectionData(MetadataApiClient Api, int TemplateId, int FieldId, int SectionId, int FolderId, int MatchingFileId, int OtherFileId);

    #endregion
}
