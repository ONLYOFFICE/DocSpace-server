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
/// POST /files/thirdparty and POST /files/thirdparty/backup with a storage that cannot be reached. The credentials
/// are checked by asking the storage, and the connection error of that request reached the caller as a 500. It is a
/// failure of the upstream storage, so it is now a 502, and nothing is saved. The address is a WebDAV one under the
/// reserved <c>.invalid</c> domain, which never resolves, so the request fails at the name lookup.
/// Re-authenticating a saved connection at an unreachable address has no case: this route always saves to the rooms
/// storage, whose update takes the stored address instead of the one sent, so the address never reaches the check.
/// </summary>
[Trait("Category", "Rooms")]
public class ThirdPartyStorageUnreachableTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    private const string UnreachableUrl = "http://autotest-unreachable.invalid/webdav";
    private const string UnreachableError = "could not be reached to check the credentials";

    [Fact]
    public async Task SaveThirdParty_UnreachableStorage_Returns502AndConnectsNothing()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var title = UniqueTitle("Unreachable");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.SaveThirdPartyAsync(
                new ThirdPartyRequestDto(
                    url: UnreachableUrl,
                    login: "autotest",
                    password: WrongPassword,
                    customerTitle: title,
                    providerKey: NextcloudProviderKey),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(502);
        ErrorMessageOf(exception.ErrorContent!.ToString()).Should().Contain(UnreachableError);
        (await ConnectedAccountTitles()).Should().NotContain(title);
    }

    [Fact]
    public async Task SaveThirdPartyBackup_UnreachableStorage_Returns502()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.SaveThirdPartyBackupAsync(
                new ThirdPartyBackupRequestDto(
                    url: UnreachableUrl,
                    login: "autotest",
                    password: WrongPassword,
                    customerTitle: UniqueTitle("Unreachable Backup"),
                    providerKey: NextcloudProviderKey),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(502);
        ErrorMessageOf(exception.ErrorContent!.ToString()).Should().Contain(UnreachableError);
    }
}
