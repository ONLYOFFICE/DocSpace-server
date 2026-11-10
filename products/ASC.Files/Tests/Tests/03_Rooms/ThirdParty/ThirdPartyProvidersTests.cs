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
/// The read-only catalogue endpoints of the third-party integration:
/// <c>GET /files/thirdparty/providers</c>, <c>/capabilities</c> and <c>/common</c>. None of them
/// connects anything, so no Nextcloud account is needed.
/// </summary>
/// <remarks>
/// The OAuth services (Box, Dropbox, OneDrive) are always listed by <c>providers</c>, but their
/// <c>connected</c> flag, client id and redirect URL — and their presence in <c>capabilities</c> —
/// depend on an OAuth application being configured for the portal, which the test environment does
/// not guarantee. Assertions that need one skip when the service reports itself as not available.
/// </remarks>
[Trait("Category", "Rooms")]
public class ThirdPartyProvidersTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    private static readonly string[] _expectedProviderNames = ["Box", "Dropbox", "OneDrive", "kDrive", "WebDav", "Nextcloud", "ownCloud"];

    private async Task<List<ProviderDto>> GetProviders(bool? excludeWebDav = null)
    {
        return (await _thirdPartyApi.GetAllProvidersAsync(excludeWebDav, TestContext.Current.CancellationToken)).Response;
    }

    [Fact]
    public async Task GetAllProviders_Owner_ListsKnownProvidersWithCompleteEntries()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var providers = await GetProviders();

        // Assert
        providers.Select(p => p.Name).Should().Contain(_expectedProviderNames);
        providers.Should().OnlyContain(p => !string.IsNullOrEmpty(p.Name) && !string.IsNullOrEmpty(p.Key));
    }

    [Fact]
    public async Task GetAllProviders_Nextcloud_IsReportedUnderProviderKeyWebDav()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var providers = await GetProviders();

        // Assert
        var nextcloud = providers.Should().ContainSingle(p => p.Name == "Nextcloud").Subject;
        nextcloud.Key.Should().Be(NextcloudProviderKey);
        nextcloud.Oauth.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllProviders_OAuthProviders_ExposeRedirectUrlAndClientId()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var providers = await GetProviders();

        // Act + Assert
        foreach (var name in new[] { "Box", "Dropbox", "OneDrive" })
        {
            var provider = providers.Should().ContainSingle(p => p.Name == name).Subject;

            Assert.SkipUnless(provider.Connected, $"No OAuth application is configured for {name} on this environment.");

            provider.Oauth.Should().BeTrue(name);
            provider.RedirectUrl.Should().NotBeNullOrEmpty(name);
            provider.ClientId.Should().NotBeNullOrEmpty(name);
        }
    }

    /// <remarks>
    /// The TypeScript suite expects exactly Box, Dropbox and OneDrive here. The portal's own
    /// configuration may enable further OAuth services (Google Drive is in the default
    /// <c>files:thirdparty:enable</c> list), so the set is checked by what must be there and what
    /// must not: every remaining entry is an OAuth service and none belongs to the WebDAV family.
    /// </remarks>
    [Fact]
    public async Task GetAllProviders_ExcludeWebDav_KeepsOnlyOAuthServices()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var providers = await GetProviders(excludeWebDav: true);

        // Assert
        providers.Select(p => p.Name).Should().Contain(["Box", "Dropbox", "OneDrive"]);
        providers.Should().OnlyContain(p => p.Oauth && p.Key != NextcloudProviderKey);
    }

    [Fact]
    public async Task GetAllProviders_ExcludeWebDavFalse_SameAsDefault()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var withDefault = await GetProviders();
        var withFalse = await GetProviders(excludeWebDav: false);

        // Assert
        withFalse.Select(p => p.Name).Should().BeEquivalentTo(withDefault.Select(p => p.Name));
    }

    [Fact]
    public async Task GetAllProviders_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.GetAllProvidersAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    /// <remarks>
    /// The TypeScript suite also expects a <c>Box</c> entry; it only appears once a Box OAuth
    /// application is configured, which the test environment does not guarantee, so it is left out.
    /// </remarks>
    [Fact]
    public async Task GetCapabilities_Owner_ListsWebDavFamilyKeys()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var capabilities = (await _thirdPartyApi.GetCapabilitiesAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        capabilities.Select(entry => entry[0]).Should().Contain(["WebDav", "kDrive"]);
    }

    [Fact]
    public async Task GetCapabilities_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.GetCapabilitiesAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetCommonThirdPartyFolders_NoLegacyConnections_ReturnsEmptyList()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var folders = (await _thirdPartyApi.GetCommonThirdPartyFoldersAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        folders.Should().BeEmpty();
    }
}
