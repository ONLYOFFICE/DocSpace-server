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

namespace ASC.Files.Core;

/// <summary>
/// The encrypted file key issued to one user.
/// </summary>
public class FileKeys
{
    public FileKeys() { }

    /// <summary>
    /// The identifier of the user the file key was issued to.
    /// </summary>
    /// <example>9924256B-447C-4F19-9dbd-8ad8c39e8ff5</example>
    public Guid UserId { get; set; }

    /// <summary>
    /// The identifier of the key pair the file key is encrypted for.
    /// </summary>
    /// <example>9924256B-447C-4F19-9dbd-8ad8c39e8ff5</example>
    public Guid PublicKeyId { get; set; }

    /// <summary>
    /// The file key, encrypted with the public key of the pair.
    /// </summary>
    /// <example>U2FsdGVkX1+Lm3s...</example>
    public string PrivateKeyEnc { get; set; }

    /// <summary>
    /// The identifier of the portal the file belongs to.
    /// </summary>
    /// <example>1</example>
    public int TenantId { get; set; }

    /// <summary>
    /// The identifier of the file the key unlocks.
    /// </summary>
    /// <example>9846</example>
    public int FileId { get; set; }

    /// <summary>
    /// The date and time when the file key was issued.
    /// </summary>
    /// <example>2025-01-01T00:00:00</example>
    public DateTime CreateOn { get; set; }
}

public class FileKeyData
{
    public Guid UserId { get; set; }

    public Guid PublicKeyId { get; set; }

    public string PrivateKeyEnc { get; set; }
}


[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class FileKeysMapper
{
    public static partial IQueryable<FileKeys> Project(this IQueryable<DbFileKeys> source);
}
