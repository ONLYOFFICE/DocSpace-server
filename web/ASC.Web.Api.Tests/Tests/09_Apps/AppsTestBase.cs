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


namespace ASC.Web.Api.Tests.Tests._09_Apps;

/// <summary>
/// Shared setup for the Apps suites (<c>/api/2.0/apps</c>, served by Web.Api). Each test owns its portal,
/// so no test has to put an application back the way it found it (the TypeScript suite does, because it
/// shares one). Which applications exist is decided by the installation, so tests take whatever the list
/// reports and fail with a readable message when there are not enough.
///
/// The settings document is an arbitrary JSON value; the generated model carries it as <c>object</c>
/// (a Newtonsoft token), which cannot be compared structurally with what was sent. Every assertion on a
/// document therefore reads the raw JSON and compares <see cref="Canonical(JsonElement)"/> forms — an SDK
/// model too narrow to express the field, not a preference. Everything else goes through the typed client.
/// </summary>
public abstract class AppsTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>The state of one application; <c>Settings</c> is the canonical JSON of the document, null when none is saved.</summary>
    protected sealed record AppState(string Id, bool Enabled, string? Settings);

    private DocSpace.API.SDK.Api.Apps.AppsApi? _appsApi;

    // The Apps client is not wired into the shared PortalClients bundle; it rides the same Web.Api client
    // (and therefore the same auth header) as every other Web.Api call of the test.
    protected DocSpace.API.SDK.Api.Apps.AppsApi AppsApi => _appsApi ??= new DocSpace.API.SDK.Api.Apps.AppsApi(
        _webApiClient, new Configuration { BasePath = _webApiClient.BaseAddress!.ToString().TrimEnd('/') });

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected static string AppPath(string id) => $"api/2.0/apps/{Uri.EscapeDataString(id)}";

    protected async Task<string[]> PickAppsAsync(int count)
    {
        var apps = await AppsApi.GetAllAsync(Ct);
        var ids = apps.Response.Select(app => app.Id).ToArray();

        ids.Should().HaveCountGreaterThanOrEqualTo(count, "the installation has to expose enough applications for this test");

        return ids[..count];
    }

    protected async Task<(HttpStatusCode Status, string Body)> SendAsync(HttpMethod method, string path, string? body = null)
    {
        using var request = new HttpRequestMessage(method, path);

        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        using var response = await _webApiClient.SendAsync(request, Ct);

        return (response.StatusCode, await response.Content.ReadAsStringAsync(Ct));
    }

    /// <summary>The <c>response</c> member of an envelope; null when it is absent or JSON null.</summary>
    protected static JsonElement? ResponseOf(string body)
    {
        using var document = JsonDocument.Parse(body);

        return document.RootElement.TryGetProperty("response", out var response) && response.ValueKind != JsonValueKind.Null
            ? response.Clone()
            : null;
    }

    protected static string? ErrorMessageOf(string body)
    {
        using var document = JsonDocument.Parse(body);

        return document.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("message", out var message)
            ? message.GetString()
            : null;
    }

    protected async Task<List<AppState>> ListStatesAsync()
    {
        var (status, body) = await SendAsync(HttpMethod.Get, "api/2.0/apps");
        status.Should().Be(HttpStatusCode.OK, body);

        return [.. ResponseOf(body)!.Value.EnumerateArray().Select(ToState)];
    }

    protected async Task<AppState> SnapshotAsync(string id)
    {
        var (status, body) = await SendAsync(HttpMethod.Get, AppPath(id));
        status.Should().Be(HttpStatusCode.OK, body);

        return ToState(ResponseOf(body)!.Value);
    }

    protected async Task<string?> ReadSettingsAsync(string id)
    {
        var (status, body) = await SendAsync(HttpMethod.Get, $"{AppPath(id)}/settings");
        status.Should().Be(HttpStatusCode.OK, body);

        return ResponseOf(body) is { } settings ? Canonical(settings) : null;
    }

    /// <summary>Saves a settings document given as raw JSON; <c>null</c> resets it.</summary>
    protected async Task SaveSettingsAsync(string id, string rawJson)
    {
        var (status, body) = await SendAsync(HttpMethod.Put, $"{AppPath(id)}/settings", $"{{\"settings\":{rawJson}}}");
        status.Should().Be(HttpStatusCode.OK, body);
    }

    protected async Task SaveEnabledAsync(string id, bool enabled)
    {
        var result = await AppsApi.SetEnabledWithHttpInfoAsync(id, new SetAppEnabledRequest(enabled), Ct);
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    protected static string Canonical(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);

        return Canonical(document.RootElement);
    }

    /// <summary>A normal form of a JSON value: object keys ordered, numbers and strings re-written the same way on both sides.</summary>
    protected static string Canonical(JsonElement element)
    {
        var builder = new StringBuilder();
        Append(builder, element);

        return builder.ToString();
    }

    private static AppState ToState(JsonElement app)
    {
        var settings = app.TryGetProperty("settings", out var value) && value.ValueKind != JsonValueKind.Null
            ? Canonical(value)
            : null;

        return new AppState(app.GetProperty("id").GetString()!, app.GetProperty("enabled").GetBoolean(), settings);
    }

    private static void Append(StringBuilder builder, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                builder.Append('{');
                var first = true;

                foreach (var property in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    first = false;
                    builder.Append(JsonSerializer.Serialize(property.Name)).Append(':');
                    Append(builder, property.Value);
                }

                builder.Append('}');
                break;
            case JsonValueKind.Array:
                builder.Append('[');
                var index = 0;

                foreach (var item in element.EnumerateArray())
                {
                    if (index++ > 0)
                    {
                        builder.Append(',');
                    }

                    Append(builder, item);
                }

                builder.Append(']');
                break;
            case JsonValueKind.Number:
                builder.Append(element.GetDouble().ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                break;
            case JsonValueKind.String:
                builder.Append(JsonSerializer.Serialize(element.GetString()));
                break;
            default:
                builder.Append(element.GetRawText());
                break;
        }
    }
}
