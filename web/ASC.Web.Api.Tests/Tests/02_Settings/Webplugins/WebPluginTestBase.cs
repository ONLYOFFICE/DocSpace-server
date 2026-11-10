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
/// Helpers shared by the web plugin suites: uploads as the owner, typed reads and the check of a refused upload.
/// </summary>
public abstract class WebPluginTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>Uploads a default package with this manifest name as the owner and leaves the client on the owner.</summary>
    protected async Task UploadAsOwnerAsync(string pluginName, string version = "1.0.0")
    {
        await UploadAsOwnerAsync(WebPluginsTestData.Manifest(pluginName, version));
    }

    /// <summary>Uploads a package built from the manifest as the owner and leaves the client on the owner.</summary>
    protected async Task UploadAsOwnerAsync(Dictionary<string, object?> manifest, params (string Name, byte[]? Content)[] extraEntries)
    {
        await _webApiClient.Authenticate(Owner);
        await WebPluginsTestData.UploadOkAsync(_webApiClient, manifest, TestContext.Current.CancellationToken, extraEntries);
    }

    // The successful reads are raw: the generated WebPluginDto marks image, cssUrl, settings and the other optional
    // fields as required, the server leaves them out when they are null, and the generated client then throws
    // "Required property 'image' not found" for nearly every plugin (P8 of the plugins spec; covered by
    // WebPluginReadTests.GetWebPlugin_GeneratedClient_ReadsAPluginWithoutOptionalFields). Refusals still go
    // through the SDK, since an error answer is never deserialised into the model.

    /// <summary>The plugin as <c>getWebPlugin</c> returns it to the current caller.</summary>
    protected async Task<WebPluginView> GetPluginAsync(string pluginName)
    {
        using var response = await _webApi.GetAsync($"api/2.0/settings/webplugins/{pluginName}", TestContext.Current.CancellationToken);

        return await _webApi.ReadAsync<WebPluginView>(response, TestContext.Current.CancellationToken);
    }

    /// <summary>The plugins <c>getWebPlugins</c> returns to the current caller.</summary>
    protected async Task<List<WebPluginView>> ListPluginsAsync(bool? enabled = null)
    {
        var query = enabled.HasValue ? $"?enabled={enabled.Value.ToString().ToLowerInvariant()}" : string.Empty;

        using var response = await _webApi.GetAsync($"api/2.0/settings/webplugins{query}", TestContext.Current.CancellationToken);

        return await _webApi.ReadAsync<List<WebPluginView>>(response, TestContext.Current.CancellationToken);
    }

    /// <summary>Switches a plugin on or off with the given settings as the current caller.</summary>
    protected async Task UpdatePluginAsync(string pluginName, bool enabled, string settings = "")
    {
        await _webpluginsApi.UpdateWebPluginWithHttpInfoAsync(pluginName, new WebPluginRequest(enabled, settings), TestContext.Current.CancellationToken);
    }

    /// <summary>Uploads the bytes as the owner and asserts the status and a text of the answer.</summary>
    protected async Task AssertUploadRefusedAsync(byte[] fileBytes, int status, string message, string fileName = "plugin.zip")
    {
        await _webApiClient.Authenticate(Owner);

        using var response = await WebPluginsTestData.UploadAsync(_webApiClient, fileBytes, fileName, TestContext.Current.CancellationToken);

        var (actualStatus, body) = await WebPluginsTestData.ReadAsync(response, TestContext.Current.CancellationToken);

        actualStatus.Should().Be(status, body);
        body.Should().Contain(message);
    }
}

/// <summary>The author of a plugin, as the plugin entry carries it.</summary>
public sealed record WebPluginAuthorView(Guid Id, string? DisplayName);

/// <summary>A plugin entry read from the raw JSON (see the note in <see cref="WebPluginTestBase"/>).</summary>
public sealed record WebPluginView(
    string Name,
    string? Version,
    string? MinDocSpaceVersion,
    string? Description,
    string? License,
    string? Author,
    string? HomePage,
    string? PluginName,
    string? Scopes,
    string? Image,
    WebPluginAuthorView? CreateBy,
    DateTime CreateOn,
    bool Enabled,
    bool System,
    string? Url,
    string? CssUrl,
    string? Settings,
    Dictionary<string, string>? NameLocale,
    Dictionary<string, string>? DescriptionLocale);
