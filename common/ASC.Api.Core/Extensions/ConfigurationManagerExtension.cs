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

namespace ASC.Api.Core.Extensions;

public static class ConfigurationManagerExtension
{
    public static ConfigurationManager AddDefaultConfiguration(
        this ConfigurationManager config,
        IHostEnvironment env
    )
    {
        var path = config["pathToConf"];

        if (!Path.IsPathRooted(path))
        {
            path = Path.GetFullPath(CrossPlatform.PathCombine(env.ContentRootPath, path));
        }

        config.SetBasePath(path);

        config.AddInMemoryCollection(new Dictionary<string, string> { { "pathToConf", path } });

        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("storage.json", optional: false, reloadOnChange: true)
            .AddJsonFile("externalresources.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"externalresources.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("kafka.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"kafka.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("rabbitmq.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"rabbitmq.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("activemq.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"activemq.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("redis.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"redis.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("zookeeper.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"zookeeper.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

        var formatsPath = Path.GetFullPath(Path.Combine(path, "document-formats", "onlyoffice-docs-formats.json"));

        if (File.Exists(formatsPath))
        {
            var readStream = File.ReadAllText(formatsPath);
            var formats = JsonSerializer.Deserialize<List<FileFormatConfig>>(readStream, new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            });

            using var memoryStream = new MemoryStream();
            JsonSerializer.Serialize(memoryStream, new FileFormatConfigList { FileFormats = formats });
            memoryStream.Position = 0;
            config.AddJsonStream(memoryStream);
        }

        return config;
    }
}

public class FileFormatConfigList
{
    public List<FileFormatConfig> FileFormats { get; set; } = [];
}

/// <summary>
/// Represents a file format configuration with supported actions and conversions
/// </summary>
public class FileFormatConfig
{
    /// <summary>
    /// Gets or sets the name of the file format
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the file format
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of supported actions for this format
    /// </summary>
    public List<string> Actions { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of formats this file can be converted to
    /// </summary>
    public List<string> Convert { get; set; } = [];

    /// <summary>
    /// Gets or sets the MIME types associated with this format
    /// </summary>
    public List<string> Mime { get; set; } = [];

    /// <summary>
    /// Checks if the format supports a specific action
    /// </summary>
    /// <param name="action">The action to check</param>
    /// <returns>True if the action is supported, false otherwise</returns>
    public bool SupportsAction(string action)
    {
        return Actions.Contains(action, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the format matches a specific MIME type
    /// </summary>
    /// <param name="mimeType">The MIME type to check</param>
    /// <returns>True if the MIME type matches, false otherwise</returns>
    public bool MatchesMimeType(string mimeType)
    {
        return Mime.Contains(mimeType, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the primary MIME type for this format
    /// </summary>
    /// <returns>The first MIME type or empty string if none available</returns>
    public string GetPrimaryMimeType()
    {
        return Mime.FirstOrDefault() ?? string.Empty;
    }
}