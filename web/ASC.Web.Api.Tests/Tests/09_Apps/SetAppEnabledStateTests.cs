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
/// PUT /api/2.0/apps/{id}/enabled — switching an application on or off for the portal. The missing-field
/// body is covered by <see cref="SetAppEnabledTests"/> (bug 84498).
/// </summary>
[Trait("Category", "Apps")]
public class SetAppEnabledStateTests(
    AspireAppFixture fixture)
    : AppsTestBase(fixture)
{
    [Fact]
    public async Task SetEnabled_Owner_FlipsTheFlagAndItIsReadBack()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        var before = await SnapshotAsync(id);

        // Act
        var result = await AppsApi.SetEnabledWithHttpInfoAsync(id, new SetAppEnabledRequest(!before.Enabled), Ct);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Response.Id.Should().Be(id);
        result.Data.Response.Enabled.Should().Be(!before.Enabled);
        (await SnapshotAsync(id)).Should().Be(before with { Enabled = !before.Enabled });
        (await ListStatesAsync()).Single(app => app.Id == id).Enabled.Should().Be(!before.Enabled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetEnabled_RepeatedRequest_IsIdempotent(bool enabled)
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await SaveEnabledAsync(id, enabled);
        var first = await SnapshotAsync(id);

        // Act
        var result = await AppsApi.SetEnabledWithHttpInfoAsync(id, new SetAppEnabledRequest(enabled), Ct);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Response.Enabled.Should().Be(enabled);
        (await SnapshotAsync(id)).Should().Be(first);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, false)]
    public async Task SetEnabled_Sequence_LastRequestDecidesTheFinalState(bool first, bool second, bool third)
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];

        foreach (var enabled in new[] { first, second, third })
        {
            // Act
            await SaveEnabledAsync(id, enabled);

            // Assert
            (await SnapshotAsync(id)).Enabled.Should().Be(enabled);
        }
    }

    [Fact]
    public async Task SetEnabled_Switching_DoesNotTouchTheSettingsDocument()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        const string document = """{"keep":["me",1],"nested":{"ok":true}}""";
        await SaveSettingsAsync(id, document);

        // Act
        await SaveEnabledAsync(id, false);
        var off = await ReadSettingsAsync(id);
        await SaveEnabledAsync(id, true);
        var on = await ReadSettingsAsync(id);

        // Assert
        off.Should().Be(Canonical(document));
        on.Should().Be(Canonical(document));
    }

    [Fact]
    public async Task SetEnabled_OneApplication_LeavesTheOthersAndTheCountAlone()
    {
        // Arrange
        var ids = await PickAppsAsync(2);
        await SaveSettingsAsync(ids[1], """{"other":"app"}""");
        var before = await ListStatesAsync();

        // Act
        await SaveEnabledAsync(ids[0], !before.Single(app => app.Id == ids[0]).Enabled);

        // Assert
        var after = await ListStatesAsync();
        after.Select(app => app.Id).Should().Equal(before.Select(app => app.Id));
        after.Where(app => app.Id != ids[0]).Should().Equal(before.Where(app => app.Id != ids[0]));
    }

    [Fact]
    public async Task SetEnabled_UnknownId_ReturnsNotFoundAndCreatesNothing()
    {
        // Arrange
        var before = await ListStatesAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.SetEnabledAsync("no-such-app", new SetAppEnabledRequest(false), Ct));

        // Assert
        exception.ErrorCode.Should().Be(404);
        (await ListStatesAsync()).Should().Equal(before);
    }

    // Raw HTTP: each body is one the typed request cannot carry (wrong-typed value, malformed JSON, a
    // JSON null body). The "enabled field missing" case is SetAppEnabled_BodyWithoutEnabled_... (bug 84498).
    [Theory]
    [InlineData("""{"enabled":null}""")]
    [InlineData("""{"enabled":"true"}""")]
    [InlineData("""{"enabled":"false"}""")]
    [InlineData("""{"enabled":1}""")]
    [InlineData("""{"enabled":0}""")]
    [InlineData("""{"enabled":[]}""")]
    [InlineData("""{"enabled":{}}""")]
    [InlineData("""{"enabled":tru""")]
    [InlineData("null")]
    public async Task SetEnabled_UnusableBody_ReturnsBadRequestAndChangesNothing(string body)
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await SaveSettingsAsync(id, """{"before":"rejected call"}""");
        var before = await SnapshotAsync(id);

        // Act
        var (status, _) = await SendAsync(HttpMethod.Put, $"{AppPath(id)}/enabled", body);

        // Assert
        status.Should().Be(HttpStatusCode.BadRequest);
        (await SnapshotAsync(id)).Should().Be(before);
    }

    // Transport-level: the generated client always sends a body.
    [Fact]
    public async Task SetEnabled_RequestWithoutBody_ReturnsUnsupportedMediaTypeAndChangesNothing()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        var before = await SnapshotAsync(id);

        // Act
        var (status, _) = await SendAsync(HttpMethod.Put, $"{AppPath(id)}/enabled");

        // Assert
        status.Should().Be(HttpStatusCode.UnsupportedMediaType);
        (await SnapshotAsync(id)).Should().Be(before);
    }
}
