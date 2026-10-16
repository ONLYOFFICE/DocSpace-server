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
// source code, which remains licensed under the GNU AGPL version 3.
//
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Files.Tests.Tests._10_Metadata;

/// <summary>
/// Covers the metadata filter on the section aliases that used to drop it on the floor: <c>@my</c>, <c>@trash</c>,
/// <c>@share</c> and <c>@forms</c> answered the unfiltered section with 200 while the numeric section id applied the
/// filter. The <c>@templates</c> alias refuses it the way the numeric id does.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataSectionAliasFiltersTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string ClientField = "Client";

    #region My documents

    [Fact]
    public async Task MyAlias_FilteredByTemplate_ReturnsOnlyTheEntriesCarryingIt()
    {
        var data = await ArrangeMyAsync();

        var content = await PollAsync(
            () => data.Api.GetSectionContentAsync("@my", data.TemplateId, cancellationToken: TestContext.Current.CancellationToken),
            c => c.FolderIds().Contains(data.FolderId) && c.FileIds().Contains(data.MatchingFileId) && !c.FileIds().Contains(data.BareFileId));

        content.FolderIds().Should().Equal(data.FolderId);
        content.FileIds().Should().Equal(new[] { data.MatchingFileId }, "the bare file carries no template");
        content.Total.Should().Be(2);
    }

    [Fact]
    public async Task MyAlias_FilteredByCondition_ReturnsOnlyTheMatchingEntry()
    {
        var data = await ArrangeMyAsync();

        var content = await PollAsync(
            () => data.Api.GetSectionContentAsync("@my", data.TemplateId, [Eq(data.FieldId, "ACME")], cancellationToken: TestContext.Current.CancellationToken),
            c => c.FolderIds().Contains(data.FolderId) && !c.FileIds().Contains(data.MatchingFileId));

        content.FolderIds().Should().Equal(data.FolderId);
        content.Files.Should().BeEmpty("the file holds another value");
        content.Total.Should().Be(1);
    }

    [Fact]
    public async Task MyAlias_WithoutMetadataFilter_ListsEverything()
    {
        var data = await ArrangeMyAsync();

        // guards the tests above: the filter is what narrows the listing
        var content = await data.Api.GetSectionContentAsync("@my", cancellationToken: TestContext.Current.CancellationToken);

        content.FolderIds().Should().Contain(data.FolderId);
        content.FileIds().Should().Contain(new[] { data.MatchingFileId, data.BareFileId });
    }

    [Fact]
    public async Task MyAlias_WithAnUnknownTemplate_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var api = new MetadataApiClient(_filesClient);

        using var response = await api.GetSectionContentResponseAsync("@my", int.MaxValue, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "the alias validates the filter the way the numeric section id does");
    }

    #endregion

    #region Trash

    [Fact]
    public async Task TrashAlias_FilteredByTemplate_ReturnsOnlyTheDeletedEntriesCarryingIt()
    {
        var data = await ArrangeTrashAsync();

        var content = await PollAsync(
            () => data.Api.GetSectionContentAsync("@trash", data.TemplateId, cancellationToken: TestContext.Current.CancellationToken),
            c => c.FolderIds().Contains(data.FolderId) && c.FileIds().Contains(data.MatchingFileId) && !c.FileIds().Contains(data.BareFileId));

        content.FolderIds().Should().Equal(data.FolderId);
        content.FileIds().Should().Equal(new[] { data.MatchingFileId }, "the bare deleted file carries no template");
    }

    [Fact]
    public async Task TrashAlias_FilteredByCondition_ReturnsTheMatchingEntry()
    {
        var data = await ArrangeTrashAsync();

        var content = await PollAsync(
            () => data.Api.GetSectionContentAsync("@trash", data.TemplateId, [Eq(data.FieldId, "Globex")], cancellationToken: TestContext.Current.CancellationToken),
            c => c.FileIds().Contains(data.MatchingFileId) && !c.FolderIds().Contains(data.FolderId));

        content.FileIds().Should().Equal(data.MatchingFileId);
        content.Folders.Should().BeEmpty("the deleted folder holds another value");
    }

    #endregion

    #region Shared with me

    [Fact]
    public async Task ShareAlias_FilteredByTemplate_ReturnsOnlyTheSharedEntriesCarryingIt()
    {
        var data = await ArrangeShareAsync();

        var content = await PollAsync(
            () => data.Api.GetSectionContentAsync("@share", data.TemplateId, cancellationToken: TestContext.Current.CancellationToken),
            c => c.FolderIds().Contains(data.FolderId) && c.FileIds().Contains(data.MatchingFileId));

        content.FolderIds().Should().Equal(data.FolderId);
        content.FileIds().Should().Equal(new[] { data.MatchingFileId }, "the bare shared file carries no template");
        content.Total.Should().Be(2);
    }

    [Fact]
    public async Task ShareAlias_FilteredByCondition_ReturnsTheMatchingFile()
    {
        var data = await ArrangeShareAsync();

        var content = await PollAsync(
            () => data.Api.GetSectionContentAsync("@share", data.TemplateId, [Eq(data.FieldId, "ACME")], cancellationToken: TestContext.Current.CancellationToken),
            c => c.FileIds().Contains(data.MatchingFileId));

        content.FileIds().Should().Equal(data.MatchingFileId);
        content.Folders.Should().BeEmpty("the shared folder holds another value");
        content.Total.Should().Be(1);
    }

    #endregion

    #region Forms

    [Fact]
    public async Task FormsAlias_FilteredByTemplate_ReturnsOnlyTheRoomsCarryingIt()
    {
        await _filesClient.Authenticate(Owner);

        var api = new MetadataApiClient(_filesClient);
        var suffix = Suffix();
        var template = await CreateTemplateAsync(api);
        var matching = await CreateFillingFormsRoom($"Form room {suffix}");
        var bare = await CreateFillingFormsRoom($"Bare form room {suffix}");

        await api.AssignFolderTemplatesAsync(matching.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);

        var content = await PollAsync(
            () => api.GetSectionContentAsync("@forms", template.Id, cancellationToken: TestContext.Current.CancellationToken),
            c => c.FolderIds().Contains(matching.Id) && !c.FolderIds().Contains(bare.Id));

        content.FolderIds().Should().Equal(new[] { matching.Id }, "the bare form room carries no template");
    }

    #endregion

    #region Sections that refuse the filter

    [Fact]
    public async Task TemplatesAlias_WithMetadataTemplateFilter_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var api = new MetadataApiClient(_filesClient);
        var template = await CreateTemplateAsync(api);

        using var response = await api.GetSectionContentResponseAsync("@templates", template.Id, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "the section cannot apply the filter and must not answer unfiltered");
    }

    #endregion

    #region Arrange

    /// <summary>
    /// In "My documents" of the owner: a folder with the value "ACME", a file with the value "Globex" and a bare file.
    /// </summary>
    private async Task<SectionData> ArrangeMyAsync()
    {
        await _filesClient.Authenticate(Owner);

        var api = new MetadataApiClient(_filesClient);
        var suffix = Suffix();
        var template = await CreateTemplateAsync(api);
        var fieldId = template.Field(ClientField).Id;

        var folder = await CreateFolderInMy($"Mine {suffix}", Owner);
        var matching = await CreateFileInMy($"matching-{suffix}.docx", Owner);
        var bare = await CreateFileInMy($"bare-{suffix}.docx", Owner);

        await api.AssignFolderTemplatesAsync(folder.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(folder.Id, [new MetadataValuePayload { FieldId = fieldId, StringValue = "ACME" }], TestContext.Current.CancellationToken);
        await SetFileValueAsync(api, matching.Id, template.Id, fieldId, "Globex");

        return new SectionData(api, template.Id, fieldId, folder.Id, matching.Id, bare.Id);
    }

    /// <summary>
    /// The same three entries, deleted into the owner's trash: the folder holds "ACME", the file "Globex", the third
    /// file nothing.
    /// </summary>
    private async Task<SectionData> ArrangeTrashAsync()
    {
        var data = await ArrangeMyAsync();

        var results = (await _filesOperationsApi.DeleteBatchItemsAsync(new DeleteBatchRequestDto
        {
            FolderIds = [new(data.FolderId)],
            FileIds = [new(data.MatchingFileId), new(data.BareFileId)],
            Immediately = false
        }, TestContext.Current.CancellationToken)).Response;

        if (results == null || results.Count == 0 || results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results?.FirstOrDefault()?.Id);
        }

        results.Should().NotContain(operation => !operation.Finished || !string.IsNullOrEmpty(operation.Error));

        return data;
    }

    /// <summary>
    /// The owner shares a folder holding "Globex", a file holding "ACME" and a bare file with a user; the alias is
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

        // the api client shares the HttpClient: from here on the section is read as the user
        await _filesClient.Authenticate(user);

        return new SectionData(api, template.Id, fieldId, folder.Id, matching.Id, bare.Id);
    }

    private static async Task<MetadataTemplateResponse> CreateTemplateAsync(MetadataApiClient api)
    {
        return await api.CreateTemplateAsync("Aliases " + Suffix(), [new MetadataFieldPayload { Name = ClientField, Type = 0 }], TestContext.Current.CancellationToken);
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
    /// The metadata documents become visible a moment after the values are written and the deletion moves the
    /// entries in the background, so the listing is polled on a deadline and the last observed state is returned for
    /// the assertion to show what was actually there.
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

    private sealed record SectionData(MetadataApiClient Api, int TemplateId, int FieldId, int FolderId, int MatchingFileId, int BareFileId);

    #endregion
}
