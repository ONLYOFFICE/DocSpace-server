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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Webplugins;

/// <summary>
/// PUT /api/2.0/settings/webplugins/{name} — enabling or disabling an uploaded web plugin.
/// </summary>
[Trait("Category", "Settings")]
public class UpdateWebPluginTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    // Same reserved-prefix routing collision as DeleteWebPluginTests: a plugin named "files-*"
    // never reaches the web plugins controller, so disabling it 404s instead of succeeding.
    [Trait("Bug", "83425")]
    [Fact]
    public async Task UpdateWebPlugin_NameWithFilesPrefix_DisablesPlugin()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        const string pluginName = "files-autotest";

        using var uploadResponse = await WebPluginsTestData.UploadPluginAsync(
            _webApiClient, pluginName, TestContext.Current.CancellationToken);
        uploadResponse.IsSuccessStatusCode.Should().BeTrue();

        // NOTE: the TS test passes `settings: null`; WebPluginRequests's generated constructor
        // throws client-side on a null `settings` (a required, non-nullable parameter), so an
        // empty JSON object is used instead — the disabled-prefix bug being asserted here is
        // unrelated to what `settings` contains.
        var request = new WebPluginRequests(enabled: false, settings: "{}");

        // Act
        var result = await _webpluginsApi.UpdateWebPluginWithHttpInfoAsync(
            pluginName, request, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
