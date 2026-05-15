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

namespace ASC.AuditTrail.Models;

public class BaseEvent
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Guid UserId { get; set; }
    public bool Mobile { get; set; }
    public IList<string> Description { get; set; }

    [Event("IpCol")]
    public string IP { get; set; }

    [Event("CountryCol")]
    public string Country { get; set; }

    [Event("CityCol")]
    public string City { get; set; }

    [Event("BrowserCol")]
    public string Browser { get; set; }

    [Event("PlatformCol")]
    public string Platform { get; set; }

    [Event("DateCol")]
    public DateTime Date { get; set; }

    [Event("UserCol")]
    public string UserName { get; set; }

    [Event("PageCol")]
    public string Page { get; set; }

    [Event("ActionCol")]
    public string ActionText { get; set; }
}

[Scope]
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public partial class BaseEventMapper(TenantUtil tenantUtil)
{
    [MapPropertyFromSource(nameof(BaseEvent.IP), Use = nameof(Resolve))]
    [MapPropertyFromSource(nameof(BaseEvent.Date), Use = nameof(ResolveDate))]
    public partial BaseEvent Map(DbLoginEvent source);
    public partial List<BaseEvent> Map(List<DbLoginEvent> source);

    [UserMapping(Default = false)]
    private static string Resolve(DbLoginEvent source)
    {
        if (!string.IsNullOrEmpty(source.Ip))
        {
            var ipSplited = source.Ip.Split(':');
            if (ipSplited.Length > 1)
            {
                return ipSplited[0];
            }
        }

        return source.Ip;
    }

    [UserMapping(Default = false)]
    private DateTime ResolveDate(DbLoginEvent source)
    {
        return tenantUtil.DateTimeFromUtc(source.Date);
    }
}