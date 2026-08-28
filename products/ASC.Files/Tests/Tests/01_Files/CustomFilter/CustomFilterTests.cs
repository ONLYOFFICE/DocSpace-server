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

namespace ASC.Files.Tests.Tests._01_Files.CustomFilter;

/// <summary>
/// Functional behaviour of <c>PUT /files/file/:fileId/customfilter</c>: enabling and disabling the
/// Custom Filter editing mode on a file, and the response for a non-existent file.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class CustomFilterTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SetCustomFilter_Enable_ReturnsEnabledWithAccessibility()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest CustomFilter Room");
        var file = await CreateFile("Autotest CustomFilter.xlsx", room.Id);

        // Act
        var result = (await _filesApi.SetCustomFilterTagAsync(file.Id, new CustomFilterParameters(true), TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Id.Should().Be(file.Id);
        result.CustomFilterEnabled.Should().BeTrue();
        result.ViewAccessibility.WebCustomFilterEditing.Should().BeTrue();
    }

    [Fact]
    public async Task SetCustomFilter_Disable_ClearsFlag()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest CustomFilter Disable Room");
        var file = await CreateFile("Autotest CustomFilter.xlsx", room.Id);
        await _filesApi.SetCustomFilterTagAsync(file.Id, new CustomFilterParameters(true), TestContext.Current.CancellationToken);

        // Act
        var result = (await _filesApi.SetCustomFilterTagAsync(file.Id, new CustomFilterParameters(false), TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Id.Should().Be(file.Id);
        // The API omits `false` values, so a disabled filter simply has no CustomFilterEnabled flag set.
        result.CustomFilterEnabled.Should().NotBe(true);
    }

    [Fact]
    public async Task SetCustomFilter_NonExistentFile_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetCustomFilterTagAsync(999999999, new CustomFilterParameters(true), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }
}
