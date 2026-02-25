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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// The tariff parameters.
/// </summary>
/// <example>
/// {
///   "openSource": true,
///   "enterprise": true,
///   "developer": true,
///   "id": 1,
///   "state": {},
///   "dueDate": "2024-01-15T10:30:00Z",
///   "delayDueDate": "2024-01-15T10:30:00Z",
///   "licenseDate": "2024-01-15T10:30:00Z",
///   "customerId": "example value",
///   "quotas": [{"id": 1, "title": "Basic Plan"}]
/// }
/// </example>
public class TariffDto
{
    /// <summary>
    /// Specifies whether the tariff is Community or not.
    /// </summary>
    /// <example>true</example>
    public bool? OpenSource { get; set; }

    /// <summary>
    /// Specifies whether the tariff is Enterprise or not.
    /// </summary>
    /// <example>true</example>
    public bool? Enterprise { get; set; }

    /// <summary>
    /// Specifies whether the tariff is Developer or not.
    /// </summary>
    /// <example>true</example>
    public bool? Developer { get; set; }

    /// <summary>
    /// The tariff ID.
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>
    /// The tariff state.
    /// </summary>
    /// <example>{}</example>
    public TariffState State { get; set; }

    /// <summary>
    /// The tariff due date.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime DueDate { get; set; }

    /// <summary>
    /// The tariff delay due date.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime DelayDueDate { get; set; }

    /// <summary>
    /// The tariff license date.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime LicenseDate { get; set; }

    /// <summary>
    /// The customer ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    public string CustomerId { get; set; }

    /// <summary>
    /// The list of quotas.
    /// </summary>
    /// <example>[{"id": 1, "title": "Basic Plan"}]</example>
    public List<Quota> Quotas { get; set; }
}