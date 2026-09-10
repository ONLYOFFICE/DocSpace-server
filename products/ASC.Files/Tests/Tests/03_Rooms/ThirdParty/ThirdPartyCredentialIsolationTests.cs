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
/// POST /files/thirdparty — every connect request must be authenticated with the credentials it
/// carries, and a connection that fails to authenticate must not be saved.
/// </summary>
[Trait("Category", "Rooms")]
public class ThirdPartyCredentialIsolationTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    private const string WrongPasswordTitle = "Autotest Session Reuse Wrong";

    /// <summary>
    /// The control case: with no prior connection to the host, a wrong password is rejected. If
    /// this one fails, credential checking is broken outright rather than leaking between
    /// requests, and <see cref="SaveThirdParty_WrongPasswordAfterCorrectConnection_AccessDenied"/>
    /// tells you nothing.
    /// </summary>
    [Fact]
    public async Task SaveThirdParty_WrongPassword_AccessDenied()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.SaveThirdPartyAsync(
                NextcloudRequest("Autotest Wrong Password", WrongPassword),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    /// <summary>
    /// Connections to one host must be isolated by credentials: a successful connect must not
    /// leave behind an authenticated session that a later request with a different password can
    /// piggyback on.
    /// </summary>
    /// <remarks>
    /// Bug 83399: connecting a correct Nextcloud account and then connecting the same host with a
    /// wrong password returned 200 and saved the connection. <c>WebDavStorage</c> built its client
    /// from the default <c>IHttpClientFactory</c> client, whose pooled handler keeps one
    /// process-wide <c>CookieContainer</c>, so the session cookie Nextcloud issued on the
    /// successful login was replayed on the next request to that host and authorized it despite
    /// the wrong password — the PROPFIND came back 207 instead of 401. Fixed by building the
    /// client from <c>customHttpClientNoCookie</c>, so no session is carried between requests.
    /// The second wrong-password attempt is kept because the replayed session did not always
    /// survive as far as the first one.
    /// </remarks>
    [Fact]
    [Trait("Bug", "83399")]
    public async Task SaveThirdParty_WrongPasswordAfterCorrectConnection_AccessDenied()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);

        var connected = await ConnectNextcloud("Autotest Session Reuse Correct");
        connected.ProviderId.Should().BeGreaterThan(0, "the correct connection is the arrange step");

        // Act — both attempts run before either is asserted on, so a failed assertion on the
        // first does not hide the outcome of the second.
        var first = await WrongPasswordAttempt();
        var second = await WrongPasswordAttempt();

        // Assert
        var titles = await ConnectedAccountTitles();
        titles.Should().NotContain(WrongPasswordTitle,
            $"a connection that failed to authenticate must not be saved (statuses: {first}, {second})");

        new[] { first, second }.Should().AllBeEquivalentTo(403);
    }

    /// <summary>
    /// Issues a connect request for the configured Nextcloud host with a wrong password and
    /// returns the HTTP status it produced — 200 when the server wrongly accepted it.
    /// </summary>
    private async Task<int> WrongPasswordAttempt()
    {
        try
        {
            await _thirdPartyApi.SaveThirdPartyAsync(
                NextcloudRequest(WrongPasswordTitle, WrongPassword),
                TestContext.Current.CancellationToken);

            return (int)HttpStatusCode.OK;
        }
        catch (ApiException e)
        {
            return e.ErrorCode;
        }
    }
}
