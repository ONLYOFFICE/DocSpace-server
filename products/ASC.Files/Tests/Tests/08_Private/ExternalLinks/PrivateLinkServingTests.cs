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


namespace ASC.Files.Tests.Tests._08_Privacy.ExternalLinks;

/// <summary>
/// A link of an item in a private room serves nothing on its address alone: whoever holds it is asked for the proof the
/// creator's browser derived from the link's secret, whether the link was set with a password or without one. An
/// ordinary link next to it still opens, so an answer here is the private item's rule and not a broken link.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkServingTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    private async Task<string> TokenOf(HttpResponseMessage response)
    {
        ((int)response.StatusCode).Should().Be(200);

        return (await SharedLinkOf(response)).GetProperty("requestToken").GetString()!;
    }

    private async Task<ExternalShareStatus> AnonymousStatus(string token)
    {
        await _filesClient.Authenticate(null);

        return (await _sharingApi.GetExternalShareDataAsync(token, cancellationToken: TestContext.Current.CancellationToken)).Response.Status;
    }

    private async Task<string> OrdinaryFileLinkToken()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Ordinary Links");
        var file = await CreateFile("ordinary.docx", room.Id);
        var link = (await _filesApi.SetFileExternalLinkAsync(
            file.Id, new FileLinkRequest(Guid.NewGuid(), FileShare.Read), TestContext.Current.CancellationToken)).Response;

        return link.SharedLink.RequestToken;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FileLink_AsksForTheProof_WithOrWithoutAPassword(bool passwordRequired)
    {
        var ordinary = await OrdinaryFileLinkToken();
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "linked.docx");
        using var response = await PutFileLink(fileId, new LinkBody { PasswordRequired = passwordRequired, ExpirationDate = InAWeek() });
        var token = await TokenOf(response);

        (await AnonymousStatus(ordinary)).Should().Be(ExternalShareStatus.Ok);
        (await AnonymousStatus(token)).Should().Be(ExternalShareStatus.RequiredPassword);
    }

    [Fact]
    public async Task FolderLink_AsksForTheProof()
    {
        var ordinary = await OrdinaryFileLinkToken();
        var room = await CreateRoomAsOwner();
        var folderId = await CreateFolderIn(room.Id);
        using var response = await PutFolderLink(folderId, new LinkBody());
        var token = await TokenOf(response);

        (await AnonymousStatus(ordinary)).Should().Be(ExternalShareStatus.Ok);
        (await AnonymousStatus(token)).Should().Be(ExternalShareStatus.RequiredPassword);
    }

    /// <summary>Lists a folder anonymously on a link token, the way a visitor's browser does.</summary>
    private async Task<FolderContentDto> ListOnToken(int folderId, string token)
    {
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, token);

        try
        {
            return (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        }
        finally
        {
            _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        }
    }

    [Fact]
    public async Task FolderLink_ListsNothingOnItsAddress()
    {
        // An ordinary folder link lists its folder the same way first, so the refusal below is the private item's.
        await _filesClient.Authenticate(Owner);
        var ordinaryRoom = await CreateCustomRoom("Autotest Ordinary Links");
        var ordinaryFolderId = await CreateFolderIn(ordinaryRoom.Id);
        await CreateFile("ordinary.docx", ordinaryFolderId);
        var ordinaryLink = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(
            ordinaryFolderId, new FolderLinkRequest(Guid.NewGuid(), FileShare.Read), TestContext.Current.CancellationToken)).Response;

        var room = await CreateRoomAsOwner();
        var folderId = await CreateFolderIn(room.Id);
        await UploadEncryptedFile(folderId, "inside.docx");
        using var response = await PutFolderLink(folderId, new LinkBody());
        var token = await TokenOf(response);

        (await ListOnToken(ordinaryFolderId, ordinaryLink.SharedLink.RequestToken)).Files.Should().ContainSingle();

        var exception = await Assert.ThrowsAsync<ApiException>(() => ListOnToken(folderId, token));

        exception.ErrorCode.Should().Be(403);
    }
}
