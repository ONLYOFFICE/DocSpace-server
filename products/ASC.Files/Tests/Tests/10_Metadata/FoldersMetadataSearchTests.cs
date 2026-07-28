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

        var content = await data.Api.GetFolderContentAsync(data.RoomId, cancellationToken: TestContext.Current.CancellationToken);

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

    #endregion

    #region Text search

    [Fact]
    public async Task Folders_TextSearch_FindsTheSubFolderByItsSystemTemplateValue()
    {
        var data = await ArrangeAsync();

        // a custom field goes to the system template, whose string values feed the global text of the metadata
        // document: the sub-folder must be found by that value rather than by its title
        var marker = "Marker" + Guid.NewGuid().ToString()[..8];

        await data.Api.AddFolderCustomFieldAsync(data.MatchingFolderId, "Reference", marker, TestContext.Current.CancellationToken);

        var content = await data.SearchByTextAsync(marker, expectedFolders: [data.MatchingFolderId], expectedFiles: []);

        content.FolderIds().Should().Equal(data.MatchingFolderId);
    }

    #endregion

    #region Arrange

    private async Task<FolderSearchData> ArrangeAsync()
    {
        await _filesClient.Authenticate(Owner);

        var api = new MetadataApiClient(_filesClient);
        var suffix = Guid.NewGuid().ToString()[..8];

        var template = await api.CreateTemplateAsync("Contracts " + suffix,
        [
            new MetadataFieldPayload { Name = ClientField, Type = 0 },
            new MetadataFieldPayload { Name = AmountField, Type = 2 }
        ], TestContext.Current.CancellationToken);

        var room = await CreateCustomRoom($"Room {suffix}");

        var matchingFolder = await CreateFolder($"Matching {suffix}", room.Id);
        var otherFolder = await CreateFolder($"Other {suffix}", room.Id);
        var bareFolder = await CreateFolder($"Bare {suffix}", room.Id);
        var nestedFolder = await CreateFolder($"Nested {suffix}", otherFolder.Id);

        var matchingFile = await CreateFile($"matching-{suffix}.docx", room.Id);
        var otherFile = await CreateFile($"other-{suffix}.docx", room.Id);
        var nestedFile = await CreateFile($"nested-{suffix}.docx", otherFolder.Id);

        var data = new FolderSearchData(api, template)
        {
            RoomId = room.Id,
            MatchingFolderId = matchingFolder.Id,
            OtherFolderId = otherFolder.Id,
            BareFolderId = bareFolder.Id,
            NestedFolderId = nestedFolder.Id,
            MatchingFileId = matchingFile.Id,
            NestedFileId = nestedFile.Id
        };

        foreach (var folderId in new[] { matchingFolder.Id, otherFolder.Id, nestedFolder.Id })
        {
            await api.AssignFolderTemplatesAsync(folderId, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        }

        foreach (var fileId in new[] { matchingFile.Id, otherFile.Id, nestedFile.Id })
        {
            await api.AssignFileTemplatesAsync(fileId, [template.Id], TestContext.Current.CancellationToken);
        }

        await api.SetFolderValuesAsync(matchingFolder.Id, [data.Value(ClientField, "ACME"), data.Amount(150)], TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(otherFolder.Id, [data.Value(ClientField, "Globex"), data.Amount(300)], TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(nestedFolder.Id, [data.Value(ClientField, "Nested"), data.Amount(900)], TestContext.Current.CancellationToken);

        await api.SetFileValuesAsync(matchingFile.Id, [data.Value(ClientField, "ACME")], TestContext.Current.CancellationToken);
        await api.SetFileValuesAsync(otherFile.Id, [data.Value(ClientField, "Globex")], TestContext.Current.CancellationToken);
        await api.SetFileValuesAsync(nestedFile.Id, [data.Value(ClientField, "Nested")], TestContext.Current.CancellationToken);

        return data;
    }

    /// <summary>
    /// The arranged room with its sub-folders and files, plus the condition builders and the polling helper.
    /// </summary>
    private sealed class FolderSearchData(MetadataApiClient api, MetadataTemplateResponse template)
    {
        public MetadataApiClient Api { get; } = api;
        public int TemplateId { get; } = template.Id;

        public int RoomId { get; init; }
        public int MatchingFolderId { get; init; }
        public int OtherFolderId { get; init; }
        public int BareFolderId { get; init; }
        public int NestedFolderId { get; init; }
        public int MatchingFileId { get; init; }
        public int NestedFileId { get; init; }

        public MetadataValuePayload Value(string fieldName, string value)
        {
            return new MetadataValuePayload { FieldId = template.Field(fieldName).Id, StringValue = value };
        }

        public MetadataValuePayload Amount(long value)
        {
            return new MetadataValuePayload { FieldId = template.Field(AmountField).Id, NumberValue = value };
        }

        public object Eq(string fieldName, string value)
        {
            return new { fieldId = template.Field(fieldName).Id, op = "eq", value };
        }

        public object Range(string fieldName, long? from, long? to)
        {
            return new
            {
                fieldId = template.Field(fieldName).Id,
                op = "range",
                from = from?.ToString(CultureInfo.InvariantCulture),
                to = to?.ToString(CultureInfo.InvariantCulture)
            };
        }

        public Task<FolderContentResponse> SearchAsync(object condition, int[] expectedFolders, int[] expectedFiles, bool? withSubFolders = null)
        {
            return PollAsync(TemplateId, [condition], filterValue: null, withSubFolders, expectedFolders, expectedFiles);
        }

        public Task<FolderContentResponse> SearchByTextAsync(string text, int[] expectedFolders, int[] expectedFiles)
        {
            return PollAsync(metadataTemplateId: null, conditions: null, filterValue: text, withSubFolders: null, expectedFolders, expectedFiles);
        }

        /// <summary>
        /// Requests the folder content, retrying until the expected identifiers are returned. The metadata values
        /// are indexed right after they are written, the retry only absorbs the indexing lag.
        /// </summary>
        private async Task<FolderContentResponse> PollAsync(int? metadataTemplateId, object[]? conditions, string? filterValue, bool? withSubFolders, int[] expectedFolders, int[] expectedFiles)
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            while (true)
            {
                var content = await Api.GetFolderContentAsync(RoomId, metadataTemplateId, conditions, filterValue, withSubFolders, TestContext.Current.CancellationToken);

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
