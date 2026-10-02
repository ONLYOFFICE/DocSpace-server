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
    private const int StringType = 0;

    #region Typed search paging

    [Theory]
    [InlineData(101)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SearchFolder_WithACountOutOfRange_ReturnsBadRequest(int count)
    {
        var api = await ArrangeAsync();
        var room = await CreateCustomRoom("Paging " + Suffix());

        // the body used to carry no range: a negative count dropped the limit and returned the whole subtree
        using var response = await api.SearchFolderResponseAsync(room.Id, new { count }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchFolder_WithANegativeStartIndex_ReturnsBadRequest()
    {
        var api = await ArrangeAsync();
        var room = await CreateCustomRoom("Offset " + Suffix());

        using var response = await api.SearchFolderResponseAsync(room.Id, new { startIndex = -1 }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(-1)]
    public async Task SearchRooms_WithACountOutOfRange_ReturnsBadRequest(int count)
    {
        var api = await ArrangeAsync();

        using var response = await api.SearchRoomsResponseAsync(new { count }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchFolder_WithTooManyConditions_ReturnsBadRequest()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Conditions " + suffix, [new MetadataFieldPayload { Name = "Client", Type = StringType }], TestContext.Current.CancellationToken);
        var fieldId = template.Field("Client").Id;
        var room = await CreateCustomRoom($"Conditions {suffix}");

        var conditions = Enumerable.Range(0, MetadataFilterHelper.MaxConditions + 1)
            .Select(i => (object)new { fieldId, op = "eq", value = $"v{i}" })
            .ToArray();

        // every condition is a nested query in the index and an EXISTS in SQL: the list is capped
        using var response = await api.SearchFolderResponseAsync(room.Id, new { metadataTemplateId = template.Id, metadataFilters = conditions }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Null lists

    [Fact]
    public async Task SetValues_WithANullList_ReturnsBadRequest()
    {
        var api = await ArrangeAsync();
        var room = await CreateCustomRoom("Null values " + Suffix());

        // "values" is declared required, which only makes the key mandatory: an explicit null used to be a server error
        using var response = await api.SetFolderValuesResponseAsync(room.Id, new { values = (object?)null }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetCustomFields_WithANullList_ReturnsBadRequest()
    {
        var api = await ArrangeAsync();
        var room = await CreateCustomRoom("Null fields " + Suffix());

        using var response = await api.SetFolderCustomFieldsResponseAsync(room.Id, new { fields = (object?)null }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Templates and fields

    [Fact]
    public async Task CreateTemplate_WithAFieldTypeOutsideTheEnum_ReturnsBadRequest()
    {
        var api = await ArrangeAsync();

        // the serializer accepts any integer for the enum; such a field used to be stored and to accept any value
        using var response = await api.CreateTemplateResponseAsync("Type " + Suffix(), [new MetadataFieldPayload { Name = "Odd", Type = 99 }], TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTemplate_WithATooLongName_ReturnsBadRequest()
    {
        var api = await ArrangeAsync();
        var name = new string('n', MetadataService.MaxNameLength + 1);

        using var response = await api.CreateTemplateResponseAsync(name, [], TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "the name column is varchar(255), the database used to answer with a server error");
    }

    [Fact]
    public async Task CreateTemplate_WithATooLongFieldName_ReturnsBadRequest()
    {
        var api = await ArrangeAsync();
        var fieldName = new string('f', MetadataService.MaxNameLength + 1);

        using var response = await api.CreateTemplateResponseAsync("Field name " + Suffix(), [new MetadataFieldPayload { Name = fieldName, Type = StringType }], TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTemplate_WithTwoFieldsOfTheSameName_ReturnsBadRequest()
    {
        var api = await ArrangeAsync();

        using var response = await api.CreateTemplateResponseAsync("Twice " + Suffix(),
            [new MetadataFieldPayload { Name = "Client", Type = StringType }, new MetadataFieldPayload { Name = "client", Type = StringType }],
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "the fields of a template are told apart by name, whatever the case");
    }

    [Fact]
    public async Task CreateField_WithTheNameOfAnExistingField_ReturnsBadRequest()
    {
        var api = await ArrangeAsync();
        var template = await api.CreateTemplateAsync("Existing " + Suffix(), [new MetadataFieldPayload { Name = "Client", Type = StringType }], TestContext.Current.CancellationToken);

        using var response = await api.CreateFieldResponseAsync(template.Id, new MetadataFieldPayload { Name = "CLIENT", Type = StringType }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateField_AddsTheFieldToTheTemplate()
    {
        var api = await ArrangeAsync();
        var template = await api.CreateTemplateAsync("Grow " + Suffix(), [new MetadataFieldPayload { Name = "Client", Type = StringType }], TestContext.Current.CancellationToken);

        var field = await api.CreateFieldAsync(template.Id, new MetadataFieldPayload { Name = "Department", Type = StringType }, TestContext.Current.CancellationToken);

        field.Name.Should().Be("Department");

        var reloaded = await api.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken);
        reloaded.Fields.Select(f => f.Name).Should().BeEquivalentTo(["Client", "Department"]);
    }

    [Fact]
    public async Task DeleteTemplate_RemovesItFromTheEntriesAndTheList()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Gone " + suffix, [new MetadataFieldPayload { Name = "Client", Type = StringType }], TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom($"Gone {suffix}");

        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(room.Id, [new MetadataValuePayload { FieldId = template.Field("Client").Id, StringValue = "ACME" }], TestContext.Current.CancellationToken);

        await api.DeleteTemplateAsync(template.Id, TestContext.Current.CancellationToken);

        (await api.GetTemplatesAsync(TestContext.Current.CancellationToken)).Should().NotContain(t => t.Id == template.Id);
        (await api.GetFolderMetadataAsync(room.Id, TestContext.Current.CancellationToken)).Should().NotContain(t => t.Id == template.Id, "the links and the values go with the template");

        using var reread = await api.GetTemplateResponseAsync(template.Id, TestContext.Current.CancellationToken);
        reread.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // the filter by the deleted template is refused, the same way an unknown one is
        using var search = await api.SearchFolderResponseAsync(room.Id, new { metadataTemplateId = template.Id }, TestContext.Current.CancellationToken);
        search.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Entry info

    [Fact]
    public async Task GetEntryInfo_CarriesTheAssignedTemplates_LikeTheListing()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Info " + suffix, [new MetadataFieldPayload { Name = "Client", Type = StringType }], TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom($"Info {suffix}");
        var folder = await CreateFolder($"Sub {suffix}", room.Id);
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        // a client re-reads a single row by the info endpoints after a socket event: without the templates the row
        // would lose in the listing what the page had shown
        (await api.GetFileInfoAsync(file.Id, TestContext.Current.CancellationToken)).AssignedMetadataTemplates.Should().BeNullOrEmpty();

        await api.AssignFileTemplatesAsync(file.Id, [template.Id], TestContext.Current.CancellationToken);
        await api.AssignFolderTemplatesAsync(folder.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);

        (await api.GetFileInfoAsync(file.Id, TestContext.Current.CancellationToken)).AssignedMetadataTemplates.Should().Equal(template.Id);
        (await api.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken)).AssignedMetadataTemplates.Should().Equal(template.Id);

        var listed = await api.GetFolderContentAsync(room.Id, withSubFolders: false, cancellationToken: TestContext.Current.CancellationToken);
        listed.Files.Single(f => f.Id == file.Id).AssignedMetadataTemplates.Should().Equal([template.Id], "the info endpoint and the listing agree");
        listed.Folders.Single(f => f.Id == folder.Id).AssignedMetadataTemplates.Should().Equal(template.Id);
    }

    #endregion

    #region Value lengths

    [Fact]
    public async Task SetValues_WithATooLongString_ReturnsBadRequest()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Long " + suffix, [new MetadataFieldPayload { Name = "Notes", Type = StringType }], TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom($"Long {suffix}");

        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);

        var value = new string('v', MetadataService.MaxStringValueLength + 1);

        // the value column is unbounded, but the index stores the value as a keyword term with a 32 KB cap: a longer
        // value used to be stored and to silently drop the entry's document from the index
        using var response = await api.SetFolderValuesResponseAsync(room.Id, [new MetadataValuePayload { FieldId = template.Field("Notes").Id, StringValue = value }], TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetCustomFields_WithATooLongValue_ReturnsBadRequest()
    {
        var api = await ArrangeAsync();
        var room = await CreateCustomRoom("Long custom " + Suffix());

        var value = new string('v', MetadataService.MaxStringValueLength + 1);

        using var response = await api.SetFolderCustomFieldsResponseAsync(room.Id, [new CustomFieldPayload("Notes", value)], TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetValues_WithTheLongestAllowedString_IsStoredAndFound()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Longest " + suffix, [new MetadataFieldPayload { Name = "Notes", Type = StringType }], TestContext.Current.CancellationToken);
        var fieldId = template.Field("Notes").Id;
        var room = await CreateCustomRoom($"Longest {suffix}");
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        await api.AssignFileTemplatesAsync(file.Id, [template.Id], TestContext.Current.CancellationToken);

        var value = new string('v', MetadataService.MaxStringValueLength);

        await api.SetFileValuesAsync(file.Id, [new MetadataValuePayload { FieldId = fieldId, StringValue = value }], TestContext.Current.CancellationToken);

        var stored = await api.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken);
        stored.Single(t => t.Id == template.Id).Field("Notes").Value!.StringValue.Should().Be(value);

        // the limit is set so the index accepts the value: the file must be found by it, not only stored
        var deadline = DateTime.UtcNow.AddSeconds(15);
        FolderContentResponse content;

        while (true)
        {
            content = await api.SearchFolderAsync(room.Id, new { metadataTemplateId = template.Id, metadataFilters = new object[] { new { fieldId, op = "eq", value } } }, TestContext.Current.CancellationToken);

            if (content.FileIds().Contains(file.Id) || DateTime.UtcNow > deadline)
            {
                break;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }

        content.FileIds().Should().Equal(file.Id);
    }

    #endregion

    private async Task<MetadataApiClient> ArrangeAsync()
    {
        await _filesClient.Authenticate(Owner);

        return new MetadataApiClient(_filesClient);
    }

    private static string Suffix()
    {
        return Guid.NewGuid().ToString()[..8];
    }
}
