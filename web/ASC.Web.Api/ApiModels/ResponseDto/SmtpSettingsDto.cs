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
/// The mail server the portal sends its letters through.
/// </summary>
public class SmtpSettingsDto
{
    /// <summary>
    /// The host name or address of the mail server. On a cloud portal that has saved no relay of its own every
    /// field of this object comes back empty, because the installation's own server is not disclosed - only
    /// `isDefaultSettings` is set there.
    /// </summary>
    /// <example>mail.example.com</example>
    [StringLength(255)]
    public string Host { get; set; }

    /// <summary>
    /// The port the mail server is reached on - conventionally 25 or 587 without encryption from the start, 465
    /// with it. It is empty when no port was stored, in which case the portal falls back to its own default.
    /// </summary>
    /// <example>25</example>
    [Range(1, 65535)]
    public int? Port { get; set; }

    /// <summary>
    /// The address the letters are sent from, which appears in the From header and is what a reply goes to.
    /// </summary>
    /// <example>notify@example.com</example>
    [StringLength(255)]
    public string SenderAddress { get; set; }

    /// <summary>
    /// The name shown beside that address in a recipient's mailbox.
    /// </summary>
    /// <example>Postman</example>
    [StringLength(255)]
    public string SenderDisplayName { get; set; }

    /// <summary>
    /// The account the portal signs in to the mail server as, meaningful only while `enableAuth` is `true`.
    /// </summary>
    /// <example>notify@example.com</example>
    [StringLength(255)]
    public string CredentialsUserName { get; set; }

    /// <summary>
    /// Always empty here: the stored password is never returned, so a client that sends these settings back has
    /// to supply it again rather than echoing what it read.
    /// </summary>
    /// <example></example>
    public string CredentialsUserPassword { get; set; }

    /// <summary>
    /// Whether the connection to the mail server is encrypted.
    /// </summary>
    /// <example>true</example>
    public bool EnableSSL { get; set; }

    /// <summary>
    /// Whether the portal signs in to the mail server at all. While it is `false` the credentials above are
    /// ignored and the server is expected to accept mail unauthenticated.
    /// </summary>
    /// <example>true</example>
    public bool EnableAuth { get; set; }

    /// <summary>
    /// Always `false` here: the flag is accepted when settings are saved but is not stored, so it never comes
    /// back set and says nothing about how the portal authenticates.
    /// </summary>
    /// <example>false</example>
    public bool UseNtlm { get; set; }

    /// <summary>
    /// Whether the portal is still on the mail configuration of the installation rather than on a relay of its
    /// own. `DELETE api/2.0/smtpsettings/smtp` puts it back to `true`, and while it is `true` on a cloud portal
    /// the fields above are blank rather than showing the installation's server.
    /// </summary>
    /// <example>true</example>
    public bool IsDefaultSettings { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class SmtpSettingsDtoMapper
{
    public static partial SmtpSettingsDto MapToDto(this SmtpSettings source);
}