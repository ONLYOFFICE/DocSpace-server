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
/// The storage the portal keeps its data in, or serves its static content from.
/// </summary>
public class StorageSettingsDto
{
    /// <summary>
    /// The storage module, or `null` when the built-in storage is used.
    /// </summary>
    /// <example>S3</example>
    public string Module { get; init; }

    /// <summary>
    /// The connection properties of the module, as they were sent.
    /// </summary>
    /// <example>{"region": "eu-central-1", "bucket": "tenant-files"}</example>
    public Dictionary<string, string> Props { get; init; }

    /// <summary>
    /// When the settings were last stored.
    /// </summary>
    /// <example>2025-01-01T12:00:00Z</example>
    public DateTime LastModified { get; init; }
}

/// <summary>
/// The state of the portal's storage encryption.
/// </summary>
public class EncryptionSettingsDto
{
    /// <summary>
    /// Whether the storage is encrypted, decrypted, or on its way to either.
    /// </summary>
    /// <example>0</example>
    public EncryptionStatus Status { get; init; }

    /// <summary>
    /// Whether the users are notified when the operation starts and ends.
    /// </summary>
    /// <example>true</example>
    public bool NotifyUsers { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class StorageSettingsDtoMapper
{
    public static partial StorageSettingsDto Map(this StorageSettings source);

    public static partial StorageSettingsDto Map(this CdnStorageSettings source);

    public static partial EncryptionSettingsDto Map(this EncryptionSettings source);
}
