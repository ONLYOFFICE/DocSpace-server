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

namespace ASC.Files.Core.IntegrationEvents.Events;

[ProtoContract]
public record CustomerOperationsReportIntegrationEvent : IntegrationEvent
{
    private CustomerOperationsReportIntegrationEvent() : base()
    {
    }

    public CustomerOperationsReportIntegrationEvent(
        Guid createBy,
        int tenantId,
        string baseUri,
        string serviceName,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string participantName = null,
        bool? credit = null,
        bool? debit = null,
        OperationType? type = null,
        OperationStatus? status = null,
        string orderBy = null,
        OperationOrderType? orderType = null,
        IDictionary<string, string> headers = null,
        bool terminate = false)
    : base(createBy, tenantId)
    {
        BaseUri = baseUri;
        ServiceName = serviceName;
        StartDate = startDate;
        EndDate = endDate;
        ParticipantName = participantName;
        Credit = credit;
        Debit = debit;
        Headers = headers;
        Terminate = terminate;
        Type = type;
        Status = status;
        OrderBy = orderBy;
        OrderType = orderType;
    }

    [ProtoMember(1)]
    public string BaseUri { get; set; }

    [ProtoMember(2)]
    public DateTime? StartDate { get; set; }

    [ProtoMember(3)]
    public DateTime? EndDate { get; set; }

    [ProtoMember(4)]
    public string ParticipantName { get; set; }

    [ProtoMember(5)]
    public bool? Credit { get; set; }

    [ProtoMember(6)]
    public bool? Debit { get; set; }

    [ProtoMember(7)]
    public IDictionary<string, string> Headers { get; set; }

    [ProtoMember(8)]
    public bool Terminate { get; set; }

    [ProtoMember(9)]
    public string ServiceName { get; set; }

    [ProtoMember(10)]
    public OperationType? Type { get; set; }

    [ProtoMember(11)]
    public OperationStatus? Status { get; set; }

    [ProtoMember(12)]
    public string OrderBy { get; set; }

    [ProtoMember(13)]
    public OperationOrderType? OrderType  { get; set; }
}
