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

namespace ASC.People.Tests.Tests._07_Quota;

/// <summary>
/// Access control for <c>PUT /people/userquota</c> and <c>PUT /people/resetquota</c>: both demand
/// <c>SecurityConstants.EditPortalSettings</c>, which only the owner and a DocSpace admin hold, plus
/// the one validation case where the requested quota exceeds the portal's total storage.
/// </summary>
public class QuotaPermissionsTests(AspireAppFixture fixture) : QuotaTestBase(fixture)
{
    [Fact]
    public async Task UpdateUserQuota_RoomAdmin_ChangesOwnQuotaLimit_ShouldThrowAccessDenied()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _peopleClient.Authenticate(roomAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _peopleQuotaApi.UpdateUserQuotaAsync(
                new UpdateMembersQuotaRequestDto([roomAdmin.Id], new UpdateMembersQuotaRequestDtoQuota((int)QuotaMinimalBytes)),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task UpdateUserQuota_RoomAdmin_ChangesOtherUsersQuotaLimit_ShouldThrowAccessDenied()
    {
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var user = await InviteContact(EmployeeType.User);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _peopleClient.Authenticate(roomAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _peopleQuotaApi.UpdateUserQuotaAsync(
                new UpdateMembersQuotaRequestDto(
                    [Owner.Id, docSpaceAdmin.Id, user.Id],
                    new UpdateMembersQuotaRequestDtoQuota((int)QuotaMinimalBytes)),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task UpdateUserQuota_User_ChangesOtherUsersQuotaLimit_ShouldThrowAccessDenied()
    {
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _peopleQuotaApi.UpdateUserQuotaAsync(
                new UpdateMembersQuotaRequestDto(
                    [Owner.Id, docSpaceAdmin.Id, roomAdmin.Id],
                    new UpdateMembersQuotaRequestDtoQuota((int)QuotaMinimalBytes)),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task UpdateUserQuota_Guest_ChangesOtherUsersQuotaLimit_ShouldThrowAccessDenied()
    {
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        await _peopleClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _peopleQuotaApi.UpdateUserQuotaAsync(
                new UpdateMembersQuotaRequestDto(
                    [Owner.Id, docSpaceAdmin.Id, roomAdmin.Id, user.Id],
                    new UpdateMembersQuotaRequestDtoQuota((int)QuotaMinimalBytes)),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task UpdateUserQuota_Anonymous_ShouldThrowUnauthorized()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _peopleQuotaApi.UpdateUserQuotaAsync(
                new UpdateMembersQuotaRequestDto([roomAdmin.Id], new UpdateMembersQuotaRequestDtoQuota((int)QuotaMinimalBytes)),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task ResetUsersQuota_RoomAdmin_ResetsOwnQuotaLimit_ShouldThrowAccessDenied()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _peopleQuotaApi.UpdateUserQuotaAsync(
            new UpdateMembersQuotaRequestDto([roomAdmin.Id], new UpdateMembersQuotaRequestDtoQuota((int)QuotaMinimalBytes)),
            TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(roomAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _peopleQuotaApi.ResetUsersQuotaAsync(
                new UpdateMembersQuotaRequestDto([roomAdmin.Id]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task ResetUsersQuota_RoomAdmin_ResetsOtherUsersQuotaLimit_ShouldThrowAccessDenied()
    {
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var user = await InviteContact(EmployeeType.User);

        await _peopleQuotaApi.UpdateUserQuotaAsync(
            new UpdateMembersQuotaRequestDto(
                [Owner.Id, docSpaceAdmin.Id, user.Id],
                new UpdateMembersQuotaRequestDtoQuota((int)QuotaMinimalBytes)),
            TestContext.Current.CancellationToken);

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _peopleClient.Authenticate(roomAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _peopleQuotaApi.ResetUsersQuotaAsync(
                new UpdateMembersQuotaRequestDto([Owner.Id, docSpaceAdmin.Id, user.Id]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task ResetUsersQuota_User_ResetsOtherUsersQuotaLimit_ShouldThrowAccessDenied()
    {
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _peopleQuotaApi.UpdateUserQuotaAsync(
            new UpdateMembersQuotaRequestDto(
                [Owner.Id, docSpaceAdmin.Id, roomAdmin.Id],
                new UpdateMembersQuotaRequestDtoQuota((int)QuotaMinimalBytes)),
            TestContext.Current.CancellationToken);

        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _peopleQuotaApi.ResetUsersQuotaAsync(
                new UpdateMembersQuotaRequestDto([Owner.Id, docSpaceAdmin.Id, roomAdmin.Id]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task ResetUsersQuota_Guest_ResetsOtherUsersQuotaLimit_ShouldThrowAccessDenied()
    {
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);

        await _peopleQuotaApi.UpdateUserQuotaAsync(
            new UpdateMembersQuotaRequestDto(
                [Owner.Id, docSpaceAdmin.Id, roomAdmin.Id, user.Id],
                new UpdateMembersQuotaRequestDtoQuota((int)QuotaMinimalBytes)),
            TestContext.Current.CancellationToken);

        var guest = await InviteGuest();
        await _peopleClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _peopleQuotaApi.ResetUsersQuotaAsync(
                new UpdateMembersQuotaRequestDto([Owner.Id, docSpaceAdmin.Id, roomAdmin.Id, user.Id]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task ResetUsersQuota_Anonymous_ShouldThrowUnauthorized()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _peopleQuotaApi.UpdateUserQuotaAsync(
            new UpdateMembersQuotaRequestDto([roomAdmin.Id], new UpdateMembersQuotaRequestDtoQuota((int)QuotaMinimalBytes)),
            TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _peopleQuotaApi.ResetUsersQuotaAsync(
                new UpdateMembersQuotaRequestDto([roomAdmin.Id]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    /// <summary>
    /// Regression coverage for bug 80301: the owner asks for a per-user quota above the portal's
    /// total storage and the API is expected to reject it with 400, not silently accept it. This
    /// host has no billing plan (unbounded total storage), so the ceiling is reproduced
    /// deterministically through the standalone-only tenant quota setting rather than the TS
    /// suite's <c>999999999999999</c>-byte constant, which both exceeds this host's unbounded
    /// storage (no plan enabled) and the SDK's <c>int</c>-typed quota wrapper.
    /// </summary>
    [Fact]
    [Trait("Bug", "80301")]
    public async Task UpdateUserQuota_OwnerRequestsMoreThanTotalStorage_ShouldThrowBadRequest()
    {
        const long cappedTotalStorageBytes = 1_048_576; // 1 MB

        await CapTotalStorageAsync(cappedTotalStorageBytes);

        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _peopleQuotaApi.UpdateUserQuotaAsync(
                new UpdateMembersQuotaRequestDto([docSpaceAdmin.Id], new UpdateMembersQuotaRequestDtoQuota((int)QuotaMinimalBytes)),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain(
            "Failed to set quota per user. The entered value is greater than the total storage.");
    }
}
