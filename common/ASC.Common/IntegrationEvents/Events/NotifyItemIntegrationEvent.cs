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

using ASC.EventBus.Events;

namespace ASC.Common.IntegrationEvents.Events;

[ProtoContract]
public record NotifyItemIntegrationEvent : IntegrationEvent
{
    [ProtoMember(1)]
    public NotifyActionItem Action { get; init; }

    [ProtoMember(2)]
    public string ObjectId { get; set; }

    [ProtoMember(3)]
    public List<Recipient> Recipients { get; set; }

    [ProtoMember(4)]
    public List<string> SenderNames { get; set; }

    [ProtoMember(5)]
    public List<Tag> Tags { get; set; }

    [ProtoMember(6)]
    public bool CheckSubsciption { get; init; }

    [ProtoMember(7)]
    public string BaseUrl { get; init; }

    private NotifyItemIntegrationEvent()
    {

    }

    public NotifyItemIntegrationEvent(Guid createBy, int tenantId) : base(createBy, tenantId)
    {
        ObjectId = "";
        Recipients = [];
        SenderNames = [];
        Tags = [];
        BaseUrl = "";
    }
}

[ProtoContract]
public record NotifyActionItem
{
    [ProtoMember(1)]
    public string Id { get; init; }
    
    [ProtoMember(2)]
    public string NotifyActionType { get; init; }
}

[ProtoContract]
public record Recipient
{
    [ProtoMember(1)]
    public string Id { get; init; }

    [ProtoMember(2)]
    public string Name { get; init; }

    [ProtoMember(3)]
    public bool CheckActivation { get; set; }

    [ProtoMember(4)]
    public List<string> Addresses { get; init; }

    [ProtoMember(5)]
    public bool IsGroup { get; set; }
}

[ProtoContract]
public record Tag
{
    [ProtoMember(1)]
    public string Key { get; init; }

    [ProtoMember(2)]
    public string Value { get; init; }
}