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
/// POST /files/thirdparty with <c>providerKey=kDrive</c>. The connection used to ignore <c>url</c>
/// and always go to the legacy shared host, whose root only lists the account's drives: the
/// connection was accepted, but no room could be created on it. A new kDrive connection now needs
/// <c>url</c> like any other WebDAV one, and a request without it is refused before the portal
/// contacts Infomaniak, so no kDrive account is needed.
/// </summary>
[Trait("Category", "Rooms")]
public class ThirdPartyKDriveConnectBugTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    private const string KDriveProviderKey = "kDrive";
    private const string KDriveLogin = "autotest@example.com";

    // FolderType.VirtualRooms and FolderType.USER: a room account and a "My documents" one, which are
    // re-authenticated by different code paths.
    private const int VirtualRoomsFolderType = 14;
    private const int MyDocumentsFolderType = 5;

    /// <remarks>
    /// Bug 84672: the providers list offered kDrive without <c>requiredConnectionUrl</c>, so the
    /// connection form never asked for the drive's address.
    /// </remarks>
    [Fact]
    [Trait("Bug", "84672")]
    public async Task GetAllProviders_KDrive_RequiresConnectionUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var providers = (await _thirdPartyApi.GetAllProvidersAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var kDrive = providers.Should().ContainSingle(p => p.Name == KDriveProviderKey).Subject;
        kDrive.RequiredConnectionUrl.Should().BeTrue("a kDrive account is reachable only at the address of its drive");
    }

    /// <remarks>
    /// Bug 84672: without <c>url</c> the connection went to the legacy shared host and was saved
    /// as soon as the credentials were accepted there. An address that is blank, cannot be parsed
    /// or is not HTTP is refused the same way.
    /// </remarks>
    [Theory]
    [Trait("Bug", "84672")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("ftp://123456.connect.kdrive.infomaniak.com")]
    public async Task SaveThirdParty_KDriveWithoutValidUrl_BadRequest(string url)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var title = UniqueTitle("kDrive Invalid Url");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.SaveThirdPartyAsync(KDriveRequest(title, url), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400, url);
        (await ConnectedAccountTitles()).Should().NotContain(title);
    }

    /// <remarks>
    /// Bug 84672: a connection saved before the address was required has none and works at the
    /// legacy shared host, but its re-authentication was checked without that host and refused as
    /// an invalid address — a room account cannot even send one, it keeps the stored address. The
    /// legacy host is asked with a wrong password, so the call still fails (403, or 500 without
    /// access to Infomaniak); what is asserted is that the address is no longer the reason.
    /// </remarks>
    [Theory]
    [Trait("Bug", "84672")]
    [InlineData(VirtualRoomsFolderType)]
    [InlineData(MyDocumentsFolderType)]
    public async Task SaveThirdParty_ReauthenticateLegacyKDrive_NotRefusedForAddress(int folderType)
    {
        // Arrange
        var title = UniqueTitle("kDrive Legacy");
        var providerId = await PlantLegacyKDrive(title, folderType);
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.SaveThirdPartyAsync(
                new ThirdPartyRequestDto(
                    login: KDriveLogin,
                    password: WrongPassword,
                    customerTitle: title,
                    providerKey: KDriveProviderKey,
                    providerId: providerId),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().NotBe(400, exception.ErrorContent?.ToString());
        exception.ErrorContent?.ToString().Should().NotContain("Invalid connection URL");
    }

    private static ThirdPartyRequestDto KDriveRequest(string customerTitle, string url)
    {
        return new ThirdPartyRequestDto(
            url: url,
            login: KDriveLogin,
            password: WrongPassword,
            customerTitle: customerTitle,
            providerKey: KDriveProviderKey);
    }

    /// <summary>
    /// Writes the owner's kDrive connection the way it was saved before the address was required:
    /// with an empty <c>url</c>. No API call writes that row any more, so it is planted directly,
    /// with the password encrypted the way the services encrypt it.
    /// </summary>
    private async Task<int> PlantLegacyKDrive(string customerTitle, int folderType)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["core:machinekey"] = Fixture.GetAppHostSetting("core:machinekey") })
            .Build();
        var password = await new InstanceCrypto(new MachinePseudoKeys(configuration)).EncryptAsync("legacy-password");

        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO files_thirdparty_account
                (tenant_id, provider, customer_title, user_name, password, token, url, user_id, folder_type, room_type, private, has_logo, create_on, modified_on)
            VALUES
                (@tenantId, 'kDrive', @title, @login, @password, '', '', @userId, @folderType, 0, 0, 0, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();
            """;

        command.Parameters.AddWithValue("@tenantId", TenantId);
        command.Parameters.AddWithValue("@title", customerTitle);
        command.Parameters.AddWithValue("@login", KDriveLogin);
        command.Parameters.AddWithValue("@password", password);
        command.Parameters.AddWithValue("@userId", Owner.Id.ToString());
        command.Parameters.AddWithValue("@folderType", folderType);

        return Convert.ToInt32(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }
}
