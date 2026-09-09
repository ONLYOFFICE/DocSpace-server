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
/// <c>PUT/GET /files/settings/autocleanup</c> - the trash bin auto-clearing setting, a per-user
/// preference combining an on/off flag with a gap. Both endpoints are covered here since they only
/// make sense read together.
/// </summary>
[Trait("Category", "Settings")]
public class AutoCleanupTests(
    AspireAppFixture fixture)
    : StorageSettingsTestBase(fixture)
{
    #region PUT /files/settings/autocleanup

    [Theory]
    [InlineData(DateToAutoCleanUp.OneWeek)]
    [InlineData(DateToAutoCleanUp.TwoWeeks)]
    [InlineData(DateToAutoCleanUp.OneMonth)]
    [InlineData(DateToAutoCleanUp.ThirtyDays)]
    [InlineData(DateToAutoCleanUp.TwoMonths)]
    [InlineData(DateToAutoCleanUp.ThreeMonths)]
    public async Task ChangeAutomaticallyCleanUp_EnablesWithGap(DateToAutoCleanUp gap)
    {
        // Act
        var response = await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(
            new AutoCleanupRequestDto { Set = true, Gap = gap }, TestContext.Current.CancellationToken);

        // Assert
        response.Response.IsAutoCleanUp.Should().BeTrue();
        response.Response.Gap.Should().Be(gap);
    }

    [Fact]
    public async Task ChangeAutomaticallyCleanUp_Disables()
    {
        // Arrange
        await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(
            new AutoCleanupRequestDto { Set = true, Gap = DateToAutoCleanUp.OneWeek }, TestContext.Current.CancellationToken);

        // Act
        var response = await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(
            new AutoCleanupRequestDto { Set = false }, TestContext.Current.CancellationToken);

        // Assert
        response.Response.IsAutoCleanUp.Should().BeFalse();
    }

    [Fact]
    public async Task ChangeAutomaticallyCleanUp_TogglesOnAndOff()
    {
        // Act & Assert
        var enabled = await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(
            new AutoCleanupRequestDto { Set = true, Gap = DateToAutoCleanUp.OneMonth }, TestContext.Current.CancellationToken);
        enabled.Response.IsAutoCleanUp.Should().BeTrue();
        enabled.Response.Gap.Should().Be(DateToAutoCleanUp.OneMonth);

        var disabled = await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(
            new AutoCleanupRequestDto { Set = false }, TestContext.Current.CancellationToken);
        disabled.Response.IsAutoCleanUp.Should().BeFalse();
    }

    [Fact]
    public async Task ChangeAutomaticallyCleanUp_ChangesGapWhileEnabled()
    {
        // Arrange
        await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(
            new AutoCleanupRequestDto { Set = true, Gap = DateToAutoCleanUp.OneWeek }, TestContext.Current.CancellationToken);

        // Act
        var response = await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(
            new AutoCleanupRequestDto { Set = true, Gap = DateToAutoCleanUp.ThreeMonths }, TestContext.Current.CancellationToken);

        // Assert
        response.Response.IsAutoCleanUp.Should().BeTrue();
        response.Response.Gap.Should().Be(DateToAutoCleanUp.ThreeMonths);
    }

    [Fact]
    public async Task ChangeAutomaticallyCleanUp_EnablingTwiceWithSameGapIsIdempotent()
    {
        // Arrange
        await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(
            new AutoCleanupRequestDto { Set = true, Gap = DateToAutoCleanUp.TwoWeeks }, TestContext.Current.CancellationToken);

        // Act
        var response = await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(
            new AutoCleanupRequestDto { Set = true, Gap = DateToAutoCleanUp.TwoWeeks }, TestContext.Current.CancellationToken);

        // Assert
        response.Response.IsAutoCleanUp.Should().BeTrue();
        response.Response.Gap.Should().Be(DateToAutoCleanUp.TwoWeeks);
    }

    [Fact]
    public async Task ChangeAutomaticallyCleanUp_EnablingWithoutGap_Succeeds()
    {
        // Act
        var response = await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(
            new AutoCleanupRequestDto { Set = true }, TestContext.Current.CancellationToken);

        // Assert
        response.Response.IsAutoCleanUp.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeAutomaticallyCleanUp_GapWithoutSet_UpdatesGap()
    {
        // Act
        var response = await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(
            new AutoCleanupRequestDto { Gap = DateToAutoCleanUp.OneMonth }, TestContext.Current.CancellationToken);

        // Assert
        response.Response.Gap.Should().Be(DateToAutoCleanUp.OneMonth);
    }

    [Fact]
    public async Task ChangeAutomaticallyCleanUp_NoBody_ReturnsBoolean()
    {
        // Act - sent raw: the generated client drops the Content-Type header together with the body,
        // so a bodyless typed call is refused by ASP.NET with 415 before the controller runs.
        using var response = await SendRawEmptyBodyPut("api/2.0/files/settings/autocleanup");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("response").GetProperty("isAutoCleanUp").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
    }

    [Fact]
    public async Task ChangeAutomaticallyCleanUp_EnabledStateIsReflectedInGetFilesSettings()
    {
        // Arrange
        await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(
            new AutoCleanupRequestDto { Set = true, Gap = DateToAutoCleanUp.TwoMonths }, TestContext.Current.CancellationToken);

        // Act
        var settings = await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        settings.Response.AutomaticallyCleanUp.IsAutoCleanUp.Should().BeTrue();
        settings.Response.AutomaticallyCleanUp.Gap.Should().Be(DateToAutoCleanUp.TwoMonths);
    }

    [Fact]
    public async Task ChangeAutomaticallyCleanUp_DisabledStateIsReflectedInGetFilesSettings()
    {
        // Arrange
        await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(
            new AutoCleanupRequestDto { Set = false }, TestContext.Current.CancellationToken);

        // Act
        var settings = await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        settings.Response.AutomaticallyCleanUp.IsAutoCleanUp.Should().BeFalse();
    }

    #endregion

    #region GET /files/settings/autocleanup

    [Fact]
    public async Task GetAutomaticallyCleanUp_ReturnsCurrentSetting()
    {
        // Act
        var response = await _filesSettingsApi.GetAutomaticallyCleanUpAsync(TestContext.Current.CancellationToken);

        // Assert
        response.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAutomaticallyCleanUp_ReflectsEnabledState()
    {
        // Arrange
        await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(
            new AutoCleanupRequestDto { Set = true, Gap = DateToAutoCleanUp.TwoWeeks }, TestContext.Current.CancellationToken);

        // Act
        var response = await _filesSettingsApi.GetAutomaticallyCleanUpAsync(TestContext.Current.CancellationToken);

        // Assert
        response.Response.IsAutoCleanUp.Should().BeTrue();
        response.Response.Gap.Should().Be(DateToAutoCleanUp.TwoWeeks);
    }

    [Fact]
    public async Task GetAutomaticallyCleanUp_ReflectsDisabledState()
    {
        // Arrange
        await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(
            new AutoCleanupRequestDto { Set = false }, TestContext.Current.CancellationToken);

        // Act
        var response = await _filesSettingsApi.GetAutomaticallyCleanUpAsync(TestContext.Current.CancellationToken);

        // Assert
        response.Response.IsAutoCleanUp.Should().BeFalse();
    }

    [Fact]
    public async Task GetAutomaticallyCleanUp_ReflectsUpdatedGap()
    {
        // Arrange
        await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(
            new AutoCleanupRequestDto { Set = true, Gap = DateToAutoCleanUp.ThreeMonths }, TestContext.Current.CancellationToken);

        // Act
        var response = await _filesSettingsApi.GetAutomaticallyCleanUpAsync(TestContext.Current.CancellationToken);

        // Assert
        response.Response.Gap.Should().Be(DateToAutoCleanUp.ThreeMonths);
    }

    [Fact]
    public async Task GetAutomaticallyCleanUp_IsIsolatedPerUser()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(
            new AutoCleanupRequestDto { Set = true, Gap = DateToAutoCleanUp.OneWeek }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(
            new AutoCleanupRequestDto { Set = true, Gap = DateToAutoCleanUp.ThreeMonths }, TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(Owner);
        var response = await _filesSettingsApi.GetAutomaticallyCleanUpAsync(TestContext.Current.CancellationToken);

        // Assert - the user's change must not leak into the owner's own setting.
        response.Response.IsAutoCleanUp.Should().BeTrue();
        response.Response.Gap.Should().Be(DateToAutoCleanUp.OneWeek);
    }

    #endregion
}
