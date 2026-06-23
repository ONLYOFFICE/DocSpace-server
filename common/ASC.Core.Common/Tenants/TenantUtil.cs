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

namespace ASC.Core.Tenants;

[Scope]
public class TenantUtil(TenantManager tenantManager)
{
    private string _timeZoneName;

    public TimeZoneInfo TimeZoneInfo
    {
        get
        {
            var tenantTimeZone = tenantManager.GetCurrentTenant().TimeZone;
            if (field == null || _timeZoneName != tenantTimeZone)
            {
                _timeZoneName = tenantTimeZone;
                field = TimeZoneConverter.GetTimeZone(tenantTimeZone);
            }
            return field;
        }
    }

    public DateTime DateTimeFromUtc(DateTime utc)
    {
        return DateTimeFromUtc(TimeZoneInfo, utc);
    }

    public DateTime DateTimeFromUtc(string timeZone, DateTime utc)
    {
        return DateTimeFromUtc(TimeZoneConverter.GetTimeZone(timeZone), utc);
    }

    private static DateTime DateTimeFromUtc(TimeZoneInfo timeZone, DateTime utc)
    {
        if (utc.Kind != DateTimeKind.Utc)
        {
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        }

        if (utc == DateTime.MinValue || utc == DateTime.MaxValue)
        {
            return utc;
        }

        return DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(utc, TimeZoneInfo.Utc, timeZone), DateTimeKind.Local);
    }


    public DateTime DateTimeToUtc(DateTime local)
    {
        return DateTimeToUtc(TimeZoneInfo, local);
    }

    private static DateTime DateTimeToUtc(TimeZoneInfo timeZone, DateTime local)
    {
        if (local.Kind == DateTimeKind.Utc || local == DateTime.MinValue || local == DateTime.MaxValue)
        {
            return local;
        }

        if (timeZone.IsInvalidTime(DateTime.SpecifyKind(local, DateTimeKind.Unspecified)))
        {
            // hack
            local = local.AddHours(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), timeZone);

    }

    public DateTime DateTimeNow()
    {
        return DateTimeNow(TimeZoneInfo);
    }

    private static DateTime DateTimeNow(TimeZoneInfo timeZone)
    {
        return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone), DateTimeKind.Local);
    }

    public DateTime DateTimeNow(string timeZone)
    {
        return DateTimeNow(TimeZoneConverter.GetTimeZone(timeZone));
    }
}