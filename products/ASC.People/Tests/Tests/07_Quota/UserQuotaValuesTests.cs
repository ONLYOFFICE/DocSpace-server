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
/// <c>PUT /people/userquota</c> and <c>PUT /people/resetquota</c>: what a negative value stores, and what happens to
/// system accounts in the id list.
/// </summary>
public class UserQuotaValuesTests(AspireAppFixture fixture) : QuotaTestBase(fixture)
{
    // The built-in guest system account (ASC.Core.Configuration.Constants.Guest).
    private static readonly Guid _systemGuestId = new("712D9EC3-5D2B-4b13-824F-71F00191DCCA");

    [Fact]
    public async Task UpdateUserQuota_NegativeValue_RemovesThePersonalLimit()
    {
        // Arrange
        await EnableUserQuotaAsync(DefaultQuotaUserBytes);
        var user = await InviteContact(EmployeeType.User);

        // Act
        var result = await _peopleQuotaApi.UpdateUserQuotaAsync(
            new UpdateMembersQuotaRequestDto([user.Id], new UpdateMembersQuotaRequestDtoQuota(-1)),
            TestContext.Current.CancellationToken);

        // Assert: no limit, instead of the portal default of 500 MB
        result.Response.Should().ContainSingle();
        result.Response![0].QuotaLimit.Should().Be(-1);
    }

    [Fact]
    public async Task UpdateUserQuota_SystemAccountInTheList_IsDropped()
    {
        // Arrange
        await EnableUserQuotaAsync(DefaultQuotaUserBytes);
        var user = await InviteContact(EmployeeType.User);

        // Act
        var result = await _peopleQuotaApi.UpdateUserQuotaAsync(
            new UpdateMembersQuotaRequestDto([_systemGuestId, user.Id], new UpdateMembersQuotaRequestDtoQuota((int)QuotaMinimalBytes)),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().ContainSingle();
        result.Response![0].Id.Should().Be(user.Id);
        result.Response[0].QuotaLimit.Should().Be(QuotaMinimalBytes);
    }

    [Fact]
    public async Task ResetUsersQuota_SystemAccountInTheList_IsDropped()
    {
        // Arrange
        await EnableUserQuotaAsync(DefaultQuotaUserBytes);
        var user = await InviteContact(EmployeeType.User);

        // Act
        var result = await _peopleQuotaApi.ResetUsersQuotaAsync(
            new UpdateMembersQuotaRequestDto([_systemGuestId, user.Id]),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().ContainSingle();
        result.Response![0].Id.Should().Be(user.Id);
        result.Response[0].QuotaLimit.Should().Be(DefaultQuotaUserBytes);
    }
}
