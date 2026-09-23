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

namespace ASC.Web.Api.ApiModel.RequestsDto;

/// <summary>
/// The portal two-factor policy: which method is in force, who must pass it, and from where it is waived.
/// </summary>
/// <example>
/// {
///   "type": "EnumValue",
///   "id": {},
///   "trustedIps": ["192.0.2.1", "203.0.113.0/24"],
///   "mandatoryUsers": [],
///   "mandatoryGroups": []
/// }
/// </example>
public class TfaRequestsDto
{
    /// <summary>
    /// The second factor the portal demands. The two methods are mutually exclusive, so switching one on switches
    /// the other off, and any value outside the defined set is read as switching TFA off rather than refused.
    /// </summary>
    /// <example>None</example>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TfaRequestsDtoType Type { get; set; }

    /// <summary>
    /// The account the request concerns, by portal user ID. Naming the portal owner is refused unless it is the
    /// caller's own account. Where an operation detaches an authenticator application, the empty GUID and the
    /// caller's own ID both mean the caller.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The list of IP addresses that bypass TFA verification. Each entry is a single address, an inclusive
    /// "from-to" range or a CIDR block. This is the whole list that is to hold afterwards, so send the addresses
    /// already trusted along with a new one; an entry that cannot be parsed fails the call with 400, and accounts
    /// named as mandatory still have to pass the challenge even from a trusted address.
    /// </summary>
    /// <example>["192.0.2.1", "198.51.100.1-198.51.100.20", "203.0.113.0/24"]</example>
    [IpAddressOrRange]
    public List<string> TrustedIps { get; set; }

    /// <summary>
    /// The accounts that must pass the challenge whatever their address, by portal user ID. This is the whole list
    /// that is to hold afterwards - leaving it out clears it rather than keeping it - and naming the portal owner is
    /// refused unless the caller is the owner.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000"]</example>
    public List<Guid> MandatoryUsers { get; set; }

    /// <summary>
    /// The groups whose members must pass the challenge whatever their address, by group ID. This is the whole list
    /// that is to hold afterwards - leaving it out clears it rather than keeping it.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000"]</example>
    public List<Guid> MandatoryGroups { get; set; }
}

/// <summary>
/// The two-factor method a portal can demand.
/// </summary>
public enum TfaRequestsDtoType
{
    /// <summary>No second factor is demanded; sending it also switches off whichever method was in force.</summary>
    [Description("None")]
    None = 0,

    /// <summary>A code sent by SMS to the number stored for the account; the portal needs an SMS provider.</summary>
    [Description("Sms")]
    Sms = 1,

    /// <summary>A code from an authenticator application the account links once and then keeps.</summary>
    [Description("App")]
    App = 2
}

/// <summary>
/// The one-time code that completes a pending two-factor step, and how long the resulting sign-in lasts.
/// </summary>
public class TfaValidateRequestsDto
{
    /// <summary>
    /// The code to check - either one from the authenticator application or one of the account's unused backup
    /// codes, which is spent by the check. A wrong code is refused with 400 and counts against the portal login
    /// attempt limit.
    /// </summary>
    /// <example>123456</example>
    public required string Code { get; set; }

    /// <summary>
    /// Whether the sign-in that follows is tied to the browser session. When it is, the session ends with the
    /// browser rather than lasting for the portal session lifetime.
    /// </summary>
    /// <example>true</example>
    public bool Session { get; set; }
}
