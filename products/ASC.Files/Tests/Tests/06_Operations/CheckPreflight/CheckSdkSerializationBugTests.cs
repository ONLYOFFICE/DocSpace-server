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

namespace ASC.Files.Tests.Tests._06_Operations.CheckPreflight;

/// <summary>
/// BUG 84727: the generated SDKs could not send the ids of the two preflight endpoints. The ids were
/// described as <c>oneOf</c> integer/string, so every generator built a wrapper class for them, and the
/// C# client put the wrapper's <c>ToString()</c> on the query ("class ... { ActualInstance: 5 }" instead of
/// <c>5</c>); the TypeScript one spread it with <c>Object.entries</c>. The server found no destination and
/// answered 400. Fixed in the OpenAPI document: an integer-or-string id in a query is now published as a
/// plain string, which is what the query carries anyway, and the server still reads a numeric text as a
/// portal id. These cases call the typed client directly, as a real caller does.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class CheckSdkSerializationBugTests(
    AspireAppFixture fixture)
    : CheckPreflightTestBase(fixture)
{
    [Theory]
    [Trait("Bug", "84727")]
    [InlineData(PreflightKind.Move)]
    [InlineData(PreflightKind.CheckDestFolder)]
    public async Task TypedCall_SendsTheDestinationId_AndTheServerAcceptsIt(PreflightKind kind)
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest Check Sdk.docx", myDocsFolderId);
        var room = await CreateCustomRoom($"Autotest Check Sdk Room {kind}");
        await CreateFile("Autotest Check Sdk.docx", room.Id);

        var destination = room.Id.ToString(CultureInfo.InvariantCulture);
        List<string> fileIds = [file.Id.ToString(CultureInfo.InvariantCulture)];

        // Act
        if (kind == PreflightKind.Move)
        {
            var clashes = (await _filesOperationsApi.CheckMoveOrCopyBatchItemsAsync(
                fileIds: fileIds, destFolderId: destination, cancellationToken: TestContext.Current.CancellationToken)).Response;

            // Assert
            clashes.Should().ContainSingle().Which.Title.Should().Be("Autotest Check Sdk.docx");
        }
        else
        {
            var verdict = (await _filesOperationsApi.CheckMoveOrCopyDestFolderAsync(
                fileIds: fileIds, destFolderId: destination, cancellationToken: TestContext.Current.CancellationToken)).Response;

            // Assert
            verdict.Result.Should().Be(CheckDestFolderResult.AllAllowed);
        }
    }
}
