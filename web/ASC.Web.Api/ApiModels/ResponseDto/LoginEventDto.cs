// (c) Copyright Ascensio System SIA 2009-2024
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

public class LoginEventDto(LoginEvent loginEvent, ApiDateTimeHelper apiDateTimeHelper)
{
    /// <summary>
    /// ID
    /// </summary>
    [OpenApiDescription("ID")]
    public int Id { get; set; } = loginEvent.Id;

    /// <summary>
    /// Date
    /// </summary>
    [OpenApiDescription("Date")]
    public ApiDateTime Date { get; set; } = apiDateTimeHelper.Get(loginEvent.Date);

    /// <summary>
    /// User
    /// </summary>
    [OpenApiDescription("User")]
    public string User { get; set; } = loginEvent.UserName;

    /// <summary>
    /// User ID
    /// </summary>
    [OpenApiDescription("User ID")]
    public Guid UserId { get; set; } = loginEvent.UserId;

    /// <summary>
    /// Login
    /// </summary>
    [OpenApiDescription("Login")]
    public string Login { get; set; } = loginEvent.Login;

    /// <summary>
    /// Action
    /// </summary>
    [OpenApiDescription("Action")]
    public string Action { get; set; } = loginEvent.ActionText;

    /// <summary>
    /// Action ID
    /// </summary>
    [OpenApiDescription("Action ID")]
    public MessageAction ActionId { get; set; } = (MessageAction)loginEvent.Action;

    /// <summary>
    /// IP
    /// </summary>
    [OpenApiDescription("IP")]
    public string IP { get; set; } = loginEvent.IP;

    /// <summary>
    /// Country
    /// </summary>
    [OpenApiDescription("Country")]
    public string Country { get; set; } = loginEvent.Country;

    /// <summary>
    /// City
    /// </summary>
    [OpenApiDescription("City")]
    public string City { get; set; } = loginEvent.City;

    /// <summary>
    /// Browser
    /// </summary>
    [OpenApiDescription("Browser")]
    public string Browser { get; set; } = loginEvent.Browser;

    /// <summary>
    /// Platform
    /// </summary>
    [OpenApiDescription("Platform")]
    public string Platform { get; set; } = loginEvent.Platform;

    /// <summary>
    /// Page
    /// </summary>
    [OpenApiDescription("Page")]
    public string Page { get; set; } = loginEvent.Page;
}