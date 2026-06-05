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

namespace ASC.Web.Api.ApiModels.RequestsDto;

/// <summary>
/// The request parameters for receiving a report on client operations.
/// </summary>
public class CustomerOperationsRequestDto : CustomerOperationsReportRequestDto
{
    /// <summary>
    /// The number of items to skip for pagination. The default value is 0.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "offset")]
    public int? Offset { get; set; }

    /// <summary>
    /// The maximum number of items to return for pagination. The default value is 25.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "limit")]
    public int? Limit { get; set; }
}

/// <summary>
/// The request parameters for generating a report on client operations.
/// </summary>
/// <example>
/// {
///   "startDate": "2024-01-01T00:00:00Z",
///   "endDate": "2024-01-31T23:59:59Z",
///   "participantName": "My Own Corporation",
///   "credit": true,
///   "debit": false
/// }
/// </example>
public class CustomerOperationsReportRequestDto
{
    /// <summary>
    /// The service name.
    /// </summary>
    /// <example>backup</example>
    public string ServiceName { get; set; }

    /// <summary>
    /// The report start date.
    /// </summary>
    /// <example>2024-01-01T00:00:00Z</example>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// The report end date.
    /// </summary>
    /// <example>2024-01-31T23:59:59Z</example>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// The participant name.
    /// </summary>
    /// <example>My Own Corporation</example>
    public string ParticipantName { get; set; }

    /// <summary>
    /// Specifies whether to include credit operations in the report.
    /// </summary>
    /// <example>true</example>
    public bool? Credit { get; set; }

    /// <summary>
    /// Specifies whether to include debit operations in the report.
    /// </summary>
    /// <example>false</example>
    public bool? Debit { get; set; }

    /// <summary>
    /// The operation type to filter by.
    /// </summary>
    /// <example>Any</example>
    public ASC.Core.Billing.OperationType? Type { get; init; }

    /// <summary>
    /// The operation status to filter by.
    /// </summary>
    /// <example>Any</example>
    public OperationStatus? Status { get; init; }

    /// <summary>
    /// The field to order by.
    /// </summary>
    /// <example>StartDate</example>
    public string OrderBy { get; init; }

    /// <summary>
    /// Order direction: Ascending or Descending.
    /// </summary>
    /// <example>Descending</example>
    public OperationOrderType? OrderType  { get; init; }
}

/// <summary>
/// The request parameters for receiving customer monthly usage statistics.
/// </summary>
/// <example>
/// {
///   "startDate": "2025-01-01T00:00:00Z",
///   "endDate": "2025-12-31T23:59:59Z"
/// }
/// </example>
public class CustomerMonthlyUsageRequestDto
{
    /// <summary>
    /// Start of the period (inclusive).
    /// </summary>
    /// <example>2025-01-01T00:00:00Z</example>
    [FromQuery(Name = "startDate")]
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End of the period (inclusive).
    /// </summary>
    /// <example>2025-12-31T23:59:59Z</example>
    [FromQuery(Name = "endDate")]
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// The request parameters for receiving customer service usage statistics.
/// </summary>
/// <example>
/// {
///   "serviceName": "backup",
///   "startDate": "2025-01-01T00:00:00Z",
///   "endDate": "2025-12-31T23:59:59Z",
///   "offset": 0,
///   "limit": 25
/// }
/// </example>
public class CustomerServiceUsageRequestDto
{
    /// <summary>
    /// The service name.
    /// </summary>
    /// <example>backup</example>
    public string ServiceName { get; set; }

    /// <summary>
    /// The participant name.
    /// </summary>
    /// <example>My Own Corporation</example>
    public string ParticipantName { get; set; }

    /// <summary>
    /// The operation status to filter by.
    /// </summary>
    /// <example>Completed</example>
    public OperationStatus? Status { get; init; }

    /// <summary>
    /// Start of the period (inclusive).
    /// </summary>
    /// <example>2025-01-01T00:00:00Z</example>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End of the period (inclusive).
    /// </summary>
    /// <example>2025-12-31T23:59:59Z</example>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Metadata key-value pairs to filter by.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; }

    /// <summary>
    /// The number of items to skip for pagination. The default value is 0.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "offset")]
    public int? Offset { get; set; }

    /// <summary>
    /// The maximum number of items to return for pagination. The default value is 25.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "limit")]
    public int? Limit { get; set; }

    /// <summary>
    /// The field to order by.
    /// </summary>
    /// <example>ServiceName</example>
    public string OrderBy { get; init; }

    /// <summary>
    /// Order direction: Ascending or Descending.
    /// </summary>
    /// <example>Descending</example>
    public OperationOrderType? OrderType { get; init; }
}
