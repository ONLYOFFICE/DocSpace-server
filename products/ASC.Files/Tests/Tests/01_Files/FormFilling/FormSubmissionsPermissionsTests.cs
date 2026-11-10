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
/// Access-level scenarios of <c>GET /files/file/{fileId}/submissions</c>: any membership of the
/// form filling room is enough to read the results (the endpoint documents that every member of the
/// room can), a caller without access to the room is refused with 403.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "FormFilling")]
public class FormSubmissionsPermissionsTests(
    AspireAppFixture fixture)
    : FormResultsTestBase(fixture)
{
    [Theory]
    [InlineData(EmployeeType.RoomAdmin, FileShare.RoomManager)]
    [InlineData(EmployeeType.RoomAdmin, FileShare.ContentCreator)]
    [InlineData(EmployeeType.User, FileShare.ContentCreator)]
    [InlineData(EmployeeType.User, FileShare.FillForms)]
    public async Task GetFormSubmissions_RoomMember_CanRead(EmployeeType type, FileShare access)
    {
        // Arrange
        var (roomId, formId) = await CreateStartedForm("Autotest Submissions Member Room");
        var member = await InviteMember(type);
        await InviteToRoom(roomId, member, access);
        await _filesClient.Authenticate(member);

        // Act
        var result = (await _filesApi.GetFormSubmissionsAsync(formId, TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Submissions.Should().NotBeNull();
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetFormSubmissions_WithoutRoomAccess_Returns403(EmployeeType type)
    {
        // Arrange
        var (_, formId) = await CreateStartedForm("Autotest Submissions No Access Room");
        var outsider = await InviteMember(type);
        await _filesClient.Authenticate(outsider);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFormSubmissionsAsync(formId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFormSubmissions_Unauthenticated_Returns401()
    {
        // Arrange
        var (_, formId) = await CreateStartedForm("Autotest Submissions Anonymous Room");
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFormSubmissionsAsync(formId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
