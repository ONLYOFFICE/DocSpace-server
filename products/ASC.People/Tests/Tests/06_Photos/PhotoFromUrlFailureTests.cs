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
/// PUT /api/2.0/people/{userId}/photo with a <c>files</c> URL that passes the SSRF check but gives no photo. The page
/// that is not an image threw an unmapped ImageMagick exception (<c>MagickMissingDelegateErrorException</c>, 500) and
/// answers 400 now. An error answer of the remote server was an <c>InvalidOperationException</c> (403) and stays so:
/// only the 500s were changed. Like <c>AddMember_ExternalAvatarUrl_ShouldSucceed</c>, the cases need the stand to reach
/// the internet.
/// </summary>
[Trait("Category", "Photos")]
public class PhotoFromUrlFailureTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UpdateMemberPhoto_UrlIsNotAnImage_Returns400()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _photosApi.UpdateMemberPhotoAsync(
                Owner.Id.ToString(), new UpdatePhotoMemberRequest("https://www.google.com/robots.txt"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("Unknown image file type");
    }

    [Fact]
    public async Task UpdateMemberPhoto_UrlAnswersNotFound_Returns403()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _photosApi.UpdateMemberPhotoAsync(
                Owner.Id.ToString(),
                new UpdatePhotoMemberRequest("https://www.google.com/images/branding/no-such-photo-5f3a.png"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain("Failed to download photo: NotFound");
    }
}
