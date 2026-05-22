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

using ProtoBuf;

namespace ASC.MessagingSystem;

[ProtoContract]
public record EventDataIntegrationEvent : IntegrationEvent
{
    private EventDataIntegrationEvent()
    {

    }

    public EventDataIntegrationEvent(Guid createBy, int tenantId)
    : base(createBy, tenantId)
    {

    }

    [ProtoMember(1)]
    public EventMessageProto RequestMessage { get; init; }
}

[ProtoContract]
public class EventMessageProto
{
    [ProtoMember(1)]
    public int Id { get; set; }

    [ProtoMember(2)]
    public string Ip { get; set; }

    [ProtoMember(3)]
    public string Initiator { get; init; }

    [ProtoMember(4)]
    public string Browser { get; set; }

    [ProtoMember(5)]
    public string Platform { get; set; }

    [ProtoMember(6)]
    public DateTime Date { get; set; }

    [ProtoMember(7)]
    public int TenantId { get; init; }

    [ProtoMember(8)]
    public Guid UserId { get; init; }

    [ProtoMember(9)]
    public string Page { get; set; }

    [ProtoMember(10)]
    public MessageAction Action { get; init; }

    [ProtoMember(11)]
    public IList<string> Description { get; init; }

    [ProtoMember(12)]
    public IEnumerable<string> Target { get; init; }

    [ProtoMember(13)]
    public string UaHeader { get; set; }

    [ProtoMember(14)]
    public bool Active { get; init; }

    [ProtoMember(15)]
    public IEnumerable<FilesAuditReference> References { get; init; }

    public static implicit operator EventMessageProto(EventMessage message)
    {
        return new EventMessageProto
        {
            Id = message.Id,
            Ip = message.Ip,
            Initiator = message.Initiator,
            Browser = message.Browser,
            Platform = message.Platform,
            Date = message.Date,
            TenantId = message.TenantId,
            UserId = message.UserId,
            Page = message.Page,
            Action = message.Action,
            Description = message.Description,
            Target = message.Target?.GetItems() ?? [],
            UaHeader = message.UaHeader,
            Active = message.Active,
            References = message.References
        };
    }

    public static implicit operator EventMessage(EventMessageProto messageProto)
    {
        return new EventMessage
        {
            Id = messageProto.Id,
            Ip = messageProto.Ip,
            Initiator = messageProto.Initiator,
            Browser = messageProto.Browser,
            Platform = messageProto.Platform,
            Date = messageProto.Date,
            TenantId = messageProto.TenantId,
            UserId = messageProto.UserId,
            Page = messageProto.Page,
            Action = messageProto.Action,
            Description = messageProto.Description,
            Target = MessageTarget.Create(messageProto.Target),
            UaHeader = messageProto.UaHeader,
            Active = messageProto.Active,
            References = messageProto.References
        };
    }
}