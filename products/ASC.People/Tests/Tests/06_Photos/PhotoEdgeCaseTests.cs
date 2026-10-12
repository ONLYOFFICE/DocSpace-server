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

namespace ASC.People.Tests.Tests._06_Photos;

/// <summary>
/// Edge cases shared by all three member-photo endpoints: anonymous access, a non-existent user,
/// a missing file part, an oversized upload and an unsupported file type.
/// </summary>
[Trait("Category", "Photos")]
public class PhotoEdgeCaseTests(
    AspireAppFixture fixture)
    : PhotosTestBase(fixture)
{
    private static readonly Guid _nonExistentUserId = Guid.Empty;

    [Fact]
    public async Task GetMemberPhoto_Unauthorized_ThrowsUnauthorized()
    {
        // Arrange
        var self = (await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken)).Response;
        await _peopleClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _photosApi.GetMemberPhotoAsync(self.Id.ToString(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetMemberPhoto_UserNotFound_Throws404()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _photosApi.GetMemberPhotoAsync(_nonExistentUserId.ToString(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent?.ToString().Should().Contain("The user could not be found");
    }

    [Fact]
    public async Task DeleteMemberPhoto_Unauthorized_ThrowsUnauthorized()
    {
        // Arrange
        var self = (await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken)).Response;
        await _peopleClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _photosApi.DeleteMemberPhotoAsync(self.Id.ToString(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task DeleteMemberPhoto_UserNotFound_Throws404()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _photosApi.DeleteMemberPhotoAsync(_nonExistentUserId.ToString(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent?.ToString().Should().Contain("The user could not be found");
    }

    [Fact]
    public async Task UploadMemberPhoto_NoFileAttached_ReturnsFileNotFoundMessage()
    {
        // Arrange
        var self = (await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken)).Response;
        await _peopleClient.Authenticate(Owner);

        // Act
        using var response = await UploadPhotoWithoutFileRaw(Owner, self.Id);

        // Assert
        // A multipart body with no file part at all never reaches the controller: model binding
        // rejects it with 400. The TypeScript suite sends an empty file and gets the endpoint's own
        // {"success": false, "message": "The uploaded file could not be found"} — a different case:
        // there the part exists and is empty, here there is no part at all.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadMemberPhoto_Unauthorized_ThrowsUnauthorized()
    {
        // Arrange
        var self = (await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken)).Response;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await UploadPhotoAsync(null, self.Id));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task UploadMemberPhoto_ImageTooLarge_ReturnsMaxSizeMessage()
    {
        // Arrange
        var self = (await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken)).Response;
        var largeImage = new byte[20 * 1024 * 1024];

        // Act
        var result = await UploadPhotoAsync(Owner, self.Id, largeImage);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("The maximum file size is exceeded (5 MB).");
    }

    [Fact]
    public async Task UploadMemberPhoto_UnsupportedFileType_ReturnsFailure()
    {
        // Arrange
        var self = (await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken)).Response;
        var svgBytes = Encoding.UTF8.GetBytes(
            "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect width=\"1\" height=\"1\"/></svg>");

        // Act
        var result = await UploadPhotoAsync(Owner, self.Id, svgBytes, fileName: "avatar.svg", contentType: "image/svg+xml");

        // Assert
        result.Success.Should().BeFalse();
    }
}
