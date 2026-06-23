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
/// The login event parameters.
/// </summary>
/// <example>
/// {
///   "id": 1,
///   "date": "2024-01-15T10:30:00Z",
///   "user": "John Doe",
///   "userId": {},
///   "login": "user@example.com",
///   "action": "User logged in",
///   "actionId": "EnumValue",
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
    /// The login event ID.
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; } = loginEvent.Id;

    /// <summary>
    /// The login event date.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime Date { get; set; } = apiDateTimeHelper.Get(loginEvent.Date);

    /// <summary>
    /// The user name of the login event.
    /// </summary>
    /// <example>John Doe</example>
    public string User { get; set; } = loginEvent.UserName;

    /// <summary>
    /// The user ID of the login event.
    /// </summary>
    /// <example>{}</example>
    public Guid UserId { get; set; } = loginEvent.UserId;

    /// <summary>
    /// The user login of the login event.
    /// </summary>
    /// <example>user@example.com</example>
    public string Login { get; set; } = loginEvent.Login;

    /// <summary>
    /// The login event action.
    /// </summary>
    /// <example>User logged in</example>
    public string Action { get; set; } = loginEvent.ActionText;

    /// <summary>
    /// The login-related action to filter events by.
    /// </summary>
    /// <example>EnumValue</example>
    public MessageAction ActionId { get; set; } = (MessageAction)loginEvent.Action;

    /// <summary>
    /// The login event IP.
    /// </summary>
    /// <example>192.0.2.1</example>
    public string IP { get; set; } = loginEvent.IP;

    /// <summary>
    /// The login event country.
    /// </summary>
    /// <example>United States</example>
    public string Country { get; set; } = loginEvent.Country;

    /// <summary>
    /// The login event city.
    /// </summary>
    /// <example>New York</example>
    public string City { get; set; } = loginEvent.City;

    /// <summary>
    /// The login event browser.
    /// </summary>
    /// <example>Chrome 120.0</example>
    public string Browser { get; set; } = loginEvent.Browser;

    /// <summary>
    /// The login event platform.
    /// </summary>
    /// <example>Windows</example>
    public string Platform { get; set; } = loginEvent.Platform;

    /// <summary>
    /// The login event page.
    /// </summary>
    /// <example>/login</example>
    public string Page { get; set; } = loginEvent.Page;
}