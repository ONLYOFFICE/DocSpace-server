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
/// Shared setup for the third-party (WebDAV/Nextcloud) suites.
/// </summary>
/// <remarks>
/// Nextcloud is the only provider we have real credentials for — the OAuth providers
/// (Box, GoogleDrive, OneDrive, Dropbox) need a token no test can mint. The credentials come
/// from the environment under the same names the TypeScript suite uses
/// (<c>NEXTCLOUD_URL</c> / <c>NEXTCLOUD_LOGIN</c> / <c>NEXTCLOUD_PASSWORD</c>), so a single
/// <c>.env</c> serves both suites. Without them these tests skip rather than fail: they need an
/// external host that CI may not be able to reach.
/// </remarks>
public abstract class ThirdPartyTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    /// <summary>
    /// A password that is never correct, used to assert that credentials are actually verified.
    /// </summary>
    protected const string WrongPassword = "definitely-wrong-password";

    private static string NextcloudUrl => Environment.GetEnvironmentVariable("NEXTCLOUD_URL") ?? "";
    private static string NextcloudLogin => Environment.GetEnvironmentVariable("NEXTCLOUD_LOGIN") ?? "";
    private static string NextcloudPassword => Environment.GetEnvironmentVariable("NEXTCLOUD_PASSWORD") ?? "";

    /// <summary>
    /// Skips the current test unless a reachable Nextcloud is configured in the environment.
    /// </summary>
    protected static void RequireNextcloud()
    {
        Assert.SkipWhen(
            string.IsNullOrEmpty(NextcloudUrl) || string.IsNullOrEmpty(NextcloudLogin) || string.IsNullOrEmpty(NextcloudPassword),
            "Nextcloud is not configured: set NEXTCLOUD_URL, NEXTCLOUD_LOGIN and NEXTCLOUD_PASSWORD.");
    }

    /// <summary>
    /// Builds a connect request for the configured Nextcloud account.
    /// </summary>
    /// <param name="customerTitle">The title the connection is saved under.</param>
    /// <param name="password">The password to authenticate with; defaults to the correct one.</param>
    protected static ThirdPartyRequestDto NextcloudRequest(string customerTitle, string? password = null)
    {
        // The generated model validates its required properties inside the constructor, so
        // customerTitle and providerKey have to be passed as arguments — an object initializer
        // throws ArgumentNullException before it ever assigns them.
        return new ThirdPartyRequestDto(
            url: NextcloudUrl,
            login: NextcloudLogin,
            password: password ?? NextcloudPassword,
            customerTitle: customerTitle,
            // "Nextcloud" is a labelled preset over plain WebDAV — the API reports the
            // resulting connection back under providerKey "WebDav".
            providerKey: "Nextcloud");
    }

    /// <summary>
    /// Connects the configured Nextcloud account and returns the third-party folder it created.
    /// </summary>
    protected async Task<FolderDtoString> ConnectNextcloud(string customerTitle)
    {
        var response = await _thirdPartyApi.SaveThirdPartyAsync(
            NextcloudRequest(customerTitle), TestContext.Current.CancellationToken);

        return response.Response;
    }

    /// <summary>
    /// Returns the titles of every currently connected third-party account.
    /// </summary>
    protected async Task<List<string>> ConnectedAccountTitles()
    {
        var accounts = await _thirdPartyApi.GetThirdPartyAccountsAsync(TestContext.Current.CancellationToken);

        return accounts.Response.Select(a => a.CustomerTitle).ToList();
    }
}
