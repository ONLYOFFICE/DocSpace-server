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

namespace ASC.AI.Tests.Tests.ProfileTests;

/// <summary>
/// The egress guard in front of <c>profiles/list-provider-models</c>: a base URL is refused before any socket is
/// opened when it is not http(s), carries credentials, does not resolve, or points at a metadata or link-local address.
/// </summary>
/// <remarks>
/// Not covered here on purpose: loopback, <c>localhost</c> and the RFC 1918 ranges. The integration-test profile sets
/// <c>AI__ALLOW_PRIVATE_BASE_URL</c> (the stub provider is a loopback address), so those are allowed in this
/// environment; the cloud-metadata and link-local ranges stay blocked even with that flag.
/// </remarks>
[Trait("Category", "Security")]
[Trait("Feature", "AI/Profiles")]
public class ProfileProviderUrlGuardTests(AspireAppFixture fixture) : ProfileTestBase(fixture)
{
    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://ssrf-attacker.invalid/resource")]
    [InlineData("gopher://ssrf-attacker.invalid/_test")]
    [InlineData("dict://ssrf-attacker.invalid/d:test")]
    [InlineData("data:text/plain,test")]
    [InlineData("mailto:test@ssrf-attacker.invalid")]
    public async Task ListProviderModels_NonHttpScheme_IsRefusedByValidation(string baseUrl)
    {
        var outcome = await ProviderModelsOutcomeAsync("deepseek", baseUrl, "sk-autotest-not-a-real-key");

        // Only http(s) may ever reach a provider, and the scheme has to be refused by validation rather than by a failed connection.
        outcome.Status.Should().Be(400, outcome.Body);
        outcome.Body.Should().Contain("baseUrl must use http or https");
    }

    [Theory]
    [InlineData("http://ssrf-attacker.invalid:9999/models")]
    [InlineData("https://ssrf-attacker.invalid/models")]
    [InlineData("HTTP://ssrf-attacker.invalid:9999/models")]
    [InlineData("HtTp://ssrf-attacker.invalid:9999/models")]
    [InlineData("http://user:pass@ssrf-attacker.invalid:9999/models")]
    [InlineData("http://provider.example@ssrf-attacker.invalid:9999/models")]
    [InlineData("http://ssrf-attacker.invalid.:9999/models")]
    [InlineData("http://ssrf-attacker.invalid:9999/models?key=sk-leak")]
    public async Task ListProviderModels_AttackerHostThatDoesNotResolve_DoesNotReturnAModelList(string baseUrl)
    {
        var outcome = await ProviderModelsOutcomeAsync("deepseek", baseUrl, "sk-autotest-not-a-real-key");

        // Whether the URL is refused up front (400) or attempted and unreachable (502), no attacker-controlled host
        // ever supplies the portal with a model catalogue.
        outcome.Status.Should().BeOneOf(400, 502);
        outcome.Body.Should().NotContain("\"id\"");
    }

    /// <summary>
    /// Bug 83005 (fixed): the route had no pre-connection egress guard - a closed loopback port answered 502 and a
    /// filtered private host hung for the connect timeout, which proved the addresses were dialled. The metadata and
    /// link-local ranges are refused with an address complaint before a socket is opened, in every spelling the URL
    /// parser normalises (decimal, hexadecimal, IPv4-mapped IPv6).
    /// </summary>
    [Theory]
    [Trait("Bug", "83005")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://169.254.170.2/v2/credentials")]
    [InlineData("http://2852039166/latest/meta-data/")]
    [InlineData("http://0xa9fea9fe/latest/meta-data/")]
    [InlineData("http://[::ffff:169.254.169.254]/latest/meta-data/")]
    [InlineData("http://[fe80::1]/v1")]
    [InlineData("http://0.0.0.0:9999/v1")]
    [InlineData("http://0/v1")]
    public async Task ListProviderModels_MetadataAndLinkLocalAddress_IsRefusedBeforeAnyConnection(string baseUrl)
    {
        var outcome = await ProviderModelsOutcomeAsync("deepseek", baseUrl, "sk-autotest-not-a-real-key");

        // The message is the point: an address complaint means the guard classified the host and stopped. A 502
        // "unreachable" would mean the socket was opened after all, and a 200 that an internal service answered.
        outcome.Status.Should().Be(400, $"{baseUrl} must not be dialled: {outcome.Body}");
        outcome.Body.Should().Contain("baseUrl host is not allowed");
    }
}
