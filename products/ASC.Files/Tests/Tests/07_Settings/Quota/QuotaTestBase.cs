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

namespace ASC.Files.Tests.Tests._07_Settings.Quota;

/// <summary>
/// Shared helpers for the room-quota suites: enabling the room-quota portal setting, the byte
/// constants the TypeScript suite ported here uses, and creating a room of a given type without
/// hard-coding a switch in every test class.
/// </summary>
public abstract class QuotaTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    protected const int QuotaMinimalBytes = 104857600; // 100 MB
    protected const int DefaultQuotaRoomBytes = 524288000; // 500 MB

    /// <summary>
    /// Enables the room-quota portal setting with <see cref="DefaultQuotaRoomBytes"/> as the
    /// default quota every new room gets. Requires the owner to already be authenticated on
    /// <c>_webApiClient</c>.
    /// </summary>
    protected async Task EnableRoomQuota(int quotaBytes = DefaultQuotaRoomBytes)
    {
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
            new QuotaSettingsRequestsDto(true, new QuotaSettingsRequestsDtoDefaultQuota(quotaBytes)),
            TestContext.Current.CancellationToken);
    }

    protected async Task<FolderDtoInteger> CreateRoomOfType(RoomType roomType, string title)
    {
        return roomType switch
        {
            RoomType.PublicRoom => await CreatePublicRoom(title),
            RoomType.FillingFormsRoom => await CreateFillingFormsRoom(title),
            RoomType.VirtualDataRoom => await CreateVirtualRoom(title),
            RoomType.CustomRoom => await CreateCustomRoom(title),
            RoomType.EditingRoom => await CreateCollaborationRoom(title),
            _ => throw new ArgumentOutOfRangeException(nameof(roomType), roomType, "No room-quota helper for this room type.")
        };
    }
}
