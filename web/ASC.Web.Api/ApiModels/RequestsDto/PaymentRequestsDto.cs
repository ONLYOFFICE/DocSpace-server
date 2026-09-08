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
/// The plan being bought and the two pages the hosted checkout returns the buyer to.
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
    /// The absolute address the hosted checkout page sends the buyer back to when the purchase is abandoned. It has
    /// to be a well-formed URL and is carried into the checkout page as it is given, so it must be reachable by the
    /// buyer rather than by the portal.
    /// </summary>
    /// <example>https://example.com/payment/back</example>
    [Url]
    [Required]
    [StringLength(255)]
    public string BackUrl { get; set; }

    /// <summary>
    /// The absolute address the hosted checkout page sends the buyer to once the payment provider accepts the
    /// purchase. Reaching it says the provider took the money, not that the portal has already been switched to the
    /// new plan, so a client that lands here reads the plan back rather than assuming it.
    /// </summary>
    /// <example>https://example.com/payment/success</example>
    [Url]
    [Required]
    [StringLength(255)]
    public string SuccessUrl { get; set; }

    /// <summary>
    /// The plan being bought, as a single pair of the plan name and the number of units of it. The key is the `name`
    /// of a monthly, non-wallet quota from `GET api/2.0/portal/payment/quotas`, and the value is how many
    /// administrators the plan is to cover, which has to be greater than zero. Exactly one pair is accepted; yearly
    /// and wallet products are refused with 400, and wallet services are bought through
    /// `PUT api/2.0/portal/payment/updatewallet` instead.
    /// </summary>
    /// <example>{ "admin": 1 }</example>
    [Length(1, 1)]
    [Required]
    public Dictionary<string, int> Quantity { get; set; }
}

/// <summary>
/// The return address carried into the link to the billing account page.
/// </summary>
public class PaymentAccountRequestDto
{
    /// <summary>
    /// The absolute address the billing account page should offer as its way back. It is appended to the returned
    /// portal-relative address as a query parameter rather than followed here, and omitting it yields the bare
    /// address of the page.
    /// </summary>
    /// <example>https://example.com</example>
    [Url]
    [StringLength(255)]
    [FromQuery(Name = "backUrl")]
    public string BackUrl { get; set; }
}

/// <summary>
/// Whether the billing facts are taken from the portal cache or fetched from the billing service.
/// </summary>
public class PaymentInformationRequestDto
{
    /// <summary>
    /// Whether the answer is fetched from the billing service instead of the portal cache. The cached copy is what a
    /// start-up needs and costs nothing; asking for a fresh one makes a remote call, so use it right after a
    /// purchase or a top-up and not on every read.
    /// </summary>
    /// <example>true</example>
    [FromQuery(Name = "refresh")]
    public bool Refresh { get; set; }
}

/// <summary>
/// The two filters that narrow the catalogue of purchasable quotas.
/// </summary>
public class QuotasRequestDto
{
    /// <summary>
    /// Which side of the catalogue is listed: `true` keeps the services paid out of the portal wallet, `false` keeps
    /// the subscription plans, and omitting it keeps both.
    /// </summary>
    /// <example>true</example>
    [FromQuery(Name = "wallet")]
    public bool? Wallet { get; set; }

    /// <summary>
    /// Which layer of the catalogue is listed: `true` keeps the add-ons that extend a plan, `false` keeps the plans
    /// themselves, and omitting it keeps both.
    /// </summary>
    /// <example>true</example>
    [FromQuery(Name = "additional")]
    public bool? Additional { get; set; }
}

/// <summary>
/// The new size of the portal subscription.
/// </summary>
public class QuantityRequestDto
{
    /// <summary>
    /// The plan and the number of units it is to cover, as a single pair. While the portal is on a priced plan the
    /// key has to be the `name` of that same plan, which `GET api/2.0/portal/payment/quota` reports, because the
    /// subscription is resized rather than swapped; the value is the total the subscription is to have afterwards,
    /// not the difference. Exactly one pair is accepted, and a value that is already in effect is refused with 400.
    /// </summary>
    /// <example>{ "admin": 1 }</example>
    [Length(1, 1)]
    [Required]
    public Dictionary<string, int> Quantity { get; set; }
}

/// <summary>
/// The wallet service being bought or scheduled, and the way its quantity is applied.
/// </summary>
public class WalletQuantityRequestDto
{
    /// <summary>
    /// The wallet service and the number of units of it, as a single pair. The key is the `serviceName` of a service
    /// from `GET api/2.0/portal/payment/walletservices`, and the value is read according to
    /// `productQuantityType`: the units to add, or the total the service is to have in the next period. Minimum
    /// quantities apply per service - disk storage starts at 100 units, the DocsCloud developer pack at 10, and the
    /// administrators may not be fewer than the portal already has. Exactly one pair is accepted, and a null or zero
    /// value cancels a change scheduled earlier rather than buying nothing.
    /// </summary>
    /// <example>{ "admin": 1 }</example>
    [Length(1, 1)]
    [Required]
    public Dictionary<string, int?> Quantity { get; set; }

    /// <summary>
    /// How the number in `quantity` is applied. `Add` buys the units straight away and charges them to the portal
    /// wallet, while `Set` charges nothing now and records the quantity the service is to have from the next period.
    /// Only these two are accepted here; `Sub` and `Renew` are refused with 400.
    /// </summary>
    /// <example>0</example>
    public ProductQuantityType ProductQuantityType { get; set; }
}

/// <summary>
/// The two pages the hosted payment-method setup returns the user to.
/// </summary>
public class CheckoutSetupUrlRequestsDto
{
    /// <summary>
    /// The absolute address the setup page sends the user back to when attaching a payment method is abandoned. It
    /// has to be a well-formed URL and must be reachable by that user rather than by the portal.
    /// </summary>
    /// <example>https://example.com/payment/back</example>
    [Url]
    [Required]
    [FromQuery]
    [StringLength(255)]
    public string BackUrl { get; set; }

    /// <summary>
    /// The absolute address the setup page sends the user to once the payment provider has stored the payment
    /// method. Reaching it means a method is now on file, which `GET api/2.0/portal/payment/customerinfo` confirms;
    /// nothing has been charged.
    /// </summary>
    /// <example>https://example.com/payment/success</example>
    [Url]
    [Required]
    [FromQuery]
    [StringLength(255)]
    public string SuccessUrl { get; set; }
}

/// <summary>
/// How much money is charged to the payment method on file and added to the portal wallet.
/// </summary>
public class TopUpDepositRequestDto
{
    /// <summary>
    /// The sum to charge, as a whole number of units of `currency` - 10 means ten dollars and not ten cents. The
    /// bounds are what one call may move, not what the wallet may hold, so a larger top-up is made of several calls.
    /// </summary>
    /// <example>1</example>
    [Range(1, 999999)]
    public int Amount { get; set; }

    /// <summary>
    /// The currency the charge is made in, as an ISO 4217 code in upper case. It has to be one of the accounting
    /// currencies this installation supports, which `GET api/2.0/portal/payment/accounting/currencies` lists; any
    /// other code is refused with 400. The money lands on the wallet sub-account of that currency, so topping up in
    /// a second currency does not add to the first one.
    /// </summary>
    /// <example>USD</example>
    [StringLength(3)]
    public string Currency { get; set; }
}

/// <summary>
/// Which wallet service is looked up in the billing catalogue.
/// </summary>
public class GetWalletServiceRequestDto
{
    /// <summary>
    /// The service to look up, given by its catalogue name. A service this installation does not sell answers 404,
    /// and the whole catalogue is `GET api/2.0/portal/payment/walletservices`.
    /// </summary>
    /// <example>Storage</example>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [FromQuery(Name = "service")]
    public required TenantWalletService Service { get; set; }
}

/// <summary>
/// Which wallet service is switched, and which way.
/// </summary>
public class ChangeWalletServiceStateRequestDto
{
    /// <summary>
    /// The service being switched, given by its catalogue name. Switching it on only makes it available to the
    /// portal; its units are still bought with `PUT api/2.0/portal/payment/updatewallet`.
    /// </summary>
    /// <example>Storage</example>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TenantWalletService Service { get; set; }

    /// <summary>
    /// Which way the service is switched: `true` makes it available to the portal, `false` withdraws it. Setting the
    /// state the service already has changes nothing.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; set; }
}

/// <summary>
/// The complete set of AI chat models that are to be barred on the portal.
/// </summary>
public class SetRestrictedAiModelsRequestDto
{
    /// <summary>
    /// The identifiers of the models no user of the portal may pick, taken from
    /// `GET api/2.0/portal/payment/ai-prices`. This is the whole set that is to hold afterwards and not a list of
    /// additions: send the models already barred together with the new one to add a restriction, leave one out to
    /// lift it, and send an empty set to lift them all.
    /// </summary>
    /// <example>["model1", "model2"]</example>
    [Required]
    public HashSet<string> Models { get; set; }
}
