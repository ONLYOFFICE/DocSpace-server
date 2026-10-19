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
/// Access-level scenarios of <c>POST /files/file/{fileId}/xlsx</c> and <c>GET /files/file/{fileId}/xlsx</c>.
/// Queueing the report needs the right to update it (room managers and content creators), while
/// reading the task is open to any authenticated caller and reports only the caller's own run.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "FormFilling")]
public class FormXlsxPermissionsTests(
    AspireAppFixture fixture)
    : FormResultsTestBase(fixture)
{
    [Theory]
    [InlineData(EmployeeType.RoomAdmin, FileShare.RoomManager)]
    [InlineData(EmployeeType.RoomAdmin, FileShare.ContentCreator)]
    [InlineData(EmployeeType.User, FileShare.ContentCreator)]
    public async Task GenerateXlsx_MemberWhoCanManageTheForm_Succeeds(EmployeeType type, FileShare access)
    {
        // Arrange
        var (roomId, formId) = await CreateStartedForm("Autotest Xlsx Manager Room");
        var member = await InviteMember(type);
        await InviteToRoom(roomId, member, access);
        await _filesClient.Authenticate(member);

        // Act
        var result = (await _filesApi.GenerateXlsxAsync(formId, TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Form.Id.Should().Be(formId);
    }

    [Theory]
    [InlineData(EmployeeType.User, FileShare.FillForms)]
    [InlineData(EmployeeType.RoomAdmin, null)]
    [InlineData(EmployeeType.User, null)]
    [InlineData(EmployeeType.Guest, null)]
    public async Task GenerateXlsx_MemberWhoCannotManageTheForm_Returns403(EmployeeType type, FileShare? access)
    {
        // Arrange
        var (roomId, formId) = await CreateStartedForm("Autotest Xlsx Denied Room");

        // Positive control: the owner can generate the report for this very form.
        await _filesApi.GenerateXlsxAsync(formId, TestContext.Current.CancellationToken);

        var member = await InviteMember(type);
        if (access is not null)
        {
            await InviteToRoom(roomId, member, access.Value);
        }

        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GenerateXlsxAsync(formId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetXlsx_MemberWithoutRoomAccess_DoesNotSeeTheOwnersTask()
    {
        // Arrange
        var (_, formId) = await CreateStartedForm("Autotest Xlsx Isolation Room");
        await _filesApi.GenerateXlsxAsync(formId, TestContext.Current.CancellationToken);

        // Positive control: the generating user sees the task.
        var ownerTask = (await _filesApi.GetXlsxAsync(formId, TestContext.Current.CancellationToken)).Response;
        ownerTask.Id.Should().NotBeNullOrEmpty();

        var member = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(member);

        // Act
        var result = await _filesApi.GetXlsxAsync(formId, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeNull();
    }

    [Fact]
    public async Task GenerateXlsx_Unauthenticated_Returns401()
    {
        // Arrange
        var (_, formId) = await CreateStartedForm("Autotest Xlsx Anonymous Generate Room");
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GenerateXlsxAsync(formId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetXlsx_Unauthenticated_Returns401()
    {
        // Arrange
        var (_, formId) = await CreateStartedForm("Autotest Xlsx Anonymous Read Room");
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetXlsxAsync(formId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
