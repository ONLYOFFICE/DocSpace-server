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
/// PUT /files/hideconfirmconvert - hides the confirmation dialog for saving the file copy in the
/// original format when converting a file. Unlike the other toggle endpoints, <c>save</c> is not
/// mirrored back: the product only ever hides the dialog (never re-shows it) and the handler always
/// returns <c>true</c> once the corresponding flag (save or open) has been set - see
/// <c>FilesSettings.HideConfirmConvert</c>.
/// </summary>
[Trait("Category", "Settings")]
public class HideConfirmConvertTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private async Task<bool> HideConfirmConvertAsync(bool? save)
    {
        var dto = save.HasValue ? new HideConfirmConvertRequestDto { Save = save.Value } : null;

        return (await _filesSettingsApi.HideConfirmConvertAsync(dto, TestContext.Current.CancellationToken)).Response;
    }

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
    public async Task HideConfirmConvert_AlwaysReturnsTrue(bool save)
    {
        // Act
        var result = await HideConfirmConvertAsync(save);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HideConfirmConvert_ReflectsSaveOrOpenFlag(bool save)
    {
        // Act
        await HideConfirmConvertAsync(save);

        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        (save ? settings.HideConfirmConvertSave : settings.HideConfirmConvertOpen).Should().BeTrue();
    }

    [Fact]
    public async Task HideConfirmConvert_SaveAndOpenAreIndependentFlags()
    {
        // Act
        await HideConfirmConvertAsync(true);
        await HideConfirmConvertAsync(false);

        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken)).Response;

        // Assert - setting one never clears the other
        settings.HideConfirmConvertSave.Should().BeTrue();
        settings.HideConfirmConvertOpen.Should().BeTrue();
    }

    [Fact]
    public async Task HideConfirmConvert_WithoutBody_StillReturnsTrue()
    {
        // Act - sent raw: the generated client drops the Content-Type header together with the body,
        // so a bodyless typed call is refused by ASP.NET with 415 before the controller runs.
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await _filesClient.PutAsync("api/2.0/files/hideconfirmconvert", content, TestContext.Current.CancellationToken);

        // Assert - the handler always returns true, regardless of "save".
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("response").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task HideConfirmConvert_IsIsolatedPerUser()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);

        // Act - only the member hides the "save" confirmation
        await _filesClient.Authenticate(user);
        await HideConfirmConvertAsync(true);

        await _filesClient.Authenticate(Owner);
        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken)).Response;

        // Assert - the owner's own setting was untouched
        settings.HideConfirmConvertSave.Should().BeFalse();
    }

    [Fact]
    public async Task HideConfirmConvert_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await HideConfirmConvertAsync(true));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [MemberData(nameof(AllowedRoles))]
    public async Task HideConfirmConvert_EveryRole_CanChangeOwnSetting(EmployeeType? employeeType)
    {
        // Arrange
        if (employeeType != null)
        {
            var member = await InviteMember(employeeType.Value);
            await _filesClient.Authenticate(member);
        }

        // Act
        var result = await HideConfirmConvertAsync(true);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HideConfirmConvert_TerminatedUser_Unauthorized()
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
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await HideConfirmConvertAsync(true));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
