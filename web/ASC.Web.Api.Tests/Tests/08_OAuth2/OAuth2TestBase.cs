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

namespace ASC.Web.Api.Tests.Tests._08_OAuth2;

/// <summary>
/// Shared plumbing for the identity (OAuth2) suites. The identity service authenticates a request
/// by the <c>x-signature</c> JWT that Web.Api issues at <c>GET /api/2.0/security/oauth2/token</c>
/// for the signed-in user — the identity side's own tests pass it as a cookie, the TS suite as a
/// header, so <see cref="ApplySignatureAsync"/> sets both on <c>_identityClient</c>.
/// </summary>
public abstract class OAuth2TestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>A minimal valid PNG data URI — the identity service validates the logo format.</summary>
    protected const string OAuthLogo =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

    /// <summary>
    /// Signs <paramref name="user"/> (the owner when null) in on <c>_webApiClient</c> and returns
    /// the identity signature JWT issued for them.
    /// </summary>
    protected async Task<string> GetSignatureAsync(User? user = null)
    {
        await _webApiClient.Authenticate(user ?? Owner);

        return (await _oauth2Api.GenerateJwtTokenAsync(TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>
    /// Puts the caller's signature JWT onto <c>_identityClient</c> — as the <c>x-signature</c>
    /// header and as the same-named cookie — so every typed identity call after this acts as
    /// <paramref name="user"/> (the owner when null).
    /// </summary>
    protected async Task ApplySignatureAsync(User? user = null)
    {
        ApplySignature(await GetSignatureAsync(user));
    }

    /// <summary>Applies a raw signature value; pass null to strip the identity auth entirely.</summary>
    protected void ApplySignature(string? signature)
    {
        _identityClient.DefaultRequestHeaders.Remove("x-signature");
        _identityClient.DefaultRequestHeaders.Remove("Cookie");

        if (signature is not null)
        {
            _identityClient.DefaultRequestHeaders.TryAddWithoutValidation("x-signature", signature);
            _identityClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", $"x-signature={signature}");
        }
    }

    /// <summary>
    /// Creates an OAuth2 client as the identity the current signature belongs to and returns its id —
    /// the shared Arrange step, mirroring the TS suite's <c>createOAuthClient</c> helper.
    /// </summary>
    protected async Task<string> CreateOAuthClientAsync(string name = "Autotest OAuth Client")
    {
        var response = await _clientManagementApi.CreateClientAsync(
            NewClientRequest(name), TestContext.Current.CancellationToken);

        return response.ClientId;
    }

    /// <summary>A valid create-client payload with the TS suite's default field values.</summary>
    protected static CreateClientRequest NewClientRequest(string name = "Autotest OAuth Client")
    {
        return new CreateClientRequest(
            name: name,
            logo: OAuthLogo,
            websiteUrl: "https://example.com",
            termsUrl: "https://example.com/terms",
            policyUrl: "https://example.com/policy",
            redirectUris: ["https://example.com/callback"],
            allowedOrigins: ["https://example.com"],
            logoutRedirectUri: "https://example.com/logout",
            scopes: ["accounts.self:read"]);
    }
}
