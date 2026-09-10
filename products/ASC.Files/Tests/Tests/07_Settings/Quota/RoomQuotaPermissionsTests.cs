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
/// Access control for the room-quota endpoints: only the portal owner may change or reset a
/// room's quota. Neither a <see cref="EmployeeType.DocSpaceAdmin"/>, a
/// <see cref="EmployeeType.RoomAdmin"/>, a <see cref="EmployeeType.User"/> nor an unauthenticated
/// caller is allowed to.
/// </summary>
[Trait("Category", "Settings")]
[Trait("Feature", "Quota")]
public class RoomQuotaPermissionsTests(
    AspireAppFixture fixture)
    : QuotaTestBase(fixture)
{
    public enum QuotaAction
    {
        Update,
        Reset
    }

    public static TheoryData<QuotaAction, EmployeeType?> NonOwnerCases =>
        new()
        {
            { QuotaAction.Update, null },
            { QuotaAction.Update, EmployeeType.DocSpaceAdmin },
            { QuotaAction.Update, EmployeeType.RoomAdmin },
            { QuotaAction.Update, EmployeeType.User },
            { QuotaAction.Reset, null },
            { QuotaAction.Reset, EmployeeType.DocSpaceAdmin },
            { QuotaAction.Reset, EmployeeType.RoomAdmin },
            { QuotaAction.Reset, EmployeeType.User },
        };

    [Theory]
    [MemberData(nameof(NonOwnerCases))]
    public async Task NonOwner_CannotChangeRoomQuota(QuotaAction action, EmployeeType? role)
    {
        await _filesClient.Authenticate(Owner);
        await _webApiClient.Authenticate(Owner);
        await EnableRoomQuota();

        var room = await CreateCustomRoom("Autotest Quota Permissions Room " + Guid.NewGuid().ToString()[..8]);

        // The reset endpoint needs a custom quota already set, otherwise resetting it is a no-op
        // regardless of who is calling it.
        if (action == QuotaAction.Reset)
        {
            await _quotaApi.UpdateRoomsQuotaAsync(
                new UpdateRoomsQuotaRequestDtoInteger([new(room.Id)], QuotaMinimalBytes),
                TestContext.Current.CancellationToken);
        }

        if (role is null)
        {
            await _filesClient.Authenticate(null);
        }
        else
        {
            var member = await InviteContact(role.Value);
            await _filesClient.Authenticate(member);
        }

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
        {
            if (action == QuotaAction.Update)
            {
                await _quotaApi.UpdateRoomsQuotaAsync(
                    new UpdateRoomsQuotaRequestDtoInteger([new(room.Id)], QuotaMinimalBytes),
                    TestContext.Current.CancellationToken);
            }
            else
            {
                await _quotaApi.ResetRoomQuotaAsync(
                    new UpdateRoomsRoomIdsRequestDtoInteger([new(room.Id)]),
                    TestContext.Current.CancellationToken);
            }
        });

        exception.ErrorCode.Should().Be(role is null ? 401 : 403);
    }
}
