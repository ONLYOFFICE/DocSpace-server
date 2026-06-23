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

namespace ASC.Notify.Messages;

[ProtoContract]
public class NotifyMessage
{
    [ProtoMember(1)]
    public string Sender { get; set; }

    [ProtoMember(2)]
    public string SenderType { get; set; }

    [ProtoMember(3)]
    public string Reciever { get; set; }

    [ProtoMember(4)]
    public string ReplyTo { get; set; }

    [ProtoMember(5)]
    public string Subject { get; set; }

    [ProtoMember(6)]
    public string ContentType { get; set; }

    [ProtoMember(7)]
    public string Content { get; set; }

    [ProtoMember(8)]
    public DateTime CreationDate { get; set; }

    [ProtoMember(9)]
    public int Priority { get; set; }

    [ProtoMember(10)]
    public NotifyMessageAttachment[] Attachments { get; set; }

    [ProtoMember(11)]
    public string AutoSubmitted { get; set; }

    [ProtoMember(12)]
    public int TenantId { get; set; }

    [ProtoMember(13)]
    public string ProductID { get; set; }

    [ProtoMember(14)]
    public string Data { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class NotifyMessageMapper
{
    [MapperIgnoreSource(nameof(NotifyQueue.Attachments))]
    [MapperIgnoreTarget(nameof(NotifyQueue.Attachments))]
    public static partial NotifyMessage Map(this NotifyQueue source);

    [MapperIgnoreSource(nameof(NotifyQueue.Attachments))]
    [MapperIgnoreTarget(nameof(NotifyQueue.Attachments))]
    public static partial NotifyQueue Map(this NotifyMessage source);
}