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


namespace ASC.Files.Tests.Tests._06_Operations.CheckConversion;

/// <summary>
/// The target formats of <c>startFileConversion</c> are the portal's conversion map, published as
/// <c>extsConvertible</c> in the Files settings: a target from the map is queued (also without the leading
/// dot), a target outside it is refused with 400 and nothing is made.
/// </summary>
[Trait("Feature", "Files")]
public class FileConversionTargetFormatTests(
    AspireAppFixture fixture)
    : FileConversionTestBase(fixture)
{
    private async Task<List<string>> TargetsOfRtf()
    {
        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken)).Response;

        settings.ExtsConvertible.Should().ContainKey(".rtf");

        return settings.ExtsConvertible[".rtf"];
    }

    [Fact]
    public async Task StartFileConversion_TargetOutsideTheConversionMap_Returns400AndMakesNothing()
    {
        // Arrange
        (await TargetsOfRtf()).Should().NotContain(".zzz");
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Convert Target Outside", myDocsFolderId);
        var id = await Put(dir.Id, "Autotest Convert Target Outside.rtf", Rtf("Outside"));
        var before = await FileTitles(dir.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Start(id, new CheckConversionRequestDto(startConvert: true, outputType: "zzz")));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await FileTitles(dir.Id)).Should().Equal(before);
    }

    [Fact]
    public async Task StartFileConversion_DotlessTargetFromTheConversionMap_IsQueued()
    {
        // Arrange
        var targets = await TargetsOfRtf();
        targets.Should().Contain(".pdf");
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Convert Target Map", myDocsFolderId);
        var id = await Put(dir.Id, "Autotest Convert Target Map.rtf", Rtf("In the map"));

        // Act
        var queued = await Start(id, new CheckConversionRequestDto(startConvert: true, outputType: "pdf"));

        // Assert
        queued.Should().NotBeEmpty();
        queued.Should().OnlyContain(r => string.IsNullOrEmpty(r.Error));
    }
}
