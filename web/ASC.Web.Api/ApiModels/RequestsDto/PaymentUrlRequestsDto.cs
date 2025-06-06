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

namespace ASC.Web.Api.Models;

/// <summary>
/// The request parameters for the payment URL configuration with quantity information.
/// </summary>
public class PaymentUrlRequestsDto
{
    /// <summary>
    /// The URL where the user will be redirected after payment processing.
    /// </summary>
    public string BackUrl { get; set; }

    /// <summary>
    /// The quantity of payment
    /// </summary>
    public Dictionary<string, int> Quantity { get; set; }
}

/// <summary>
/// The request parameters for the payment quantity specifications.
/// </summary>
public class QuantityRequestDto
{
    /// <summary>
    /// The mapping of item identifiers with their respective quantities in the payment.
    /// </summary>
    public Dictionary<string, int> Quantity { get; set; }
}

/// <summary>
/// The request parameters for the wallet payment quantity specifications.
/// </summary>
public class WalletQuantityRequestDto
{
    /// <summary>
    /// The mapping of item identifiers with their respective quantities in the payment.
    /// </summary>
    public Dictionary<string, int?> Quantity { get; set; }

    /// <summary>
    /// The type of action performed on a quantity of product.
    /// </summary>
    public ProductQuantityType ProductQuantityType { get; set; }
}

/// <summary>
/// Chechout setup URL request parameters
/// </summary>
public class CheckoutSetupUrlRequestsDto
{
    /// <summary>
    /// Back URL
    /// </summary>
    [FromQuery]
    public string BackUrl { get; set; }
}

/// <summary>
/// Put money on deposit request parameters
/// </summary>
public class TopUpDepositRequestDto
{
    /// <summary>
    /// Amount
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; }
}