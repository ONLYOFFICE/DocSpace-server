﻿// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Files.Core.RoomTemplates.Events;

[ProtoContract]
public record CreateRoomTemplateIntegrationEvent : IntegrationEvent
{
    [ProtoMember(6)]
    public int RoomId { get; set; }

    [ProtoMember(7)]
    public string Title { get; set; }

    [ProtoMember(8)]
    public LogoSettings Logo { get; set; }

    [ProtoMember(9)]
    public IEnumerable<string> Tags { get; set; }

    [ProtoMember(10)]
    public IEnumerable<string> Emails { get; set; }

    [ProtoMember(11)]
    public IEnumerable<Guid> Groups { get; set; } 
    [ProtoMember(12)]
    public string TaskId { get; set; }

    [ProtoMember(13)]
    public bool CopyLogo { get; set; }

    [ProtoMember(14)]
    public string Cover { get; set; }

    [ProtoMember(15)]
    public string Color { get; set; }

    [ProtoMember(16)]
    public long? Quota { get; set; }

    public CreateRoomTemplateIntegrationEvent(Guid createBy, int tenantId) : base(createBy, tenantId)
    {
    }

    protected CreateRoomTemplateIntegrationEvent()
    {
    }
}

[ProtoContract]
public record LogoSettings
{
    [ProtoMember(1)]
    public string TmpFile { get; set; }

    [ProtoMember(2)]
    public int X { get; set; }

    [ProtoMember(3)]
    public int Y { get; set; }

    [ProtoMember(4)]
    public uint Width { get; set; }

    [ProtoMember(5)]
    public uint Height { get; set; }
}