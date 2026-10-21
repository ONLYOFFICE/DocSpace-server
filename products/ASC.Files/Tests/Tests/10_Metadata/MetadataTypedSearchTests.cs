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
// design elements, icons, logos, and text, are the property of
// Ascensio System SIA and are protected by copyright and trademark laws.
// These elements may not be used in derivative works or modified in any way
// without prior written permission from Ascensio System SIA.
//
// Pursuant to Section 7 § 3(b) of the License you must retain the original
// Product logo when distributing the program. Pursuant to Section 7 § 3(e) we
// decline to grant you any rights under trademark law for use of our trademarks.
//
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Files.Tests.Tests._10_Metadata;

/// <summary>
/// Covers the typed search endpoints: the metadata filter as a request body, for the clients that build the conditions as
/// objects instead of the JSON string the listings take in the query. Both roads must validate and answer identically.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataTypedSearchTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string ClientField = "Client";

    [Fact]
    public async Task SearchFolder_ByCondition_ReturnsTheMatchingEntries()
    {
        var data = await ArrangeAsync();

        var body = new FolderMetadataSearch(data.TemplateId, [new MetadataFilterConditionRequest(fieldId: data.FieldId, op: "eq", value: "ACME")]);

        var content = await PollFolderSearchAsync(data.RoomId, body, data.MatchingFolderId, data.MatchingFileId);

        content.FolderIds().Should().Equal(data.MatchingFolderId);
        content.FileIds().Should().Equal(data.MatchingFileId);
    }

    [Fact]
    public async Task SearchFolder_AgreesWithTheListing()
    {
        var data = await ArrangeAsync();

        var condition = new MetadataFilterConditionRequest(fieldId: data.FieldId, op: "eq", value: "ACME");

        var typed = await PollFolderSearchAsync(data.RoomId, new FolderMetadataSearch(data.TemplateId, [condition]), data.MatchingFolderId, data.MatchingFileId);
        var listed = await _foldersApi.GetFolderContentWithMetadataAsync(data.RoomId, data.TemplateId, [condition], cancellationToken: TestContext.Current.CancellationToken);

        typed.FolderIds().Should().Equal(listed.FolderIds());
        typed.FileIds().Should().Equal(listed.FileIds());
        typed.Total.Should().Be(listed.Total);
    }

    [Fact]
    public async Task SearchFolder_WithAnUnknownField_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();

        var body = new FolderMetadataSearch(metadataFilters: [new MetadataFilterConditionRequest(fieldId: int.MaxValue, op: "eq", value: "ACME")]);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SearchFolderWithMetadataAsync(data.RoomId, body, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400, "the typed road validates the conditions the same way the listing does");
    }

    [Fact]
    public async Task SearchFolder_WithANumberGivenAsAJsonNumber_IsAccepted()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();

        var template = await CreateTemplateAsync("Amount " + suffix, [new MetadataFieldRequest("Amount", MetadataFieldType.Number)]);
        var fieldId = template.Field("Amount").Id;
        var room = await CreateCustomRoom($"Amount {suffix}");
        var matching = await CreateFolder($"Five {suffix}", room.Id);
        var other = await CreateFolder($"Seven {suffix}", room.Id);

        await _metadataApi.AssignFolderTemplatesAsync(matching.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.AssignFolderTemplatesAsync(other.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderValuesAsync(matching.Id, new SetMetadataValues([new MetadataValueRequest(fieldId, numberValue: 5)]), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderValuesAsync(other.Id, new SetMetadataValues([new MetadataValueRequest(fieldId, numberValue: 7)]), TestContext.Current.CancellationToken);

        // the value and the bounds are declared as strings; a client building the condition from a number used to get a 400
        var equal = new { metadataTemplateId = template.Id, metadataFilters = new object[] { new { fieldId, op = "eq", value = 5 } } };
        var range = new { metadataTemplateId = template.Id, metadataFilters = new object[] { new { fieldId, op = "range", from = 1, to = 6 } } };

        // raw: MetadataFilterConditionRequest declares value/from/to as strings, so the SDK cannot send a JSON number
        var raw = new RawApiClient(_filesClient);

        var byValue = await PollFolderSearchAsync(() => SearchFolderRawAsync(raw, room.Id, equal), c => c.FolderIds().Contains(matching.Id));
        byValue.FolderIds().Should().Equal(matching.Id);

        var byRange = await PollFolderSearchAsync(() => SearchFolderRawAsync(raw, room.Id, range), c => c.FolderIds().Contains(matching.Id));
        byRange.FolderIds().Should().Equal(matching.Id);

        // the listing takes the same conditions as a JSON string in the query, read by the same converter; the string is
        // built here because the listing helper serializes the typed conditions, whose value is a JSON string
        var listed = await _foldersApi.GetFolderContentWithMetadataAsync(room.Id,
            metadataTemplateId: template.Id,
            rawMetadataFilters: JsonSerializer.Serialize(new object[] { new { fieldId, op = "eq", value = 5 } }),
            cancellationToken: TestContext.Current.CancellationToken);
        listed.FolderIds().Should().Equal(matching.Id);
    }

    [Fact]
    public async Task SearchFolder_OnTheTemplatesSection_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();
        var templatesId = await GetSectionRootIdAsync("api/2.0/files/@templates");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SearchFolderWithMetadataAsync(templatesId, new FolderMetadataSearch(data.TemplateId), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400, "the section cannot apply the filter whichever road it arrives by");
    }

    [Fact]
    public async Task SearchFolder_OnTheRecentSection_ReturnsTheMatchingFile()
    {
        var data = await ArrangeAsync();
        var recentId = await GetFolderIdAsync(FolderType.Recent, Owner);

        await _filesApi.AddFileToRecentAsync(data.MatchingFileId, TestContext.Current.CancellationToken);
        await _filesApi.AddFileToRecentAsync(data.OtherFileId, TestContext.Current.CancellationToken);

        var body = new FolderMetadataSearch(data.TemplateId, [new MetadataFilterConditionRequest(fieldId: data.FieldId, op: "eq", value: "ACME")]);

        // the Recent tag is written after the request returns: the typed search of the section is polled until the file shows up
        var deadline = DateTime.UtcNow.AddSeconds(15);
        FolderContentResponse content;

        while (true)
        {
            content = await _foldersApi.SearchFolderWithMetadataAsync(recentId, body, TestContext.Current.CancellationToken);

            if (content.FileIds().Contains(data.MatchingFileId) || DateTime.UtcNow > deadline)
            {
                break;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }

        content.FileIds().Should().Equal(new[] { data.MatchingFileId }, "the other recent file holds another value");
        content.Folders.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchRooms_ByCustomFieldName_ReturnsTheMatchingRoom()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var field = $"Client {suffix}";
        var matching = await CreateCustomRoom($"Match {suffix}");
        var other = await CreateCustomRoom($"Other {suffix}");

        await _metadataApi.SetFolderCustomFieldsAsync(matching.Id, new SetCustomFields([new CustomFieldRequest(field, "ACME")]), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderCustomFieldsAsync(other.Id, new SetCustomFields([new CustomFieldRequest(field, "Globex")]), TestContext.Current.CancellationToken);

        var body = new RoomsMetadataSearchRequestDto(metadataFilters: [new MetadataFilterConditionRequest(name: field, op: "eq", value: "ACME")]);

        var deadline = DateTime.UtcNow.AddSeconds(15);
        RoomsContentResponse rooms;

        while (true)
        {
            rooms = await _roomsApi.SearchRoomsWithMetadataAsync(body, TestContext.Current.CancellationToken);

            if (rooms.RoomIds().Contains(matching.Id) || DateTime.UtcNow > deadline)
            {
                break;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }

        rooms.RoomIds().Should().Contain(matching.Id);
        rooms.RoomIds().Should().NotContain(other.Id);
    }

    /// <summary>
    /// Repeats the search until both the expected folder and the expected file are in the answer: the two metadata
    /// documents are refreshed independently, so one of them can be visible a moment before the other.
    /// </summary>
    private Task<FolderContentResponse> PollFolderSearchAsync(int folderId, FolderMetadataSearch body, int expectedFolderId, int expectedFileId)
    {
        return PollFolderSearchAsync(
            () => _foldersApi.SearchFolderWithMetadataAsync(folderId, body, TestContext.Current.CancellationToken),
            c => c.FolderIds().Contains(expectedFolderId) && c.FileIds().Contains(expectedFileId));
    }

    private static async Task<FolderContentResponse> PollFolderSearchAsync(Func<Task<FolderContentResponse>> search, Func<FolderContentResponse, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var content = await search();

            if (until(content) || DateTime.UtcNow > deadline)
            {
                return content;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }

    private static async Task<FolderContentResponse> SearchFolderRawAsync(RawApiClient raw, int folderId, object body)
    {
        using var response = await raw.PostAsync($"api/2.0/files/{folderId}/search", body, TestContext.Current.CancellationToken);

        return await MetadataListings.ReadAsync<FolderContentResponse>(response, TestContext.Current.CancellationToken);
    }

    private async Task<SearchData> ArrangeAsync()
    {
        await _filesClient.Authenticate(Owner);

        var suffix = Suffix();

        var template = await CreateTemplateAsync("Typed " + suffix, [new MetadataFieldRequest(ClientField, MetadataFieldType.String)]);
        var fieldId = template.Field(ClientField).Id;

        var room = await CreateCustomRoom($"Typed {suffix}");
        var matchingFolder = await CreateFolder($"Matching {suffix}", room.Id);
        var otherFolder = await CreateFolder($"Other {suffix}", room.Id);
        var matchingFile = await CreateFile($"matching-{suffix}.docx", room.Id);
        var otherFile = await CreateFile($"other-{suffix}.docx", room.Id);

        await _metadataApi.AssignFolderTemplatesAsync(matchingFolder.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderValuesAsync(matchingFolder.Id, new SetMetadataValues([new MetadataValueRequest(fieldId, stringValue: "ACME")]), TestContext.Current.CancellationToken);
        await _metadataApi.AssignFolderTemplatesAsync(otherFolder.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderValuesAsync(otherFolder.Id, new SetMetadataValues([new MetadataValueRequest(fieldId, stringValue: "Globex")]), TestContext.Current.CancellationToken);
        await _metadataApi.AssignFileTemplatesAsync(matchingFile.Id, new AssignMetadataTemplates([template.Id]), TestContext.Current.CancellationToken);
        await _metadataApi.SetFileValuesAsync(matchingFile.Id, new SetMetadataValues([new MetadataValueRequest(fieldId, stringValue: "ACME")]), TestContext.Current.CancellationToken);
        await _metadataApi.AssignFileTemplatesAsync(otherFile.Id, new AssignMetadataTemplates([template.Id]), TestContext.Current.CancellationToken);
        await _metadataApi.SetFileValuesAsync(otherFile.Id, new SetMetadataValues([new MetadataValueRequest(fieldId, stringValue: "Globex")]), TestContext.Current.CancellationToken);

        return new SearchData(template.Id, fieldId, room.Id, matchingFolder.Id, matchingFile.Id, otherFile.Id);
    }

    private async Task<MetadataTemplateDto> CreateTemplateAsync(string name, List<MetadataFieldRequest> fields)
    {
        return (await _metadataApi.CreateTemplateAsync(new CreateMetadataTemplateRequestDto(name, true, fields), TestContext.Current.CancellationToken)).Response;
    }

    private sealed record SearchData(int TemplateId, int FieldId, int RoomId, int MatchingFolderId, int MatchingFileId, int OtherFileId);

    private static string Suffix()
    {
        return Guid.NewGuid().ToString()[..8];
    }
}
