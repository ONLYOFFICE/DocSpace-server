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

namespace ASC.Files.Tests.Tests._07_Settings.Editor;

/// <summary>
/// Shared functional and permission coverage for the files-settings toggle endpoints that all take a
/// <see cref="SettingsRequestDto"/> and mirror the requested "set" value back both in the response and
/// in <see cref="FilesSettingsDto"/>: SetOpenEditorInSameTab, KeepNewFileName, HideConfirmCancelOperation
/// and HideConfirmRoomLifetime are shaped identically - only the endpoint and the reflected field differ,
/// which the derived classes supply.
/// </summary>
public abstract class SettingsToggleTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>Calls the endpoint under test and returns the boolean it reports. A null <paramref name="set"/>
    /// sends the request without a body - the SDK skips serialising the DTO entirely when it is null,
    /// which is exactly the "no body" case the TS suite sends over raw HTTP.</summary>
    protected abstract Task<bool> ToggleAsync(bool? set);

    /// <summary>The endpooint's route, for the raw no-body case.</summary>
    protected abstract string TogglePath { get; }

    /// <summary>Picks out of <see cref="FilesSettingsDto"/> the field this endpoint's toggle is reflected in.</summary>
    protected abstract bool ReflectedField(FilesSettingsDto settings);

    public static TheoryData<EmployeeType?> AllowedRoles =>
    [
        null, // the portal owner
        EmployeeType.DocSpaceAdmin,
        EmployeeType.RoomAdmin,
        EmployeeType.User,
        EmployeeType.Guest
    ];

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Toggle_ReturnsSetValue(bool set)
    {
        // Act
        var result = await ToggleAsync(set);

        // Assert
        result.Should().Be(set);
    }

    [Fact]
    public async Task Toggle_OnThenOff_ChangesBothWays()
    {
        (await ToggleAsync(true)).Should().BeTrue();
        (await ToggleAsync(false)).Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Toggle_RepeatedCall_IsIdempotent(bool set)
    {
        // Arrange
        await ToggleAsync(set);

        // Act
        var result = await ToggleAsync(set);

        // Assert
        result.Should().Be(set);
    }

    [Fact]
    public async Task Toggle_WithoutBody_BindsToDefaultSetFalse()
    {
        // Act - sent raw: the generated client drops the Content-Type header together with the body,
        // so a bodyless typed call is refused by ASP.NET with 415 before the controller runs. An
        // empty JSON object binds to a default SettingsRequestDto, whose "Set" defaults to false.
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await _filesClient.PutAsync(TogglePath, content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Toggle_StateIsReflectedInGetFilesSettings(bool set)
    {
        // Arrange
        await ToggleAsync(set);

        // Act
        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        ReflectedField(settings).Should().Be(set);
    }

    [Fact]
    public async Task Toggle_IsIsolatedPerUser()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        await ToggleAsync(false);

        // Act
        await _filesClient.Authenticate(user);
        await ToggleAsync(true);

        await _filesClient.Authenticate(Owner);
        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken)).Response;

        // Assert - the owner's own setting was untouched by the member's change
        ReflectedField(settings).Should().BeFalse();
    }

    [Fact]
    public async Task Toggle_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await ToggleAsync(true));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [MemberData(nameof(AllowedRoles))]
    public async Task Toggle_EveryRole_CanChangeOwnSetting(EmployeeType? employeeType)
    {
        // Arrange
        if (employeeType != null)
        {
            var member = await InviteMember(employeeType.Value);
            await _filesClient.Authenticate(member);
        }

        // Act
        var result = await ToggleAsync(true);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Toggle_TerminatedUser_Unauthorized()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);

        // Sign the user in while they are still active: the test is about the token going dead once
        // the account is terminated, so it has to be issued before the status change.
        await _filesClient.Authenticate(user);

        await _peopleClient.Authenticate(Owner);
        await _userStatusApi.UpdateUserStatusAsync(
            EmployeeStatus.Terminated,
            new UpdateMembersRequestDto([user.Id], resendAll: false),
            TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await ToggleAsync(true));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
