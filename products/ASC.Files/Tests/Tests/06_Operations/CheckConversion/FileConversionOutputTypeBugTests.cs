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
/// <c>PUT /api/2.0/files/file/{fileId}/checkconversion</c> — <c>outputType</c>. The TypeScript suite
/// marks these <c>BUG-PENDING</c> (no bug number yet): the method documentation says that what the
/// portal cannot produce is answered without work or rejected, but <c>outputType</c> is ignored and an
/// unknown value is accepted.
/// </summary>
[Trait("Category", "Bug")]
[Trait("Feature", "Files")]
public class FileConversionOutputTypeBugTests(
    AspireAppFixture fixture)
    : FileConversionTestBase(fixture)
{
    [Fact]
    public async Task StartFileConversion_FormatThePortalCannotProduce_IsNotSilentlyTurnedIntoADocx()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Convert OutputJunk", myDocsFolderId);
        var id = await Put(dir.Id, "Autotest Convert OutputJunk.rtf", Rtf("Junk format"));
        var before = await FileTitles(dir.Id);

        // Act
        var queued = new List<ConversationResultDto>();
        var rejected = false;

        try
        {
            queued = await Start(id, new CheckConversionRequestDto(startConvert: true, outputType: "zzz"));
        }
        catch (ApiException)
        {
            rejected = true;
        }

        // Assert - answered without work, or rejected; and nothing was made
        if (!rejected)
        {
            queued.Should().BeEmpty("outputType zzz is something the portal cannot produce");
        }

        (await FileTitles(dir.Id)).Should().Equal(before);
    }
}
