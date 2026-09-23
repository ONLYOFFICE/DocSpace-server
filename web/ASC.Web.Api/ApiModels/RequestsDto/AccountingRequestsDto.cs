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
/// The query parameters for reading a page of the portal's wallet movements: the same filters as the operations
/// report, plus the paging window.
/// </summary>
public class CustomerOperationsRequestDto : CustomerOperationsReportRequestDto
{
    /// <summary>
    /// The number of movements to skip before the first one returned, for walking through a long history page by
    /// page. Counted after the filters and the ordering are applied, and starts at 0 when omitted.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "offset")]
    public int? Offset { get; set; }

    /// <summary>
    /// The maximum number of movements returned in one page. Defaults to 25 when omitted; the answer echoes the
    /// window back next to `totalQuantity`, `totalPage` and `currentPage`, so the next `offset` can be computed
    /// without counting the items.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "limit")]
    public int? Limit { get; set; }
}

/// <summary>
/// The filters that select which wallet movements are reported: the services, the period, the participant, the
/// direction and the outcome of the movement, and the ordering.
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
    /// The wallet services whose movements are kept, named the way the billing catalogue names them - `backup`,
    /// `ai-tools`, `ai-search`, `disk-storage`, `docscloud`. Take the values from the `serviceName` field of
    /// `GET api/2.0/portal/payment/walletservices`; the match ignores case, a name this installation does not sell
    /// fails the call with 404, and an omitted list keeps every service. A bare string is accepted in place of an
    /// array for backward compatibility.
    /// </summary>
    /// <example>[backup]</example>
    [JsonConverter(typeof(SingleOrArrayStringJsonConverter))]
    public List<string> ServiceName { get; set; }

    /// <summary>
    /// The beginning of the reported period, inclusive. Read in the portal time zone rather than in UTC, so a
    /// movement at the edge of the period falls where the portal sees it; defaults to the portal creation date.
    /// </summary>
    /// <example>2024-01-01T00:00:00Z</example>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// The end of the reported period, inclusive. Read in the portal time zone rather than in UTC, and defaults to
    /// the moment the call is made.
    /// </summary>
    /// <example>2024-01-31T23:59:59Z</example>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// The participant whose movements are kept - the account the accounting service records as the cause of a
    /// movement. A movement caused by a portal user carries that user ID here, and one caused by the portal itself
    /// carries the customer name; surrounding whitespace is trimmed, and an omitted value keeps every participant.
    /// </summary>
    /// <example>My Own Corporation</example>
    public string ParticipantName { get; set; }

    /// <summary>
    /// Whether movements that add money to the wallet - top-ups, refunds and corrections in the portal's favour -
    /// are kept. Both directions are reported when neither this nor `debit` is given.
    /// </summary>
    /// <example>true</example>
    public bool? Credit { get; set; }

    /// <summary>
    /// Whether movements that take money out of the wallet - the charges of the wallet services - are kept. Both
    /// directions are reported when neither this nor `credit` is given.
    /// </summary>
    /// <example>false</example>
    public bool? Debit { get; set; }

    /// <summary>
    /// The kind of movement to keep, which says what caused the money to move rather than how it ended. Every kind
    /// is reported when it is omitted.
    /// </summary>
    /// <example>ServicePayment</example>
    public ASC.Core.Billing.OperationType? Type { get; init; }

    /// <summary>
    /// The outcome to keep. A movement that is still being settled is reported as pending and may change later,
    /// while the other outcomes are final; every outcome is reported when this is omitted.
    /// </summary>
    /// <example>Completed</example>
    public OperationStatus? Status { get; init; }

    /// <summary>
    /// The name of the field the movements are sorted by, spelled as the accounting service names it, such as
    /// `StartDate` or `ServiceName`. Surrounding whitespace is trimmed, and the accounting service applies its own
    /// ordering when this is omitted.
    /// </summary>
    /// <example>StartDate</example>
    public string OrderBy { get; init; }

    /// <summary>
    /// The direction the field named in `orderBy` is sorted in. Newest or largest first is what the accounting
    /// service does by default, so leaving this out sorts the same way as asking for descending explicitly.
    /// </summary>
    /// <example>Descending</example>
    public OperationOrderType? OrderType  { get; init; }
}

/// <summary>
/// The filters that select which wallet service consumption is reported: the services, the period, the participant,
/// the outcome, the usage metadata and the ordering.
/// </summary>
/// <example>
/// {
///   "serviceName": "backup",
///   "startDate": "2024-01-01T00:00:00Z",
///   "endDate": "2024-01-31T23:59:59Z",
///   "participantName": "My Own Corporation"
/// }
/// </example>
public class CustomerServiceUsageReportRequestDto
{
    /// <summary>
    /// The wallet services whose consumption is reported, named the way the billing catalogue names them -
    /// `backup`, `ai-tools`, `ai-search`, `disk-storage`, `docscloud`. Take the values from the `serviceName` field
    /// of `GET api/2.0/portal/payment/walletservices`; the match ignores case, a name this installation does not
    /// sell fails the call with 404, and an omitted list reports every service. A bare string is accepted in place
    /// of an array for backward compatibility.
    /// </summary>
    /// <example>[backup]</example>
    [JsonConverter(typeof(SingleOrArrayStringJsonConverter))]
    public List<string> ServiceName { get; set; }

    /// <summary>
    /// The beginning of the reported period, inclusive. Read in the portal time zone rather than in UTC, and
    /// defaults to the portal creation date.
    /// </summary>
    /// <example>2024-01-01T00:00:00Z</example>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// The end of the reported period, inclusive. Read in the portal time zone rather than in UTC, and defaults to
    /// the moment the call is made.
    /// </summary>
    /// <example>2024-01-31T23:59:59Z</example>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// The participant whose consumption is reported - the account the accounting service records as the consumer.
    /// Consumption caused by a portal user carries that user ID here; surrounding whitespace is trimmed, and an
    /// omitted value reports every participant.
    /// </summary>
    /// <example>My Own Corporation</example>
    public string ParticipantName { get; set; }

    /// <summary>
    /// The outcome to keep. Consumption that is still being settled is reported as pending and may change later,
    /// while the other outcomes are final; every outcome is reported when this is omitted.
    /// </summary>
    /// <example>Completed</example>
    public OperationStatus? Status { get; init; }

    /// <summary>
    /// The usage annotations a wallet service records alongside its consumption, as the key and value pairs that
    /// must all match for a record to be reported. The keys are chosen by the service that writes them, so read
    /// them off the `metadata` of the records returned by `GET api/2.0/portal/payment/customer/usage` rather than
    /// guessing; an omitted map reports every record.
    /// </summary>
    /// <example>{"key1": "value1", "key2": "value2"}</example>
    public Dictionary<string, string> Metadata { get; set; }

    /// <summary>
    /// The name of the field the per-service totals are sorted by, spelled as the accounting service names it, such
    /// as `ServiceName` or `StartDate`. Surrounding whitespace is trimmed, and the accounting service applies its
    /// own ordering when this is omitted.
    /// </summary>
    /// <example>ServiceName</example>
    public string OrderBy { get; init; }

    /// <summary>
    /// The direction the field named in `orderBy` is sorted in. Newest or largest first is what the accounting
    /// service does by default, so leaving this out sorts the same way as asking for descending explicitly.
    /// </summary>
    /// <example>Descending</example>
    public OperationOrderType? OrderType { get; init; }
}

/// <summary>
/// The period covered by the monthly wallet spending report.
/// </summary>
/// <example>
/// {
///   "startDate": "2025-01-01T00:00:00Z",
///   "endDate": "2025-12-31T23:59:59Z"
/// }
/// </example>
public class CustomerMonthlyUsageReportRequestDto
{
    /// <summary>
    /// The beginning of the reported period, inclusive. The months are cut in the portal time zone rather than in
    /// UTC, so spending at the turn of a month falls where the portal sees it; defaults to the portal creation date.
    /// </summary>
    /// <example>2025-01-01T00:00:00Z</example>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// The end of the reported period, inclusive. Cut in the portal time zone in the same way as `startDate`, and
    /// defaults to the moment the call is made.
    /// </summary>
    /// <example>2025-12-31T23:59:59Z</example>
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// The period covered by the monthly wallet spending totals.
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
    /// The beginning of the reported period, inclusive. The months are cut in the portal time zone rather than in
    /// UTC, so spending at the turn of a month falls where the portal sees it; defaults to the portal creation date.
    /// </summary>
    /// <example>2025-01-01T00:00:00Z</example>
    [FromQuery(Name = "startDate")]
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// The end of the reported period, inclusive. Cut in the portal time zone in the same way as `startDate`, and
    /// defaults to the moment the call is made.
    /// </summary>
    /// <example>2025-12-31T23:59:59Z</example>
    [FromQuery(Name = "endDate")]
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// The query parameters that select which wallet service consumption is added up and how the totals are paged.
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
    /// The wallet services whose consumption is added up, named the way the billing catalogue names them -
    /// `backup`, `ai-tools`, `ai-search`, `disk-storage`, `docscloud`. Take the values from the `serviceName` field
    /// of `GET api/2.0/portal/payment/walletservices`; the match ignores case, a name this installation does not
    /// sell fails the call with 404, and an omitted list covers every service.
    /// </summary>
    /// <example>[backup]</example>
    public List<string> ServiceName { get; set; }

    /// <summary>
    /// The participant whose consumption is added up - the account the accounting service records as the consumer.
    /// Consumption caused by a portal user carries that user ID here; surrounding whitespace is trimmed, and an
    /// omitted value covers every participant.
    /// </summary>
    /// <example>My Own Corporation</example>
    public string ParticipantName { get; set; }

    /// <summary>
    /// The outcome to keep. Consumption that is still being settled is reported as pending and may change later,
    /// while the other outcomes are final; every outcome is counted when this is omitted.
    /// </summary>
    /// <example>Completed</example>
    public OperationStatus? Status { get; init; }

    /// <summary>
    /// The beginning of the reported period, inclusive. Read in the portal time zone rather than in UTC, and
    /// defaults to the portal creation date.
    /// </summary>
    /// <example>2025-01-01T00:00:00Z</example>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// The end of the reported period, inclusive. Read in the portal time zone rather than in UTC, and defaults to
    /// the moment the call is made.
    /// </summary>
    /// <example>2025-12-31T23:59:59Z</example>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// The usage annotations a wallet service records alongside its consumption, as the key and value pairs that
    /// must all match for a record to be counted. The keys are chosen by the service that writes them, so read them
    /// off the `metadata` of the records already returned rather than guessing; an omitted map counts every record.
    /// </summary>
    /// <example>{"key1": "value1", "key2": "value2"}</example>
    public Dictionary<string, string> Metadata { get; set; }

    /// <summary>
    /// The number of per-service totals to skip before the first one returned. Counted after the filters and the
    /// ordering are applied, and starts at 0 when omitted.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "offset")]
    public int? Offset { get; set; }

    /// <summary>
    /// The maximum number of per-service totals returned in one page. Defaults to 25 when omitted; the answer echoes
    /// the window back with its paging information, so the next `offset` can be computed without counting the items.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "limit")]
    public int? Limit { get; set; }

    /// <summary>
    /// The name of the field the per-service totals are sorted by, spelled as the accounting service names it, such
    /// as `ServiceName` or `StartDate`. Surrounding whitespace is trimmed, and the accounting service applies its
    /// own ordering when this is omitted.
    /// </summary>
    /// <example>ServiceName</example>
    public string OrderBy { get; init; }

    /// <summary>
    /// The direction the field named in `orderBy` is sorted in. Newest or largest first is what the accounting
    /// service does by default, so leaving this out sorts the same way as asking for descending explicitly.
    /// </summary>
    /// <example>Descending</example>
    public OperationOrderType? OrderType { get; init; }
}

/// <summary>
/// The parameters that select whose prices are read from the accounting service.
/// </summary>
public class ServicePricesRequestDto
{
    /// <summary>
    /// The service whose price list is read, named the way the billing catalogue names it, such as `ai-tools` or
    /// `backup`. Take the value from the `serviceName` field of `GET api/2.0/portal/payment/walletservices`; a name
    /// the accounting service does not price yields an empty list rather than an error.
    /// </summary>
    /// <example>ai-tools</example>
    [StringLength(255)]
    [FromRoute(Name = "serviceName")]
    public string ServiceName { get; init; }

    /// <summary>
    /// Whether the answer is narrowed to the prices in force at the moment of the call. Leaving it false also
    /// returns the retired and the not yet started ones, which is what pricing a movement recorded in the past
    /// needs.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "active")]
    public bool Active { get; init; }
}


/// <summary>
/// Deserializes a value that historically was a single JSON string but is now a list:
/// accepts both <c>"backup"</c> and <c>["backup"]</c>, keeping the request contract
/// backward compatible for existing clients. Always serializes as an array.
/// </summary>
public class SingleOrArrayStringJsonConverter : JsonConverter<List<string>>
{
    public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return [reader.GetString()];
        }

        return JsonSerializer.Deserialize<List<string>>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
