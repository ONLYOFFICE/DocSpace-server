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

namespace ASC.Web.Api.ApiModel.RequestsDto;

/// <summary>
/// The request parameters for configuring trusted mail domains and visitor invitation settings.
/// </summary>
public class MailDomainSettingsRequestsDto
{
    /// <summary>
    /// Defines how trusted domains are handled and validated.
    /// </summary>
    public TenantTrustedDomainsType Type { get; set; }

    /// <summary>
    /// The list of authorized email domains that are considered trusted.
    /// </summary>
    public List<string> Domains { get; set; }

    /// <summary>
    /// Specifies the default permission level for the invited users (visitors or not).
    /// </summary>
    public bool InviteUsersAsVisitors { get; set; }
}

/// <summary>
/// The request parameters for the administrator message configuration.
/// </summary>
public class AdminMessageBaseSettingsRequestsDto
{
    /// <summary>
    /// The email address used for sending administrator messages.
    /// </summary>
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    /// <summary>
    /// The locale identifier for message localization.
    /// </summary>
    public string Culture { get; set; }
}

/// <summary>
/// The request parameters for configuring the administrator message content.
/// </summary>
public class AdminMessageSettingsRequestsDto : AdminMessageBaseSettingsRequestsDto
{
    /// <summary>
    /// The content of the administrator message to be sent.
    /// </summary>
    [StringLength(255)]
    public string Message { get; set; }
}

/// <summary>
/// The request parameters for enabling or disabling administrator messaging system.
/// </summary>
public class TurnOnAdminMessageSettingsRequestDto
{
    /// <summary>
    /// The global switch for the administrator messaging functionality.
    /// </summary>
    public bool TurnOn { get; set; }
}