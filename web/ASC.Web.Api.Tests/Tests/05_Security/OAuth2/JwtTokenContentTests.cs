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

namespace ASC.Web.Api.Tests.Tests._05_Security.OAuth2;

/// <summary>
/// GET /api/2.0/security/oauth2/token — what the identity signature says (who, for how long, the
/// developer-tools flag) and that it does not sign anyone in to the portal API.
/// </summary>
[Trait("Category", "Security")]
public class JwtTokenContentTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GenerateJwtToken_Member_CarriesCallerIdAndFiveMinuteExpiry()
    {
        // Arrange
        var member = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(member);
        var issuedAround = DateTimeOffset.UtcNow;

        // Act
        var token = (await _oauth2Api.GenerateJwtTokenAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        var payload = ReadPayload(token);
        payload.GetProperty("sub").GetString().Should().Be(member.Id.ToString());
        payload.GetProperty("is_guest").GetString().Should().Be("false");
        var expires = DateTimeOffset.FromUnixTimeSeconds(payload.GetProperty("exp").GetInt64());
        expires.Should().BeCloseTo(issuedAround.AddMinutes(5), TimeSpan.FromSeconds(30));
    }

    [Theory]
    [InlineData(false, "true")]
    [InlineData(true, "false")]
    public async Task GenerateJwtToken_DevToolsSetting_IsCarriedAsIsPublic(bool limitedAccess, string expectedIsPublic)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var securityApi = new DocSpace.API.SDK.Api.Security.AccessToDevToolsApi(
            _webApiClient, new Configuration { BasePath = _webApiClient.BaseAddress!.ToString().TrimEnd('/') });
        await securityApi.SetTenantDevToolsAccessSettingsAsync(
            new TenantDevToolsAccessSettingsRequestDto(limitedAccess), TestContext.Current.CancellationToken);

        // Act
        var token = (await _oauth2Api.GenerateJwtTokenAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        ReadPayload(token).GetProperty("is_public").GetString().Should().Be(expectedIsPublic);
    }

    /// <remarks>Raw HTTP: the typed client has no way to send a bearer token other than the portal's own.</remarks>
    [Fact]
    public async Task GenerateJwtToken_SignatureAsBearer_PortalApiRefusesTheCall()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var token = (await _oauth2Api.GenerateJwtTokenAsync(TestContext.Current.CancellationToken)).Response;
        await _webApiClient.Authenticate(null);
        _webApiClient.DefaultRequestHeaders.Remove("Authorization");
        _webApiClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        // Act
        using var response = await _webApiClient.GetAsync("api/2.0/security/oauth2/token", TestContext.Current.CancellationToken);

        // Assert — the endpoint needs a signed-in caller; with the signature as the only credential it is
        // refused (the stand answers 403), so the signature does not sign anyone in to the portal API.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    private static JsonElement ReadPayload(string token)
    {
        var part = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
        part = part.PadRight(part.Length + ((4 - (part.Length % 4)) % 4), '=');

        using var json = JsonDocument.Parse(Convert.FromBase64String(part));

        return json.RootElement.Clone();
    }
}
