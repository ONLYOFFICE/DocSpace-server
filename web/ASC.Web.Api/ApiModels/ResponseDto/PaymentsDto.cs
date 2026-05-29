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

using OperationType = ASC.Core.Billing.OperationType;

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// Represents a report containing a collection of operations.
/// </summary>
/// <example>
/// {
///   "collection": [{"id": "op1", "type": "payment"}],
///   "offset": 1,
///   "limit": 1,
///   "totalQuantity": 1,
///   "totalPage": 1,
///   "currentPage": 1
/// }
/// </example>
public class ReportDto
{
    /// <summary>
    /// A collection of operations.
    /// </summary>
    /// <example>[{"id": "op1", "type": "payment"}]</example>
    public List<OperationDto> Collection { get; set; }
    /// <summary>
    /// The report data offset.
    /// </summary>
    /// <example>1</example>
    public int Offset { get; set; }
    /// <summary>
    /// The report data limit.
    /// </summary>
    /// <example>1</example>
    public int Limit { get; set; }
    /// <summary>
    /// The total quantity of operations in the report.
    /// </summary>
    /// <example>1</example>
    public int TotalQuantity { get; set; }
    /// <summary>
    /// The total number of pages in the report.
    /// </summary>
    /// <example>1</example>
    public int TotalPage { get; set; }
    /// <summary>
    /// The current page number of the report.
    /// </summary>
    /// <example>1</example>
    public int CurrentPage { get; set; }

    public ReportDto(Report report, ApiDateTimeHelper apiDateTimeHelper, Dictionary<string, string> participantDisplayNames, string filterServiceName)
    {
        Offset = report.Offset;
        Limit = report.Limit;
        TotalQuantity = report.TotalQuantity;
        TotalPage = report.TotalPage;
        CurrentPage = report.CurrentPage;

        Collection = [];

        if (report.Collection != null)
        {
            foreach (var operation in report.Collection)
            {
                Collection.Add(new OperationDto(operation, apiDateTimeHelper, participantDisplayNames, filterServiceName));
            }
        }
    }
}

/// <summary>
/// Represents an operation.
/// </summary>
public class OperationDto
{
    /// <summary>
    /// The date when the operation took place.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime Date { get; set; }
    /// <summary>
    /// The service related to the operation.
    /// </summary>
    /// <example>Storage</example>
    public string Service { get; set; }
    /// <summary>
    /// The brief operation description.
    /// </summary>
    /// <example>Storage quota increase</example>
    public string Description { get; set; }
    /// <summary>
    /// The detailed information about the operation.
    /// </summary>
    /// <example>Increased storage from 50GB to 100GB</example>
    public string Details { get; set; }
    /// <summary>
    /// The service unit.
    /// </summary>
    /// <example>GB</example>
    public string ServiceUnit { get; set; }
    /// <summary>
    /// The quantity of the service used.
    /// </summary>
    /// <example>1</example>
    public int Quantity { get; set; }
    /// <summary>
    /// The three-character ISO 4217 currency symbol of the operation.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; set; }
    /// <summary>
    /// The credit amount of the operation.
    /// </summary>
    /// <example>99.99</example>
    public decimal Credit { get; set; }
    /// <summary>
    /// The debit amount of the operation.
    /// </summary>
    /// <example>99.99</example>
    public decimal Debit { get; set; }
    /// <summary>
    /// The participant original name.
    /// </summary>
    /// <example>Example Name</example>
    public string ParticipantName { get; set; }
    /// <summary>
    /// The participant display name.
    /// </summary>
    /// <example>Example Name</example>
    public string ParticipantDisplayName { get; set; }
    /// <summary>
    /// AI Agent id.
    /// </summary>
    /// <example>123</example>
    public string AgentId { get; set; }
    /// <summary>
    /// AI Agent name.
    /// </summary>
    /// <example>My AI Agent</example>
    public string AgentTitle { get; set; }
    /// <summary>
    /// Type of the operation
    /// </summary>
    /// <example>Unknown</example>
    public OperationType Type { get; set; }

    public OperationDto(Operation operation, ApiDateTimeHelper apiDateTimeHelper, Dictionary<string, string> participantDisplayNames, string filterServiceName)
    {
        var (description, unitOfMeasurement, quantity) = WalletServiceDescriptionManager.GetServiceDescriptionAndUom(operation, filterServiceName, operation.Metadata);
        var (agentId, agentTitle) = WalletServiceDescriptionManager.GetAgentInfo(operation.Metadata);

        Date = apiDateTimeHelper.Get(operation.Date);
        Service = operation.Service;
        Description = description;
        Details = WalletServiceDescriptionManager.GetServiceDetails(operation.Metadata);
        ServiceUnit = unitOfMeasurement;
        Quantity = quantity;
        Currency = operation.Currency;
        Credit = operation.Credit;
        Debit = operation.Debit;
        ParticipantName = operation.ParticipantName;
        ParticipantDisplayName = operation.ParticipantName != null && participantDisplayNames.TryGetValue(operation.ParticipantName, out var value)
            ? value
            : operation.ParticipantName;
        AgentId = agentId;
        AgentTitle = agentTitle;
        Type = operation.Type;
    }
}

/// <summary>
/// The customer information.
/// </summary>
public class CustomerInfoDto(CustomerInfo customerInfo, EmployeeDto employeeDto)
{
    /// <summary>
    /// The portal ID.
    /// </summary>
    /// <example>portal-001</example>
    public string PortalId { get; private set; } = customerInfo.PortalId;

    /// <summary>
    /// The customer's payment method.
    /// </summary>
    /// <example>0</example>
    public PaymentMethodStatus PaymentMethodStatus { get; private set; } = customerInfo.PaymentMethodStatus;

    /// <summary>
    /// The customer email address.
    /// </summary>
    /// <example>user@example.com</example>
    public string Email { get; private set; } = customerInfo.Email?.ToLowerInvariant();

    /// <summary>
    /// The paying user.
    /// </summary>
    /// <example>{"displayName": "John Doe", "email": "john.doe@example.com"}</example>
    public EmployeeDto Payer { get; private set; } = employeeDto;
}
