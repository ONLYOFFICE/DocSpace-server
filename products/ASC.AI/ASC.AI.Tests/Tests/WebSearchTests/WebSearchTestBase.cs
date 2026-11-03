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
/// Shared plumbing of the web-search suites ported from <c>ai/web-search/*.spec.ts</c>.
/// </summary>
/// <remarks>
/// The integration-test host is standalone: the ONLYOFFICE AI gateway is not configured, so the add-on that the
/// TypeScript suite flips through the wallet does not exist, and the web-search configuration is the one an
/// administrator stores by hand (<c>web-search/configure</c>, <c>web-search/set-active-config</c>), kept per portal.
/// The suites therefore store a config pointing at <see cref="WebSearchProviderStub"/>. The reads and the well-formed
/// stores go through the typed <c>WebSearchApi</c>; the validation cases that send bodies a typed DTO cannot carry go
/// raw.
/// </remarks>
public abstract class WebSearchTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected const string WebSearchPath = "/api/2.0/ai/web-search";
    protected const string PassthroughPath = "/api/2.0/ai/websearch/v1";

    /// <summary>The provider name that keeps its stored base URL on the passthrough (see <see cref="WebSearchProviderStub"/>).</summary>
    public const string StubProvider = "stub-search";

    public const string StubKey = "sk-web-search-secret";

    protected const string MissingEntityId = "999999999";

    protected const string ProviderRequired = "config.provider is required and must be a non-empty string";
    protected const string ConfigRequired = "config is required and must be an object";

    /// <summary>The typed client of the web-search routes, on the chat client (so it acts as whoever that is signed in as).</summary>
    protected DocSpace.API.SDK.Api.AI.WebSearchApi SdkWebSearch => _aiWebSearch;

    /// <summary>One answer of a web-search route: status, body and the parsed JSON when there is some.</summary>
    protected sealed record Reply(int Status, string Text, string? ContentType = null)
    {
        public JsonElement Json
        {
            get
            {
                try
                {
                    using var document = JsonDocument.Parse(Text);

                    return document.RootElement.Clone();
                }
                catch (JsonException)
                {
                    return default;
                }
            }
        }

        public string? Error => Str("error");

        /// <summary>The string field of an object body, or null when the body is no object or lacks it.</summary>
        public string? Str(string name) =>
            Json.ValueKind == JsonValueKind.Object && Json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        public bool IsTrue => Text.Trim() == "true";
        public bool IsFalse => Text.Trim() == "false";
        public bool IsNull => Text.Trim() == "null";

        public override string ToString() => $"{Status}: {Text}";
    }

    protected async Task<Reply> SendAsync(HttpMethod method, string path, string? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        }

        using var response = await _aiChatClient.SendAsync(request, TestContext.Current.CancellationToken);

        return new Reply(
            (int)response.StatusCode,
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>
    /// Asks <c>is-configured</c> through the typed client. A non-2xx comes back as a <see cref="Reply"/> with the status
    /// and body of the <see cref="ApiException"/>; an answer is the text <c>true</c> or <c>false</c>.
    /// </summary>
    protected async Task<Reply> IsConfiguredAsync(string? entityId = null)
    {
        try
        {
            var response = await SdkWebSearch.AiWebSearchIsConfiguredWithHttpInfoAsync(entityId, TestContext.Current.CancellationToken);

            return new Reply((int)response.StatusCode, JsonSerializer.Serialize(response.Data), "application/json");
        }
        catch (ApiException exception)
        {
            return new Reply(exception.ErrorCode, exception.ErrorContent?.ToString() ?? string.Empty);
        }
    }

    /// <summary>
    /// Reads the active config through the typed client. A non-2xx comes back as a <see cref="Reply"/> with the status
    /// and body of the <see cref="ApiException"/>; an unconfigured scope is the text <c>null</c>.
    /// </summary>
    protected async Task<Reply> GetActiveConfigAsync(string? entityId = null)
    {
        try
        {
            var response = await SdkWebSearch.AiWebSearchGetActiveConfigWithHttpInfoAsync(entityId, TestContext.Current.CancellationToken);

            return new Reply((int)response.StatusCode, response.Data?.ToJson() ?? "null", "application/json");
        }
        catch (ApiException exception)
        {
            return new Reply(exception.ErrorCode, exception.ErrorContent?.ToString() ?? string.Empty);
        }
    }

    protected Task<Reply> ClearAsync(string body = "{}") =>
        SendAsync(HttpMethod.Delete, $"{WebSearchPath}/clear", body);

    // Raw: a body the typed request cannot carry (malformed config, wrong-typed fields, no config at all).
    protected Task<Reply> SetActiveAsync(string body) =>
        SendAsync(HttpMethod.Put, $"{WebSearchPath}/set-active-config", body);

    // Raw: a body the typed request cannot carry (malformed config, wrong-typed fields, no config at all).
    protected Task<Reply> ConfigureAsync(string body) =>
        SendAsync(HttpMethod.Put, $"{WebSearchPath}/configure", body);

    /// <summary>Stores a well-formed config through the typed client; a non-2xx comes back as a <see cref="Reply"/>.</summary>
    protected async Task<Reply> SetActiveAsync(string provider, string? key, string? baseUrl, string? entityId = null)
    {
        try
        {
            var response = await SdkWebSearch.AiWebSearchSetActiveConfigWithHttpInfoAsync(
                new AiWebSearchSetActiveConfigRequest(new AiWebSearchConfig(provider, key!, baseUrl!), entityId!),
                TestContext.Current.CancellationToken);

            return new Reply((int)response.StatusCode, response.Data.ToJson(), "application/json");
        }
        catch (ApiException exception)
        {
            return new Reply(exception.ErrorCode, exception.ErrorContent?.ToString() ?? string.Empty);
        }
    }

    /// <summary>Configures with a well-formed config through the typed client; a non-2xx comes back as a <see cref="Reply"/>.</summary>
    protected async Task<Reply> ConfigureAsync(string provider, string? key, string? baseUrl, string? entityId = null)
    {
        try
        {
            var response = await SdkWebSearch.AiWebSearchConfigureWithHttpInfoAsync(
                new AiWebSearchConfigureRequest(new AiWebSearchConfig(provider, key!, baseUrl!), entityId!),
                TestContext.Current.CancellationToken);

            return new Reply((int)response.StatusCode, response.Data.ToJson(), "application/json");
        }
        catch (ApiException exception)
        {
            return new Reply(exception.ErrorCode, exception.ErrorContent?.ToString() ?? string.Empty);
        }
    }

    protected Task<Reply> TestConnectionAsync(string body) =>
        SendAsync(HttpMethod.Post, $"{WebSearchPath}/test-connection", body);

    protected Task<Reply> PassthroughAsync(string route, string body, string? query = null) =>
        SendAsync(HttpMethod.Post, $"{PassthroughPath}/{route}{query}", body);

    /// <summary>A <c>{config:{provider,key,baseUrl}}</c> body of configure and set-active-config.</summary>
    protected static string ConfigJson(string provider, string? key, string? baseUrl, string? entityId = null) =>
        JsonSerializer.Serialize(new { config = new { provider, key, baseUrl }, entityId });

    /// <summary>The flat <c>{provider,key,baseUrl}</c> body of test-connection.</summary>
    protected static string ProbeJson(string provider, string? key, string? baseUrl) =>
        JsonSerializer.Serialize(new { provider, key, baseUrl });

    /// <summary>The three routes that take a <c>baseUrl</c> and guard it the same way.</summary>
    public static readonly string[] BaseUrlRoutes = ["configure", "set-active-config", "test-connection"];

    /// <summary>Sends a config with the given provider and base URL to one of <see cref="BaseUrlRoutes"/>.</summary>
    protected Task<Reply> SendToRouteAsync(string route, string provider, string? baseUrl) => route switch
    {
        "configure" => ConfigureAsync(provider, "k", baseUrl),
        "set-active-config" => SetActiveAsync(provider, "k", baseUrl),
        _ => TestConnectionAsync(ProbeJson(provider, "k", baseUrl))
    };

    /// <summary>Stores a config for <see cref="StubProvider"/> as whoever the chat client is signed in as (an administrator).</summary>
    protected async Task StoreStubAsync(WebSearchProviderStub stub, string? key = StubKey, string provider = StubProvider)
    {
        var result = await SdkWebSearch.AiWebSearchSetActiveConfigAsync(
            new AiWebSearchSetActiveConfigRequest(new AiWebSearchConfig(provider, key!, stub.BaseUrl)),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
    }

    /// <summary>Reads both state routes as the owner and asserts the unconfigured answer; signs the owner back in.</summary>
    protected async Task ExpectNothingStoredAsync(string? entityId = null)
    {
        await ActAsOwnerAsync();

        var configured = await IsConfiguredAsync(entityId);
        var active = await GetActiveConfigAsync(entityId);

        new { configured = configured.ToString(), active = active.ToString() }
            .Should().BeEquivalentTo(new { configured = "200: false", active = "200: null" }, "nothing may be stored");
    }

    protected async Task ExpectStoredAsync(string? provider = null)
    {
        await ActAsOwnerAsync();

        var configured = await IsConfiguredAsync();
        var active = await GetActiveConfigAsync();

        configured.IsTrue.Should().BeTrue(configured.ToString());
        active.Status.Should().Be(200, active.ToString());
        if (provider is not null)
        {
            active.Str("provider").Should().Be(provider, active.ToString());
        }
    }

    protected async Task<User> ActAsAsync(EmployeeType type)
    {
        var user = type == EmployeeType.Guest
            ? await InviteGuest(TestContext.Current.CancellationToken)
            : await InviteContact(type, TestContext.Current.CancellationToken);

        await _aiChatClient.Authenticate(user);

        return user;
    }

    protected async Task ActAsOwnerAsync() => await _aiChatClient.Authenticate(Owner);

    protected async Task ActAsAnonymousAsync() => await _aiChatClient.Authenticate(null);

    protected async Task<int> CreateCustomRoomAsync(string? title = null)
    {
        await FilesHttpClient.Authenticate(Owner);
        var room = await RoomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title ?? $"room-{Guid.NewGuid():N}", roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken);

        return room.Response.Id;
    }

    protected async Task<int> CreateFolderAsync(string? title = null)
    {
        var parent = await GetMyDocumentsFolderIdAsync();
        var folder = await FoldersApi.CreateFolderAsync(
            parent,
            new CreateFolderRequest(title ?? $"folder-{Guid.NewGuid():N}"),
            TestContext.Current.CancellationToken);

        return folder.Response.Id;
    }

    public static TheoryData<string> ScopeKinds => new() { "aiRoom", "customRoom", "folder" };

    /// <summary>The id of a fresh entity of the given kind, as the owner: what a chat is opened in.</summary>
    protected async Task<string> CreateScopeAsync(string kind) => kind switch
    {
        "aiRoom" => (await CreateRoomAsync()).ToString(),
        "customRoom" => (await CreateCustomRoomAsync()).ToString(),
        _ => (await CreateFolderAsync()).ToString()
    };
}
