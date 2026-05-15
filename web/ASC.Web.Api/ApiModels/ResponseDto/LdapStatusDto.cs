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
/// The status parameters of the synchronization with LDAP server.
/// </summary>
/// <example>
/// {
///   "completed": true,
///   "id": "00000000-0000-0000-0000-000000000001",
///   "status": "InProgress",
///   "error": "Connection timeout",
///   "warning": "Certificate not verified",
///   "percents": 1,
///   "certificateConfirmRequest": "Please verify certificate",
///   "source": "ldap.example.com",
///   "operationType": "Sync"
/// }
/// </example>
public class LdapStatusDto
{
    /// <summary>
    /// Specifies if the LDAP synchronization is completed or not.
    /// </summary>
    /// <example>true</example>
    public bool Completed { get; set; }

    /// <summary>
    /// The LDAP ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    public string Id { get; set; }

    /// <summary>
    /// The LDAP status.
    /// </summary>
    /// <example>InProgress</example>
    public string Status { get; set; }

    /// <summary>
    /// The LDAP error message.
    /// </summary>
    /// <example>Connection timeout</example>
    public string Error { get; set; }

    /// <summary>
    /// The LDAP warning message.
    /// </summary>
    /// <example>Certificate not verified</example>
    public string Warning { get; set; }

    /// <summary>
    /// The percentage of the LDAP operation completion.
    /// </summary>
    /// <example>1</example>
    public int Percents { get; set; }

    /// <summary>
    /// The LDAP certificate confirmation request.
    /// </summary>
    /// <example>Please verify certificate</example>
    public string CertificateConfirmRequest { get; set; }

    /// <summary>
    /// The LDAP source.
    /// </summary>
    /// <example>ldap.example.com</example>
    public string Source { get; set; }

    /// <summary>
    /// The LDAP operation type.
    /// </summary>
    /// <example>Sync</example>
    public string OperationType { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class LdapStatusDtoMapper
{
    public static partial LdapStatusDto MapToDto(this LdapOperationStatus source);
}