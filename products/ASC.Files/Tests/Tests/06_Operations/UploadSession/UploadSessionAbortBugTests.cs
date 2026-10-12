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

namespace ASC.Files.Tests.Tests._06_Operations.UploadSession;

/// <summary>
/// <c>DELETE /api/2.0/files/{folderId}/session/{sessionId}</c> (<c>abortUploadSession</c>) - the
/// two bugs open against this endpoint: a wrong status code for a session that never existed, and
/// a missing access check that lets any authenticated user abort someone else's upload.
/// </summary>
[Trait("Category", "Bug")]
[Trait("Feature", "Files")]
public class UploadSessionAbortBugTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <remarks>
    /// BUG 82278: aborting a session whose id never existed answers 500 instead of the 404 a missing
    /// resource calls for - the controller does not check whether the session was found before
    /// touching it.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82278")]
    public async Task AbortUploadSession_NonExistentSessionId_ReturnsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.AbortUploadSessionAsync(
                "00000000-0000-0000-0000-000000000000", myDocsFolderId, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    /// <remarks>
    /// BUG 82276: abortUploadSession never checks that the caller owns the session or has
    /// ManageRoom rights over the room it belongs to - any authenticated room member, no matter how
    /// little access they were granted, can abort another member's session. A ContentCreator
    /// aborting the room owner's session has to be refused.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82276")]
    public async Task AbortUploadSession_ContentCreatorAbortsAnotherMembersSession_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest AbortSession Perm CrossUser Room");

        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.ContentCreator);

        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            room.Id,
            new SessionRequest("Autotest AbortSession Perm CrossUser Owner.docx", 256, createNewIfExist: true),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        await _filesClient.Authenticate(member);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.AbortUploadSessionAsync(
                session.Id, room.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    /// <remarks>
    /// BUG 82276: same missing check, for a user who was never invited to the room at all.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82276")]
    public async Task AbortUploadSession_UserWithoutRoomAccessAbortsSession_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest AbortSession Perm NoAccess Room");

        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            room.Id,
            new SessionRequest("Autotest AbortSession Perm NoAccess Owner.docx", 256, createNewIfExist: true),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        var outsider = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(outsider);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.AbortUploadSessionAsync(
                session.Id, room.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}
