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

namespace ASC.Files.Tests.Tests._08_Privacy.ExternalLinks;

/// <summary>
/// The page that opens a link of a private item gets a Content-Security-Policy of its own, without the domains of the
/// portal's CSP settings: a script from any of them could read the secret after <c>#</c>. The portal builds and caches
/// it beside the full value, which keeps those domains; <c>GET security/csp</c> returns both, and the proxy sends the
/// narrow one on <c>share/private</c>. The SDK model has no <c>privateLinkHeader</c> yet, so the answer is read raw.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkCspTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    private const string ExtraDomain = "https://cdn.example.com";

    private RawApiClient WebApi => new(_webApiClient);

    private async Task<JsonElement> ReadCsp(HttpResponseMessage response)
    {
        ((int)response.StatusCode).Should().Be(200);

        return await WebApi.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ADomainOfTheCspSettings_IsInTheFullValue_AndNotInTheValueOfThePrivateLinkPage()
    {
        await _webApiClient.Authenticate(Owner);

        using var saved = await WebApi.PostAsync("api/2.0/security/csp", new { domains = new[] { ExtraDomain } }, TestContext.Current.CancellationToken);
        var answer = await ReadCsp(saved);

        await _webApiClient.Authenticate(null);
        using var read = await WebApi.GetAsync("api/2.0/security/csp", TestContext.Current.CancellationToken);
        var current = await ReadCsp(read);

        foreach (var csp in new[] { answer, current })
        {
            var full = csp.GetProperty("header").GetString()!;
            var narrow = csp.GetProperty("privateLinkHeader").GetString()!;

            full.Should().Contain(ExtraDomain);
            narrow.Should().Contain("script-src").And.Contain("connect-src").And.NotContain(ExtraDomain);
        }
    }

    [Fact]
    public async Task WithoutExtraDomains_TheValueOfThePrivateLinkPage_IsNoWiderThanTheFullOne()
    {
        await _webApiClient.Authenticate(null);

        using var read = await WebApi.GetAsync("api/2.0/security/csp", TestContext.Current.CancellationToken);
        var csp = await ReadCsp(read);

        var full = Sources(csp.GetProperty("header").GetString()!);
        var narrow = Sources(csp.GetProperty("privateLinkHeader").GetString()!);

        narrow.Should().NotBeEmpty();
        foreach (var (directive, sources) in narrow)
        {
            full.Should().ContainKey(directive);
            sources.Should().BeSubsetOf(full[directive], "directive {0}", directive);
        }

        // The web search integration of the stand's configuration (`csp:websearch`) adds Google to the full value only.
        full["connect-src"].Should().Contain(IntegrationDomain);
        narrow["connect-src"].Should().NotContain(IntegrationDomain);
    }

    private const string IntegrationDomain = "*.google.com";

    private static Dictionary<string, string[]> Sources(string header) =>
        header.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(d => d.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToDictionary(parts => parts[0], parts => parts[1..]);
}
