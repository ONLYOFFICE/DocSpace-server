// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Core.Billing;

public interface ITariffService
{
    Task<IDictionary<string, Dictionary<string, decimal>>> GetProductPriceInfoAsync(string partnerId, params string[] productIds);
    Task<IEnumerable<PaymentInfo>> GetPaymentsAsync(int tenantId);
    Task<Tariff> GetTariffAsync(int tenantId, bool withRequestToPaymentSystem = true, bool refresh = false);
    Task<Uri> GetShoppingUriAsync(int tenant, string affiliateId, string partnerId, string currency = null, string language = null, string customerEmail = null, Dictionary<string, int> quantity = null, string backUrl = null, bool checkoutSetup = false);
    Task DeleteDefaultBillingInfoAsync();
    Task SetTariffAsync(int tenantId, Tariff tariff, List<TenantQuota> quotas = null);
    Task<Uri> GetAccountLinkAsync(int tenant, string backUrl);
    Task<bool> PaymentChangeAsync(int tenantId, Dictionary<string, int> quantity);
    int GetPaymentDelay();
    Task<Tariff> GetBillingInfoAsync(int? tenant = null, int? id = null);
    bool IsConfigured();
    Task<string> GetCustomerInfoAsync(int tenant);
    Task<string> PutOnDepositAsync(int tenant, long amount, string currency);

    Task<Balance> GetCustomerBalanceAsync(int tenantId);
    Task<Session> OpenCustomerSessionAsync(int tenantId, int serviceAccount, string externalRef, int quantity);
    Task<bool> PerformCustomerOperationAsync(int tenantId, int serviceAccount, int sessionId, int quantity);
    Task<Report> GetCustomerOperationsAsync(int tenantId, DateTime utcStartDate, DateTime utcEndDate, bool? credit, bool? withdrawal);
    Task<List<Currency>> GetAllCurrenciesAsync();
}
