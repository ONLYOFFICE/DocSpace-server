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
        _connectionStringSettings = new Lazy<ConnectionStringCollection>(new ConnectionStringCollection(_configuration.GetSection("ConnectionStrings").Get<IEnumerable<ConnectionStringSettings>>()));
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