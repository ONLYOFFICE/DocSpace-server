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

namespace ASC.Files.Tests.Tests._07_Settings.Editor;

/// <summary>GET /files/module - the "Documents" module information.</summary>
[Trait("Category", "Settings")]
public class GetFilesModuleTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    public static TheoryData<EmployeeType?> AllowedRoles =>
    [
        null, // the portal owner
        EmployeeType.DocSpaceAdmin,
        EmployeeType.RoomAdmin,
        EmployeeType.User,
        EmployeeType.Guest
    ];

    [Fact]
    public async Task GetFilesModule_ReturnsModuleWithExpectedFields()
    {
        // Act
        var module = (await _filesSettingsApi.GetFilesModuleAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        module.Should().NotBeNull();
        module.Id.Should().NotBeEmpty();
        module.Title.Should().NotBeNullOrEmpty();
        module.Link.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetFilesModule_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.GetFilesModuleAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [MemberData(nameof(AllowedRoles))]
    public async Task GetFilesModule_EveryRole_CanRead(EmployeeType? employeeType)
    {
        // Arrange
        if (employeeType != null)
        {
            var member = await InviteMember(employeeType.Value);
            await _filesClient.Authenticate(member);
        }

        // Act
        var module = (await _filesSettingsApi.GetFilesModuleAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        module.Title.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetFilesModule_TerminatedUser_Unauthorized()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);

        await _filesClient.Authenticate(user);

        await _peopleClient.Authenticate(Owner);
        await _userStatusApi.UpdateUserStatusAsync(
            EmployeeStatus.Terminated,
            new UpdateMembersRequestDto([user.Id], resendAll: false),
            TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.GetFilesModuleAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
