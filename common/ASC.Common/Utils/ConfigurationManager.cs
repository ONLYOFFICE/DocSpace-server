﻿// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Common.Utils;

public class ConnectionStringCollection(IEnumerable<ConnectionStringSettings> data) : IEnumerable<ConnectionStringSettings>
{
    private readonly List<ConnectionStringSettings> _data = data.ToList();

    public ConnectionStringSettings this[string name] => _data.Find(r => r.Name == name);

    public IEnumerator<ConnectionStringSettings> GetEnumerator()
    {
        return _data.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

[Singleton]
public class ConfigurationExtension
{
    public string this[string key]
    {
        get => _configuration[key];
        set => _configuration[key] = value;
    }

    private readonly IConfiguration _configuration;
    private readonly Lazy<ConnectionStringCollection> _connectionStringSettings;

    public ConfigurationExtension(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionStringSettings = new Lazy<ConnectionStringCollection>(new ConnectionStringCollection(_configuration.GetSection($"ConnectionStrings").Get<IEnumerable<ConnectionStringSettings>>()));
    }
    
    public void GetSetting<T>(string section, T instance)
    {
        var sectionSettings = _configuration.GetSection(section);

        sectionSettings.Bind(instance);
    }

    public ConnectionStringCollection GetConnectionStrings()
    {
        return _connectionStringSettings.Value;
    }

    public ConnectionStringSettings GetConnectionStrings(string key, string region = "current")
    {
        if (region == "current")
        {
            return GetConnectionStrings()[key];
        }

        var connectionStrings = new ConnectionStringCollection(_configuration.GetSection($"regions:{region}:ConnectionStrings").Get<IEnumerable<ConnectionStringSettings>>());
        return connectionStrings[key];
    }
}
