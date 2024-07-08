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
            Target = message.Target.GetItems(),
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
