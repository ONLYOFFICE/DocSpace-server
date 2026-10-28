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


namespace ASC.Web.Api.Tests.Tests._03_Authentication;

/// <summary>
/// GET /api/2.0/authentication with credentials that do not authenticate. Every invalid
/// credential on this endpoint is reported as <c>response: false</c> with 200 — it never rejects.
/// </summary>
[Trait("Category", "Authentication")]
public class AuthCheckInvalidCredentialsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// Bug 84500: <c>BasicAuthHandler</c> threw an unhandled FormatException / "Unable to get
    /// password" for a Basic value that is not valid base64 (or decodes without a colon) and the
    /// endpoint answered 500 with the stack trace. Raw HTTP: the SDK cannot send an arbitrary
    /// Authorization header value.
    /// </summary>
    [Trait("Bug", "84500")]
    [Theory]
    [InlineData("Basic !!!not-base64")]
    [InlineData("Basic bm9jb2xvbg==")]
    public async Task GetIsAuthentificated_MalformedBasicHeader_ReturnsFalse(string authorization)
    {
        // Arrange
        await _webApiClient.Authenticate(null);
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/2.0/authentication");
        request.Headers.TryAddWithoutValidation("Authorization", authorization);

        // Act
        using var response = await _webApiClient.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("response").GetBoolean().Should().BeFalse();
    }
}
