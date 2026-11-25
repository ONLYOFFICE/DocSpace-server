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

namespace ASC.Web.Core;

[Singleton]
public class WebPluginConfigSettings
{
    public WebPluginConfigSettings(IConfiguration configuration, ILogger<WebPluginConfigSettings> logger)
    {
        Enabled = configuration.ReadBool("plugins:enabled", false, logger);
        Upload = configuration.ReadBool("plugins:upload", false, logger);
        Delete = configuration.ReadBool("plugins:delete", false, logger);
        Personal = configuration.ReadBool("plugins:personal", true, logger);
        // plugins a portal holds: 1 to 1000, 100 when not set
        MaxCount = configuration.ReadInt("plugins:maxCount", 100, 1, 1000, logger);
        // personal plugins a member holds: 1 to 100, 10 when not set
        PersonalMaxCount = configuration.ReadInt("plugins:personalMaxCount", 10, 1, 100, logger);
        // bytes of an uploaded package: 1024 (1 KB) to 104857600 (100 MB), 5242880 (5 MB) when not set
        MaxSize = configuration.ReadLong("plugins:maxSize", 5L * 1024L * 1024L, 1024L, 100L * 1024L * 1024L, logger);
        Extension = configuration["plugins:extension"] ?? ".zip";
        AssetExtensions = ReadAssetExtensions(configuration, logger);
        // asset files in a package: 0 to 100, 10 when not set
        AssetMaxCount = configuration.ReadInt("plugins:assetMaxCount", 10, 0, 100, logger);
    }

    private static string[] ReadAssetExtensions(IConfiguration configuration, ILogger logger)
    {
        var section = configuration.GetSection("plugins:assetExtensions");

        if (section.Value != null)
        {
            logger.WarningAssetExtensionsNotAList(section.Path);

            return [];
        }

        return section.GetChildren()
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
    }

    public bool Enabled { get; }
    public bool Upload { get; }
    public bool Delete { get; }
    public bool Personal { get; }
    public int MaxCount { get; }
    public int PersonalMaxCount { get; }
    public long MaxSize { get; }
    public string Extension { get; }
    public string[] AssetExtensions { get; }
    public int AssetMaxCount { get; }
}

public class WebPluginSettings : ISettings<WebPluginSettings>
{
    public Dictionary<string, WebPluginState> EnabledPlugins { get; set; }

    public static Guid ID => new("{B33CB1F2-1FE6-4BD5-83D0-0D9C217490F5}");

    public WebPluginSettings GetDefault()
    {
        return new WebPluginSettings();
    }

    public DateTime LastModified { get; set; }
}

public class PersonalWebPluginsSettings : ISettings<PersonalWebPluginsSettings>
{
    public bool Enabled { get; set; }

    public static Guid ID => new("{2120AD18-1091-4061-8E4B-27BF0A988BCE}");

    public PersonalWebPluginsSettings GetDefault()
    {
        return new PersonalWebPluginsSettings();
    }

    public DateTime LastModified { get; set; }
}

public class WebPluginState(bool enabled, string settings)
{
    public bool Enabled { get; set; } = enabled;
    public string Settings { get; set; } = settings;
}
