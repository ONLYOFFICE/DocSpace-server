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
namespace ASC.Web.Api.Tests.Tests._02_Settings.Storage;

/// <summary>
/// PUT /api/2.0/settings/storage and /storage/cdn. Only the refusals are exercised: a module that is not
/// configured on the host is rejected before anything is written, and an empty body fails model binding before
/// the handler runs.
/// </summary>
[Trait("Category", "Settings")]
public class StorageUpdateTests(AspireAppFixture fixture) : StorageTestBase(fixture)
{
    [Fact]
    public async Task UpdateStorage_UnconfiguredModule_ThrowsBadRequest()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Storage.UpdateStorageAsync(UnconfiguredModuleRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task UpdateCdnStorage_UnconfiguredModule_ThrowsBadRequest()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Storage.UpdateCdnStorageAsync(UnconfiguredModuleRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    // A missing body cannot be expressed by the typed client, so this goes raw.
    [Theory]
    [InlineData("api/2.0/settings/storage")]
    [InlineData("api/2.0/settings/storage/cdn")]
    public async Task UpdateStorage_EmptyBody_ReturnsBadRequest(string path)
    {
        // Act
        using var response = await _webApi.PutRawAsync(path, string.Empty, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
