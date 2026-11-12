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

namespace ASC.AI.Tests.Tests.WebSearchTests;

/// <summary>
/// The <c>baseUrl</c> egress guard on <c>configure</c>, <c>set-active-config</c> and <c>test-connection</c> (Bug 83005's
/// fix): the host is checked before any socket is opened, so a refusal is a 400 from the guard and not the provider's
/// answer, whatever the provider name.
/// </summary>
/// <remarks>
/// The integration-test profile sets <c>AI__ALLOW_PRIVATE_BASE_URL</c> so that the stub model and search providers can
/// run on the loopback, which makes loopback, RFC 1918 and carrier-grade addresses legal here. What the guard refuses
/// in every mode is the unspecified range, link-local (including the cloud metadata address) and IPv6 link-local:
/// those are the host cases below. The loopback spellings of the TypeScript list (127.1, decimal, hex, octal, ::1,
/// localhost) cannot be refused on this stand and are not ported; the first control shows the loopback is dialled.
/// </remarks>
[Trait("Category", "Security")]
[Trait("Feature", "AI/WebSearch")]
public class WebSearchEgressGuardTests(AspireAppFixture fixture) : WebSearchTestBase(fixture)
{
    private const string HostNotAllowed = "baseUrl host is not allowed";

    private static readonly string[] _blockedHosts =
    [
        "http://169.254.169.254/latest/meta-data/",
        "http://0.0.0.0:1/v1",
        "http://0:1/v1",
        "http://[fe80::1]:1/v1",
        "http://[::ffff:169.254.169.254]/"
    ];

    public static TheoryData<string, string> BlockedHosts()
    {
        var data = new TheoryData<string, string>();
        foreach (var route in BaseUrlRoutes)
        {
            foreach (var url in _blockedHosts)
            {
                data.Add(route, url);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(BlockedHosts))]
    [Trait("Bug", "83005")]
    public async Task BlockedHost_IsRefusedWith400BeforeAnySocketOpens(string route, string url)
    {
        var reply = await SendToRouteAsync(route, "onlyoffice", url);

        reply.Status.Should().Be(400, reply.ToString());
        reply.Error.Should().Be(HostNotAllowed);
        await ExpectNothingStoredAsync();
    }

    /// <summary>The guard is about the host, not the provider: <c>exa</c> is refused the same.</summary>
    [Theory]
    [MemberData(nameof(BaseUrlRoutesData))]
    public async Task BlockedHost_IsRefusedForTheExaProviderToo(string route)
    {
        var reply = await SendToRouteAsync(route, "exa", "http://169.254.169.254/");

        reply.Status.Should().Be(400, reply.ToString());
        reply.Error.Should().Be(HostNotAllowed);
    }

    public static TheoryData<string> BaseUrlRoutesData => new(BaseUrlRoutes);

    /// <summary>
    /// The control the refusals need: a host that is allowed passes the guard and meets the next check, so the 400s
    /// above are the host rules and not a blanket refusal of <c>baseUrl</c>. On this stand that host is the
    /// loopback stub provider.
    /// </summary>
    [Theory]
    [MemberData(nameof(BaseUrlRoutesData))]
    public async Task AllowedHost_PassesTheGuardAndReachesTheNextCheck(string route)
    {
        using var stub = new WebSearchProviderStub();

        var reply = await SendToRouteAsync(route, "onlyoffice", stub.BaseUrl);

        reply.Status.Should().Be(200, reply.ToString());
        // configure echoes the stored config (baseUrl included) on success, so the guard's refusal is told apart by
        // its `error` field, not by the word appearing in the body.
        reply.Error.Should().BeNull("an allowed host is not refused by the guard");
        if (route != "test-connection")
        {
            await ExpectStoredAsync("onlyoffice");
        }
    }
}
