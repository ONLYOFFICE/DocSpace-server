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
/// PUT /api/2.0/apps/{id}/settings with bodies the endpoint cannot use: the call fails and the previously
/// saved document survives. Raw HTTP throughout — the typed client can only send a well-formed object with
/// a <c>settings</c> member. An absent member means "no document", exactly like an explicit null.
/// </summary>
[Trait("Category", "Apps")]
public class SetAppSettingsInvalidBodyTests(
    AspireAppFixture fixture)
    : AppsTestBase(fixture)
{
    private const string Survivor = """{"survives":true,"list":[1,2]}""";

    [Theory]
    [InlineData("""{"settings":{"a":""")]
    [InlineData("""{"settings":{"a":1}""")]
    [InlineData("""{"settings":{"a":1,}}""")]
    [InlineData("""{settings:{"a":1}}""")]
    [InlineData("5")]
    [InlineData("\"text\"")]
    [InlineData("[1,2]")]
    [InlineData("null")]
    [InlineData("")]
    public async Task SetSettings_UnusableBody_ReturnsBadRequestAndTheSavedDocumentSurvives(string body)
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await SaveSettingsAsync(id, Survivor);
        var before = await SnapshotAsync(id);

        // Act
        var (status, _) = await SendAsync(HttpMethod.Put, $"{AppPath(id)}/settings", body);

        // Assert
        status.Should().Be(HttpStatusCode.BadRequest);
        (await ReadSettingsAsync(id)).Should().Be(Canonical(Survivor));
        (await SnapshotAsync(id)).Should().Be(before);
    }

    [Fact]
    public async Task SetSettings_RequestWithoutBody_ReturnsUnsupportedMediaTypeAndTheSavedDocumentSurvives()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await SaveSettingsAsync(id, Survivor);

        // Act
        var (status, _) = await SendAsync(HttpMethod.Put, $"{AppPath(id)}/settings");

        // Assert
        status.Should().Be(HttpStatusCode.UnsupportedMediaType);
        (await ReadSettingsAsync(id)).Should().Be(Canonical(Survivor));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"survives":false}""")]
    public async Task SetSettings_BodyWithoutSettingsMember_ResetsTheDocumentLikeNull(string body)
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await SaveSettingsAsync(id, Survivor);
        var enabled = (await SnapshotAsync(id)).Enabled;

        // Act
        var (status, responseBody) = await SendAsync(HttpMethod.Put, $"{AppPath(id)}/settings", body);

        // Assert
        status.Should().Be(HttpStatusCode.OK, responseBody);
        ResponseOf(responseBody)!.Value.GetProperty("id").GetString().Should().Be(id);
        (await ReadSettingsAsync(id)).Should().BeNull();
        (await SnapshotAsync(id)).Enabled.Should().Be(enabled);
    }
}
