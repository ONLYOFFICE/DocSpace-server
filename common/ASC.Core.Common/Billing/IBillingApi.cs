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

namespace ASC.Core.Billing;

/// <summary>
/// Type-safe REST contract for the external billing service, implemented by Refit.
/// Every endpoint is a POST whose body preserves the billing multimap wire format: each field is an
/// array of string values, e.g. <c>{"PortalId":["x"],"ProductId":["a","b"]}</c>. The request bodies are
/// modelled by the strongly-typed <c>*RequestDto</c> classes (every property is a <see cref="List{T}"/>
/// of <see cref="string"/>); unset optional fields are <c>null</c> and omitted by the serializer's
/// <see cref="JsonIgnoreCondition.WhenWritingNull"/> setting.
/// Errors arrive as 200 OK with a <c>{"Message":"error...</c> body and are mapped to exceptions by the
/// ExceptionFactory. All paths are relative — the base address, authentication and resilience are configured
/// in <see cref="BillingHttpClientExtension.AddBillingHttpClient"/>. The public wrapper is <see cref="BillingClient"/>.
/// </summary>
public interface IBillingApi
{
    [Post("/billing/GetAccountLink")]
    Task<string> GetAccountLinkAsync([Body] GetAccountLinkRequestDto requestDto);

    [Post("/billing/GetActiveResources")]
    Task<PaymentLast[]> GetActiveResourcesAsync(
        [Body] BillingPortalRequestDto requestDto,
        [Property(BillingHttpClientExtension.RetryOptionKey)] bool refresh);

    [Post("/billing/GetPayments")]
    Task<List<PaymentInfo>> GetPaymentsAsync([Body] BillingPortalRequestDto requestDto);

    [Post("/billing/GetSinglePaymentUrl")]
    Task<string> GetSinglePaymentUrlAsync([Body] GetPaymentUrlRequestDto requestDto);

    [Post("/billing/GetCustomerInfo")]
    Task<CustomerInfo> GetCustomerInfoAsync([Body] BillingPortalRequestDto requestDto);

    /// <remarks>Returns the raw response body; the billing service answers with the JSON string <c>"ok"</c> on success.</remarks>
    [Post("/billing/Deposit")]
    Task<string> DepositAsync([Body] DepositRequestDto requestDto);

    [Post("/billing/ChangeSubscription")]
    Task<bool> ChangeSubscriptionAsync([Body] ChangeSubscriptionRequestDto requestDto);

    [Post("/billing/SwitchSubscription")]
    Task<bool> SwitchSubscriptionAsync([Body] SwitchSubscriptionRequestDto requestDto);

    [Post("/billing/CalculateSwitchSubscription")]
    Task<PaymentCalculation> CalculateSwitchSubscriptionAsync([Body] CalculateSwitchSubscriptionRequestDto requestDto);

    [Post("/billing/CalculateSubscription")]
    Task<PaymentCalculation> CalculateSubscriptionAsync([Body] CalculateSubscriptionRequestDto requestDto);

    [Post("/billing/GetProductsPrices")]
    Task<Dictionary<int, Dictionary<string, Dictionary<string, decimal>>>> GetProductsPricesAsync([Body] GetProductsPricesRequestDto requestDto);

    [Post("/billing/GetSubscriptionBalanceInfo")]
    Task<SubscriptionBalanceInfo> GetSubscriptionBalanceInfoAsync([Body] BillingProductRequestDto requestDto);

    [Post("/billing/SubscriptionBalanceToWallet")]
    Task<SubscriptionToWalletResult> SubscriptionBalanceToWalletAsync([Body] BillingProductRequestDto requestDto);

    /// <remarks>Returns the raw response body; the billing service answers with the JSON string <c>"ok"</c> on success.</remarks>
    [Post("/billing/getwdocstrial")]
    Task<string> GetDocsCloudTrialAsync([Body] BillingPortalRequestDto requestDto);
}
