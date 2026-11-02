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

namespace ASC.Web.Api.Tests.Tests._08_OAuth2.ClientManagement;

/// <summary>
/// Shared plumbing for the OAuth2 client-management suites (create/update/delete/activate/
/// regenerate/revoke a client, plus the tenant- and user-wide bulk deletes).
/// </summary>
public abstract class ClientManagementTestBase(
    AspireAppFixture fixture)
    : OAuth2TestBase(fixture)
{
    /// <summary>
    /// A valid create-client payload with every URL field exposed individually — unlike
    /// <see cref="OAuth2TestBase.NewClientRequest"/>, which hardcodes them all, so it cannot be
    /// reused by the validation cases that need to flip exactly one field to an invalid value.
    /// </summary>
    protected static CreateClientRequest ValidCreateClientRequest(
        string name = "Test OAuth Client",
        string? logo = OAuthLogo,
        string? websiteUrl = "https://example.com",
        string? termsUrl = "https://example.com/terms",
        string? policyUrl = "https://example.com/policy",
        List<string>? redirectUris = null,
        List<string>? allowedOrigins = null,
        string? logoutRedirectUri = "https://example.com/logout",
        List<string>? scopes = null)
    {
        return new CreateClientRequest(
            name: name,
            logo: logo!,
            websiteUrl: websiteUrl!,
            termsUrl: termsUrl!,
            policyUrl: policyUrl!,
            redirectUris: redirectUris ?? ["https://example.com/callback"],
            allowedOrigins: allowedOrigins ?? ["https://example.com"],
            logoutRedirectUri: logoutRedirectUri!,
            scopes: scopes ?? ["openid", "files:read"]);
    }

    /// <summary>
    /// Signs in as <paramref name="user"/> (the owner when null), applies the resulting signature
    /// to <c>_identityClient</c> and creates a client under that identity. Leaves the signature
    /// applied, so the caller can keep acting as the same identity right after.
    /// </summary>
    protected async Task<ClientResponse> CreateClientAsAsync(User? user = null, string name = "Test Client")
    {
        await ApplySignatureAsync(user);

        return await _clientManagementApi.CreateClientAsync(
            ValidCreateClientRequest(name), TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Asserts that a 400 response from the identity service carries an <c>errors</c> entry with
    /// the given <paramref name="field"/>, <paramref name="code"/> and <paramref name="message"/>.
    /// </summary>
    protected static void AssertValidationError(ApiException exception, string field, string code, string message)
    {
        exception.ErrorCode.Should().Be(400);

        using var json = JsonDocument.Parse(exception.ErrorContent?.ToString() ?? "{}");
        var errors = json.RootElement.GetProperty("errors").EnumerateArray()
            .Select(e => (
                Field: e.GetProperty("field").GetString(),
                Code: e.GetProperty("code").GetString(),
                Message: e.GetProperty("message").GetString()))
            .ToList();

        errors.Should().Contain(e => e.Field == field && e.Code == code && e.Message == message);
    }

    /// <summary>
    /// A valid update-client request: the identity service's PUT requires <c>name</c>, <c>logo</c>,
    /// <c>allowed_origins</c>, <c>redirect_uris</c> and non-empty <c>scopes</c> on every update
    /// (the generated constructor rejects a null for any of them client-side), so the cases that
    /// test one field start from this and flip only that one.
    /// </summary>
    protected static UpdateClientRequest ValidUpdateClientRequest(
        string name = "Updated OAuth Client",
        string? description = null,
        string logo = OAuthLogo,
        List<string>? allowedOrigins = null,
        List<string>? redirectUris = null,
        List<string>? scopes = null)
    {
        return new UpdateClientRequest(
            name: name,
            description: description!,
            logo: logo,
            scopes: scopes ?? ["accounts.self:read"],
            allowPkce: false,
            allowedOrigins: allowedOrigins ?? ["https://example.com"],
            redirectUris: redirectUris ?? ["https://example.com/callback"],
            isPublic: false);
    }
}
