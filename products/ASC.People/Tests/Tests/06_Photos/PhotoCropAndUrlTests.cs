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
/// The two-step avatar flow and the URL variant: <c>POST /people/{userid}/photo</c> with
/// <c>autosave</c> false keeps a temporary file, <c>POST /people/{userid}/photo/thumbnails</c> crops
/// it into the avatar (zero size = the whole image), and <c>PUT /people/{userid}/photo</c> takes a
/// URL. All three act on the caller's own account only.
/// </summary>
[Trait("Category", "Photos")]
public class PhotoCropAndUrlTests(
    AspireAppFixture fixture)
    : PhotosTestBase(fixture)
{
    [Fact]
    public async Task UploadMemberPhoto_WithoutAutosave_ReturnsTempFileAndKeepsAvatar()
    {
        var member = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(member);
        var before = (await _photosApi.GetMemberPhotoAsync(member.Id.ToString(), TestContext.Current.CancellationToken)).Response;

        var tmpFile = await UploadTempPhotoAsync(member);

        var after = (await _photosApi.GetMemberPhotoAsync(member.Id.ToString(), TestContext.Current.CancellationToken)).Response;
        tmpFile.Should().NotBeNullOrEmpty();
        PathOf(after.Original).Should().Be(PathOf(before.Original), "a temporary upload is not the avatar yet");
    }

    [Fact]
    public async Task UploadMemberPhoto_WithAutosave_ReplacesAvatar()
    {
        var member = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(member);
        var before = (await _photosApi.GetMemberPhotoAsync(member.Id.ToString(), TestContext.Current.CancellationToken)).Response;

        var result = await UploadPhotoAsync(member, member.Id);

        var after = (await _photosApi.GetMemberPhotoAsync(member.Id.ToString(), TestContext.Current.CancellationToken)).Response;
        result.Success.Should().BeTrue();
        PathOf(after.Original).Should().NotBe(PathOf(before.Original));
    }

    [Fact]
    public async Task CreateMemberPhotoThumbnails_FromTempFileWithZeroSize_MakesItTheAvatar()
    {
        var member = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(member);
        var before = (await _photosApi.GetMemberPhotoAsync(member.Id.ToString(), TestContext.Current.CancellationToken)).Response;
        var tmpFile = await UploadTempPhotoAsync(member);

        var result = (await _photosApi.CreateMemberPhotoThumbnailsAsync(
            member.Id.ToString(),
            new ThumbnailsRequest(tmpFile: tmpFile, x: 0, y: 0, width: 0, height: 0),
            TestContext.Current.CancellationToken)).Response;

        PathOf(result.Original).Should().NotBe(PathOf(before.Original), "the cropped temporary file became the avatar");
    }

    [Fact]
    public async Task CreateMemberPhotoThumbnails_OnAnotherAccount_ReturnsForbidden()
    {
        var member = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(Owner);
        var tmpFile = await UploadTempPhotoAsync(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _photosApi.CreateMemberPhotoThumbnailsAsync(
                member.Id.ToString(),
                new ThumbnailsRequest(tmpFile: tmpFile),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateMemberPhoto_OnAnotherAccount_ReturnsForbidden()
    {
        var member = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _photosApi.UpdateMemberPhotoAsync(
                member.Id.ToString(),
                new UpdatePhotoMemberRequest("https://example.com/avatar.png"),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateMemberPhoto_EmptyUrl_ReturnsBadRequest()
    {
        var member = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(member);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _photosApi.UpdateMemberPhotoAsync(
                member.Id.ToString(),
                new UpdatePhotoMemberRequest(""),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    /// <summary>
    /// The URL of the current avatar (the <c>original</c> path without its <c>hash</c> query) is
    /// recognised and nothing is downloaded, so even a path no URL validation would accept passes.
    /// </summary>
    [Fact]
    public async Task UpdateMemberPhoto_CurrentAvatarUrl_SkipsDownloadAndKeepsAvatar()
    {
        var member = await InviteContact(EmployeeType.User);
        await UploadPhotoAsync(member, member.Id);
        var before = (await _photosApi.GetMemberPhotoAsync(member.Id.ToString(), TestContext.Current.CancellationToken)).Response;

        var result = (await _photosApi.UpdateMemberPhotoAsync(
            member.Id.ToString(),
            new UpdatePhotoMemberRequest(PathOf(before.Original)),
            TestContext.Current.CancellationToken)).Response;

        PathOf(result.Original).Should().Be(PathOf(before.Original));
    }

    /// <summary>Uploads the test image with <c>autosave</c> false and returns the temporary file it names.</summary>
    private async Task<string> UploadTempPhotoAsync(User actor)
    {
        await _peopleClient.Authenticate(actor);
        await using var stream = new MemoryStream(CreateTestImageBytes());

        var result = (await _photosApi.UploadMemberPhotoAsync(
            actor.Id.ToString(),
            new FileParameter("avatar.png", "image/png", stream),
            autosave: false,
            TestContext.Current.CancellationToken)).Response;

        result.Success.Should().BeTrue(result.Message);

        return result.Data.ToString()!.Trim('"');
    }

    /// <summary>A photo URL without the <c>hash</c> query the server appends for caching.</summary>
    private static string PathOf(string url)
    {
        var index = url.IndexOf("?hash=", StringComparison.Ordinal);

        return index < 0 ? url : url[..index];
    }
}
