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

namespace ASC.Files.Tests.Tests._07_Settings.Storage;

/// <summary>
/// <c>PUT /files/storeforcesave</c> and <c>PUT /files/updateifexist</c> are compatibility stubs: whatever is sent,
/// they store nothing and answer the constant false, for every role.
/// </summary>
[Trait("Category", "Settings")]
public class CompatibilityStubsTests(
    AspireAppFixture fixture)
    : StorageSettingsTestBase(fixture)
{
    public static TheoryData<EmployeeType> Roles =>
    [
        EmployeeType.DocSpaceAdmin,
        EmployeeType.User,
        EmployeeType.Guest
    ];

    [Theory]
    [MemberData(nameof(Roles))]
    public async Task StoreForcesave_RepeatedCalls_AlwaysAnswerFalse(EmployeeType role)
    {
        // Arrange
        await SignInAs(role);

        // Act
        var first = await _filesSettingsApi.StoreForcesaveAsync(TestContext.Current.CancellationToken);
        var second = await _filesSettingsApi.StoreForcesaveAsync(TestContext.Current.CancellationToken);

        // Assert - a stub that stored anything would flip the answer on the second call
        first.Response.Should().BeFalse();
        second.Response.Should().BeFalse();
        (await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken)).Response.StoreForcesave.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(Roles))]
    public async Task UpdateFileIfExist_EitherValue_AnswersFalse(EmployeeType role)
    {
        // Arrange
        await SignInAs(role);

        // Act
        var on = await _filesSettingsApi.UpdateFileIfExistAsync(new SettingsRequestDto { Set = true }, TestContext.Current.CancellationToken);
        var off = await _filesSettingsApi.UpdateFileIfExistAsync(new SettingsRequestDto { Set = false }, TestContext.Current.CancellationToken);
        var onAgain = await _filesSettingsApi.UpdateFileIfExistAsync(new SettingsRequestDto { Set = true }, TestContext.Current.CancellationToken);

        // Assert
        on.Response.Should().BeFalse();
        off.Response.Should().BeFalse();
        onAgain.Response.Should().BeFalse();
    }

    [Fact]
    public async Task CompatibilityStubs_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var storeForcesave = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.StoreForcesaveAsync(TestContext.Current.CancellationToken));
        var updateIfExist = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.UpdateFileIfExistAsync(new SettingsRequestDto { Set = true }, TestContext.Current.CancellationToken));

        // Assert
        storeForcesave.ErrorCode.Should().Be(401);
        updateIfExist.ErrorCode.Should().Be(401);
    }

    private async Task SignInAs(EmployeeType role)
    {
        var user = role == EmployeeType.Guest ? await InviteGuest() : await InviteContact(role);
        await _filesClient.Authenticate(user);
    }
}
