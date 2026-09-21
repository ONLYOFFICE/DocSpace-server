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

namespace ASC.Web.Api.Tests.Tests._08_OAuth2.ClientQuerying;

/// <summary>
/// Shared plumbing for the client-querying suites (<c>GET /api/2.0/clients*</c>).
///
/// The three pageable list endpoints (<c>GetClients</c>, <c>GetClientsInfo</c>, <c>GetConsents</c>)
/// have generated models — <c>PageableResponse</c>, <c>PageableResponseClientInfoResponse</c>,
/// <c>PageableModificationResponse</c> — whose <c>last_client_id</c>/<c>last_created_on</c>/
/// <c>last_modified_on</c> cursor fields are typed as non-nullable <c>DateTime</c>/<c>string</c>
/// but arrive as JSON <c>null</c> whenever the caller has no last-seen cursor yet, which is every
/// positive case in these suites: <c>JsonConvert.DeserializeObject</c> throws converting that
/// <c>null</c> to <c>DateTime</c> before the caller ever sees a response. That is an SDK/OpenAPI
/// generation defect — the cursor fields should be nullable — so the positive cases in the classes
/// built on this base read the envelope as raw JSON via <see cref="GetPageAsync"/> instead of the
/// typed <c>ClientQueryingApi</c> methods. The negative (403) cases are unaffected, since the
/// error body is never run through the pageable model, and keep using the typed calls.
/// </summary>
public abstract class ClientQueryingTestBase(
    AspireAppFixture fixture)
    : OAuth2TestBase(fixture)
{
    /// <summary>
    /// Issues a raw GET against the identity service and returns the parsed JSON body — the
    /// carve-out for the pageable envelope described above, mirroring <c>ScopeManagementTests</c>.
    /// </summary>
    protected async Task<JsonElement> GetPageAsync(string path)
    {
        using var response = await _identityClient.GetAsync(path, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // The raw body is passed as a literal (not a composite-format "because" reason) since it
        // is arbitrary JSON and may itself contain '{'/'}' — FluentAssertions would otherwise try
        // to parse it as a format string and throw.
        response.StatusCode.Should().Be(HttpStatusCode.OK, "{0}", body);

        using var json = JsonDocument.Parse(body);

        return json.RootElement.Clone();
    }
}
