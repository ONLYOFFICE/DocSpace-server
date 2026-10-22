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
/// Covers the folder listing filtered by metadata: the sub-folders, the mixed folders + files result,
/// the totals and the subtree ("pass-through") behaviour.
/// </summary>
[Trait("Category", "Metadata")]
public class FoldersMetadataSearchTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string ClientField = "Client";
    private const string AmountField = "Amount";
    private const string HiddenField = "Reference";

    #region Sub-folders

    [Fact]
    public async Task Folders_FilteredByMetadata_ReturnOnlyTheMatchingSubFolder()
    {
        var data = await ArrangeAsync();

        var content = await data.SearchAsync(data.Eq(ClientField, "ACME"), expectedFolders: [data.MatchingFolderId], expectedFiles: []);

        // the sub-folder holding another value and the one without metadata at all must be filtered out
        content.FolderIds().Should().Equal(data.MatchingFolderId);
    }

    [Fact]
    public async Task Folders_WithoutMetadataFilter_ReturnAllSubFolders()
    {
        var data = await ArrangeAsync();

        var content = await _foldersApi.GetFolderContentWithMetadataAsync(data.RoomId, cancellationToken: TestContext.Current.CancellationToken);

        content.FolderIds().Should().Contain([data.MatchingFolderId, data.OtherFolderId, data.BareFolderId]);
    }

    [Fact]
    public async Task Folders_FilteredByMetadata_ReportTheAssignedTemplates()
    {
        var data = await ArrangeAsync();

        var content = await data.SearchAsync(data.Eq(ClientField, "ACME"), expectedFolders: [data.MatchingFolderId], expectedFiles: []);

        content.Folders.Should().ContainSingle()
            .Which.AssignedMetadataTemplates.Should().Contain(data.TemplateId);
    }

    #endregion

    #region Mixed listing and totals

    [Fact]
    public async Task Folders_FilteredByMetadata_ReturnBothTheMatchingFolderAndTheMatchingFile()
    {
        var data = await ArrangeAsync();

        var content = await data.SearchAsync(
            data.Eq(ClientField, "ACME"),
            expectedFolders: [data.MatchingFolderId],
            expectedFiles: [data.MatchingFileId]);

        content.FolderIds().Should().Equal(data.MatchingFolderId);
        content.FileIds().Should().Equal(data.MatchingFileId);
    }

    [Fact]
    public async Task Folders_FilteredByMetadata_ReportTheTotalOfTheFilteredListing()
    {
        var data = await ArrangeAsync();

        var content = await data.SearchAsync(
            data.Eq(ClientField, "ACME"),
            expectedFolders: [data.MatchingFolderId],
            expectedFiles: [data.MatchingFileId]);

        // the folders count has its own query with a shortcut that bypasses the filters: if the filter is not
        // pushed into it, the total silently includes every sub-folder
        content.Total.Should().Be(2, "the total must count only the entries the filter left");
        content.Count.Should().Be(2);
    }

    [Fact]
    public async Task Folders_FilteredByMetadataWithNoMatches_ReportZeroTotal()
    {
        var data = await ArrangeAsync();

        var content = await data.SearchAsync(data.Eq(ClientField, "NoSuchClient"), expectedFolders: [], expectedFiles: []);

        content.Folders.Should().BeEmpty();
        content.Files.Should().BeEmpty();
        content.Total.Should().Be(0);
    }

    #endregion

    #region Template filter

    [Fact]
    public async Task Folders_FilteredByTemplateAlone_ReturnTheEntriesCarryingIt()
    {
        var data = await ArrangeAsync();

        // the template id without conditions used to be read for validation only, so the listing came back unfiltered
        var content = await _foldersApi.GetFolderContentWithMetadataAsync(data.RoomId, data.TemplateId, cancellationToken: TestContext.Current.CancellationToken);

        content.FolderIds().Should().BeEquivalentTo(new[] { data.MatchingFolderId, data.OtherFolderId, data.NestedFolderId }, "the bare folder has no template");
        content.FileIds().Should().BeEquivalentTo(new[] { data.MatchingFileId, data.OtherFileId, data.NestedFileId });
        content.Total.Should().Be(6, "the total must count only the entries carrying the template");
    }

    #endregion

    #region Subtree behaviour

    [Fact]
    public async Task Folders_FilteredByMetadataAlone_SearchThroughTheWholeSubtree()
    {
        var data = await ArrangeAsync();

        // the nested folder and file live two levels below the room; the endpoint defaults to withSubFolders=true,
        // so a metadata filter on its own must reach them — the subtree expansion used to require a text search
        var content = await data.SearchAsync(
            data.Eq(ClientField, "Nested"),
            expectedFolders: [data.NestedFolderId],
            expectedFiles: [data.NestedFileId]);

        content.FolderIds().Should().Equal(data.NestedFolderId);
        content.FileIds().Should().Equal(data.NestedFileId);
    }

    [Fact]
    public async Task Folders_FilteredByMetadataWithoutSubFolders_StayInTheCurrentFolder()
    {
        var data = await ArrangeAsync();

        var content = await data.SearchAsync(
            data.Eq(ClientField, "Nested"),
            expectedFolders: [],
            expectedFiles: [],
            withSubFolders: false);

        content.Folders.Should().BeEmpty("the nested entries are below the current folder");
        content.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task Folders_FilteredByNumberRangeAlone_SearchThroughTheWholeSubtree()
    {
        var data = await ArrangeAsync();

        var content = await data.SearchAsync(
            data.Range(AmountField, from: 900, to: 900),
            expectedFolders: [data.NestedFolderId],
            expectedFiles: []);

        content.FolderIds().Should().Equal(data.NestedFolderId);
    }

    [Fact]
    public async Task Folders_FilteredByNumberRangeWithAValue_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();

        // "from" together with "value" used to become the range [from, value] and answered an empty 200
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderContentWithMetadataAsync(data.RoomId, data.TemplateId, [data.RangeWithValue(AmountField, from: 10, value: 5)],
                cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400, "a number condition takes either a value or a range");
    }

    #endregion

    #region Index consistency

    [Fact]
    public async Task Folders_AfterTheTemplateIsUnassigned_AreNotReturnedByTheFilterRightAway()
    {
        var data = await ArrangeAsync();

        var before = await data.SearchAsync(data.Eq(ClientField, "ACME"), expectedFolders: [data.MatchingFolderId], expectedFiles: [data.MatchingFileId]);
        before.FolderIds().Should().Equal(data.MatchingFolderId);

        await _metadataApi.UnassignFolderTemplateAsync(data.MatchingFolderId, data.TemplateId, TestContext.Current.CancellationToken);

        // the stale document used to be deleted without a refresh, so a listing right after the unassign still returned
        // the folder; this is a single request on purpose, a retry would hide exactly that lag
        var after = await _foldersApi.GetFolderContentWithMetadataAsync(data.RoomId, data.TemplateId, [data.Eq(ClientField, "ACME")], cancellationToken: TestContext.Current.CancellationToken);

        after.FolderIds().Should().BeEmpty("the folder lost its values together with the template");
        after.FileIds().Should().Equal(new[] { data.MatchingFileId }, "the file keeps its own value");
    }

    #endregion

    #region Text search

    [Fact]
    public async Task Folders_TextSearchWithExtension_DoesNotReturnOtherExtensionsMatchedByMetadata()
    {
        var data = await ArrangeAsync();

        var marker = "Marker" + Guid.NewGuid().ToString()[..8];
        var sheet = await CreateFile($"sheet-{Guid.NewGuid().ToString()[..8]}.xlsx", data.RoomId);

        await _metadataApi.SetFolderCustomFieldsAsync(data.MatchingFolderId, new SetCustomFields([new CustomFieldRequest("Reference", marker)]), TestContext.Current.CancellationToken);
        await _metadataApi.SetFileCustomFieldsAsync(sheet.Id, new SetCustomFields([new CustomFieldRequest("Reference", marker)]), TestContext.Current.CancellationToken);

        var byText = await data.SearchByTextAsync(marker, expectedFolders: [data.MatchingFolderId], expectedFiles: [sheet.Id]);
        byText.FileIds().Should().Equal(new[] { sheet.Id }, "the text search must find the sheet by its system template value first");

        // the metadata ids used to be united with the title search after the extension was applied, so a .xlsx matched
        // by its metadata slipped into a listing limited to .docx
        var byTextAndExtension = await _foldersApi.GetFolderContentWithMetadataAsync(data.RoomId, filterValue: marker, extension: "docx", cancellationToken: TestContext.Current.CancellationToken);

        byTextAndExtension.FileIds().Should().BeEmpty("the sheet does not have the requested extension");
    }

    [Fact]
    public async Task Folders_TextSearch_FindsTheSubFolderByItsSystemTemplateValue()
    {
        var data = await ArrangeAsync();

        // a custom field goes to the system template, whose string values feed the global text of the metadata
        // document: the sub-folder must be found by that value rather than by its title
        var marker = "Marker" + Guid.NewGuid().ToString()[..8];

        await _metadataApi.SetFolderCustomFieldsAsync(data.MatchingFolderId, new SetCustomFields([new CustomFieldRequest("Reference", marker)]), TestContext.Current.CancellationToken);

        var content = await data.SearchByTextAsync(marker, expectedFolders: [data.MatchingFolderId], expectedFiles: []);

        content.FolderIds().Should().Equal(data.MatchingFolderId);

        // the page says where the text was met: a custom field is reported by its name alone, its template is hidden
        var match = content.Folders.Single().MetadataMatches.Should().ContainSingle().Which;
        match.FieldName.Should().Be("Reference");
        match.Value.Should().Be(marker);
        match.TemplateId.Should().BeNull();
        match.FieldId.Should().BeNull();
    }

    [Fact]
    public async Task Folders_TextSearch_FindsTheSubFolderByItsTemplateFieldValue()
    {
        var data = await ArrangeAsync();

        // the string values of every template feed the global text, not only the custom fields: the sub-folder is
        // found by the value of a template field, and the page names the template and the field it was met in
        var marker = "Marker" + Guid.NewGuid().ToString()[..8];

        await _metadataApi.SetFolderValuesAsync(data.MatchingFolderId, new SetMetadataValues([data.Value(ClientField, marker + " Corp")]), TestContext.Current.CancellationToken);

        var content = await data.SearchByTextAsync(marker, expectedFolders: [data.MatchingFolderId], expectedFiles: []);

        content.FolderIds().Should().Equal(data.MatchingFolderId);

        var match = content.Folders.Single().MetadataMatches.Should().ContainSingle().Which;
        match.TemplateId.Should().Be(data.TemplateId);
        match.FieldId.Should().Be(data.FieldId(ClientField));
        match.FieldName.Should().Be(ClientField);
        match.Value.Should().Be(marker + " Corp", "the whole stored value is reported, not the matching part");
    }

    [Fact]
    public async Task Folders_TextSearch_ReportsEveryMatchingFieldOfTheEntry()
    {
        var data = await ArrangeAsync();

        var marker = "Marker" + Guid.NewGuid().ToString()[..8];

        await _metadataApi.SetFileValuesAsync(data.MatchingFileId, new SetMetadataValues([data.Value(ClientField, marker)]), TestContext.Current.CancellationToken);
        await _metadataApi.SetFileCustomFieldsAsync(data.MatchingFileId, new SetCustomFields([new CustomFieldRequest("Reference", marker)]), TestContext.Current.CancellationToken);

        var content = await data.SearchByTextAsync(marker, expectedFolders: [], expectedFiles: [data.MatchingFileId]);

        content.FileIds().Should().Equal(data.MatchingFileId);

        var matches = content.Files.Single().MetadataMatches;
        matches.Should().HaveCount(2);
        matches.Should().ContainSingle(m => m.FieldName == ClientField && m.TemplateId == data.TemplateId);
        matches.Should().ContainSingle(m => m.FieldName == "Reference" && m.TemplateId == null);
    }

    [Fact]
    public async Task Folders_TextSearch_SkipsTheHiddenTemplateValues_UnlessAsked()
    {
        var data = await ArrangeAsync();

        var marker = "Marker" + Guid.NewGuid().ToString()[..8];

        await data.AssignHiddenValueAsync(data.OtherFolderId, marker);

        // the presence is seen first, with the hidden templates asked for, so the absence below is not the indexing lag
        var included = await data.SearchByTextAsync(marker, expectedFolders: [data.OtherFolderId], expectedFiles: [], includeHiddenMetadata: true);

        included.FolderIds().Should().Equal(data.OtherFolderId);
        included.Folders.Single().MetadataMatches.Should().ContainSingle(m => m.FieldName == HiddenField && m.Value == marker);

        var excluded = await data.SearchByTextAsync(marker, expectedFolders: [], expectedFiles: []);

        excluded.FolderIds().Should().BeEmpty("the web client never shows a hidden template, so it must not find by its values either");

        // the typed search takes the same flag in its body; the index already holds the value, so one read is enough
        var typedIncluded = await _foldersApi.SearchFolderWithMetadataAsync(data.RoomId, new FolderMetadataSearch(includeHiddenMetadata: true, filterValue: marker), TestContext.Current.CancellationToken);
        var typedExcluded = await _foldersApi.SearchFolderWithMetadataAsync(data.RoomId, new FolderMetadataSearch(filterValue: marker), TestContext.Current.CancellationToken);

        typedIncluded.FolderIds().Should().Equal(data.OtherFolderId);
        typedExcluded.FolderIds().Should().BeEmpty();
    }

    [Fact]
    public async Task Folders_TextSearch_ReportsTheHiddenTemplateMatches_OnlyWhenAsked()
    {
        var data = await ArrangeAsync();

        // the folder is found by its title either way; what differs is whether the page tells about the hidden value
        var marker = "Marker" + Guid.NewGuid().ToString()[..8];
        var folder = await CreateFolder($"{marker} folder", data.RoomId);

        await data.AssignHiddenValueAsync(folder.Id, marker);

        var excluded = await data.SearchByTextAsync(marker, expectedFolders: [folder.Id], expectedFiles: []);

        excluded.FolderIds().Should().Equal(folder.Id);
        excluded.Folders.Single().MetadataMatches.Should().BeNullOrEmpty("a field the web client never shows must not be reported as the place of the match");

        var included = await data.SearchByTextAsync(marker, expectedFolders: [folder.Id], expectedFiles: [], includeHiddenMetadata: true);

        included.Folders.Single().MetadataMatches.Should().ContainSingle(m => m.FieldName == HiddenField && m.Value == marker);
    }

    [Fact]
    public async Task Folders_HiddenTemplateCondition_IsAppliedWithoutAsking()
    {
        var data = await ArrangeAsync();

        var marker = "Marker" + Guid.NewGuid().ToString()[..8];

        var hidden = await data.AssignHiddenValueAsync(data.OtherFolderId, marker);

        // a condition naming the field of a hidden template is an explicit request for it, no flag needed
        var content = await data.SearchAsync(
            new MetadataFilterConditionRequest(fieldId: hidden.Field(HiddenField).Id, op: "eq", value: marker),
            expectedFolders: [data.OtherFolderId], expectedFiles: [], templateId: hidden.Id);

        content.FolderIds().Should().Equal(data.OtherFolderId);
    }

    [Fact]
    public async Task Folders_ListedWithoutText_CarryNoMetadataMatches()
    {
        var data = await ArrangeAsync();

        var content = await data.SearchAsync(data.Eq(ClientField, "ACME"), expectedFolders: [data.MatchingFolderId], expectedFiles: [data.MatchingFileId]);

        content.Folders.Should().OnlyContain(f => f.MetadataMatches == null, "the matches describe the text search, which this listing did not run");
        content.Files.Should().OnlyContain(f => f.MetadataMatches == null);
    }

    #endregion

    #region Access

    [Fact]
    public async Task Folders_AnonymousWithMetadataFilter_IsRefusedBeforeTheFilterIsValidated()
    {
        var data = await ArrangeAsync();

        await _filesClient.Authenticate(null);

        // the listing endpoint is anonymous; an unknown template used to answer 400 before the folder access was checked,
        // which told a caller without any access which template ids exist in the tenant
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderContentWithMetadataAsync(data.RoomId, metadataTemplateId: int.MaxValue, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401, "the access check comes before the filter validation");
    }

    #endregion

    #region Tenant without metadata

    [Fact]
    public async Task Folders_ListedBeforeTheFirstTemplate_ReportTheTemplateAssignedAfterwards()
    {
        await _filesClient.Authenticate(Owner);

        var suffix = Guid.NewGuid().ToString()[..8];
        var room = await CreateCustomRoom($"Fresh {suffix}");
        var folder = await CreateFolder($"Folder {suffix}", room.Id);

        // a listing of a tenant without templates skips the metadata queries and remembers that there is nothing to
        // ask for; the first template must drop that memory, or the assignments made after it stay invisible
        var before = await _foldersApi.GetFolderContentWithMetadataAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken);
        before.Folders.Should().ContainSingle().Which.AssignedMetadataTemplates.Should().BeNullOrEmpty();

        var template = await CreateTemplateAsync(_metadataApi, "Late " + suffix, [new MetadataFieldRequest(ClientField, MetadataFieldType.String)]);
        await _metadataApi.AssignFolderTemplatesAsync(folder.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);

        var after = await _foldersApi.GetFolderContentWithMetadataAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken);
        after.Folders.Should().ContainSingle().Which.AssignedMetadataTemplates.Should().Contain(template.Id);
    }

    [Fact]
    public async Task Folders_SearchedByTextBeforeTheFirstCustomField_FindTheValueSetAfterwards()
    {
        await _filesClient.Authenticate(Owner);

        var suffix = Guid.NewGuid().ToString()[..8];
        var room = await CreateCustomRoom($"Fresh {suffix}");
        var folder = await CreateFolder($"Folder {suffix}", room.Id);
        var marker = "Marker" + suffix;

        // a text search of a tenant without custom fields skips the metadata part and remembers that there is nothing
        // to look at; the first custom field creates the system template and must drop that memory
        var before = await _foldersApi.GetFolderContentWithMetadataAsync(room.Id, filterValue: marker, cancellationToken: TestContext.Current.CancellationToken);
        before.Folders.Should().BeEmpty();

        await _metadataApi.SetFolderCustomFieldsAsync(folder.Id, new SetCustomFields([new CustomFieldRequest("Reference", marker)]), TestContext.Current.CancellationToken);

        var after = await PollByTextAsync(room.Id, marker, expectedFolders: [folder.Id]);
        after.FolderIds().Should().Equal(folder.Id);
    }

    /// <summary>
    /// Requests the folder content by text until the expected folders are returned; the retry only absorbs the indexing lag.
    /// </summary>
    private async Task<FolderContentResponse> PollByTextAsync(int roomId, string text, int[] expectedFolders)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        while (true)
        {
            var content = await _foldersApi.GetFolderContentWithMetadataAsync(roomId, filterValue: text, cancellationToken: TestContext.Current.CancellationToken);

            if (content.FolderIds().Order().SequenceEqual(expectedFolders.Order()) || deadline.IsCancellationRequested)
            {
                return content;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }

    #endregion

    #region Arrange

    private async Task<FolderSearchData> ArrangeAsync()
    {
        await _filesClient.Authenticate(Owner);

        var suffix = Guid.NewGuid().ToString()[..8];

        var template = await CreateTemplateAsync(_metadataApi, "Contracts " + suffix,
        [
            new MetadataFieldRequest(ClientField, MetadataFieldType.String),
            new MetadataFieldRequest(AmountField, MetadataFieldType.Number)
        ]);

        var room = await CreateCustomRoom($"Room {suffix}");

        var matchingFolder = await CreateFolder($"Matching {suffix}", room.Id);
        var otherFolder = await CreateFolder($"Other {suffix}", room.Id);
        var bareFolder = await CreateFolder($"Bare {suffix}", room.Id);
        var nestedFolder = await CreateFolder($"Nested {suffix}", otherFolder.Id);

        var matchingFile = await CreateFile($"matching-{suffix}.docx", room.Id);
        var otherFile = await CreateFile($"other-{suffix}.docx", room.Id);
        var nestedFile = await CreateFile($"nested-{suffix}.docx", otherFolder.Id);

        var data = new FolderSearchData(_foldersApi, _metadataApi, template)
        {
            RoomId = room.Id,
            MatchingFolderId = matchingFolder.Id,
            OtherFolderId = otherFolder.Id,
            BareFolderId = bareFolder.Id,
            NestedFolderId = nestedFolder.Id,
            MatchingFileId = matchingFile.Id,
            OtherFileId = otherFile.Id,
            NestedFileId = nestedFile.Id
        };

        foreach (var folderId in new[] { matchingFolder.Id, otherFolder.Id, nestedFolder.Id })
        {
            await _metadataApi.AssignFolderTemplatesAsync(folderId, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        }

        foreach (var fileId in new[] { matchingFile.Id, otherFile.Id, nestedFile.Id })
        {
            await _metadataApi.AssignFileTemplatesAsync(fileId, new AssignMetadataTemplates([template.Id]), TestContext.Current.CancellationToken);
        }

        await _metadataApi.SetFolderValuesAsync(matchingFolder.Id, new SetMetadataValues([data.Value(ClientField, "ACME"), data.Amount(150)]), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderValuesAsync(otherFolder.Id, new SetMetadataValues([data.Value(ClientField, "Globex"), data.Amount(300)]), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderValuesAsync(nestedFolder.Id, new SetMetadataValues([data.Value(ClientField, "Nested"), data.Amount(900)]), TestContext.Current.CancellationToken);

        await _metadataApi.SetFileValuesAsync(matchingFile.Id, new SetMetadataValues([data.Value(ClientField, "ACME")]), TestContext.Current.CancellationToken);
        await _metadataApi.SetFileValuesAsync(otherFile.Id, new SetMetadataValues([data.Value(ClientField, "Globex")]), TestContext.Current.CancellationToken);
        await _metadataApi.SetFileValuesAsync(nestedFile.Id, new SetMetadataValues([data.Value(ClientField, "Nested")]), TestContext.Current.CancellationToken);

        // the values reach the index with a small lag; a negative search polled before that lag is over would
        // pass for the wrong reason, so every test starts from a state where the written values are searchable
        var indexed = await data.SearchAsync(data.Eq(ClientField, "Nested"), expectedFolders: [data.NestedFolderId], expectedFiles: [data.NestedFileId]);
        indexed.FolderIds().Should().Equal(new[] { data.NestedFolderId }, "the arranged values must be searchable before the test starts");
        indexed.FileIds().Should().Equal(data.NestedFileId);

        return data;
    }

    private static async Task<MetadataTemplateDto> CreateTemplateAsync(MetadataApi metadataApi, string name, List<MetadataFieldRequest> fields, bool visible = true)
    {
        return (await metadataApi.CreateTemplateAsync(new CreateMetadataTemplateRequestDto(name, visible, fields), TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>
    /// The arranged room with its sub-folders and files, plus the condition builders and the polling helper.
    /// </summary>
    private sealed class FolderSearchData(FoldersApi foldersApi, MetadataApi metadataApi, MetadataTemplateDto template)
    {
        public int TemplateId { get; } = template.Id;

        public int RoomId { get; init; }
        public int MatchingFolderId { get; init; }
        public int OtherFolderId { get; init; }
        public int BareFolderId { get; init; }
        public int NestedFolderId { get; init; }
        public int MatchingFileId { get; init; }
        public int OtherFileId { get; init; }
        public int NestedFileId { get; init; }

        public int FieldId(string fieldName)
        {
            return template.Field(fieldName).Id;
        }

        public MetadataValueRequest Value(string fieldName, string value)
        {
            return new MetadataValueRequest(template.Field(fieldName).Id, stringValue: value);
        }

        public MetadataValueRequest Amount(long value)
        {
            return new MetadataValueRequest(template.Field(AmountField).Id, numberValue: value);
        }

        public MetadataFilterConditionRequest Eq(string fieldName, string value)
        {
            return new MetadataFilterConditionRequest(fieldId: template.Field(fieldName).Id, op: "eq", value: value);
        }

        public MetadataFilterConditionRequest Range(string fieldName, long? from, long? to)
        {
            return new MetadataFilterConditionRequest(
                fieldId: template.Field(fieldName).Id,
                op: "range",
                from: from?.ToString(CultureInfo.InvariantCulture)!,
                to: to?.ToString(CultureInfo.InvariantCulture)!);
        }

        public MetadataFilterConditionRequest RangeWithValue(string fieldName, long from, long value)
        {
            return new MetadataFilterConditionRequest(
                fieldId: template.Field(fieldName).Id,
                op: "range",
                from: from.ToString(CultureInfo.InvariantCulture),
                value: value.ToString(CultureInfo.InvariantCulture));
        }

        public Task<FolderContentResponse> SearchAsync(MetadataFilterConditionRequest condition, int[] expectedFolders, int[] expectedFiles, bool? withSubFolders = null, int? templateId = null)
        {
            return PollAsync(templateId ?? TemplateId, [condition], filterValue: null, withSubFolders, expectedFolders, expectedFiles);
        }

        public Task<FolderContentResponse> SearchByTextAsync(string text, int[] expectedFolders, int[] expectedFiles, bool? includeHiddenMetadata = null)
        {
            return PollAsync(metadataTemplateId: null, conditions: null, filterValue: text, withSubFolders: null, expectedFolders, expectedFiles, includeHiddenMetadata);
        }

        /// <summary>
        /// Creates a hidden template with one string field, assigns it to the folder and stores the value in it.
        /// </summary>
        public async Task<MetadataTemplateDto> AssignHiddenValueAsync(int folderId, string value)
        {
            var hidden = await CreateTemplateAsync(metadataApi, "Hidden " + Guid.NewGuid().ToString()[..8],
                [new MetadataFieldRequest(HiddenField, MetadataFieldType.String)], visible: false);

            await metadataApi.AssignFolderTemplatesAsync(folderId, new AssignMetadataTemplates([hidden.Id], cascade: false), TestContext.Current.CancellationToken);
            await metadataApi.SetFolderValuesAsync(folderId,
                new SetMetadataValues([new MetadataValueRequest(hidden.Field(HiddenField).Id, stringValue: value)]), TestContext.Current.CancellationToken);

            return hidden;
        }

        /// <summary>
        /// Requests the folder content, retrying until the expected identifiers are returned. The metadata values
        /// are indexed right after they are written, the retry only absorbs the indexing lag.
        /// </summary>
        private async Task<FolderContentResponse> PollAsync(int? metadataTemplateId, MetadataFilterConditionRequest[]? conditions, string? filterValue, bool? withSubFolders, int[] expectedFolders, int[] expectedFiles, bool? includeHiddenMetadata = null)
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            while (true)
            {
                var content = await foldersApi.GetFolderContentWithMetadataAsync(RoomId, metadataTemplateId, conditions, filterValue, withSubFolders,
                    includeHiddenMetadata: includeHiddenMetadata, cancellationToken: TestContext.Current.CancellationToken);

                var matched = content.FolderIds().Order().SequenceEqual(expectedFolders.Order()) &&
                    content.FileIds().Order().SequenceEqual(expectedFiles.Order());

                if (matched || deadline.IsCancellationRequested)
                {
                    return content;
                }

                await Task.Delay(200, TestContext.Current.CancellationToken);
            }
        }
    }

    #endregion
}
