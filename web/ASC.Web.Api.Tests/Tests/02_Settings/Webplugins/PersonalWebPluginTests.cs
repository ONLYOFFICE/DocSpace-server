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
/// alone, and shadowed by a portal plugin that takes the same name. The calls on one personal plugin by name are in
/// <see cref="PersonalWebPluginManagementTests"/>.
/// </summary>
/// <remarks>
/// A refusal for want of rights is a <c>SecurityException</c>, and the API answers every one of them with the message
/// "Access denied" whatever text the exception carries (<c>CustomExceptionHandler</c>), so that is what a 403 here is
/// checked for.
/// </remarks>
[Trait("Category", "Settings")]
public class PersonalWebPluginTests(
    AspireAppFixture fixture)
    : WebPluginTestBase(fixture)
{
    public static TheoryData<string> InvalidCspDomains => new()
    {
        "'unsafe-inline'",
        "https:",
        "*",
        "*.com",
        "https://first.personal-csp.example https://second.personal-csp.example",
        "https://personal-csp.example;script-src",
        // every address is valid, only the length of the list (over 15 KB) is not
        string.Join(',', Enumerable.Range(0, 400).Select(i => $"https://d{i:000}.personal-csp-limit.example.com"))
    };

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
        var ownerPlugins = await ListPluginsAsync();

        await _webApiClient.Authenticate(otherUser);
        var otherPlugins = await ListPluginsAsync();
        var otherPersonalPlugins = await ListPluginsAsync(personal: true);

        // Assert
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var uploadedPlugin = await _webApi.ReadAsync<WebPluginView>(uploadResponse, TestContext.Current.CancellationToken);
        uploadedPlugin.Personal.Should().BeTrue();
        uploadedPlugin.Enabled.Should().BeTrue();
        uploadedPlugin.CspDomains.Should().BeEmpty();
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
            .Should().Contain("A plugin with the same name or pluginName is already installed for the whole portal").And.NotContain("Wrong plugin archive");
    }

    [Fact]
    public async Task UploadPersonalWebPlugin_PluginNameOfPortalPlugin_Refused()
    {
        // Arrange
        const string pluginName = "personal-js-autotest";
        var portalManifest = WebPluginsTestData.Manifest("portal-js-autotest");
        portalManifest["pluginName"] = "sharedJsAutotest";
        await UploadAsOwnerAsync(portalManifest);
        var personalManifest = WebPluginsTestData.Manifest(pluginName);
        personalManifest["pluginName"] = "sharedJsAutotest";
        await _webApiClient.Authenticate(await InviteMember(EmployeeType.User));

        // Act
        using var response = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, personalManifest, true, TestContext.Current.CancellationToken);
        var (status, body) = await WebPluginsTestData.ReadAsync(response, TestContext.Current.CancellationToken);
        var personalPlugins = await ListPluginsAsync(personal: true);

        // Assert
        status.Should().Be(400, body);
        body.Should().Contain("A plugin with the same name or pluginName is already installed for the whole portal").And.NotContain("Wrong plugin archive");
        personalPlugins.Should().NotContain(x => x.Name == pluginName);
    }

    [Fact]
    public async Task UploadPersonalWebPlugin_PluginNameOfOwnPersonalPlugin_Refused()
    {
        // Arrange
        var firstManifest = WebPluginsTestData.Manifest("first-js-autotest");
        firstManifest["pluginName"] = "ownJsAutotest";
        var secondManifest = WebPluginsTestData.Manifest("second-js-autotest");
        secondManifest["pluginName"] = "ownJsAutotest";
        await UploadPersonalAsync(await InviteMember(EmployeeType.User), firstManifest);

        // Act
        using var response = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, secondManifest, true, TestContext.Current.CancellationToken);
        var (status, body) = await WebPluginsTestData.ReadAsync(response, TestContext.Current.CancellationToken);
        var personalPlugins = await ListPluginsAsync(personal: true);

        // Assert
        status.Should().Be(400, body);
        body.Should().Contain("Plugin already exist").And.NotContain("Wrong plugin archive");
        personalPlugins.Select(x => x.Name).Should().Equal("first-js-autotest");
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
        var availablePlugins = await ListPluginsAsync();
        var personalPlugins = await ListPluginsAsync(personal: true);

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
            .Should().Contain("Access denied");
    }

    [Theory]
    [InlineData(EmployeeType.User, 400, "Changes to system plugins are prohibited")]
    [InlineData(EmployeeType.Guest, 403, "Access denied")]
    public async Task UploadPersonalWebPlugin_SystemFlag_RefusedAndNotInstalled(EmployeeType employeeType, int expectedStatus, string expectedMessage)
    {
        // Arrange
        const string pluginName = "personal-system-autotest";
        await _webApiClient.Authenticate(await InviteMember(employeeType));

        // Act
        using var response = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, $"{WebPluginsTestData.UploadPath}?personal=true&system=true", WebPluginsTestData.Manifest(pluginName), TestContext.Current.CancellationToken);
        var (status, body) = await WebPluginsTestData.ReadAsync(response, TestContext.Current.CancellationToken);
        var availablePlugins = await ListPluginsAsync();

        // Assert
        status.Should().Be(expectedStatus, body);
        body.Should().Contain(expectedMessage);
        availablePlugins.Should().NotContain(x => x.Name == pluginName);
    }

    [Theory]
    [MemberData(nameof(InvalidCspDomains))]
    public async Task UploadPersonalWebPlugin_InvalidCspDomains_RefusedAndNotInstalled(string cspDomains)
    {
        // Arrange
        const string pluginName = "invalid-csp-autotest";
        var manifest = WebPluginsTestData.Manifest(pluginName);
        manifest["cspDomains"] = cspDomains;
        await _webApiClient.Authenticate(await InviteMember(EmployeeType.User));

        // Act
        using var response = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, manifest, true, TestContext.Current.CancellationToken);
        var (status, body) = await WebPluginsTestData.ReadAsync(response, TestContext.Current.CancellationToken);
        var personalPlugins = await ListPluginsAsync(personal: true);

        // Assert
        status.Should().Be(400, body);
        body.Should().Contain("The cspDomains value lists an invalid address or is too long").And.NotContain("Wrong plugin archive");
        personalPlugins.Should().NotContain(x => x.Name == pluginName);
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
            .Should().Contain("Access denied");
    }

    [Fact]
    public async Task UploadPersonalWebPlugin_RequestFromPortalPage_Installed()
    {
        // Arrange
        const string pluginName = "same-origin-autotest";
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        using var response = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, pluginName, true, TestContext.Current.CancellationToken, AuthCookie(user), ("Sec-Fetch-Site", "same-origin"));
        var personalPlugins = await ListPluginsAsync(personal: true);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        personalPlugins.Should().ContainSingle(x => x.Name == pluginName && x.Personal);
    }

    [Fact]
    public async Task UploadPersonalWebPlugin_CookieWithPortalOrigin_Installed()
    {
        // Arrange
        const string pluginName = "portal-origin-autotest";
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        using var response = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, pluginName, true, TestContext.Current.CancellationToken, AuthCookie(user));
        var personalPlugins = await ListPluginsAsync(personal: true);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        personalPlugins.Should().ContainSingle(x => x.Name == pluginName && x.Personal);
    }

    [Fact]
    public async Task UploadPersonalWebPlugin_BearerFromAnotherSite_Installed()
    {
        // Arrange
        const string pluginName = "bearer-autotest";
        await _webApiClient.Authenticate(await InviteMember(EmployeeType.User));

        // Act
        using var response = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, pluginName, true, TestContext.Current.CancellationToken, ("Sec-Fetch-Site", "cross-site"));
        var personalPlugins = await ListPluginsAsync(personal: true);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        personalPlugins.Should().ContainSingle(x => x.Name == pluginName && x.Personal);
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
        var personalPlugins = await ListPluginsAsync(personal: true);

        await _webApiClient.Authenticate(otherUser);
        var otherUserCsp = await GetCspAsync(personalCspCookie);

        // Assert
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        personalCspCookie.Should().Be("1");
        ownerCsp.Header.Should().Contain(cspDomain);
        ownerCsp.Domains.Should().NotContain(cspDomain);
        ownerCspWithoutCookie.Header.Should().NotContain(cspDomain);
        otherUserCsp.Header.Should().NotContain(cspDomain);
        personalPlugins.Should().ContainSingle(x => x.Name == "personal-csp-autotest" && x.CspDomains == cspDomain);
    }

    [Fact]
    public async Task UploadPersonalWebPlugin_DomainsOverTheHeaderLimit_ForbiddenWithoutCookieAndPluginKept()
    {
        // Arrange: each domain goes into every directive, so 120 of them push the header well over the 15 KB limit
        // while the list itself stays far below the 15 KB a manifest may declare
        const string pluginName = "personal-csp-limit";
        var manifest = WebPluginsTestData.Manifest(pluginName);
        manifest["cspDomains"] = string.Join(',', Enumerable.Range(0, 120).Select(i => $"https://d{i:000}.personal-limit.example.com"));
        await _webApiClient.Authenticate(await InviteMember(EmployeeType.User));

        // Act: the browser still holds the cookie from an earlier page
        using var uploadResponse = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, manifest, true, TestContext.Current.CancellationToken, ("Cookie", $"{PersonalCspCookieName}=1"));
        var (status, body) = await WebPluginsTestData.ReadAsync(uploadResponse, TestContext.Current.CancellationToken);
        using var listRequest = new HttpRequestMessage(HttpMethod.Get, "api/2.0/settings/webplugins");
        listRequest.Headers.TryAddWithoutValidation("Cookie", $"{PersonalCspCookieName}=1");
        using var listResponse = await _webApiClient.SendAsync(listRequest, TestContext.Current.CancellationToken);
        var personalPlugins = await ListPluginsAsync(personal: true);

        // Assert: the refusal leaves the cookie alone, and the list, which cannot build the header either, clears it
        status.Should().Be(403, body);
        body.Should().Contain("CSP header size exceeds maximum allowed size of 15360 bytes.");
        GetPersonalCspSetCookie(uploadResponse).Should().BeNull();
        var clearedCookie = GetPersonalCspSetCookie(listResponse);
        clearedCookie.Should().NotBeNull();
        ExpiresOf(clearedCookie!).Should().BeBefore(DateTimeOffset.UtcNow);
        personalPlugins.Should().ContainSingle(x => x.Name == pluginName && x.Enabled);
    }

    [Fact]
    public async Task PersonalWebPluginWithoutDomains_HeaderAlreadyOverTheLimit_InstalledAndSwitchedOn()
    {
        // Arrange: a plugin whose 120 domains push the member's header over the 15 KB limit is kept, switched on
        var manifest = WebPluginsTestData.Manifest("personal-csp-over");
        manifest["cspDomains"] = string.Join(',', Enumerable.Range(0, 120).Select(i => $"https://d{i:000}.personal-over.example.com"));
        await _webApiClient.Authenticate(await InviteMember(EmployeeType.User));
        using var overResponse = await WebPluginsTestData.UploadPluginAsync(_webApiClient, manifest, true, TestContext.Current.CancellationToken);
        overResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Act: a plugin that adds no domains goes in, and is switched off and on again
        using var uploadResponse = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, WebPluginsTestData.Manifest("personal-csp-plain"), true, TestContext.Current.CancellationToken);
        var (uploadStatus, uploadBody) = await WebPluginsTestData.ReadAsync(uploadResponse, TestContext.Current.CancellationToken);
        using var offResponse = await _webApiClient.PutAsJsonAsync(
            "api/2.0/settings/webplugins/personal-csp-plain?personal=true", new { enabled = false, settings = "" }, TestContext.Current.CancellationToken);
        using var onResponse = await _webApiClient.PutAsJsonAsync(
            "api/2.0/settings/webplugins/personal-csp-plain?personal=true", new { enabled = true, settings = "" }, TestContext.Current.CancellationToken);
        var (onStatus, onBody) = await WebPluginsTestData.ReadAsync(onResponse, TestContext.Current.CancellationToken);
        using var overOffResponse = await _webApiClient.PutAsJsonAsync(
            "api/2.0/settings/webplugins/personal-csp-over?personal=true", new { enabled = false, settings = "" }, TestContext.Current.CancellationToken);
        using var overOnResponse = await _webApiClient.PutAsJsonAsync(
            "api/2.0/settings/webplugins/personal-csp-over?personal=true", new { enabled = true, settings = "" }, TestContext.Current.CancellationToken);
        var (overOnStatus, overOnBody) = await WebPluginsTestData.ReadAsync(overOnResponse, TestContext.Current.CancellationToken);
        var personalPlugins = await ListPluginsAsync(personal: true);

        // Assert: only switching the plugin with the domains back on is refused, and its new state is kept
        uploadStatus.Should().Be(200, uploadBody);
        offResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        onStatus.Should().Be(200, onBody);
        overOffResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        overOnStatus.Should().Be(403, overOnBody);
        overOnBody.Should().Contain("CSP header size exceeds maximum allowed size of 15360 bytes.");
        personalPlugins.Should().ContainSingle(x => x.Name == "personal-csp-over" && x.Enabled);
    }

    [Fact]
    public async Task GetPortalSettings_User_ReportsPersonalPluginsOn()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteMember(EmployeeType.User));

        // Act
        using var response = await _webApiClient.GetAsync("api/2.0/settings", TestContext.Current.CancellationToken);
        var (status, body) = await WebPluginsTestData.ReadAsync(response, TestContext.Current.CancellationToken);

        // Assert: read raw, the generated PluginsDto has no personal field yet; the stand does not set plugins:personal,
        // which defaults to true
        status.Should().Be(200, body);
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("response").GetProperty("plugins").GetProperty("personal").GetBoolean().Should().BeTrue();
    }

    private static (string Name, string Value) AuthCookie(User user)
    {
        return ("Cookie", $"asc_auth_key={user.Token}");
    }
}
