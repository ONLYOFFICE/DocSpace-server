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
/// The refusals and listings of the third-party integration that need no live storage service: who may list the
/// services and connect an account, what an unknown account id answers, and that a connection the service cannot
/// confirm stores nothing.
/// </summary>
[Trait("Category", "Rooms")]
public class ThirdPartyAccessRefusalTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    /// <summary>
    /// A WebDAV address on the loopback interface where nothing listens: the connection attempt is refused at once,
    /// so the credentials check fails without a live storage service.
    /// </summary>
    private const string UnreachableWebDavUrl = "http://127.0.0.1:1/remote.php/dav/";

    private const string CreateDeniedMessage = "You don't have enough permission to create";

    private static readonly string[] _addressRequiredPresets = ["kDrive", "WebDav", "Nextcloud", "ownCloud"];

    private static ThirdPartyRequestDto UnreachableWebDavRequest(int? providerId = null)
    {
        return new ThirdPartyRequestDto(
            url: UnreachableWebDavUrl,
            login: "autotest",
            password: "autotest",
            customerTitle: UniqueTitle("Unreachable"),
            providerKey: "WebDav",
            providerId: providerId);
    }

    [Fact]
    public async Task GetAllProviders_WebDavPresets_ShareKeyWebDavAndAllButYandexRequireAddress()
    {
        // Act
        var providers = (await _thirdPartyApi.GetAllProvidersAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        foreach (var name in _addressRequiredPresets)
        {
            var preset = providers.Should().ContainSingle(p => p.Name == name).Subject;
            preset.Key.Should().Be(NextcloudProviderKey, name);
            preset.RequiredConnectionUrl.Should().BeTrue(name);
        }

        // Yandex is offered only where the installation enables it; when it is, it is the one preset without an address
        var yandex = providers.SingleOrDefault(p => p.Name == "Yandex");

        if (yandex is not null)
        {
            yandex.Key.Should().Be(NextcloudProviderKey);
            yandex.RequiredConnectionUrl.Should().BeFalse();
        }
    }

    [Fact]
    public async Task GetAllProviders_Guest_ReturnsEmptyList()
    {
        // Arrange
        await _filesClient.Authenticate(await InviteGuest());

        // Act
        var providers = (await _thirdPartyApi.GetAllProvidersAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        providers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCapabilities_Guest_ReturnsEmptyList()
    {
        // Arrange
        await _filesClient.Authenticate(await InviteGuest());

        // Act
        var capabilities = (await _thirdPartyApi.GetCapabilitiesAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        capabilities.Should().BeEmpty();
    }

    [Fact]
    public async Task ProviderLists_ThirdPartySwitchOff_ReturnEmptyListsToOwner()
    {
        // Arrange
        await _filesSettingsApi.ChangeAccessToThirdpartyAsync(new SettingsRequestDto { Set = false }, TestContext.Current.CancellationToken);

        // Act
        var providers = (await _thirdPartyApi.GetAllProvidersAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        var capabilities = (await _thirdPartyApi.GetCapabilitiesAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        providers.Should().BeEmpty();
        capabilities.Should().BeEmpty();
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task SaveThirdParty_UserOrGuest_ThrowsForbiddenBeforeContactingService(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(await InviteMember(employeeType));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.SaveThirdPartyAsync(UnreachableWebDavRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain(CreateDeniedMessage);
    }

    [Fact]
    public async Task SaveThirdParty_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.SaveThirdPartyAsync(UnreachableWebDavRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    /// <remarks>
    /// The room admin and the DocSpace admin pass the access check (their error is not the create refusal a user
    /// gets) and then fail on the credentials check; nothing is stored, so the account list stays empty.
    /// </remarks>
    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task SaveThirdParty_UnreachableWebDavAddress_RefusedAndNothingStored(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(await InviteMember(employeeType));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.SaveThirdPartyAsync(UnreachableWebDavRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorContent!.ToString().Should().NotContain(CreateDeniedMessage);
        (await ConnectedAccountTitles()).Should().BeEmpty();
    }

    [Fact]
    public async Task SaveThirdParty_UnknownProviderId_ThrowsNotFound()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.SaveThirdPartyAsync(UnreachableWebDavRequest(int.MaxValue), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteThirdParty_UnknownProviderId_ThrowsNotFound()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.DeleteThirdPartyAsync(int.MaxValue, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteThirdParty_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.DeleteThirdPartyAsync(int.MaxValue, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetThirdPartyAccounts_User_ReturnsEmptyList()
    {
        // Arrange
        await _filesClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var accounts = (await _thirdPartyApi.GetThirdPartyAccountsAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        accounts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetThirdPartyAccounts_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.GetThirdPartyAccountsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
