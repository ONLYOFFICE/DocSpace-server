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
namespace ASC.Web.Api.Tests.Tests._02_Settings.Sso;

/// <summary>
/// Shared plumbing for the SAML single sign-on suites.
/// </summary>
/// <remarks>
/// <see cref="PortalClients"/> does not expose <c>SSOApi</c> yet, so the typed client is built here on the
/// per-test Web.Api <see cref="HttpClient"/> (same base address, same auth headers as every other Web.Api client).
///
/// The TypeScript suite ran against SaaS, where every SSO call is refused with 402 until the plan includes SSO
/// (<c>paymentsApi.setupPayment()</c>). The integration host is standalone, where <c>CheckSsoPermissionsAsync</c>
/// does not look at the plan at all, so the paid-plan tests are ported as plain calls and the 402 tests are
/// merged into them. SSO settings are stored per tenant, so writes stay inside the test's own portal.
/// </remarks>
public abstract class SsoTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected const string ValidBinding = "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST";

    private SSOApi? _ssoApi;

    protected SSOApi Sso => _ssoApi ??= new SSOApi(
        _webApiClient,
        new Configuration { BasePath = _webApiClient.BaseAddress!.ToString().TrimEnd('/') });

    /// <summary>
    /// The <c>serializeSettings</c> string of a minimal valid SAML configuration. <paramref name="customize"/>
    /// edits the top-level object before it is serialized.
    /// </summary>
    protected static string SettingsJson(Action<Dictionary<string, object?>>? customize = null)
    {
        var settings = new Dictionary<string, object?>
        {
            ["idpSettings"] = new Dictionary<string, object?>
            {
                ["entityId"] = "https://idp.example.com/entity",
                ["ssoUrl"] = "https://idp.example.com/sso",
                ["ssoBinding"] = ValidBinding
            },
            ["fieldMapping"] = new Dictionary<string, object?>
            {
                ["firstName"] = "givenName",
                ["lastName"] = "sn",
                ["email"] = "mail"
            }
        };

        customize?.Invoke(settings);

        return JsonSerializer.Serialize(settings);
    }

    protected async Task<SsoSettingsDto> SaveAsync(string serializeSettings)
    {
        var saved = await Sso.SaveSsoSettingsV2Async(
            new SsoSettingsRequestDto(serializeSettings), TestContext.Current.CancellationToken);

        return saved.Response;
    }
}
