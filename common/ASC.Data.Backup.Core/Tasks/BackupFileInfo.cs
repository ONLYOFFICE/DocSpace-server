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

namespace ASC.Data.Backup.Tasks;

public class BackupFileInfo
{
    public string Domain { get; init; }
    public string Module { get; init; }
    public string Path { get; init; }
    public int Tenant { get; init; }

    public BackupFileInfo() { }

    public BackupFileInfo(string domain, string module, string path, int tenant = -1)
    {
        Domain = domain;
        Module = module;
        Path = path;
        Tenant = tenant;
    }

    public XElement ToXElement()
    {
        var xElement = new XElement("file",
                            new XElement("domain", Domain),
                            new XElement("module", Module),
                            new XElement("path", Path));

        if (Tenant != -1)
        {
            xElement.Add(new XElement("tenant", Tenant));
        }

        return xElement;
    }

    public static BackupFileInfo FromXElement(XElement el)
    {
        return new BackupFileInfo
        {
            Domain = el.Element("domain").ValueOrDefault(),
            Module = el.Element("module").ValueOrDefault(),
            Path = el.Element("path").ValueOrDefault(),
            Tenant = Convert.ToInt32(el.Element("tenant").ValueOrDefault() ?? "-1")
        };
    }
}