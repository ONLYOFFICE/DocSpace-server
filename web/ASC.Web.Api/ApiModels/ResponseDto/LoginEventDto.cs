// (c) Copyright Ascensio System SIA 2009-2026
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