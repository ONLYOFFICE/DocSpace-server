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
/// A personal web plugin addressed with <c>personal=true</c> (GET, PUT and DELETE
/// /api/2.0/settings/webplugins/{name}): who may call it, whose plugin it finds, how it meets a portal plugin of the
/// same name, and the personal CSP cookie and policy that follow its state. The 403 of a refusal carries
/// "Access denied" (see <see cref="PersonalWebPluginTests"/>).
/// </summary>
[Trait("Category", "Settings")]
public class PersonalWebPluginManagementTests(
    AspireAppFixture fixture)
    : WebPluginTestBase(fixture)
{
    private const string PortalCspDomain = "https://conditional-personal.example.com";

    [Theory]
    [InlineData("GET", null)]
    [InlineData("GET", "guest-autotest")]
    [InlineData("PUT", "guest-autotest")]
    [InlineData("DELETE", "guest-autotest")]
    public async Task PersonalWebPluginRequest_Guest_Forbidden(string method, string? pluginName)
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteMember(EmployeeType.Guest));

        // Act
        using var response = await SendPersonalAsync(new HttpMethod(method), pluginName);
        var (status, body) = await WebPluginsTestData.ReadAsync(response, TestContext.Current.CancellationToken);

        // Assert
        status.Should().Be(403, body);
        body.Should().Contain("Access denied");
    }

    [Fact]
    public async Task UpdateAndDeletePersonalWebPlugin_PluginOwner_SwitchesOffThenRemovesOnce()
    {
        // Arrange
        const string pluginName = "switch-off-autotest";
        await UploadPersonalAsync(await InviteMember(EmployeeType.User), pluginName);
        (await GetPluginAsync(pluginName, personal: true)).Enabled.Should().BeTrue();

        // Act
        using var updateResponse = await SendPersonalAsync(HttpMethod.Put, pluginName);
        var (updateStatus, updateBody) = await WebPluginsTestData.ReadAsync(updateResponse, TestContext.Current.CancellationToken);
        var switchedOff = await GetPluginAsync(pluginName, personal: true);

        using var deleteResponse = await SendPersonalAsync(HttpMethod.Delete, pluginName);
        var (deleteStatus, deleteBody) = await WebPluginsTestData.ReadAsync(deleteResponse, TestContext.Current.CancellationToken);
        using var repeatedDeleteResponse = await SendPersonalAsync(HttpMethod.Delete, pluginName);
        var (repeatedStatus, repeatedBody) = await WebPluginsTestData.ReadAsync(repeatedDeleteResponse, TestContext.Current.CancellationToken);
        var personalPlugins = await ListPluginsAsync(personal: true);

        // Assert
        updateStatus.Should().Be(200, updateBody);
        switchedOff.Enabled.Should().BeFalse();
        deleteStatus.Should().Be(200, deleteBody);
        repeatedStatus.Should().Be(404, repeatedBody);
        repeatedBody.Should().Contain("Plugin not found");
        personalPlugins.Should().NotContain(x => x.Name == pluginName);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task PersonalWebPluginRequest_AnotherUsersPlugin_NotFoundAndPluginKept(string method)
    {
        // Arrange
        const string pluginName = "foreign-autotest";
        var pluginOwner = await InviteMember(EmployeeType.User);
        var otherUser = await InviteMember(EmployeeType.User);
        await UploadPersonalAsync(pluginOwner, pluginName);
        await _webApiClient.Authenticate(otherUser);

        // Act
        using var response = await SendPersonalAsync(new HttpMethod(method), pluginName);
        var (status, body) = await WebPluginsTestData.ReadAsync(response, TestContext.Current.CancellationToken);

        await _webApiClient.Authenticate(pluginOwner);
        var ownerPlugins = await ListPluginsAsync(personal: true);

        // Assert
        status.Should().Be(404, body);
        body.Should().Contain("Plugin not found");
        ownerPlugins.Should().ContainSingle(x => x.Name == pluginName)
            .Which.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetWebPlugin_PersonalPluginWithoutPersonalFlag_NotFound()
    {
        // Arrange
        const string pluginName = "unflagged-autotest";
        await UploadPersonalAsync(await InviteMember(EmployeeType.User), pluginName);
        (await GetPluginAsync(pluginName, personal: true)).Personal.Should().BeTrue();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webpluginsApi.GetWebPluginAsync(pluginName, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent!.ToString().Should().Contain("Plugin not found");
    }

    [Fact]
    public async Task DeletePortalWebPlugin_NameOfPersonalPlugin_PersonalCopyListedAgain()
    {
        // Arrange
        const string pluginName = "unshadowed-autotest";
        var user = await InviteMember(EmployeeType.User);
        await UploadPersonalAsync(user, pluginName);
        await UploadAsOwnerAsync(pluginName);

        // Act
        await _webpluginsApi.DeleteWebPluginWithHttpInfoAsync(pluginName, TestContext.Current.CancellationToken);

        await _webApiClient.Authenticate(user);
        var availablePlugins = await ListPluginsAsync();

        // Assert
        var plugin = availablePlugins.Should().ContainSingle(x => x.Name == pluginName).Subject;
        plugin.Personal.Should().BeTrue();
        plugin.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePersonalWebPlugin_ShadowedCopy_PortalPluginKept()
    {
        // Arrange
        const string pluginName = "shadow-delete-autotest";
        var user = await InviteMember(EmployeeType.User);
        await UploadPersonalAsync(user, pluginName);
        await UploadAsOwnerAsync(pluginName);
        await _webApiClient.Authenticate(user);

        // Act
        using var response = await SendPersonalAsync(HttpMethod.Delete, pluginName);
        var (status, body) = await WebPluginsTestData.ReadAsync(response, TestContext.Current.CancellationToken);
        var availablePlugins = await ListPluginsAsync();
        var personalPlugins = await ListPluginsAsync(personal: true);

        // Assert
        status.Should().Be(200, body);
        var plugin = availablePlugins.Should().ContainSingle(x => x.Name == pluginName).Subject;
        plugin.Personal.Should().BeFalse();
        plugin.Enabled.Should().BeTrue();
        personalPlugins.Should().NotContain(x => x.Name == pluginName);
    }

    [Fact]
    public async Task UpdatePersonalWebPlugin_SwitchedOffWithCookie_ClearsCookieAndDomain()
    {
        // Arrange
        const string pluginName = "csp-off-autotest";
        const string cspDomain = "https://csp-off.autotest.example";
        var manifest = WebPluginsTestData.Manifest(pluginName);
        manifest["cspDomains"] = cspDomain;
        await UploadPersonalAsync(Owner, manifest);
        using var listResponse = await _webApiClient.GetAsync("api/2.0/settings/webplugins", TestContext.Current.CancellationToken);
        var personalCspCookie = GetPersonalCspCookie(listResponse);
        personalCspCookie.Should().MatchRegex("^[0-9a-f]{64}$");
        (await GetCspAsync(personalCspCookie)).Header.Should().Contain(cspDomain);

        // Act
        using var updateResponse = await SendPersonalAsync(HttpMethod.Put, pluginName, personalCspCookie);
        var (status, body) = await WebPluginsTestData.ReadAsync(updateResponse, TestContext.Current.CancellationToken);
        var csp = await GetCspAsync(personalCspCookie);

        // Assert
        status.Should().Be(200, body);
        var clearedCookie = GetPersonalCspSetCookie(updateResponse);
        clearedCookie.Should().NotBeNull("switching off the last personal plugin with domains clears the cookie the request carried");
        GetPersonalCspCookie(updateResponse).Should().BeEmpty();
        ExpiresOf(clearedCookie!).Should().BeBefore(DateTimeOffset.UtcNow);
        csp.Header.Should().NotContain(cspDomain);
    }

    [Fact]
    public async Task GetCspSettings_CookieWithoutPersonalDomains_NotModified()
    {
        // Arrange: the policy the owner saves carries a Last-Modified; the member installs no personal plugin
        const string anyPersonalCspCookie = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        await _cspApi.ConfigureCspAsync(new CspRequestDto([PortalCspDomain]), TestContext.Current.CancellationToken);
        await _webApiClient.Authenticate(await InviteMember(EmployeeType.User));

        using var first = await SendCspAsync(anyPersonalCspCookie);
        var lastModified = ConditionalGet.Header(first, "Last-Modified");
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        lastModified.Should().NotBeNullOrEmpty();

        // Act
        using var second = await SendCspAsync(anyPersonalCspCookie, lastModified);

        // Assert
        second.StatusCode.Should().Be(HttpStatusCode.NotModified);
        (await second.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
    }

    [Fact]
    public async Task GetCspSettings_CookieWithPersonalDomains_NeverNotModified()
    {
        // Arrange: the policy the owner saves carries a Last-Modified, which a request without the cookie gets back
        const string cspDomain = "https://csp-fresh.autotest.example";
        await _cspApi.ConfigureCspAsync(new CspRequestDto([PortalCspDomain]), TestContext.Current.CancellationToken);
        var manifest = WebPluginsTestData.Manifest("csp-fresh-autotest");
        manifest["cspDomains"] = cspDomain;
        await UploadPersonalAsync(await InviteMember(EmployeeType.User), manifest);
        using var listResponse = await _webApiClient.GetAsync("api/2.0/settings/webplugins", TestContext.Current.CancellationToken);
        var personalCspCookie = GetPersonalCspCookie(listResponse);
        personalCspCookie.Should().MatchRegex("^[0-9a-f]{64}$");

        using var portalCsp = await SendCspAsync(null);
        var lastModified = ConditionalGet.Header(portalCsp, "Last-Modified");
        lastModified.Should().NotBeNullOrEmpty();

        // Act
        using var withoutCookie = await SendCspAsync(null, lastModified);
        var withCookie = await GetCspAsync(personalCspCookie, lastModified);

        // Assert
        withoutCookie.StatusCode.Should().Be(HttpStatusCode.NotModified);
        withCookie.Header.Should().Contain(cspDomain);
    }

    /// <summary>
    /// Sends GET, PUT or DELETE with <c>personal=true</c> as the current caller, on the list when
    /// <paramref name="pluginName"/> is null; a PUT switches the plugin off. Raw HTTP: the generated SDK has no
    /// <c>personal</c> parameter on the plugin operations yet and cannot send the personal CSP cookie.
    /// </summary>
    private async Task<HttpResponseMessage> SendPersonalAsync(HttpMethod method, string? pluginName, string? personalCspCookie = null)
    {
        var path = pluginName is null ? "api/2.0/settings/webplugins" : $"api/2.0/settings/webplugins/{pluginName}";

        using var request = new HttpRequestMessage(method, $"{path}?personal=true");

        if (method == HttpMethod.Put)
        {
            request.Content = new StringContent("{\"enabled\":false,\"settings\":\"\"}", Encoding.UTF8, "application/json");
        }

        if (personalCspCookie is not null)
        {
            request.Headers.TryAddWithoutValidation("Cookie", $"{PersonalCspCookieName}={personalCspCookie}");
        }

        return await _webApiClient.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>The expiry a <c>Set-Cookie</c> line gives its cookie, or null when it gives none.</summary>
    private static DateTimeOffset? ExpiresOf(string setCookie)
    {
        var expires = setCookie.Split(';', StringSplitOptions.TrimEntries)
            .FirstOrDefault(x => x.StartsWith("expires=", StringComparison.OrdinalIgnoreCase));

        return expires is null ? null : DateTimeOffset.Parse(expires["expires=".Length..], CultureInfo.InvariantCulture);
    }
}
