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

namespace ASC.Files.Tests.Tests._03_Rooms.Tags;

/// <summary>
/// Functional coverage of <c>POST /files/tags</c> (create/global-tag catalog behavior).
/// Permission coverage (who is allowed to call it) already lives in
/// <c>Permissions/RoomTagCreatePermissionsTests</c>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomTagCreateTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task CreateRoomTag_SameTagCanBeAttachedToMultipleRooms()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagName = "SharedGlobalTag";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagName), TestContext.Current.CancellationToken);

        var room1 = await CreateCustomRoom("Autotest Shared Room A");
        var room2 = await CreateCustomRoom("Autotest Shared Room B");

        // Act
        await _roomsApi.AddRoomTagsAsync(room1.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);
        await _roomsApi.AddRoomTagsAsync(room2.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Assert
        var info1 = (await _roomsApi.GetRoomInfoAsync(room1.Id, TestContext.Current.CancellationToken)).Response;
        var info2 = (await _roomsApi.GetRoomInfoAsync(room2.Id, TestContext.Current.CancellationToken)).Response;
        (info1.Tags ?? []).Should().Contain(tagName);
        (info2.Tags ?? []).Should().Contain(tagName);
    }

    [Fact]
    public async Task CreateRoomTag_CyrillicName_Accepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "Тег Кириллица";

        // Act
        var created = (await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken)).Response;

        // Assert
        created.Should().Be(name);
        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Should().Contain(name);
    }

    /// <remarks>
    /// Bug 81682, closed as by-design: a tag name carrying a character outside the Basic Multilingual
    /// Plane is refused. <c>files_tag.name</c> is declared <c>utf8</c> / <c>utf8_general_ci</c>
    /// (<c>products/ASC.Files/Core/Core/EF/DbFilesTag.cs</c>), and MySQL's <c>utf8</c> holds three
    /// bytes per character, so a four-byte one cannot be stored — the same decision that was taken for
    /// room group names. The refusal comes from the database write rather than from validation, so the
    /// status is 500; this test pins the refusal and will go red if the column ever moves to
    /// <c>utf8mb4</c> or the name starts being validated up front.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81682")]
    public async Task CreateRoomTag_NameOutsideBmp_Rejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "Tag 🚀 Emoji";

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(500);

        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Should().NotContain(name, "a refused name must not be stored");
    }

    [Fact]
    public async Task CreateRoomTag_EmptyName_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(""), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    /// <remarks>
    /// A spaces-only tag name used to be accepted (200) instead of rejected — there is no
    /// server-side trim/blank check on the tag name.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81683")]
    public async Task CreateRoomTag_SpacesOnlyName_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("   "), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    // The next two cases post a body the typed DTO cannot express: CreateTagRequestDto's public
    // constructor requires a non-null name and throws client-side for a missing/null value, so no
    // request would ever reach the server. Raw HTTP is the only way to exercise the server's own
    // validation of these payloads.

    [Fact]
    public async Task CreateRoomTag_MissingNameField_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await SendRawTagCreate("{}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRoomTag_NullName_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await SendRawTagCreate("""{"name":null}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Non-string JSON values for "name" cannot be assigned to CreateTagRequestDto.Name (string),
    // so these also go through raw HTTP.
    [Theory]
    [InlineData("""{"name":12345}""")]
    [InlineData("""{"name":true}""")]
    [InlineData("""{"name":{"foo":"bar"}}""")]
    [InlineData("""{"name":["a","b"]}""")]
    public async Task CreateRoomTag_NonStringName_BadRequest(string body)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await SendRawTagCreate(body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRoomTag_VeryLongName_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(new string('a', 10000)), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateRoomTag_DuplicateName_DoesNotCreateDuplicateEntry()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "DuplicateTagOnce";
        var created = (await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken)).Response;
        created.Should().Be(name);

        // Act
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);

        // Assert
        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Count(t => name.Equals(t)).Should().Be(1);
    }

    [Fact]
    public async Task CreateRoomTag_CaseInsensitiveNames_DoNotCreateSeparateTags()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("CaseTag"), TestContext.Current.CancellationToken);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("casetag"), TestContext.Current.CancellationToken);

        // Assert
        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Should().Contain("CaseTag");
        list.Should().NotContain("casetag");
        list.Count(t => t is string s && s.Equals("casetag", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
    }

    [Fact]
    public async Task CreateRoomTag_LeadingTrailingSpaces_PreservedAsIs()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "  PaddedTag  ";

        // Act
        var created = (await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken)).Response;

        // Assert
        new[] { name, name.Trim() }.Should().Contain(created);
    }

    [Fact]
    public async Task CreateRoomTag_DeletedTagCanBeRecreated()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "RecreatableTag";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);
        await _roomsApi.DeleteCustomTagsAsync(new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);

        var listAfterDelete = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        listAfterDelete.Should().NotContain(name);

        // Act
        var recreated = (await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken)).Response;

        // Assert
        recreated.Should().Be(name);
        var listAfterRecreate = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        listAfterRecreate.Should().Contain(name);
    }

    [Fact]
    public async Task AddRoomTags_AutoCreatesTagNeverCreatedViaCreateRoomTag()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "AutoCreatedByAddRoomTags";
        var listBefore = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        listBefore.Should().NotContain(name);

        var room = await CreateCustomRoom("Autotest Room Auto-Tag");

        // Act
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);

        // Assert
        var listAfter = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        listAfter.Should().Contain(name);

        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        (info.Tags ?? []).Should().Contain(name);
    }

    /// <summary>
    /// Sends a raw POST /api/2.0/files/tags with an arbitrary JSON body, bypassing the typed SDK
    /// so that payloads the generated <see cref="CreateTagRequestDto"/> cannot express can be tested.
    /// </summary>
    private async Task<HttpResponseMessage> SendRawTagCreate(string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/2.0/files/tags")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
