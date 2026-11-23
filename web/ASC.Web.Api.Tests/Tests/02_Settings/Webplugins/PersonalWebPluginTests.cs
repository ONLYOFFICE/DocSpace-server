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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Webplugins;

/// <summary>
/// Personal web plugins — installed by a member for themselves with <c>personal=true</c>, listed for that member
/// alone, and shadowed by a portal plugin that takes the same name.
/// </summary>
[Trait("Category", "Settings")]
public class PersonalWebPluginTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UploadPersonalWebPlugin_User_ListedForOwnerOnly()
    {
        // Arrange
        const string pluginName = "personal-autotest";
        var pluginOwner = await InviteMember(EmployeeType.User);
        var otherUser = await InviteMember(EmployeeType.User);

        // Act
        await _webApiClient.Authenticate(pluginOwner);
        using var uploadResponse = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, pluginName, true, TestContext.Current.CancellationToken);
        var ownerPlugins = await GetPluginsAsync(false);

        await _webApiClient.Authenticate(otherUser);
        var otherPlugins = await GetPluginsAsync(false);
        var otherPersonalPlugins = await GetPluginsAsync(true);

        // Assert
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ownerPlugins.Should().ContainSingle(x => x.Name == pluginName && x.Personal);
        otherPlugins.Should().NotContain(x => x.Name == pluginName);
        otherPersonalPlugins.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadPersonalWebPlugin_NameOfPortalPlugin_Refused()
    {
        // Arrange
        const string pluginName = "portal-autotest";
        using var portalResponse = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, pluginName, TestContext.Current.CancellationToken);
        var user = await InviteMember(EmployeeType.User);

        // Act
        await _webApiClient.Authenticate(user);
        using var personalResponse = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, pluginName, true, TestContext.Current.CancellationToken);

        // Assert
        portalResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        personalResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await personalResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Contain("Wrong plugin archive. A plugin with the same name or pluginName is already installed for the whole portal");
    }

    [Fact]
    public async Task UploadPortalWebPlugin_NameOfPersonalPlugin_ShadowsPersonalCopy()
    {
        // Arrange
        const string pluginName = "shadowed-autotest";
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);
        using var personalResponse = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, pluginName, true, TestContext.Current.CancellationToken);

        // Act
        await _webApiClient.Authenticate(Owner);
        using var portalResponse = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, pluginName, TestContext.Current.CancellationToken);

        await _webApiClient.Authenticate(user);
        var availablePlugins = await GetPluginsAsync(false);
        var personalPlugins = await GetPluginsAsync(true);

        // Assert
        personalResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        portalResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        availablePlugins.Should().ContainSingle(x => x.Name == pluginName)
            .Which.Personal.Should().BeFalse();
        personalPlugins.Should().ContainSingle(x => x.Name == pluginName && x.Personal);
    }

    [Fact]
    public async Task UploadPersonalWebPlugin_Guest_Forbidden()
    {
        // Arrange
        var guest = await InviteMember(EmployeeType.Guest);
        await _webApiClient.Authenticate(guest);

        // Act
        using var response = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, "guest-autotest", true, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Contain("Personal plugins disabled");
    }

    [Theory]
    [InlineData("Sec-Fetch-Site", "cross-site", true)]
    [InlineData("Sec-Fetch-Site", "same-site", true)]
    [InlineData("Sec-Fetch-Site", "cross-site", false)]
    public async Task UploadWebPlugin_RequestFromAnotherSite_Forbidden(string header, string value, bool personal)
    {
        // Arrange
        var user = personal ? await InviteMember(EmployeeType.User) : Owner;
        await _webApiClient.Authenticate(user);

        // Act
        using var response = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, "cross-site-autotest", personal, TestContext.Current.CancellationToken, AuthCookie(user), (header, value));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Contain("The request comes from another origin");
    }

    [Fact]
    public async Task UploadPersonalWebPlugin_RequestFromPortalPage_Installed()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        using var response = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, "same-origin-autotest", true, TestContext.Current.CancellationToken, AuthCookie(user), ("Sec-Fetch-Site", "same-origin"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UploadPersonalWebPlugin_CookieWithPortalOrigin_Installed()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        using var response = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, "portal-origin-autotest", true, TestContext.Current.CancellationToken, AuthCookie(user));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UploadPersonalWebPlugin_BearerFromAnotherSite_Installed()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteMember(EmployeeType.User));

        // Act
        using var response = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, "bearer-autotest", true, TestContext.Current.CancellationToken, ("Sec-Fetch-Site", "cross-site"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UploadPersonalWebPlugin_WithCspDomains_OnlyOwnerGetsThemInCspHeader()
    {
        // Arrange
        const string cspDomain = "https://personal-csp.autotest.example";
        var owner = await InviteMember(EmployeeType.User);
        var otherUser = await InviteMember(EmployeeType.User);
        var manifest = WebPluginsTestData.Manifest("personal-csp-autotest");
        manifest["cspDomains"] = cspDomain;

        await _webApiClient.Authenticate(owner);
        using var uploadResponse = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, manifest, true, TestContext.Current.CancellationToken);
        using var listResponse = await _webApiClient.GetAsync("api/2.0/settings/webplugins", TestContext.Current.CancellationToken);
        var personalCspCookie = GetPersonalCspCookie(listResponse);

        // Act
        var ownerCsp = await GetCspAsync(personalCspCookie);
        var ownerCspWithoutCookie = await GetCspAsync(null);

        await _webApiClient.Authenticate(otherUser);
        var otherUserCsp = await GetCspAsync(personalCspCookie);

        // Assert
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        personalCspCookie.Should().MatchRegex("^[0-9a-f]{64}$");
        ownerCsp.Header.Should().Contain(cspDomain);
        ownerCsp.Domains.Should().NotContain(cspDomain);
        ownerCspWithoutCookie.Header.Should().NotContain(cspDomain);
        otherUserCsp.Header.Should().NotContain(cspDomain);
    }

    private static (string Name, string Value) AuthCookie(User user)
    {
        return ("Cookie", $"asc_auth_key={user.Token}");
    }

    private static string GetPersonalCspCookie(HttpResponseMessage response)
    {
        const string prefix = "asc_personal_csp=";

        var cookie = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(x => x.StartsWith(prefix, StringComparison.Ordinal))
            : null;

        return cookie?[prefix.Length..].Split(';')[0] ?? string.Empty;
    }

    /// <summary>
    /// Reads the CSP over raw HTTP: the generated SDK cannot send the <c>asc_personal_csp</c> cookie.
    /// </summary>
    private async Task<(string Header, List<string> Domains)> GetCspAsync(string personalCspCookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/2.0/security/csp");

        if (!string.IsNullOrEmpty(personalCspCookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", $"asc_personal_csp={personalCspCookie}");
        }

        using var response = await _webApiClient.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(body);
        var csp = json.RootElement.GetProperty("response");

        return (
            csp.GetProperty("header").GetString() ?? string.Empty,
            csp.GetProperty("domains").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList());
    }

    /// <summary>
    /// Lists the plugins over raw HTTP: the generated SDK has no <c>personal</c> parameter on the list operation yet,
    /// and its plugin model fails on a plugin without the optional fields (P8 of the plugins spec).
    /// </summary>
    private async Task<List<(string Name, bool Personal)>> GetPluginsAsync(bool personal)
    {
        var url = personal ? "api/2.0/settings/webplugins?personal=true" : "api/2.0/settings/webplugins";

        using var response = await _webApiClient.GetAsync(url, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(body);

        return json.RootElement.GetProperty("response").EnumerateArray()
            .Select(x => (
                x.GetProperty("name").GetString() ?? string.Empty,
                x.TryGetProperty("personal", out var personalProperty) && personalProperty.GetBoolean()))
            .ToList();
    }
}
