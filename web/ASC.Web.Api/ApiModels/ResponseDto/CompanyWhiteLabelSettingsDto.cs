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
/// The vendor details the About page and the notification letters print, shared by the whole installation.
/// </summary>
public class CompanyWhiteLabelSettingsDto
{
    /// <summary>
    /// The vendor name the About page shows and the letters sign off with. Until details are saved it holds
    /// whatever the installation ships as its built-in vendor, and it is empty on an installation that ships none.
    /// </summary>
    /// <example>My Own Corporation</example>
    public required string CompanyName { get; set; }

    /// <summary>
    /// The address the vendor name links to, as an absolute URL with its scheme. Empty under the same conditions
    /// as `companyName`.
    /// </summary>
    /// <example>https://www.example.com</example>
    public required string Site { get; set; }

    /// <summary>
    /// The mailbox the About page offers for reaching the vendor. It is not the portal's own support address, and
    /// it is empty under the same conditions as `companyName`.
    /// </summary>
    /// <example>contact@example.com</example>
    [EmailAddress]
    public required string Email { get; set; }

    /// <summary>
    /// The postal address of the vendor as one free-form line, in the shape it was saved in - no structure is
    /// imposed on it.
    /// </summary>
    /// <example>123 Business St, New York, NY 10001</example>
    public required string Address { get; set; }

    /// <summary>
    /// The telephone number of the vendor in the shape it was saved in, with no dialling format enforced.
    /// </summary>
    /// <example>+1-800-555-0123</example>
    public required string Phone { get; set; }

    /// <summary>
    /// Whether these details are those of the licensor of the product itself rather than of a reseller. Saving
    /// through `POST api/2.0/settings/rebranding/company` always clears it, so only details that came with the
    /// installation can report `true`.
    /// </summary>
    /// <example>false</example>
    public required bool IsLicensor { get; set; }

    /// <summary>
    /// Whether the About page is hidden from the interface. A plan that does not include branding cannot switch it
    /// on: the value is stored as `false` in that case, so it can come back different from what was saved.
    /// </summary>
    /// <example>false</example>
    public required bool HideAbout { get; set; }

    /// <summary>
    /// Whether every field above still matches the installation's built-in vendor details. It turns `false` as
    /// soon as one of them is saved differently and `true` again after
    /// `DELETE api/2.0/settings/rebranding/company`.
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
