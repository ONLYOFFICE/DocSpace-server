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

namespace ASC.Core;

[ProtoContract]
public class SubscriptionMethod
{
    [ProtoMember(1)]
    public int Tenant { get; set; }

    [ProtoMember(2)]
    public string Source { get; set; }

    [ProtoMember(3)]
    public string Action { get; set; }

    [ProtoMember(4)]
    public string Recipient { get; set; }

    [ProtoMember(5)]
    public string[] Methods { get; set; }
    
    public static implicit operator SubscriptionMethod(SubscriptionMethodCache cache)
    {
        return new SubscriptionMethod
        {
            Tenant = cache.Tenant,
            Source = cache.SourceId,
            Action = cache.ActionId,
            Recipient = cache.RecipientId
        };
    }

    public static implicit operator SubscriptionMethodCache(SubscriptionMethod cache)
    {
        return new SubscriptionMethodCache
        {
            Tenant = cache.Tenant,
            SourceId = cache.Source,
            ActionId = cache.Action,
            RecipientId = cache.Recipient
        };
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class SubscriptionMethodMapper
{
    private static readonly char[] _separator = ['|'];

    [MapProperty(nameof(DbSubscriptionMethod.TenantId), nameof(SubscriptionMethod.Tenant))]
    [MapProperty(nameof(DbSubscriptionMethod.Sender), nameof(SubscriptionMethod.Methods), Use = nameof(MapSenderToMethods))]
    public static partial SubscriptionMethod Map(this DbSubscriptionMethod source);

    public static string[] MapSenderToMethods(string sender)
    {
        return sender.Split(_separator, StringSplitOptions.RemoveEmptyEntries);
    }
}