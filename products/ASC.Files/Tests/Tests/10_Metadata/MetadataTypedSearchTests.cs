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

        var body = new { metadataTemplateId = data.TemplateId, metadataFilters = new object[] { new { fieldId = data.FieldId, op = "eq", value = "ACME" } } };

        var content = await PollFolderSearchAsync(data.Api, data.RoomId, body, data.MatchingFolderId, data.MatchingFileId);

        content.FolderIds().Should().Equal(data.MatchingFolderId);
        content.FileIds().Should().Equal(data.MatchingFileId);
    }

    [Fact]
    public async Task SearchFolder_AgreesWithTheListing()
    {
        var data = await ArrangeAsync();

        var condition = new { fieldId = data.FieldId, op = "eq", value = "ACME" };

        var typed = await PollFolderSearchAsync(data.Api, data.RoomId, new { metadataTemplateId = data.TemplateId, metadataFilters = new object[] { condition } }, data.MatchingFolderId, data.MatchingFileId);
        var listed = await data.Api.GetFolderContentAsync(data.RoomId, data.TemplateId, [condition], cancellationToken: TestContext.Current.CancellationToken);

        typed.FolderIds().Should().Equal(listed.FolderIds());
        typed.FileIds().Should().Equal(listed.FileIds());
        typed.Total.Should().Be(listed.Total);
    }

    [Fact]
    public async Task SearchFolder_WithAnUnknownField_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();

        var body = new { metadataFilters = new object[] { new { fieldId = int.MaxValue, op = "eq", value = "ACME" } } };

        using var response = await data.Api.SearchFolderResponseAsync(data.RoomId, body, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "the typed road validates the conditions the same way the listing does");
    }

    [Fact]
    public async Task SearchFolder_OnTheTemplatesSection_ReturnsBadRequest()
    {
        var data = await ArrangeAsync();
        var templatesId = await GetSectionRootIdAsync("api/2.0/files/@templates");

        using var response = await data.Api.SearchFolderResponseAsync(templatesId, new { metadataTemplateId = data.TemplateId }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "the section cannot apply the filter whichever road it arrives by");
    }

    [Fact]
    public async Task SearchFolder_OnTheRecentSection_ReturnsTheMatchingFile()
    {
        var data = await ArrangeAsync();
        var recentId = await GetFolderIdAsync(FolderType.Recent, Owner);

        await _filesApi.AddFileToRecentAsync(data.MatchingFileId, TestContext.Current.CancellationToken);
        await _filesApi.AddFileToRecentAsync(data.OtherFileId, TestContext.Current.CancellationToken);

        var body = new { metadataTemplateId = data.TemplateId, metadataFilters = new object[] { new { fieldId = data.FieldId, op = "eq", value = "ACME" } } };

        // the Recent tag is written after the request returns: the typed search of the section is polled until the file shows up
        var deadline = DateTime.UtcNow.AddSeconds(15);
        FolderContentResponse content;

        while (true)
        {
            content = await data.Api.SearchFolderAsync(recentId, body, TestContext.Current.CancellationToken);

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
        var api = new MetadataApiClient(_filesClient);
        var suffix = Suffix();
        var field = $"Client {suffix}";
        var matching = await CreateCustomRoom($"Match {suffix}");
        var other = await CreateCustomRoom($"Other {suffix}");

        await api.SetFolderCustomFieldAsync(matching.Id, field, "ACME", TestContext.Current.CancellationToken);
        await api.SetFolderCustomFieldAsync(other.Id, field, "Globex", TestContext.Current.CancellationToken);

        var body = new { metadataFilters = new object[] { new { name = field, op = "eq", value = "ACME" } } };

        var deadline = DateTime.UtcNow.AddSeconds(15);
        RoomsContentResponse rooms;

        while (true)
        {
            rooms = await api.SearchRoomsAsync(body, TestContext.Current.CancellationToken);

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
    private static async Task<FolderContentResponse> PollFolderSearchAsync(MetadataApiClient api, int folderId, object body, int expectedFolderId, int expectedFileId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var content = await api.SearchFolderAsync(folderId, body, TestContext.Current.CancellationToken);

            if ((content.FolderIds().Contains(expectedFolderId) && content.FileIds().Contains(expectedFileId)) || DateTime.UtcNow > deadline)
            {
                return content;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }

    private async Task<SearchData> ArrangeAsync()
    {
        await _filesClient.Authenticate(Owner);

        var api = new MetadataApiClient(_filesClient);
        var suffix = Suffix();

        var template = await api.CreateTemplateAsync("Typed " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = 0 }], TestContext.Current.CancellationToken);
        var fieldId = template.Field(ClientField).Id;

        var room = await CreateCustomRoom($"Typed {suffix}");
        var matchingFolder = await CreateFolder($"Matching {suffix}", room.Id);
        var otherFolder = await CreateFolder($"Other {suffix}", room.Id);
        var matchingFile = await CreateFile($"matching-{suffix}.docx", room.Id);
        var otherFile = await CreateFile($"other-{suffix}.docx", room.Id);

        await api.AssignFolderTemplatesAsync(matchingFolder.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(matchingFolder.Id, [new MetadataValuePayload { FieldId = fieldId, StringValue = "ACME" }], TestContext.Current.CancellationToken);
        await api.AssignFolderTemplatesAsync(otherFolder.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(otherFolder.Id, [new MetadataValuePayload { FieldId = fieldId, StringValue = "Globex" }], TestContext.Current.CancellationToken);
        await api.AssignFileTemplatesAsync(matchingFile.Id, [template.Id], TestContext.Current.CancellationToken);
        await api.SetFileValuesAsync(matchingFile.Id, [new MetadataValuePayload { FieldId = fieldId, StringValue = "ACME" }], TestContext.Current.CancellationToken);
        await api.AssignFileTemplatesAsync(otherFile.Id, [template.Id], TestContext.Current.CancellationToken);
        await api.SetFileValuesAsync(otherFile.Id, [new MetadataValuePayload { FieldId = fieldId, StringValue = "Globex" }], TestContext.Current.CancellationToken);

        return new SearchData(api, template.Id, fieldId, room.Id, matchingFolder.Id, matchingFile.Id, otherFile.Id);
    }

    private sealed record SearchData(MetadataApiClient Api, int TemplateId, int FieldId, int RoomId, int MatchingFolderId, int MatchingFileId, int OtherFileId);

    private static string Suffix()
    {
        return Guid.NewGuid().ToString()[..8];
    }
}
