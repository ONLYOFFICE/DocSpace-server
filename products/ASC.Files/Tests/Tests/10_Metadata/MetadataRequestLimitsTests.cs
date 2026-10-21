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
/// Covers the request limits the metadata endpoints answer with a bad request instead of a server error or an
/// unbounded answer: the paging of the typed search, the null lists, the field type outside the enum, the name and
/// value lengths, the duplicate field names and the size of a filter.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataRequestLimitsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    #region Typed search paging

    [Theory]
    [InlineData(101)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SearchFolder_WithACountOutOfRange_ReturnsBadRequest(int count)
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Paging " + Suffix());

        // the body used to carry no range: a negative count dropped the limit and returned the whole subtree
        if (count == 0)
        {
            // raw: FolderMetadataSearch.Count is an int with EmitDefaultValue = false, so the SDK leaves a zero count out of the body
            using var response = await new RawApiClient(_filesClient).PostAsync($"api/2.0/files/{room.Id}/search", new { count }, TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            return;
        }

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SearchFolderWithMetadataAsync(room.Id, new FolderMetadataSearch(count: count), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SearchFolder_WithANegativeStartIndex_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Offset " + Suffix());

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SearchFolderWithMetadataAsync(room.Id, new FolderMetadataSearch(startIndex: -1), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(-1)]
    public async Task SearchRooms_WithACountOutOfRange_ReturnsBadRequest(int count)
    {
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SearchRoomsWithMetadataAsync(new RoomsMetadataSearchRequestDto(count: count), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SearchFolder_WithTooManyConditions_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Conditions " + suffix, [new MetadataFieldRequest("Client", MetadataFieldType.String)]);
        var fieldId = template.Field("Client").Id;
        var room = await CreateCustomRoom($"Conditions {suffix}");

        var conditions = Enumerable.Range(0, MetadataFilterHelper.MaxConditions + 1)
            .Select(i => new MetadataFilterConditionRequest(fieldId: fieldId, op: "eq", value: $"v{i}"))
            .ToList();

        // every condition is a nested query in the index and an EXISTS in SQL: the list is capped
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SearchFolderWithMetadataAsync(room.Id, new FolderMetadataSearch(template.Id, conditions), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    #endregion

    #region Null lists

    [Fact]
    public async Task SetValues_WithANullList_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Null values " + Suffix());

        // "values" is declared required, which only makes the key mandatory: an explicit null used to be a server error
        // raw: SetMetadataValues throws ArgumentNullException client-side for a null list, so the SDK cannot send it
        using var response = await new RawApiClient(_filesClient).PutRawAsync($"api/2.0/files/metadata/folder/{room.Id}/values", """{"values":null}""", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetCustomFields_WithANullList_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Null fields " + Suffix());

        // raw: SetCustomFields throws ArgumentNullException client-side for a null list, so the SDK cannot send it
        using var response = await new RawApiClient(_filesClient).PutRawAsync($"api/2.0/files/metadata/folder/{room.Id}/customFields", """{"fields":null}""", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Templates and fields

    [Fact]
    public async Task CreateTemplate_WithAFieldTypeOutsideTheEnum_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);

        // the serializer accepts any integer for the enum; such a field used to be stored and to accept any value
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateTemplateAsync("Type " + Suffix(), [new MetadataFieldRequest("Odd", (MetadataFieldType)99)]));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateTemplate_WithATooLongName_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var name = new string('n', MetadataService.MaxNameLength + 1);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateTemplateAsync(name, []));

        exception.ErrorCode.Should().Be(400, "the name column is varchar(255), the database used to answer with a server error");
    }

    [Fact]
    public async Task CreateTemplate_WithATooLongFieldName_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var fieldName = new string('f', MetadataService.MaxNameLength + 1);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateTemplateAsync("Field name " + Suffix(), [new MetadataFieldRequest(fieldName, MetadataFieldType.String)]));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateTemplate_WithTwoFieldsOfTheSameName_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateTemplateAsync("Twice " + Suffix(),
                [new MetadataFieldRequest("Client", MetadataFieldType.String), new MetadataFieldRequest("client", MetadataFieldType.String)]));

        exception.ErrorCode.Should().Be(400, "the fields of a template are told apart by name, whatever the case");
    }

    [Fact]
    public async Task CreateField_WithTheNameOfAnExistingField_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync("Existing " + Suffix(), [new MetadataFieldRequest("Client", MetadataFieldType.String)]);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.CreateFieldAsync(template.Id, new MetadataFieldRequest("CLIENT", MetadataFieldType.String), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateField_AddsTheFieldToTheTemplate()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync("Grow " + Suffix(), [new MetadataFieldRequest("Client", MetadataFieldType.String)]);

        var field = (await _metadataApi.CreateFieldAsync(template.Id, new MetadataFieldRequest("Department", MetadataFieldType.String), TestContext.Current.CancellationToken)).Response;

        field.Name.Should().Be("Department");

        var reloaded = (await _metadataApi.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken)).Response;
        reloaded.Fields.Select(f => f.Name).Should().BeEquivalentTo(["Client", "Department"]);
    }

    [Fact]
    public async Task DeleteTemplate_RemovesItFromTheEntriesAndTheList()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Gone " + suffix, [new MetadataFieldRequest("Client", MetadataFieldType.String)]);
        var room = await CreateCustomRoom($"Gone {suffix}");

        await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderValuesAsync(room.Id,
            new SetMetadataValues([new MetadataValueRequest(template.Field("Client").Id, stringValue: "ACME")]), TestContext.Current.CancellationToken);

        await _metadataApi.DeleteTemplateAsync(template.Id, TestContext.Current.CancellationToken);

        (await _metadataApi.GetTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Should().NotContain(t => t.Id == template.Id);
        (await _metadataApi.GetFolderMetadataAsync(room.Id, TestContext.Current.CancellationToken)).Response.Templates
            .Should().NotContain(t => t.Id == template.Id, "the links and the values go with the template");

        var reread = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken));
        reread.ErrorCode.Should().Be(404);

        // the filter by the deleted template is refused, the same way an unknown one is
        var search = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.SearchFolderWithMetadataAsync(room.Id, new FolderMetadataSearch(metadataTemplateId: template.Id), TestContext.Current.CancellationToken));
        search.ErrorCode.Should().Be(400);
    }

    #endregion

    #region Entry info

    [Fact]
    public async Task GetEntryInfo_CarriesTheAssignedTemplates_LikeTheListing()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Info " + suffix, [new MetadataFieldRequest("Client", MetadataFieldType.String)]);
        var room = await CreateCustomRoom($"Info {suffix}");
        var folder = await CreateFolder($"Sub {suffix}", room.Id);
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        // a client re-reads a single row by the info endpoints after a socket event: without the templates the row
        // would lose in the listing what the page had shown
        (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.AssignedMetadataTemplates.Should().BeNullOrEmpty();

        await _metadataApi.AssignFileTemplatesAsync(file.Id, new AssignMetadataTemplates([template.Id]), TestContext.Current.CancellationToken);
        await _metadataApi.AssignFolderTemplatesAsync(folder.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);

        (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.AssignedMetadataTemplates.Should().Equal(template.Id);
        (await _foldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken)).Response.AssignedMetadataTemplates.Should().Equal(template.Id);

        var listed = await _foldersApi.GetFolderContentWithMetadataAsync(room.Id, withSubFolders: false, cancellationToken: TestContext.Current.CancellationToken);
        listed.Files.Single(f => f.Id == file.Id).AssignedMetadataTemplates.Should().Equal([template.Id], "the info endpoint and the listing agree");
        listed.Folders.Single(f => f.Id == folder.Id).AssignedMetadataTemplates.Should().Equal(template.Id);
    }

    #endregion

    #region Value lengths

    [Fact]
    public async Task SetValues_WithATooLongString_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Long " + suffix, [new MetadataFieldRequest("Notes", MetadataFieldType.String)]);
        var room = await CreateCustomRoom($"Long {suffix}");

        await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);

        var value = new string('v', MetadataService.MaxStringValueLength + 1);

        // the value column is unbounded, but the index stores the value as a keyword term with a 32 KB cap: a longer
        // value used to be stored and to silently drop the entry's document from the index
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.SetFolderValuesAsync(room.Id,
                new SetMetadataValues([new MetadataValueRequest(template.Field("Notes").Id, stringValue: value)]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SetCustomFields_WithATooLongValue_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Long custom " + Suffix());

        var value = new string('v', MetadataService.MaxStringValueLength + 1);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.SetFolderCustomFieldsAsync(room.Id, new SetCustomFields([new CustomFieldRequest("Notes", value)]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SetValues_WithTheLongestAllowedString_IsStoredAndFound()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Longest " + suffix, [new MetadataFieldRequest("Notes", MetadataFieldType.String)]);
        var fieldId = template.Field("Notes").Id;
        var room = await CreateCustomRoom($"Longest {suffix}");
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        await _metadataApi.AssignFileTemplatesAsync(file.Id, new AssignMetadataTemplates([template.Id]), TestContext.Current.CancellationToken);

        var value = new string('v', MetadataService.MaxStringValueLength);

        await _metadataApi.SetFileValuesAsync(file.Id, new SetMetadataValues([new MetadataValueRequest(fieldId, stringValue: value)]), TestContext.Current.CancellationToken);

        var stored = (await _metadataApi.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken)).Response.Templates;
        stored.Single(t => t.Id == template.Id).Field("Notes").Value.StringValue.Should().Be(value);

        // the limit is set so the index accepts the value: the file must be found by it, not only stored
        var deadline = DateTime.UtcNow.AddSeconds(15);
        FolderContentResponse content;

        while (true)
        {
            content = await _foldersApi.SearchFolderWithMetadataAsync(room.Id,
                new FolderMetadataSearch(template.Id, [new MetadataFilterConditionRequest(fieldId: fieldId, op: "eq", value: value)]), TestContext.Current.CancellationToken);

            if (content.FileIds().Contains(file.Id) || DateTime.UtcNow > deadline)
            {
                break;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }

        content.FileIds().Should().Equal(file.Id);
    }

    #endregion

    private async Task<MetadataTemplateDto> CreateTemplateAsync(string name, List<MetadataFieldRequest> fields, bool visible = true)
    {
        return (await _metadataApi.CreateTemplateAsync(new CreateMetadataTemplateRequestDto(name, visible, fields), TestContext.Current.CancellationToken)).Response;
    }

    private static string Suffix()
    {
        return Guid.NewGuid().ToString()[..8];
    }
}
