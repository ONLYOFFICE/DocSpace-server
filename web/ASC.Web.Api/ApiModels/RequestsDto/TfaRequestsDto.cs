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

namespace ASC.Web.Api.ApiModel.RequestsDto;

/// <summary>
/// The request parameters for configuring the Two-Factor Authentication (TFA) settings.
/// </summary>
/// <example>
/// {
///   "type": "EnumValue",
///   "id": {},
///   "trustedIps": ["item1", "item2"],
///   "mandatoryUsers": [],
///   "mandatoryGroups": []
/// }
/// </example>
public class TfaRequestsDto
{
    /// <summary>
    /// The two-factor authentication type.
    /// </summary>
    /// <example>None</example>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TfaRequestsDtoType Type { get; set; }

    /// <summary>
    /// The ID of the user for whom the TFA settings are being configured.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The list of IP addresses that bypass TFA verification.
    /// </summary>
    /// <example>["item1", "item2"]</example>
    public List<string> TrustedIps { get; set; }

    /// <summary>
    /// The list of user IDs for whom TFA is mandatory.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000"]</example>
    public List<Guid> MandatoryUsers { get; set; }

    /// <summary>
    /// The list group IDs whose members must use TFA.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000"]</example>
    public List<Guid> MandatoryGroups { get; set; }
}

/// <summary>
/// The two-factor authentication type.
/// </summary>
public enum TfaRequestsDtoType
{
    [Description("None")]
    None = 0,

    [Description("Sms")]
    Sms = 1,

    [Description("App")]
    App = 2
}

/// <summary>
/// The request parameters for validating the two-factor authentication codes.
/// </summary>
public class TfaValidateRequestsDto
{
    /// <summary>
    /// The verification code provided by the user.
    /// </summary>
    /// <example>123456</example>
    public required string Code { get; set; }
}