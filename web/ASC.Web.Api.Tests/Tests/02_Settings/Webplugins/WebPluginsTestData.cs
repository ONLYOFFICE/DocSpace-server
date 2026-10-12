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
/// Builds and uploads a minimal, valid web plugin package — shared by every suite in this
/// folder. <c>POST /api/2.0/settings/webplugins</c> is a multipart file upload, and the
/// generated <c>AddWebPluginFromFileAsync</c> exposes no way to attach the file (it only takes
/// the <c>system</c> query flag), so the upload has to go over raw HTTP through the already
/// authenticated <see cref="BaseTest._webApiClient"/> — the DTO gap, not a preference.
/// </summary>
internal static class WebPluginsTestData
{
    /// <summary>
    /// Builds a zip archive containing a <c>config.json</c> plugin manifest and the
    /// <c>plugin.js</c> entry the extractor requires — the minimum a plugin package needs to pass
    /// validation.
    /// </summary>
    public static byte[] CreatePluginZip(string pluginName)
    {
        var sanitizedPluginName = new string(pluginName.Where(char.IsLetterOrDigit).ToArray());

        var configJson = JsonSerializer.Serialize(new
        {
            name = pluginName,
            version = "1.0.0",
            description = "Autotest plugin",
            scopes = "rooms",
            pluginName = sanitizedPluginName,
            author = "Autotest",
            license = "MIT",
            homePage = ""
        });

        using var memoryStream = new MemoryStream();

        using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "config.json", configJson);
            AddEntry(archive, "plugin.js", $"window.Plugins = window.Plugins || {{}}; window.Plugins.{sanitizedPluginName} = {{}};");
        }

        return memoryStream.ToArray();
    }

    private static void AddEntry(System.IO.Compression.ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    /// <summary>
    /// Uploads a freshly built plugin package as the currently authenticated <paramref name="webApiClient"/>
    /// caller, returning the raw HTTP response so the test can assert on its status.
    /// </summary>
    public static async Task<HttpResponseMessage> UploadPluginAsync(HttpClient webApiClient, string pluginName, CancellationToken cancellationToken)
    {
        var zipBytes = CreatePluginZip(pluginName);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", "plugin.zip");

        return await webApiClient.PostAsync("api/2.0/settings/webplugins", content, cancellationToken);
    }
}
