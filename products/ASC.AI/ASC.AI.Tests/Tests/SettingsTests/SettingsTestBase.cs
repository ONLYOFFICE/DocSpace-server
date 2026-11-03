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

namespace ASC.AI.Tests.Tests.SettingsTests;

/// <summary>
/// Helpers of the AI settings suites (<c>/api/2.0/ai/config*</c>, <c>/api/2.0/ai/web-search/*</c>, ported from the
/// settings specs of the TypeScript suite).
/// </summary>
/// <remarks>
/// The integration-test host is standalone: there is no AI gateway, so the portal reports itself AI-ready when the AI
/// switch is on AND the portal holds at least one profile (<c>AiAccessibility.GetStatusAsync</c>). The wallet state
/// the TypeScript suite switched with <c>enableAiGateway</c> is replaced by creating a profile on the stub model.
/// Vectorization settings are managed by DocSpace admins (nobody when a gateway is configured).
/// </remarks>
public abstract class SettingsTestBase(AspireAppFixture fixture) : ChatTests.ChatPermissionTestBase(fixture)
{
    protected const string ConfigRoute = "/api/2.0/ai/config";
    protected const string UserConfigRoute = "/api/2.0/ai/config/user";
    protected const string VectorizationRoute = "/api/2.0/ai/config/vectorization";
    protected const string WebSearchIsConfiguredRoute = "/api/2.0/ai/web-search/is-configured";
    protected const string WebSearchActiveConfigRoute = "/api/2.0/ai/web-search/get-active-config";

    protected const string ChatRecommendedModelVisible = "chatRecommendedModelVisible";

    /// <summary>The portal settings client (Web.Api), for the AI switch itself.</summary>
    protected CommonSettingsApi CommonSettings => CommonSettingsApi;

    /// <summary>The vectorization client of the chat service.</summary>
    protected DocSpace.API.SDK.Api.AI.VectorizationApi Vectorization => _aiVectorization;

    /// <summary>The web-search client of the chat service.</summary>
    protected DocSpace.API.SDK.Api.AI.WebSearchApi WebSearch => _aiWebSearch;

    /// <summary>
    /// <c>web-search/get-active-config</c> through the typed client: 200 with the body <c>null</c> while no provider is
    /// configured (or the config as JSON), otherwise the status and error body of the ApiException.
    /// </summary>
    protected async Task<(HttpStatusCode Status, string Body)> ReadActiveConfigAsync()
    {
        try
        {
            var config = await WebSearch.AiWebSearchGetActiveConfigAsync(cancellationToken: TestContext.Current.CancellationToken);

            return (HttpStatusCode.OK, config is null ? "null" : config.ToJson());
        }
        catch (ApiException exception)
        {
            return ((HttpStatusCode)exception.ErrorCode, exception.ErrorContent?.ToString() ?? string.Empty);
        }
    }

    /// <summary>
    /// A read of one of the two web-search routes through the typed client.
    /// </summary>
    protected Task<(HttpStatusCode Status, string Body)> ReadWebSearchRouteAsync(string route) =>
        route == WebSearchActiveConfigRoute ? ReadActiveConfigAsync() : ReadIsConfiguredAsync();

    /// <summary><c>web-search/is-configured</c> through the typed client: 200 with <c>true</c>/<c>false</c>, or the refusal.</summary>
    protected async Task<(HttpStatusCode Status, string Body)> ReadIsConfiguredAsync()
    {
        try
        {
            var configured = await WebSearch.AiWebSearchIsConfiguredAsync(cancellationToken: TestContext.Current.CancellationToken);

            return (HttpStatusCode.OK, configured ? "true" : "false");
        }
        catch (ApiException exception)
        {
            return ((HttpStatusCode)exception.ErrorCode, exception.ErrorContent?.ToString() ?? string.Empty);
        }
    }

    /// <summary>Invites a member of the type and signs the chat client in as them.</summary>
    protected async Task<User> ActAsMemberAsync(EmployeeType type)
    {
        var member = await InviteMemberAsync(type);
        await _aiChatClient.Authenticate(member);

        return member;
    }

    protected async Task<AiSettingsDto> ReadConfigAsync() =>
        (await _aiSettings.AiSettingsGetAsync(TestContext.Current.CancellationToken)).Response;

    protected async Task<bool> ReadVisibleAsync() =>
        (await _aiSettings.AiSettingsGetUserAsync(TestContext.Current.CancellationToken)).Response.ChatRecommendedModelVisible;

    protected async Task<bool> WriteVisibleAsync(bool value)
    {
        var written = await _aiSettings.AiSettingsSetUserAsync(
            new Dictionary<string, object> { [ChatRecommendedModelVisible] = value },
            TestContext.Current.CancellationToken);

        return written.Response.ChatRecommendedModelVisible;
    }

    /// <summary>The raw GET of a route, parsed: for the cases about which fields the body carries.</summary>
    protected async Task<JsonElement> ReadRawResponseAsync(string route)
    {
        using var response = await new RawApiClient(_aiChatClient).GetAsync(route, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        return JsonDocument.Parse(body).RootElement.GetProperty("response").Clone();
    }

    /// <summary>The status and body of a raw request on the chat service, signed in as whoever the chat client is.</summary>
    protected Task<(HttpStatusCode Status, string Body)> RawAsync(HttpMethod method, string route, string? json = null) =>
        SendRawAsync(method, route, json);

    /// <summary>Whether the portal AI switch (<c>settings/ai-access</c>) is on, read as <paramref name="user"/>.</summary>
    protected async Task<bool> ReadAiAccessAsync(User? user)
    {
        await WebApiHttpClient.Authenticate(user);

        return (await CommonSettings.GetTenantAiAccessSettingsAsync(TestContext.Current.CancellationToken)).Response.Enabled;
    }

    /// <summary>Tries to set the portal AI switch as <paramref name="user"/>; returns the status the write ended with.</summary>
    protected async Task<int> TrySetAiAccessAsync(User? user, bool enabled)
    {
        await WebApiHttpClient.Authenticate(user);

        return await StatusOfAsync(async () => await CommonSettings.SetTenantAiAccessSettingsAsync(
            new TenantAiAccessSettingsRequestDto(enabled: enabled),
            TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// The status of <c>web-search/is-configured</c> (200 for everyone but a Guest, 403 when the portal AI switch is
    /// off) once it is <paramref name="open"/> or the deadline passes; the last observed status either way. The AI
    /// service learns of a switch flip through its own settings cache a moment after Web.Api stored it.
    /// </summary>
    protected async Task<HttpStatusCode> WaitForTheAiGateAsync(bool open)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (true)
        {
            var (status, _) = await ReadIsConfiguredAsync();

            if ((status == HttpStatusCode.OK) == open || DateTime.UtcNow >= deadline)
            {
                return status;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>A profile on the stub model: the portal turns AI-ready once it holds one.</summary>
    protected Task MakePortalAiReadyAsync() => GetProfileAsync();
}
