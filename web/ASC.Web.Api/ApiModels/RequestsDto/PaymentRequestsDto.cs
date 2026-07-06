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

namespace ASC.Web.Api.Models;

/// <summary>
/// The request parameters for the payment URL configuration with quantity information.
/// </summary>
/// <example>
/// {
///   "backUrl": "https://example.com/payment/back",
///   "successUrl": "https://example.com/payment/success",
///   "quantity": {}
/// }
/// </example>
public class PaymentUrlRequestDto
{
    /// <summary>
    /// The URL where the user will be redirected after payment cancellation.
    /// </summary>
    /// <example>https://example.com/payment/back</example>
    [Url]
    [Required]
    [StringLength(255)]
    public string BackUrl { get; set; }

    /// <summary>
    /// The URL where the user will be redirected after successful payment.
    /// </summary>
    /// <example>https://example.com/payment/success</example>
    [Url]
    [Required]
    [StringLength(255)]
    public string SuccessUrl { get; set; }

    /// <summary>
    /// The payment quantity.
    /// </summary>
    /// <example>{}</example>
    public Dictionary<string, int> Quantity { get; set; }
}

/// <summary>
/// The request parameters for handling the payment redirect URL.
/// </summary>
public class PaymentAccountRequestDto
{
    /// <summary>
    /// The URL where the user will be redirected after payment processing.
    /// </summary>
    /// <example>https://example.com</example>
    [Url]
    [StringLength(255)]
    [FromQuery(Name = "backUrl")]
    public string BackUrl { get; set; }
}

/// <summary>
/// The request parameters for managing the payment information.
/// </summary>
public class PaymentInformationRequestDto
{
    /// <summary>
    /// Specifies whether to refresh the payment information cache or not.
    /// </summary>
    /// <example>true</example>
    [FromQuery(Name = "refresh")]
    public bool Refresh { get; set; }
}

/// <summary>
/// The request parameters for getting the quotas.
/// </summary>
public class QuotasRequestDto
{
    /// <summary>
    /// Specifies whether to return the wallet quotas only.
    /// </summary>
    /// <example>true</example>
    [FromQuery(Name = "wallet")]
    public bool? Wallet { get; set; }

    /// <summary>
    /// Specifies whether to return additional quotas only.
    /// </summary>
    /// <example>true</example>
    [FromQuery(Name = "additional")]
    public bool? Additional { get; set; }
}

/// <summary>
/// The request parameters for specifying payment quantity.
/// </summary>
public class QuantityRequestDto
{
    /// <summary>
    /// The mapping of item identifiers to their respective quantities in the payment.
    /// </summary>
    /// <example>{}</example>
    public Dictionary<string, int> Quantity { get; set; }
}

/// <summary>
/// The request parameters for specifying wallet payment quantity.
/// </summary>
public class WalletQuantityRequestDto
{
    /// <summary>
    /// The mapping of item identifiers to their respective quantities in the payment.
    /// </summary>
    /// <example>{}</example>
    public Dictionary<string, int?> Quantity { get; set; }

    /// <summary>
    /// The type of action performed on a product's quantity.
    /// </summary>
    /// <example>0</example>
    public ProductQuantityType ProductQuantityType { get; set; }
}

/// <summary>
/// The request parameters for getting the checkout setup page URL.
/// </summary>
public class CheckoutSetupUrlRequestsDto
{
    /// <summary>
    /// The URL where the user will be redirected after setup cancellation.
    /// </summary>
    /// <example>https://example.com/payment/back</example>
    [Url]
    [Required]
    [FromQuery]
    [StringLength(255)]
    public string BackUrl { get; set; }

    /// <summary>
    /// The URL where the user will be redirected after successful payment.
    /// </summary>
    /// <example>https://example.com/payment/success</example>
    [Url]
    [Required]
    [FromQuery]
    [StringLength(255)]
    public string SuccessUrl { get; set; }
}

/// <summary>
/// The request parameters for putting money on deposit.
/// </summary>
public class TopUpDepositRequestDto
{
    /// <summary>
    /// The amount of money for the operation.
    /// </summary>
    /// <example>1</example>
    [Range(1, 999999)]
    public int Amount { get; set; }

    /// <summary>
    /// The three-character ISO 4217 currency symbol.
    /// </summary>
    /// <example>USD</example>
    [StringLength(3)]
    public string Currency { get; set; }
}

/// <summary>
/// The request parameters for getting the specified wallet service.
/// </summary>
public class GetWalletServiceRequestDto
{
    /// <summary>
    /// The wallet service type.
    /// </summary>
    /// <example>Storage</example>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [FromQuery(Name = "service")]
    public required TenantWalletService Service { get; set; }
}

/// <summary>
/// The request parameters for changing the tenant wallet service state.
/// </summary>
public class ChangeWalletServiceStateRequestDto
{
    /// <summary>
    /// The wallet service type.
    /// </summary>
    /// <example>Storage</example>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TenantWalletService Service { get; set; }

    /// <summary>
    /// Specifies whether the wallet service is enabled.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; set; }
}

/// <summary>
/// The request parameters for crediting AI quota to the customer AI subaccount.
/// </summary>
public class CreditAiBalanceRequestDto
{
    /// <summary>
    /// The amount to transfer from the main balance to the AI subaccount.
    /// </summary>
    /// <example>100.00</example>
    [Range(0.01, 999999)]
    public decimal Amount { get; set; }

    /// <summary>
    /// The three-character ISO 4217 currency symbol.
    /// </summary>
    /// <example>USD</example>
    [StringLength(3)]
    public string Currency { get; set; }
}

/// <summary>
/// The request parameters for setting restricted AI models.
/// </summary>
public class SetRestrictedAiModelsRequestDto
{
    /// <summary>
    /// The set of restricted AI model IDs.
    /// </summary>
    /// <example>["model1", "model2"]</example>
    [Required]
    public HashSet<string> Models { get; set; }
}
