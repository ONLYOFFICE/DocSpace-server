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
    public WebPluginConfigSettings(ConfigurationExtension configuration)
    {
        configuration.GetSetting("plugins", this);
    }

    public bool Enabled { get; set; }
    public bool Upload { get; set; }
    public bool Delete { get; set; }

    public int MaxCount
    {
        get => field > 0 ? field : 100;
        set;
    }

    public long MaxSize
    {
        get => field > 0 ? field : 5L * 1024L * 1024L;
        set;
    }

    public string Extension
    {
        get => field ?? ".zip";
        set;
    }

    public string[] AssetExtensions
    {
        get => field ?? [];
        set;
    }

    public int AssetMaxCount
    {
        get => field > 0 ? field : 10;
        set;
    }
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

public class WebPluginState(bool enabled, string settings)
{
    public bool Enabled { get; set; } = enabled;
    public string Settings { get; set; } = settings;
}
