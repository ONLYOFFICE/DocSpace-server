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

namespace ASC.Files.Tests.Tests._01_Files.Links;

/// <summary>
/// What an anonymous visitor opening a file through its external link gets to see: nothing that
/// identifies the room creator or the creator of the link.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Files")]
public class FileLinkPrivacyTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// BUG 80495: the editor page opened from <c>/doceditor?share=...</c> carried the display name and
    /// the email of the room creator and of the link creator.
    /// </summary>
    /// <remarks>
    /// The TS test fetches the Next.js <c>/doceditor</c> page and scans its HTML. That page is a client
    /// application the integration-test AppHost does not run, so the page itself cannot be fetched here.
    /// The page is rendered on the server from the API answers an anonymous holder of the link gets
    /// (<c>GET files/file/{id}</c> and <c>GET files/file/{id}/openedit</c> with the request token), so this
    /// asserts that those raw answers do not contain the emails or display names of either person.
    /// </remarks>
    [Fact]
    [Trait("Bug", "80495")]
    public async Task ExternalLinkAnonymousResponses_DoNotExposeRoomCreatorAndLinkCreator()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Share Link Privacy Room");
        var file = await CreateFile("Autotest Share Link Privacy File.docx", room.Id);

        var linkCreator = await InviteContact(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, linkCreator, FileShare.RoomManager);

        var ownerName = file.CreatedBy.DisplayName;

        await _filesClient.Authenticate(linkCreator);
        var linkCreatorName = (await CreateFile("Autotest Link Creator File.docx", room.Id)).CreatedBy.DisplayName;

        var link = (await _filesApi.CreateFilePrimaryExternalLinkAsync(
            file.Id,
            new FileLinkRequest(access: FileShare.Read),
            TestContext.Current.CancellationToken)).Response;

        // Act - open the link anonymously
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, link.SharedLink.RequestToken);

        var info = await _filesApi.GetFileInfoWithHttpInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);
        var openEdit = await _filesApi.OpenEditFileWithHttpInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        var forbidden = new[]
        {
            Owner.Email,
            Owner.Email.Replace("@", "%40"),
            ownerName,
            linkCreator.Email,
            linkCreator.Email.Replace("@", "%40"),
            linkCreatorName
        };

        foreach (var body in new[] { info.RawContent, openEdit.RawContent })
        {
            foreach (var value in forbidden)
            {
                body.Should().NotContain(value);
            }
        }
    }
}
