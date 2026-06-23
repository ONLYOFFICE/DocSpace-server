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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// The company white label settings.
/// </summary>
public class CompanyWhiteLabelSettingsDto
{
    /// <summary>
    /// The company name.
    /// </summary>
    /// <example>My Own Corporation</example>
    public required string CompanyName { get; set; }

    /// <summary>
    /// The company site.
    /// </summary>
    /// <example>https://www.example.com</example>
    public required string Site { get; set; }

    /// <summary>
    /// The company email address.
    /// </summary>
    /// <example>contact@example.com</example>
    [EmailAddress]
    public required string Email { get; set; }

    /// <summary>
    /// The company address.
    /// </summary>
    /// <example>123 Business St, New York, NY 10001</example>
    public required string Address { get; set; }

    /// <summary>
    /// The company phone number.
    /// </summary>
    /// <example>+1-800-555-0123</example>
    public required string Phone { get; set; }

    /// <summary>
    /// Specifies if a company is a licensor or not.
    /// </summary>
    /// <example>false</example>
    public required bool IsLicensor { get; set; }

    /// <summary>
    /// Specifies if the About page is visible or not.
    /// </summary>
    /// <example>false</example>
    public required bool HideAbout { get; set; }

    /// <summary>
    /// Specifies if these settings are default or not.
    /// </summary>
    /// <example>true</example>
    public required bool IsDefault { get; set; }
}

[Scope]
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public partial class CompanyWhiteLabelSettingsDtoMapper(CompanyWhiteLabelSettingsHelper companyWhiteLabelSettingsHelper)
{
    [MapPropertyFromSource(nameof(CompanyWhiteLabelSettingsDto.IsDefault), Use = nameof(GetIsDefault))]
    public partial CompanyWhiteLabelSettingsDto Map(CompanyWhiteLabelSettings source);

    private bool GetIsDefault(CompanyWhiteLabelSettings source)
    {
        return companyWhiteLabelSettingsHelper.IsDefault(source);
    }
}
