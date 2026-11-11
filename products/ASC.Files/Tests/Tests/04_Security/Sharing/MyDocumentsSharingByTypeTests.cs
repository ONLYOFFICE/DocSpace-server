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


namespace ASC.Files.Tests.Tests._04_Security.Sharing;

/// <summary>
/// Sharing from My documents by sharer type: a quick filling link on a PDF form in My documents is open to
/// every type that has the section, and a guest the sharer may not share with is skipped and gets no access
/// to the file.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Sharing")]
public class MyDocumentsSharingByTypeTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task CreatePrimaryExternalLink_FormInMyDocuments_FillingLinkCreated(EmployeeType sharerType)
    {
        // Arrange
        var sharer = await InviteContact(sharerType);
        await _filesClient.Authenticate(sharer);
        var form = await CreateFileInMy($"Autotest {sharerType} Form.pdf", sharer);

        // Act
        var link = (await _filesApi.CreateFilePrimaryExternalLinkAsync(form.Id, new FileLinkRequest(access: FileShare.FillForms), TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Access.Should().Be(FileShare.FillForms);

        var primary = (await _filesApi.GetFilePrimaryExternalLinkAsync(form.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        primary.Access.Should().Be(FileShare.FillForms);
    }

    /// <summary>
    /// A User sharing with a guest, and a Room admin sharing with a guest someone else invited: the request
    /// succeeds, the guest is skipped, and the guest cannot open the file.
    /// </summary>
    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task SetFileSecurityInfo_SkippedGuest_CannotOpenFile(EmployeeType sharerType)
    {
        // Arrange
        var sharer = await InviteContact(sharerType);
        var guest = await InviteGuest();

        await _filesClient.Authenticate(sharer);
        var file = await CreateFileInMy("Autotest Skipped Guest.docx", sharer);

        await _sharingApi.SetFileSecurityInfoAsync(
            file.Id,
            new SecurityInfoSimpleRequestDto { Share = [new() { ShareTo = guest.Id, Access = FileShare.Read }] },
            TestContext.Current.CancellationToken);

        var shares = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        shares.Should().NotContain(s => s.SharedToUser != null && s.SharedToUser.Id == guest.Id);

        // Act
        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
