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

namespace ASC.Files.Tests.Tests._03_Rooms.ThirdParty;

/// <summary>
/// The backup storage account without a live storage service: who may connect it, the switch, the empty read, and
/// that a connection the service cannot confirm leaves no backup account behind.
/// </summary>
[Trait("Category", "Rooms")]
public class ThirdPartyBackupAccessTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    private const string UnreachableWebDavUrl = "http://127.0.0.1:1/remote.php/dav/";

    private const string CreateDeniedMessage = "You don't have enough permission to create";

    private static ThirdPartyBackupRequestDto UnreachableBackupRequest()
    {
        return new ThirdPartyBackupRequestDto(
            url: UnreachableWebDavUrl,
            login: "autotest",
            password: "autotest",
            customerTitle: UniqueTitle("Backup"),
            providerKey: "WebDav");
    }

    [Fact]
    public async Task GetBackupThirdPartyAccount_NoneConnected_ReturnsNothing()
    {
        // Act
        var folder = (await _thirdPartyApi.GetBackupThirdPartyAccountAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        folder.Should().BeNull();
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task SaveThirdPartyBackup_NotDocSpaceAdmin_ThrowsForbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(await InviteMember(employeeType));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.SaveThirdPartyBackupAsync(UnreachableBackupRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain(CreateDeniedMessage);
    }

    [Fact]
    public async Task SaveThirdPartyBackup_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.SaveThirdPartyBackupAsync(UnreachableBackupRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task SaveThirdPartyBackup_ThirdPartySwitchOff_ThrowsForbidden()
    {
        // Arrange
        await _filesSettingsApi.ChangeAccessToThirdpartyAsync(new SettingsRequestDto { Set = false }, TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.SaveThirdPartyBackupAsync(UnreachableBackupRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain(CreateDeniedMessage);
    }

    [Fact]
    public async Task SaveThirdPartyBackup_DocSpaceAdminUnreachableWebDavAddress_NoBackupAccountStored()
    {
        // Arrange
        await _filesClient.Authenticate(await InviteContact(EmployeeType.DocSpaceAdmin));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.SaveThirdPartyBackupAsync(UnreachableBackupRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorContent!.ToString().Should().NotContain(CreateDeniedMessage);
        await _filesClient.Authenticate(Owner);
        (await _thirdPartyApi.GetBackupThirdPartyAccountAsync(TestContext.Current.CancellationToken)).Response.Should().BeNull();
    }
}
