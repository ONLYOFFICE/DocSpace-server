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
/// The parameters a client hashes a password with before sending it.
/// </summary>
public class PasswordHashSettingsDto
{
    /// <summary>
    /// The length of the hash, in bytes.
    /// </summary>
    /// <example>32</example>
    public int Size { get; init; }

    /// <summary>
    /// The number of PBKDF2 iterations.
    /// </summary>
    /// <example>100000</example>
    public int Iterations { get; init; }

    /// <summary>
    /// The salt the installation hashes passwords with.
    /// </summary>
    /// <example>random_salt_value</example>
    public string Salt { get; init; }
}

/// <summary>
/// The rules a portal name is checked against.
/// </summary>
public class DomainNameRulesDto
{
    /// <summary>
    /// The pattern the portal name has to match.
    /// </summary>
    /// <example>^[a-z0-9]([a-z0-9-]){1,61}[a-z0-9]$</example>
    public string Regex { get; init; }

    /// <summary>
    /// The shortest portal name accepted.
    /// </summary>
    /// <example>6</example>
    public int MinLength { get; init; }

    /// <summary>
    /// The longest portal name accepted.
    /// </summary>
    /// <example>63</example>
    public int MaxLength { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class PortalRulesDtoMapper
{
    public static partial PasswordHashSettingsDto Map(this PasswordHasher source);

    public static partial DomainNameRulesDto Map(this TenantDomainValidator source);
}
