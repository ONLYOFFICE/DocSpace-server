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
/// Builds and uploads web plugin packages — shared by every suite in this folder.
/// <c>POST /api/2.0/settings/webplugins</c> is a multipart file upload, and the generated
/// <c>AddWebPluginFromFileAsync</c> exposes no way to attach the file (it only takes the
/// <c>system</c> query flag), so the upload has to go over raw HTTP through the already
/// authenticated <see cref="BaseTest._webApiClient"/> — the DTO gap, not a preference.
/// </summary>
internal static class WebPluginsTestData
{
    public const string UploadPath = "api/2.0/settings/webplugins";

    /// <summary>The <c>pluginName</c> the default manifest derives from a manifest <c>name</c>.</summary>
    public static string JsName(string pluginName)
    {
        return new string(pluginName.Where(char.IsLetterOrDigit).ToArray());
    }

    /// <summary>
    /// A valid <c>config.json</c> manifest; a test changes or removes keys before packing it.
    /// </summary>
    public static Dictionary<string, object?> Manifest(string pluginName, string version = "1.0.0")
    {
        return new Dictionary<string, object?>
        {
            ["name"] = pluginName,
            ["version"] = version,
            ["description"] = "Autotest plugin",
            ["scopes"] = "rooms",
            ["pluginName"] = JsName(pluginName),
            ["author"] = "Autotest",
            ["license"] = "MIT",
            ["homePage"] = ""
        };
    }

    /// <summary>The bytes of a text entry.</summary>
    public static byte[] Text(string content)
    {
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content);
    }

    /// <summary>
    /// Packs the given entries into a zip archive. A <c>null</c> content creates a directory entry.
    /// </summary>
    public static byte[] CreateZip(IEnumerable<(string Name, byte[]? Content)> entries)
    {
        using var memoryStream = new MemoryStream();

        using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = archive.CreateEntry(name, System.IO.Compression.CompressionLevel.Optimal);

                if (content is null)
                {
                    continue;
                }

                using var entryStream = entry.Open();
                entryStream.Write(content);
            }
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// A package from a manifest: <c>config.json</c> and <c>plugin.js</c> (each left out when its
    /// flag is off) plus the extra entries.
    /// </summary>
    public static byte[] CreatePluginZip(
        Dictionary<string, object?> manifest,
        bool withConfig = true,
        bool withScript = true,
        params (string Name, byte[]? Content)[] extraEntries)
    {
        var entries = new List<(string Name, byte[]? Content)>();

        if (withConfig)
        {
            entries.Add(("config.json", Text(JsonSerializer.Serialize(manifest))));
        }

        if (withScript)
        {
            var jsName = manifest.TryGetValue("pluginName", out var value) ? value?.ToString() : "plugin";
            entries.Add(("plugin.js", Text($"window.Plugins = window.Plugins || {{}}; window.Plugins.{jsName} = {{}};")));
        }

        entries.AddRange(extraEntries);

        return CreateZip(entries);
    }

    /// <summary>
    /// Builds a zip archive containing a <c>config.json</c> plugin manifest and the
    /// <c>plugin.js</c> entry the extractor requires — the minimum a plugin package needs to pass
    /// validation.
    /// </summary>
    public static byte[] CreatePluginZip(string pluginName)
    {
        return CreatePluginZip(Manifest(pluginName));
    }

    /// <summary>
    /// Uploads the given bytes as one file of a multipart form, as the currently authenticated caller.
    /// </summary>
    public static async Task<HttpResponseMessage> UploadAsync(HttpClient webApiClient, byte[] fileBytes, string fileName, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", fileName);

        return await webApiClient.PostAsync(UploadPath, content, cancellationToken);
    }

    /// <summary>
    /// Uploads a freshly built plugin package as the currently authenticated <paramref name="webApiClient"/>
    /// caller, returning the raw HTTP response so the test can assert on its status.
    /// </summary>
    public static Task<HttpResponseMessage> UploadPluginAsync(HttpClient webApiClient, string pluginName, CancellationToken cancellationToken)
    {
        return UploadAsync(webApiClient, CreatePluginZip(pluginName), "plugin.zip", cancellationToken);
    }

    /// <summary>
    /// Uploads a package built from <paramref name="manifest"/> and fails the test unless the upload succeeds.
    /// </summary>
    public static async Task UploadOkAsync(HttpClient webApiClient, Dictionary<string, object?> manifest, CancellationToken cancellationToken, params (string Name, byte[]? Content)[] extraEntries)
    {
        using var response = await UploadAsync(webApiClient, CreatePluginZip(manifest, true, true, extraEntries), "plugin.zip", cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        response.IsSuccessStatusCode.Should().BeTrue($"the upload of {manifest["name"]} must succeed, the server answered {(int)response.StatusCode}: {body}");
    }

    /// <summary>The status and the body text of a response, for the assertions on a refused upload.</summary>
    public static async Task<(int Status, string Body)> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        return ((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }
}
