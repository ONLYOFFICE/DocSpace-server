// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Web.Core;

[Singleton]
public class WebPluginConfigSettings
{
    public WebPluginConfigSettings(ConfigurationExtension configuration)
    {
        configuration.GetSetting("plugins", this);
    }

    private long _maxSize;
    private string _extension;
    private string[] _assetExtensions;

    public bool Enabled { get; set; }
    public bool Upload { get; set; }
    public bool Delete { get; set; }

    public long MaxSize
    {
        get => _maxSize > 0 ? _maxSize : 5L * 1024L * 1024L;
        set => _maxSize = value;
    }

    public string Extension
    {
        get => _extension ?? ".zip";
        set => _extension = value;
    }

    public string[] AssetExtensions
    {
        get => _assetExtensions ?? [];
        set => _assetExtensions = value;
    }
}

public class WebPluginSettings : ISettings<WebPluginSettings>
{
    public Dictionary<string, WebPluginState> EnabledPlugins { get; set; }

    [JsonIgnore]
    public Guid ID
    {
        get { return new Guid("{B33CB1F2-1FE6-4BD5-83D0-0D9C217490F5}"); }
    }

    public WebPluginSettings GetDefault()
    {
        return new WebPluginSettings();
    }
}

public class WebPluginState(bool enabled, string settings)
{
    public bool Enabled { get; set; } = enabled;
    public string Settings { get; set; } = settings;
}