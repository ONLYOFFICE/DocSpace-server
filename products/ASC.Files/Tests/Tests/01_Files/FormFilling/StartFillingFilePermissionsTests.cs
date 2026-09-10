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

namespace ASC.Files.Tests.Tests._01_Files.FormFilling;

/// <summary>
/// Access-level scenarios of <c>PUT /files/file/:fileId/startfilling</c>.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "FormFilling")]
public class StartFillingFilePermissionsTests(
    AspireAppFixture fixture)
    : FormFillingTestBase(fixture)
{
    private async Task<int> SetupStartedForm()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest StartFilling Perm Room " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);
        await StartFormFilling(form.Id);

        return form.Id;
    }

    [Fact]
    public async Task StartFillingFile_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var formId = await SetupStartedForm();
        await _filesClient.Authenticate(null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.StartFillingFileAsync(formId, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task StartFillingFile_DocSpaceAdmin_ReturnsForm()
    {
        // Arrange
        var formId = await SetupStartedForm();
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        var result = (await _filesApi.StartFillingFileAsync(formId, TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Id.Should().Be(formId);
    }

    /// <summary>
    /// BUG 81400: a <see cref="EmployeeType.User"/> with no access to the room at all was answered
    /// 200 instead of 403 - <c>StartFillingAsync</c> only rejected callers holding an explicit
    /// <see cref="FileShare.FillForms"/> ACE on the room, not those holding none. Fixed by rejecting
    /// callers with no room membership unless they can manage the room (owner/portal admin).
    /// </summary>
    [Trait("Bug", "81400")]
    [Fact]
    public async Task StartFillingFile_UserWithoutFileAccess_ReturnsForbidden()
    {
        // Arrange
        var formId = await SetupStartedForm();
        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.StartFillingFileAsync(formId, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>See <see cref="StartFillingFile_UserWithoutFileAccess_ReturnsForbidden"/>, for a guest.</summary>
    [Trait("Bug", "81400")]
    [Fact]
    public async Task StartFillingFile_GuestWithoutFileAccess_ReturnsForbidden()
    {
        // Arrange
        var formId = await SetupStartedForm();
        var guest = await InviteMember(EmployeeType.Guest);
        await _filesClient.Authenticate(guest);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.StartFillingFileAsync(formId, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}
