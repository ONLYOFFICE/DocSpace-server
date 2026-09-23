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

namespace ASC.Web.Api.ApiModel.ResponseDto;

/// <summary>
/// One entry of the portal login history: a sign-in, a sign-out or a failed attempt, and where it came from.
/// </summary>
/// <example>
/// {
///   "id": 1,
///   "date": "2024-01-15T10:30:00Z",
///   "user": "John Doe",
///   "userId": "00000000-0000-0000-0000-000000000001",
///   "login": "user@example.com",
///   "action": "User logged in",
///   "actionId": "LoginSuccess",
///   "iP": "192.0.2.1",
///   "country": "United States",
///   "city": "New York",
///   "browser": "Chrome 120.0",
///   "platform": "Windows",
///   "page": "/login"
/// }
/// </example>
public class LoginEventDto(LoginEvent loginEvent, ApiDateTimeHelper apiDateTimeHelper)
{
    /// <summary>
    /// The ID of the recorded sign-in. When the entry is a successful sign-in that is still open, this is also
    /// the value `GET api/2.0/security/activeconnections` reports as the connection's `id`.
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; } = loginEvent.Id;

    /// <summary>
    /// When the attempt was made, in the portal time zone. The `from` and `to` filters are read as UTC instants,
    /// so the two do not line up on a portal that is not on UTC.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime Date { get; set; } = apiDateTimeHelper.Get(loginEvent.Date);

    /// <summary>
    /// The display name of the account the attempt was made against, taken from the account as it stands now
    /// rather than as it stood at the time. A localised placeholder stands in when there is no account to read,
    /// which is the usual case for a failed attempt on an address nobody owns.
    /// </summary>
    /// <example>John Doe</example>
    public string User { get; set; } = loginEvent.UserName;

    /// <summary>
    /// The ID of that account, which is what the `userId` filter of this operation matches on. It is the empty
    /// GUID when the attempt could not be tied to an account.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    public Guid UserId { get; set; } = loginEvent.UserId;

    /// <summary>
    /// The login string as it was typed - normally the email address. It is the only field that survives a failed
    /// attempt against an unknown account, which makes it the one to read when `user` is a placeholder.
    /// </summary>
    /// <example>user@example.com</example>
    public string Login { get; set; } = loginEvent.Login;

    /// <summary>
    /// The event as a readable sentence in the portal language. On `GET api/2.0/security/audit/login/last` each
    /// substituted value is cut to 50 characters; the filtered operation substitutes them in full.
    /// </summary>
    /// <example>User logged in</example>
    public string Action { get; set; } = loginEvent.ActionText;

    /// <summary>
    /// What happened, as the `action` filter of this operation spells it: a successful sign-in, a failed one, a
    /// sign-out. Use this rather than parsing `action`, which is prose and changes with the portal language.
    /// </summary>
    /// <example>LoginSuccess</example>
    public MessageAction ActionId { get; set; } = (MessageAction)loginEvent.Action;

    /// <summary>
    /// The IP address the attempt came from, with the port stripped off.
    /// </summary>
    /// <example>192.0.2.1</example>
    public string IP { get; set; } = loginEvent.IP;

    /// <summary>
    /// The English name of the country the IP address is located in, empty when the address cannot be located -
    /// the normal outcome for private and loopback addresses.
    /// </summary>
    /// <example>United States</example>
    public string Country { get; set; } = loginEvent.Country;

    /// <summary>
    /// The city the IP address is located in, empty under the same conditions as `country`.
    /// </summary>
    /// <example>New York</example>
    public string City { get; set; } = loginEvent.City;

    /// <summary>
    /// The browser and its version as parsed from the user agent of the attempt, empty when the client sent none
    /// that could be parsed.
    /// </summary>
    /// <example>Chrome 120.0</example>
    public string Browser { get; set; } = loginEvent.Browser;

    /// <summary>
    /// The operating system as parsed from the same user agent, empty under the same conditions as `browser`.
    /// </summary>
    /// <example>Windows</example>
    public string Platform { get; set; } = loginEvent.Platform;

    /// <summary>
    /// Where in the portal the attempt was made from: the referrer of the request, or that request's own path
    /// when it carried no referrer. Long values are cut off at 512 characters.
    /// </summary>
    /// <example>/login</example>
    public string Page { get; set; } = loginEvent.Page;
}