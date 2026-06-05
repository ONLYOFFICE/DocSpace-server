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

public interface ITariffService
{
    Task<Dictionary<string, Dictionary<string, decimal>>> GetProductPriceInfoAsync(string partnerId, bool wallet, List<string> productIds);
    Task<IEnumerable<PaymentInfo>> GetPaymentsAsync(int tenantId);
    Task<Tariff> GetTariffAsync(int tenantId, bool withRequestToPaymentSystem = true, bool refresh = false);
    Task<Uri> GetShoppingUriAsync(int tenant, string affiliateId, string partnerId, string currency = null, string language = null, string customerEmail = null, Dictionary<string, int> quantity = null, string backUrl = null, string successUrl = null, bool checkoutSetup = false);
    Task<bool> UpdateNextQuantityAsync(int tenant, Tariff tariffInfo, int quotaId, int? nextQuantity);
    Task DeleteDefaultBillingInfoAsync();
    Task SetTariffAsync(int tenantId, Tariff tariff, List<TenantQuota> quotas = null);
    Task<Uri> GetAccountLinkAsync(int tenant, string backUrl);
    Task<bool> PaymentChangeAsync(int tenantId, Dictionary<string, int> quantity, ProductQuantityType productQuantityType, string currency, bool checkQuota, string customerParticipantName, Dictionary<string, string> metadata = null);
    Task<PaymentCalculation> PaymentCalculateAsync(int tenantId, Dictionary<string, int> quantity, ProductQuantityType productQuantityType, string currency);
    int GetPaymentDelay();
    Task<Tariff> GetBillingInfoAsync(int? tenant = null, int? id = null);
    bool IsConfigured();
    Task<CustomerInfo> GetCustomerInfoAsync(int tenantId, bool refresh = false);
    Task<bool> TopUpDepositAsync(int tenantId, decimal amount, string currency, string customerParticipantName, string siteName, Dictionary<string, string> metadata = null, bool waitForChanges = false);

    Task<Balance> GetCustomerBalanceAsync(int tenantId, bool refresh = false);
    Task<Balance> GetCustomerAiBalanceAsync(int tenantId, bool refresh = false);
    Task<Session> OpenCustomerSessionAsync(int tenantId, string serviceName, string externalRef, int quantity, int duration);
    Task<bool> CloseCustomerSessionAsync(int tenantId, int sessionId);
    Task<Session> ExtendCustomerSessionAsync(int tenantId, int sessionId, int duration);
    Task<bool> CompleteCustomerSessionAsync(int tenantId, string serviceName, int sessionId, int quantity, string customerParticipantName, Dictionary<string, string> metadata = null);
    Task<ServicePayment> MakeAiCreditAsync(int tenantId, decimal amount, string currency, string customerParticipantName, Dictionary<string, string> metadata = null);
    Task<Report> GetCustomerOperationsAsync(int tenantId, OperationFilter filter);
    Task<List<CustomerMonthlyUsage>> GetCustomerMonthlyUsageAsync(int tenantId, DateTime? utcStartDate, DateTime? utcEndDate);
    Task<UsageReport> GetCustomerServiceUsageAsync(int tenantId, UsageFilter filter);
    Task<List<Currency>> GetAllAccountingCurrenciesAsync();
    List<string> GetSupportedAccountingCurrencies();

    Task<bool> IsFreeTariffAsync(Tariff tariff);
}
