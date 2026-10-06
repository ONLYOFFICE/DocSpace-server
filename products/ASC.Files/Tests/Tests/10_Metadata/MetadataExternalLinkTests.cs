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
/// Covers the metadata reached by an anonymous caller through an external link of the room: the templates with their
/// values are read as a member reads them, the values of a file are written through a link that grants editing and
/// refused through one that grants viewing, a caller with neither a session nor a link key is refused as
/// unauthenticated, a link of another room grants nothing, and the other writes stay closed to the anonymous callers.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataExternalLinkTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const int StringType = 0;
    private const string ClientField = "Client";

    [Fact]
    public async Task Metadata_ReadThroughTheRoomLink_ReturnsTheTemplatesWithTheirValues()
    {
        var data = await ArrangeAsync();
        var token = await GetPrimaryLinkTokenAsync(data.RoomId);

        await UseLinkAsync(token, async () =>
        {
            var file = await data.Api.GetFileMetadataAsync(data.FileId, TestContext.Current.CancellationToken);
            file.Should().ContainSingle(t => t.Id == data.TemplateId).Which.Field(ClientField).Value!.StringValue.Should().Be("File", "the link grants reading the file, and its metadata with it");

            var folder = await data.Api.GetFolderMetadataAsync(data.FolderId, TestContext.Current.CancellationToken);
            folder.Should().ContainSingle(t => t.Id == data.TemplateId).Which.Field(ClientField).Value!.StringValue.Should().Be("Folder", "the folder below the room is read through the same link");
        });
    }

    [Fact]
    public async Task Metadata_WithoutASessionOrALinkKey_ReturnsUnauthorized()
    {
        var data = await ArrangeAsync();

        await _filesClient.Authenticate(null);

        using var file = await data.Api.GetFileMetadataResponseAsync(data.FileId, TestContext.Current.CancellationToken);
        file.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "a stranger is told to authenticate, not that the file is forbidden or missing");

        using var folder = await data.Api.GetFolderMetadataResponseAsync(data.FolderId, TestContext.Current.CancellationToken);
        folder.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Metadata_ThroughTheLinkOfAnotherRoom_ReturnsForbidden()
    {
        var data = await ArrangeAsync();
        var otherRoom = await CreateCustomRoom($"Other {data.Suffix}");
        var token = await GetPrimaryLinkTokenAsync(otherRoom.Id);

        await UseLinkAsync(token, async () =>
        {
            using var response = await data.Api.GetFileMetadataResponseAsync(data.FileId, TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "a link opens its own room only");
        });
    }

    [Fact]
    public async Task FileValues_ThroughAnEditingLink_AreWritten()
    {
        var data = await ArrangeAsync();
        var token = await GetPrimaryLinkTokenAsync(data.RoomId, FileShare.Editing);

        await UseLinkAsync(token, async () =>
        {
            await data.Api.SetFileValuesAsync(data.FileId, [new MetadataValuePayload { FieldId = data.FieldId, StringValue = "Changed" }], TestContext.Current.CancellationToken);

            var written = await data.Api.GetFileMetadataAsync(data.FileId, TestContext.Current.CancellationToken);
            written.Should().ContainSingle(t => t.Id == data.TemplateId).Which.Field(ClientField).Value!.StringValue.Should().Be("Changed", "the link that lets the caller edit the document lets them fill its card");
        });

        await _filesClient.Authenticate(Owner);

        var metadata = await data.Api.GetFileMetadataAsync(data.FileId, TestContext.Current.CancellationToken);
        metadata.Should().ContainSingle(t => t.Id == data.TemplateId).Which.Field(ClientField).Value!.StringValue.Should().Be("Changed", "the value written through the link is the value of the file");
    }

    [Fact]
    public async Task FileValues_ThroughAReadLink_ReturnForbidden()
    {
        var data = await ArrangeAsync();
        var token = await GetPrimaryLinkTokenAsync(data.RoomId);

        await UseLinkAsync(token, async () =>
        {
            using var response = await data.Api.SetFileValuesResponseAsync(data.FileId, [new MetadataValuePayload { FieldId = data.FieldId, StringValue = "Changed" }], TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "a viewing link reads the card and does not fill it");
        });

        await _filesClient.Authenticate(Owner);

        var metadata = await data.Api.GetFileMetadataAsync(data.FileId, TestContext.Current.CancellationToken);
        metadata.Should().ContainSingle(t => t.Id == data.TemplateId).Which.Field(ClientField).Value!.StringValue.Should().Be("File", "the refused write changed nothing");
    }

    [Fact]
    public async Task FileValues_WithoutASessionOrALinkKey_ReturnUnauthorized()
    {
        var data = await ArrangeAsync();

        await _filesClient.Authenticate(null);

        using var response = await data.Api.SetFileValuesResponseAsync(data.FileId, [new MetadataValuePayload { FieldId = data.FieldId, StringValue = "Changed" }], TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OtherWrites_ThroughAnEditingLink_ReturnUnauthorized()
    {
        var data = await ArrangeAsync();
        var token = await GetPrimaryLinkTokenAsync(data.RoomId, FileShare.Editing);

        await UseLinkAsync(token, async () =>
        {
            // the folder values, the assignments and the custom fields are not open to the anonymous callers: a folder
            // value is inherited by the files others put there later, an assignment needs the portal dictionary, and a
            // custom field name is created for the whole portal
            using var folderValues = await data.Api.SetFolderValuesResponseAsync(data.FolderId, [new MetadataValuePayload { FieldId = data.FieldId, StringValue = "Changed" }], TestContext.Current.CancellationToken);
            folderValues.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            using var unassigned = await data.Api.UnassignFileTemplateResponseAsync(data.FileId, data.TemplateId, TestContext.Current.CancellationToken);
            unassigned.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            using var customFields = await data.Api.SetFileCustomFieldsResponseAsync(data.FileId, [new CustomFieldPayload("Note", "Changed")], TestContext.Current.CancellationToken);
            customFields.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        });
    }

    #region Arrange

    private async Task<LinkData> ArrangeAsync()
    {
        await _filesClient.Authenticate(Owner);

        var api = new MetadataApiClient(_filesClient);
        var suffix = Guid.NewGuid().ToString()[..8];

        var template = await api.CreateTemplateAsync("Link " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = StringType }], TestContext.Current.CancellationToken);
        var fieldId = template.Field(ClientField).Id;

        var room = await CreateCustomRoom($"Link {suffix}");
        var folder = await CreateFolder($"Folder {suffix}", room.Id);
        var file = await CreateFile($"doc-{suffix}.docx", folder.Id);

        await api.AssignFolderTemplatesAsync(folder.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(folder.Id, [new MetadataValuePayload { FieldId = fieldId, StringValue = "Folder" }], TestContext.Current.CancellationToken);

        await api.AssignFileTemplatesAsync(file.Id, [template.Id], TestContext.Current.CancellationToken);
        await api.SetFileValuesAsync(file.Id, [new MetadataValuePayload { FieldId = fieldId, StringValue = "File" }], TestContext.Current.CancellationToken);

        return new LinkData(api, template.Id, fieldId, room.Id, folder.Id, file.Id, suffix);
    }

    /// <summary>
    /// The key of the primary external link of the room, created by the owner; with an access other than reading the
    /// link is changed to it first.
    /// </summary>
    private async Task<string> GetPrimaryLinkTokenAsync(int roomId, FileShare? access = null)
    {
        await _filesClient.Authenticate(Owner);

        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        if (access is { } share && share != link.Access)
        {
            link = (await _roomsApi.SetRoomLinkAsync(roomId, new RoomLinkRequest(link.SharedLink.Id, share, linkType: LinkType.External), TestContext.Current.CancellationToken)).Response;
        }

        return link.SharedLink.RequestToken;
    }

    /// <summary>
    /// Runs the action as an anonymous caller carrying the link key, and takes the key off the client afterwards: the
    /// client is shared by the whole test and the next authentication must not keep an old link.
    /// </summary>
    private async Task UseLinkAsync(string token, Func<Task> action)
    {
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, token);

        try
        {
            await action();
        }
        finally
        {
            _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        }
    }

    private sealed record LinkData(MetadataApiClient Api, int TemplateId, int FieldId, int RoomId, int FolderId, int FileId, string Suffix);

    #endregion
}
