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
/// One page of the portal wallet's money movements, with the paging figures needed to walk the rest.
/// </summary>
/// <example>
/// {
///   "collection": [{"service": "disk-storage", "debit": 14.0}],
///   "offset": 0,
///   "limit": 25,
///   "totalQuantity": 137,
///   "totalPage": 6,
///   "currentPage": 1
/// }
/// </example>
public class ReportDto
{
    /// <summary>
    /// The movements on this page - top-ups, charges, refunds and corrections alike, newest first. It is empty
    /// for a page past the end of the report as well as for a period in which nothing happened.
    /// </summary>
    /// <example>[{"service": "disk-storage", "debit": 14.0}]</example>
    public List<OperationDto> Collection { get; set; }
    /// <summary>
    /// How many movements were skipped before this page, echoed from the request so a client need not remember
    /// what it asked for.
    /// </summary>
    /// <example>0</example>
    public int Offset { get; set; }
    /// <summary>
    /// How many movements one page may hold, echoed from the request; it is 25 unless another value was asked
    /// for. A full page is not proof that more exist - compare `currentPage` with `totalPage`.
    /// </summary>
    /// <example>25</example>
    public int Limit { get; set; }
    /// <summary>
    /// How many movements match the filters in total, across every page.
    /// </summary>
    /// <example>137</example>
    public long TotalQuantity { get; set; }
    /// <summary>
    /// How many pages those movements come to at the current `limit`.
    /// </summary>
    /// <example>6</example>
    public int TotalPage { get; set; }
    /// <summary>
    /// Which of those pages this one is, as the billing service numbers them. Page through by advancing `offset`
    /// rather than this value, which nothing accepts as an argument.
    /// </summary>
    /// <example>1</example>
    public int CurrentPage { get; set; }

    public ReportDto(Report report, ApiDateTimeHelper apiDateTimeHelper, Dictionary<string, string> participantDisplayNames)
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
                Collection.Add(new OperationDto(operation, apiDateTimeHelper, participantDisplayNames));
            }
        }
    }
}

/// <summary>
/// One movement on the portal wallet: what it was for, who caused it, and how much money it moved.
/// </summary>
public class OperationDto
{
    /// <summary>
    /// When the movement was booked, in the portal time zone - the same zone the `startDate` and `endDate`
    /// filters are read in, so the two do line up here.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime Date { get; set; }
    /// <summary>
    /// The wallet service the movement belongs to, by its stable key. It is what the `serviceName` filter
    /// matches on, and it is empty for a movement that belongs to no service, such as a top-up.
    /// </summary>
    /// <example>disk-storage</example>
    public string Service { get; set; }
    /// <summary>
    /// A one-line summary of the movement in the portal language, already composed from the service and the
    /// quantity - meant to be printed as it is rather than parsed.
    /// </summary>
    /// <example>Storage quota increase</example>
    public string Description { get; set; }
    /// <summary>
    /// The longer explanation of the same movement, where the service recorded one. It is empty for a movement
    /// that has nothing to add to `description`.
    /// </summary>
    /// <example>Increased storage from 50GB to 100GB</example>
    public string Details { get; set; }
    /// <summary>
    /// What `quantity` counts for this service, in the portal language. AI consumption is reported in tokens
    /// here rather than in the AI credits the service is sold in.
    /// </summary>
    /// <example>GB</example>
    public string ServiceUnit { get; set; }
    /// <summary>
    /// How many units the movement covers, in the unit named by `serviceUnit`. It is `0` for a movement that
    /// moves money without consuming a service.
    /// </summary>
    /// <example>1</example>
    public int Quantity { get; set; }
    /// <summary>
    /// The currency `credit` and `debit` are expressed in, as a three-letter ISO 4217 code. It is the accounting
    /// currency of the wallet, which need not be the currency the subscription is priced in.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; set; }
    /// <summary>
    /// The amount that went into the wallet. It is `0` on a movement that only took money out, so the pair of
    /// `credit` and `debit` is what shows which way the money went; the `credit` and `debit` filters of the
    /// operation select the two directions by exactly this.
    /// </summary>
    /// <example>99.99</example>
    public decimal Credit { get; set; }
    /// <summary>
    /// The amount that was taken out of the wallet, `0` on a movement that put money in.
    /// </summary>
    /// <example>99.99</example>
    public decimal Debit { get; set; }
    /// <summary>
    /// Who caused the movement, as the billing service records them - an internal name, which is what the
    /// `participantName` filter matches on. Show `participantDisplayName` instead.
    /// </summary>
    /// <example>john.doe@example.com</example>
    public string ParticipantName { get; set; }
    /// <summary>
    /// The same person as their portal display name. It falls back to `participantName` when the name belongs to
    /// no portal account, so it is never empty while `participantName` is filled.
    /// </summary>
    /// <example>John Doe</example>
    public string ParticipantDisplayName { get; set; }
    /// <summary>
    /// What kind of thing an AI operation was run on - an agent, a file, a folder, a room or a form. It is empty
    /// on any movement that is not an AI charge.
    /// </summary>
    /// <example>Agent</example>
    public string SourceType { get; set; }
    /// <summary>
    /// The title that thing had when the operation ran, kept as recorded, so it does not follow a later rename.
    /// Empty under the same conditions as `sourceType`.
    /// </summary>
    /// <example>My AI Agent</example>
    public string SourceTitle { get; set; }
    /// <summary>
    /// The identifier of that thing, to look it up in the module it belongs to. Empty under the same conditions
    /// as `sourceType`.
    /// </summary>
    /// <example>123</example>
    public string SourceId { get; set; }
    /// <summary>
    /// What kind of movement this is - a payment, a charge, a refund, a correction. It is what the `type` filter
    /// matches on, and `Unknown` covers a movement the billing service reported under a kind this build does not
    /// recognise.
    /// </summary>
    /// <example>Unknown</example>
    public OperationType Type { get; set; }

    public OperationDto(Operation operation, ApiDateTimeHelper apiDateTimeHelper, Dictionary<string, string> participantDisplayNames)
    {
        var (description, unitOfMeasurement, quantity) = WalletServiceDescriptionManager.GetServiceDescriptionAndUom(operation, operation.Metadata);
        var (sourceId, sourceType, sourceTitle) = WalletServiceDescriptionManager.GetSourceInfo(operation.Metadata);

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
        SourceType = sourceType;
        SourceTitle = sourceTitle;
        SourceId = sourceId;
        Type = operation.Type;
    }
}

/// <summary>
/// What the portal spent from its wallet in one calendar month, added up across every service.
/// </summary>
/// <example>
/// {
///   "year": 2025,
///   "month": 1,
///   "currency": "USD",
///   "totalAmount": 199.98,
///   "operationCount": 3
/// }
/// </example>
public class CustomerMonthlyUsageDto
{
    /// <summary>
    /// The year the month belongs to. Months are cut in the portal time zone, so a movement at the edge of a
    /// month falls where the portal sees it and not where UTC does.
    /// </summary>
    /// <example>2025</example>
    public int Year { get; set; }

    /// <summary>
    /// The month itself, January being 1. Only months that had spending appear at all, so a gap in the list is a
    /// month with nothing in it rather than missing data.
    /// </summary>
    /// <example>1</example>
    public int Month { get; set; }

    /// <summary>
    /// The currency `totalAmount` is expressed in, as a three-letter ISO 4217 code - the accounting currency of
    /// the wallet.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; set; }

    /// <summary>
    /// What the month came to across every service, as a positive amount spent rather than a signed balance.
    /// </summary>
    /// <example>199.98</example>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// How many separate movements that total was added up from, for a client that wants to show the weight
    /// behind a figure. The movements themselves are in `GET api/2.0/portal/payment/customer/operations`.
    /// </summary>
    /// <example>3</example>
    public int OperationCount { get; set; }

    public CustomerMonthlyUsageDto(CustomerMonthlyUsage usage)
    {
        Year = usage.Year;
        Month = usage.Month;
        Currency = usage.Currency;
        TotalAmount = usage.TotalAmount;
        OperationCount = usage.OperationCount;
    }
}

/// <summary>
/// What one wallet service was consumed and cost over the requested period, added up rather than listed.
/// </summary>
/// <example>
/// {
///   "service": "disk-storage",
///   "title": "Additional disk storage",
///   "serviceUnit": "GB",
///   "currency": "USD",
///   "totalQuantity": 100,
///   "totalAmount": 49.99,
///   "operationCount": 2,
///   "price": 0.14,
///   "subscription": true
/// }
/// </example>
public class CustomerServiceUsageDto
{
    /// <summary>
    /// The stable key of the service, which is what the `serviceName` filter of this operation matches on and
    /// what `GET api/2.0/portal/payment/walletservice` looks a service up by.
    /// </summary>
    /// <example>disk-storage</example>
    public string Service { get; set; }

    /// <summary>
    /// The service name in the portal language, for printing rather than matching.
    /// </summary>
    /// <example>Additional disk storage</example>
    public string Title { get; set; }

    /// <summary>
    /// What `totalQuantity` counts, in the portal language. AI consumption is reported in tokens here rather
    /// than in the AI credits the service is sold in, so it does not line up with the price list.
    /// </summary>
    /// <example>GB</example>
    public string ServiceUnit { get; set; }

    /// <summary>
    /// The currency `totalAmount` and `price` are expressed in, as a three-letter ISO 4217 code.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; set; }

    /// <summary>
    /// How many units of the service were consumed over the period, in the unit named by `serviceUnit`.
    /// </summary>
    /// <example>100</example>
    public int TotalQuantity { get; set; }

    /// <summary>
    /// What that consumption cost over the period. It is what was actually charged, so it can differ from
    /// `price` times `totalQuantity` when the price changed inside the period.
    /// </summary>
    /// <example>49.99</example>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// How many separate charges the total was added up from. The charges themselves are in
    /// `GET api/2.0/portal/payment/customer/operations`.
    /// </summary>
    /// <example>2</example>
    public int OperationCount { get; set; }

    /// <summary>
    /// What one unit of the service costs today, not what it cost during the period. It is `0` when the service
    /// is no longer on the installation's price list.
    /// </summary>
    /// <example>0.14</example>
    public decimal Price { get; set; }

    /// <summary>
    /// Whether the service is billed as a standing subscription rather than per unit consumed. It is derived
    /// from today's price list, so it describes the service as it is sold now.
    /// </summary>
    /// <example>true</example>
    public bool Subscription { get; set; }

    public CustomerServiceUsageDto(CustomerServiceUsage usage, Dictionary<string, TenantQuota> walletQuotas, Dictionary<string, string> customUom)
    {
        var (serviceName, title, serviceUnit) = WalletServiceDescriptionManager.GetServiceTitleAndUom(usage.Service, customUom);

        if (!walletQuotas.TryGetValue(usage.Service, out var quota))
        {
            walletQuotas.TryGetValue(serviceName, out quota);
        }

        Service = serviceName;
        Title = title;
        ServiceUnit = serviceUnit;
        Currency = usage.Currency;
        TotalQuantity = usage.TotalQuantity;
        TotalAmount = usage.TotalAmount;
        OperationCount = usage.OperationCount;
        Price = quota?.Price ?? 0;
        Subscription = !string.IsNullOrEmpty(quota?.ProductId);
    }
}

/// <summary>
/// One page of the per-service consumption totals, with the paging figures needed to walk the rest.
/// </summary>
/// <example>
/// {
///   "collection": [{"service": "backup", "totalAmount": 49.99}],
///   "offset": 0,
///   "limit": 25,
///   "totalQuantity": 1,
///   "totalPage": 1,
///   "currentPage": 1
/// }
/// </example>
public class CustomerServiceUsageReportDto
{
    /// <summary>
    /// The services on this page, one entry per service rather than per charge. It is empty for a period in
    /// which nothing was consumed as well as for a page past the end of the report.
    /// </summary>
    /// <example>[{"service": "backup", "totalAmount": 49.99}]</example>
    public List<CustomerServiceUsageDto> Collection { get; set; }

    /// <summary>
    /// How many entries were skipped before this page, echoed from the request.
    /// </summary>
    /// <example>0</example>
    public int Offset { get; set; }

    /// <summary>
    /// How many entries one page may hold, echoed from the request; it is 25 unless another value was asked for.
    /// </summary>
    /// <example>25</example>
    public int Limit { get; set; }

    /// <summary>
    /// How many services match the filters in total, across every page - services, not charges.
    /// </summary>
    /// <example>1</example>
    public long TotalQuantity { get; set; }

    /// <summary>
    /// How many pages those entries come to at the current `limit`.
    /// </summary>
    /// <example>1</example>
    public int TotalPage { get; set; }

    /// <summary>
    /// Which of those pages this one is, as the billing service numbers them. Page through by advancing `offset`
    /// rather than this value, which nothing accepts as an argument.
    /// </summary>
    /// <example>1</example>
    public int CurrentPage { get; set; }

    public CustomerServiceUsageReportDto(UsageReport report, Dictionary<string, TenantQuota> walletQuotas, Dictionary<string, string> customUom)
    {
        Offset = report.Offset;
        Limit = report.Limit;
        TotalQuantity = report.TotalQuantity;
        TotalPage = report.TotalPage;
        CurrentPage = report.CurrentPage;

        Collection = [];

        if (report.Collection != null)
        {
            foreach (var usage in report.Collection)
            {
                Collection.Add(new CustomerServiceUsageDto(usage, walletQuotas, customUom));
            }
        }
    }
}

/// <summary>
/// One wallet service the portal is running right now, with the allowance it grants where that is counted.
/// </summary>
/// <example>
/// {
///   "service": "disk-storage",
///   "serviceUnit": "GB",
///   "subscription": true,
///   "title": "Additional disk storage",
///   "limit": 500,
///   "used": 320
/// }
/// </example>
public class ActiveServiceDto
{
    /// <summary>
    /// The stable key of the service, which is what `POST api/2.0/portal/payment/servicestate` takes to switch
    /// it off again.
    /// </summary>
    /// <example>disk-storage</example>
    public string Service { get; set; }

    /// <summary>
    /// What `limit` and `used` count, in the portal language - gigabytes, editor seats, credits.
    /// </summary>
    /// <example>GB</example>
    public string ServiceUnit { get; set; }

    /// <summary>
    /// Whether the service is billed as a standing subscription rather than per unit consumed. Only a
    /// subscription can carry `limit` and `used`.
    /// </summary>
    /// <example>true</example>
    public bool Subscription { get; set; }

    /// <summary>
    /// The service name in the portal language, for printing rather than matching.
    /// </summary>
    /// <example>Additional disk storage</example>
    public string Title { get; set; }

    /// <summary>
    /// How much of the service the portal is entitled to. It is empty for a service whose consumption is not
    /// counted this way, which is not the same as a service without a limit.
    /// </summary>
    /// <example>500</example>
    public int? Limit { get; set; }

    /// <summary>
    /// How much of that allowance is in use - the editors currently active for the cloud editors, the units
    /// already consumed for disk storage. Empty under the same conditions as `limit`.
    /// </summary>
    /// <example>320</example>
    public int? Used { get; set; }
}

/// <summary>
/// The billing customer behind the portal, and which portal member pays for it.
/// </summary>
public class CustomerInfoDto(CustomerInfo customerInfo, EmployeeDto employeeDto)
{
    /// <summary>
    /// The portal's identifier in the billing system, which is what support and invoices refer to. It is not the
    /// portal alias.
    /// </summary>
    /// <example>portal-001</example>
    public string PortalId { get; private set; } = customerInfo.PortalId;

    /// <summary>
    /// Whether a payment method is stored for the account and usable. Without one the portal can hold a wallet
    /// balance but cannot be charged automatically.
    /// </summary>
    /// <example>0</example>
    public PaymentMethodStatus PaymentMethodStatus { get; private set; } = customerInfo.PaymentMethodStatus;

    /// <summary>
    /// The customer's payment method type.
    /// </summary>
    /// <example>card</example>
    public string PaymentMethodType { get; private set; } = customerInfo.PaymentMethodType;

    /// <summary>
    /// Indicates whether the customer's payment method is delayed, i.e. the money reaches the wallet only after
    /// the transfer settles rather than immediately.
    /// </summary>
    /// <example>false</example>
    public bool IsDelayedPaymentMethod { get; private set; } = customerInfo.IsDelayedPaymentMethod;

    /// <summary>
    /// The address the billing account is registered to, lower-cased. It need not belong to a portal member,
    /// which is exactly when `payer` stays empty.
    /// </summary>
    /// <example>user@example.com</example>
    public string Email { get; private set; } = customerInfo.Email?.ToLowerInvariant();

    /// <summary>
    /// The portal member whose account is behind the billing address. It is empty when `email` matches no member
    /// of this portal, and while it is empty every operation of this group that only the payer may call is out
    /// of reach for everybody.
    /// </summary>
    /// <example>{"displayName": "John Doe", "email": "john.doe@example.com"}</example>
    public EmployeeDto Payer { get; private set; } = employeeDto;
}
