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
/// BUG 84727: the generated SDK cannot send the ids of the two preflight endpoints. The ids are
/// <c>oneOf</c> wrappers (a number for a portal folder, a string for a third-party one) and
/// <c>ClientUtils.ParameterToString</c> writes such a wrapper with <c>Convert.ToString</c>, so the
/// query carries "class CheckMoveOrCopyBatchItemsDestFolderIdParameter { ActualInstance: 5 }" instead of
/// <c>5</c>; the server finds no destination and answers 400. The TypeScript SDK fails the same way
/// (it spreads the id with <c>Object.entries</c>). Every other preflight test works around it with a
/// raw request (see <see cref="CheckPreflightTestBase"/>); these go through the typed client, as a
/// real caller does, with a numeric and with a string destination id, and are red until it is fixed.
/// </summary>
[Trait("Category", "Bug")]
[Trait("Feature", "Files")]
public class CheckSdkSerializationBugTests(
    AspireAppFixture fixture)
    : CheckPreflightTestBase(fixture)
{
    [Theory]
    [Trait("Bug", "84727")]
    [InlineData(PreflightKind.Move, false)]
    [InlineData(PreflightKind.Move, true)]
    [InlineData(PreflightKind.CheckDestFolder, false)]
    [InlineData(PreflightKind.CheckDestFolder, true)]
    public async Task TypedCall_SendsTheDestinationId_AndTheServerAcceptsIt(PreflightKind kind, bool destinationAsString)
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest Check Sdk.docx", myDocsFolderId);
        var room = await CreateCustomRoom($"Autotest Check Sdk Room {kind} {destinationAsString}");
        await CreateFile("Autotest Check Sdk.docx", room.Id);

        // Premise: the server accepts exactly this request when it is built by hand.
        var control = await Preflight(kind, fileIds: [file.Id], destFolderId: room.Id);
        control.Status.Should().Be(HttpStatusCode.OK);

        CheckMoveOrCopyBatchItemsDestFolderIdParameter destination = destinationAsString
            ? new(room.Id.ToString(CultureInfo.InvariantCulture))
            : new(room.Id);
        List<CheckMoveOrCopyBatchItemsFolderIdsParameterInner> fileIds = [new(file.Id)];

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
