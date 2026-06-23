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
/// The SMTP settings parameters.
/// </summary>
public class SmtpSettingsDto
{
    /// <summary>
    /// The SMTP host.
    /// </summary>
    /// <example>mail.example.com</example>
    [StringLength(255)]
    public string Host { get; set; }

    /// <summary>
    /// The SMTP port.
    /// </summary>
    /// <example>25</example>
    [Range(1, 65535)]
    public int? Port { get; set; }

    /// <summary>
    /// The sender address.
    /// </summary>
    /// <example>notify@example.com</example>
    [StringLength(255)]
    public string SenderAddress { get; set; }

    /// <summary>
    /// The sender display name.
    /// </summary>
    /// <example>Postman</example>
    [StringLength(255)]
    public string SenderDisplayName { get; set; }

    /// <summary>
    /// The credentials username.
    /// </summary>
    /// <example>notify@example.com</example>
    [StringLength(255)]
    public string CredentialsUserName { get; set; }

    /// <summary>
    /// The credentials user password.
    /// </summary>
    /// <example>example value</example>
    public string CredentialsUserPassword { get; set; }

    /// <summary>
    /// Specifies whether the SSL is enabled or not.
    /// </summary>
    /// <example>true</example>
    public bool EnableSSL { get; set; }

    /// <summary>
    /// Specifies whether the authentication is enabled or not.
    /// </summary>
    /// <example>true</example>
    public bool EnableAuth { get; set; }

    /// <summary>
    /// Specifies whether to use NTLM or not.
    /// </summary>
    /// <example>true</example>
    public bool UseNtlm { get; set; }

    /// <summary>
    /// Specifies if the current settings are default or not.
    /// </summary>
    /// <example>true</example>
    public bool IsDefaultSettings { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class SmtpSettingsDtoMapper
{
    public static partial SmtpSettingsDto MapToDto(this SmtpSettings source);
}