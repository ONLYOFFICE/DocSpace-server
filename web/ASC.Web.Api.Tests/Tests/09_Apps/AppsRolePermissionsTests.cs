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


namespace ASC.Web.Api.Tests.Tests._09_Apps;

/// <summary>
/// /api/2.0/apps by role. Reads are open to every authenticated member; writes need the right to edit
/// portal settings, which DocSpace admins have and room admins, users and guests lack. The order of
/// checks for a write is: media type (415), body binding (400), permission (403), id lookup (404) — so a
/// caller without the right gets 403 for an unknown id and nothing reveals whether an application exists.
/// </summary>
[Trait("Category", "Apps")]
public class AppsRolePermissionsTests(
    AspireAppFixture fixture)
    : AppsTestBase(fixture)
{
    private const string Marker = """{"savedBy":"owner"}""";

    [Fact]
    public async Task Owner_ReadsAndWritesThroughAllFiveMethods()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        var before = await SnapshotAsync(id);
        const string document = """{"writtenBy":"owner","list":[1,2]}""";

        // Act
        var list = await AppsApi.GetAllWithHttpInfoAsync(Ct);
        var one = await AppsApi.GetWithHttpInfoAsync(id, Ct);
        var settings = await AppsApi.GetSettingsWithHttpInfoAsync(id, Ct);
        var toggled = await AppsApi.SetEnabledWithHttpInfoAsync(id, new SetAppEnabledRequest(!before.Enabled), Ct);
        await SaveSettingsAsync(id, document);

        // Assert
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        list.Data.Response.Single(app => app.Id == id).Enabled.Should().Be(before.Enabled);
        one.StatusCode.Should().Be(HttpStatusCode.OK);
        one.Data.Response.Id.Should().Be(id);
        settings.StatusCode.Should().Be(HttpStatusCode.OK);
        toggled.StatusCode.Should().Be(HttpStatusCode.OK);
        toggled.Data.Response.Enabled.Should().Be(!before.Enabled);
        (await SnapshotAsync(id)).Should().Be(new AppState(id, !before.Enabled, Canonical(document)));
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin, true)]
    [InlineData(EmployeeType.RoomAdmin, false)]
    [InlineData(EmployeeType.User, false)]
    [InlineData(EmployeeType.Guest, false)]
    public async Task Member_ReadsEnabledApplication_AndWritesOnlyWithPortalSettingsRight(EmployeeType role, bool canWrite)
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await SaveSettingsAsync(id, Marker);
        var before = await SnapshotAsync(id);

        var member = await InviteMember(role);
        await _webApiClient.Authenticate(member);

        // Every authenticated role can read
        (await ListStatesAsync()).Single(app => app.Id == id).Should().Be(before);
        (await AppsApi.GetWithHttpInfoAsync(id, Ct)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadSettingsAsync(id)).Should().Be(Canonical(Marker));

        // setEnabled
        await AssertWriteAsync(canWrite,
            () => AppsApi.SetEnabledAsync(id, new SetAppEnabledRequest(!before.Enabled), Ct));
        (await SnapshotAsync(id)).Enabled.Should().Be(canWrite ? !before.Enabled : before.Enabled);

        // setSettings
        await AssertWriteAsync(canWrite,
            () => AppsApi.SetSettingsAsync(id, new SetAppSettingsRequest(new { savedBy = role.ToString() }), Ct));
        (await ReadSettingsAsync(id)).Should().Be(canWrite ? Canonical($"{{\"savedBy\":\"{role}\"}}") : Canonical(Marker));

        // resetting the settings
        await AssertWriteAsync(canWrite,
            () => AppsApi.SetSettingsAsync(id, new SetAppSettingsRequest(), Ct));
        (await ReadSettingsAsync(id)).Should().Be(canWrite ? null : Canonical(Marker));
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin, true)]
    [InlineData(EmployeeType.RoomAdmin, false)]
    [InlineData(EmployeeType.User, false)]
    [InlineData(EmployeeType.Guest, false)]
    public async Task Member_ReadsDisabledApplication_AndWritesOnlyWithPortalSettingsRight(EmployeeType role, bool canWrite)
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        const string marker = """{"disabledMarker":"x"}""";
        await SaveSettingsAsync(id, marker);
        await SaveEnabledAsync(id, false);

        var member = await InviteMember(role);
        await _webApiClient.Authenticate(member);

        // Reads still return the disabled state and settings
        var disabled = new AppState(id, false, Canonical(marker));
        (await ListStatesAsync()).Single(app => app.Id == id).Should().Be(disabled);
        (await SnapshotAsync(id)).Should().Be(disabled);
        (await ReadSettingsAsync(id)).Should().Be(Canonical(marker));

        // setSettings
        const string written = """{"disabledMarker":"rewritten"}""";
        await AssertWriteAsync(canWrite,
            () => AppsApi.SetSettingsAsync(id, new SetAppSettingsRequest(new { disabledMarker = "rewritten" }), Ct));
        var afterSettings = await SnapshotAsync(id);
        afterSettings.Settings.Should().Be(canWrite ? Canonical(written) : Canonical(marker));
        afterSettings.Enabled.Should().BeFalse();

        // setEnabled
        await AssertWriteAsync(canWrite,
            () => AppsApi.SetEnabledAsync(id, new SetAppEnabledRequest(true), Ct));
        (await SnapshotAsync(id)).Enabled.Should().Be(canWrite);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task User_WriteIsDenied_ForEnabledAndDisabledApplicationAlike(bool enabled)
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await SaveEnabledAsync(id, enabled);
        var before = await SnapshotAsync(id);

        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        var toggle = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.SetEnabledAsync(id, new SetAppEnabledRequest(!enabled), Ct));
        var save = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.SetSettingsAsync(id, new SetAppSettingsRequest(new { nope = true }), Ct));

        // Assert
        toggle.ErrorCode.Should().Be(403);
        save.ErrorCode.Should().Be(403);
        (await SnapshotAsync(id)).Should().Be(before);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task Member_WithoutRight_GetsForbiddenBeforeTheIdIsLookedUp(EmployeeType role)
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await SaveSettingsAsync(id, Marker);
        var before = await SnapshotAsync(id);

        var member = await InviteMember(role);
        await _webApiClient.Authenticate(member);

        // Act
        // An unknown id is 403, never 404
        var unknownToggle = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.SetEnabledAsync("no-such-app", new SetAppEnabledRequest(false), Ct));
        var unknownSave = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.SetSettingsAsync("no-such-app", new SetAppSettingsRequest(new { }), Ct));

        // The refusal does not reveal whether the application exists
        var real = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.SetEnabledAsync(id, new SetAppEnabledRequest(!before.Enabled), Ct));

        // Assert
        unknownToggle.ErrorCode.Should().Be(403);
        unknownSave.ErrorCode.Should().Be(403);
        real.ErrorCode.Should().Be(403);
        ErrorMessageOf(unknownToggle.ErrorContent!.ToString()!).Should().Be(ErrorMessageOf(real.ErrorContent!.ToString()!));

        // A body that cannot even be bound is rejected by the framework first, with the same 400 for every caller
        foreach (var (path, body) in new[]
        {
            ("enabled", """{"enabled":"true"}"""),
            ("enabled", """{"enabled":tru"""),
            ("settings", """{"settings":{"a":"""),
            ("settings", "5"),
            // A missing "enabled" is a validation error since bug 84498 was fixed, so it never reaches the permission check
            ("enabled", "{}"),
        })
        {
            var (status, _) = await SendAsync(HttpMethod.Put, $"{AppPath(id)}/{path}", body);
            status.Should().Be(HttpStatusCode.BadRequest, $"{path}: {body}");
        }

        // A body that binds but is semantically empty reaches the permission check and is refused
        foreach (var (path, body) in new[]
        {
            ("settings", """{"a":5}"""),
            ("settings", """{"settings":null}"""),
        })
        {
            var (status, _) = await SendAsync(HttpMethod.Put, $"{AppPath(id)}/{path}", body);
            status.Should().Be(HttpStatusCode.Forbidden, $"{path}: {body}");
        }

        (await SnapshotAsync(id)).Should().Be(before);
    }

    private static async Task AssertWriteAsync<T>(bool canWrite, Func<Task<T>> write)
    {
        if (canWrite)
        {
            await write();
            return;
        }

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await write());
        exception.ErrorCode.Should().Be(403);
    }
}
