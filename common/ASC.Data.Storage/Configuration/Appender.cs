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

namespace ASC.Data.Storage.Configuration;

[Singleton]
public class StorageConfigExtension(IConfiguration configuration)
{
    public Storage Storage { get; init; } = configuration.GetSection("storage").Get<Storage>();
}


public class Storage
{
    public IEnumerable<Appender> Appender { get; set; }
    public IEnumerable<Handler> Handler { get; set; }
    public IEnumerable<Module> Module { get; set; }

    public Module GetModuleElement(string name)
    {
        return Module?.FirstOrDefault(r => r.Name == name);
    }
    public Handler GetHandler(string name)
    {
        return Handler?.FirstOrDefault(r => r.Name == name);
    }
}

public class Appender
{
    public string Name { get; set; }
    public string Append { get; set; }
    public string AppendSecure { get; set; }
    public string Extensions { get; set; }
}

public class Handler
{
    public string Name { get; set; }
    public string Type { get; set; }
    public IEnumerable<Properties> Property { get; set; }

    public IDictionary<string, string> GetProperties()
    {
        return Property == null || !Property.Any() ? new Dictionary<string, string>()
            : Property.ToDictionary(r => r.Name, r => r.Value);
    }
}
public class Properties
{
    public string Name { get; set; }
    public string Value { get; set; }
}

public class Module
{
    public string Name { get; set; }
    public string Data { get; set; }
    public string Type { get; set; }
    public string Path { get; set; }
    public ACL Acl { get; set; } = ACL.Read;
    public string VirtualPath { get; set; }
    public TimeSpan Expires { get; set; }
    public bool Visible { get; set; } = true;
    public bool AppendTenantId { get; set; } = true;
    public bool Public { get; set; }
    public bool DisableMigrate { get; set; }
    public bool Count { get; set; } = true;
    public bool DisabledEncryption { get; set; }
    public IEnumerable<Module> Domain { get; set; } = new List<Module>();
    public string ValidatorType { get; set; }
    public bool? ContentAsAttachment { get; set; }
}