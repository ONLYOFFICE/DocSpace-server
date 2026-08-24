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
/// Access-level scenarios of <c>POST /files/masterform/:fileId/checkfillformdraft</c>.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "FormFilling")]
public class CheckFillFormDraftPermissionsTests(
    AspireAppFixture fixture)
    : FormFillingTestBase(fixture)
{
    private async Task<int> SetupForm()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest CheckFillFormDraft Perm Room " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);
        await StartFormFilling(form.Id);

        return form.Id;
    }

    /// <summary>
    /// BUG 81441: the endpoint is <c>[AllowAnonymous]</c>, so ASP.NET's own auth middleware never
    /// rejects an unauthenticated caller with 401 - the app-level check further downstream throws a
    /// plain <c>SecurityException</c>, which the API maps to 403 instead.
    /// </summary>
    [Trait("Bug", "81441")]
    [Fact]
    public async Task CheckFillFormDraft_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var formId = await SetupForm();
        await _filesClient.Authenticate(null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CheckFillFormDraftAsync(
                formId, new CheckFillFormDraft(version: 1), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task CheckFillFormDraft_UserWithoutRoomAccess_ReturnsForbidden()
    {
        // Arrange
        var formId = await SetupForm();
        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CheckFillFormDraftAsync(
                formId, new CheckFillFormDraft(version: 1), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CheckFillFormDraft_GuestWithoutRoomAccess_ReturnsForbidden()
    {
        // Arrange
        var formId = await SetupForm();
        var guest = await InviteMember(EmployeeType.Guest);
        await _filesClient.Authenticate(guest);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CheckFillFormDraftAsync(
                formId, new CheckFillFormDraft(version: 1), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}
