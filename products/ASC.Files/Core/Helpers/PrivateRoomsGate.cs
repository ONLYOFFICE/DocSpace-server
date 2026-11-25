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

namespace ASC.Files.Core.Helpers;

/// <summary>
/// The end-to-end encrypted rooms app switches the private rooms on and off for a portal. While it is off, no private
/// room, folder or file of one is listed, served or created; the records stay, so turning the app on brings them back.
/// The key routes of a user are not behind it.
/// </summary>
[Scope]
public class PrivateRoomsGate(AppSettingsService appSettingsService, TenantManager tenantManager, IDaoFactory daoFactory)
{
    public const string AppId = "e2e-rooms";

    // Asked for every private entry a request checks; the answer is kept for the request (this service is scoped), not
    // across requests, so a change of the app is seen by the next request. It is kept with its tenant: a background
    // scope may switch tenants.
    private (int TenantId, bool Enabled)? _enabled;

    public async Task<bool> IsEnabledAsync()
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        if (_enabled is not { } cached || cached.TenantId != tenantId)
        {
            cached = (tenantId, await appSettingsService.GetAppAsync(tenantId, AppId) is { Enabled: true });
            _enabled = cached;
        }

        return cached.Enabled;
    }

    /// <summary>Whether the entry is a private room or lies in one while the app is off.</summary>
    public async Task<bool> IsHiddenAsync<T>(FileEntry<T> entry)
    {
        if (entry == null || await IsEnabledAsync())
        {
            return false;
        }

        return await DocSpaceHelper.IsOrLocatedInPrivateRoomAsync(entry, daoFactory.GetCacheFolderDao<T>());
    }

    /// <summary>Answers 404 for an entry that <see cref="IsHiddenAsync{T}"/> hides.</summary>
    public async Task ThrowIfHiddenAsync<T>(FileEntry<T> entry)
    {
        if (await IsHiddenAsync(entry))
        {
            throw new ItemNotFoundException(entry.FileEntryType == FileEntryType.File
                ? FilesCommonResource.ErrorMessage_FileNotFound
                : FilesCommonResource.ErrorMessage_FolderNotFound);
        }
    }

    /// <summary>Answers 403 to a request for a new private room while the app is off.</summary>
    public async Task DemandEnabledAsync()
    {
        if (!await IsEnabledAsync())
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_PrivateRoomsAppOff);
        }
    }
}
