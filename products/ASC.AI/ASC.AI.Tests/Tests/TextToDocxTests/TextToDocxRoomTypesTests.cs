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

namespace ASC.AI.Tests.Tests.TextToDocxTests;

/// <summary>
/// The room types an answer can be saved into, measured against <c>POST /files/{folderId}/file</c> as the oracle:
/// the export goes where the Files API puts a document and is refused where the Files API refuses one. The AI room,
/// whose export lands in Chat outputs, is in <see cref="TextToDocxThreadExportTests"/>. Ported from messages.spec.ts,
/// "AI Messages - text-to-docx into every room type".
/// </summary>
[Trait("Category", "API")]
[Trait("Feature", "AI/TextToDocx")]
public class TextToDocxRoomTypesTests(AspireAppFixture fixture) : TextToDocxPortTestBase(fixture)
{
    [Theory]
    [InlineData(RoomType.PublicRoom)]
    [InlineData(RoomType.VirtualDataRoom)]
    public async Task Export_IntoRoomThatTakesDocuments_AgreesWithTheFilesApi(RoomType roomType)
    {
        var roomId = await CreateRoomOfTypeAsync(roomType);

        // The premise: this room's root takes a document at all (throws otherwise).
        await FilesApi.CreateFileAsync(roomId, new CreateFileRequest($"Control {RandomSuffix()}"), TestContext.Current.CancellationToken);

        var title = $"Exported {RandomSuffix()}";
        await ExportAcceptedAsync(title, "The assistant said hello.", roomId);

        var exported = await WaitForExportedFileAsync(roomId, $"{title}.docx");
        exported.Should().NotBeNull($"no \"{title}.docx\" in the {roomType}");
        exported!.FileExst.Should().Be(".docx");

        (await ReadDocxTextAsync(exported.Id)).Should().Contain("The assistant said hello.");
    }

    /// <summary>
    /// A form filling room's root holds PDF forms: the Files API turns a document away, and the export, aimed at the
    /// same folder and subject to the same rule, answers 202 and writes nothing — the user is told the answer was
    /// saved and it is gone. The export must refuse what the room refuses.
    /// </summary>
    [Fact]
    [Trait("Bug", "83070")]
    public async Task Export_IntoFormFillingRoom_IsRefusedLikeTheFilesApiRefusesIt()
    {
        var roomId = await CreateRoomOfTypeAsync(RoomType.FillingFormsRoom);

        // The premise: this room type refuses a document, and says so. (The Files API delivers that refusal as a 500,
        // a defect of its own — hence the check on the message rather than on the status.)
        var control = await Assert.ThrowsAsync<ApiException>(async () =>
            await FilesApi.CreateFileAsync(roomId, new CreateFileRequest($"Control {RandomSuffix()}"), TestContext.Current.CancellationToken));
        control.ErrorContent?.ToString().Should().Contain("PDF form");

        var title = $"Exported {RandomSuffix()}";
        var result = await TryExportAsync(BuildExport(title, "The assistant said hello.", roomId));

        // The document is nowhere — root and every subfolder. This half stays true after the fix.
        await WaitForExportToSettleAsync(result.StatusCode);

        var landed = await ListFolderFilesAsync(roomId);
        var room = (await FoldersApi.GetFolderByFolderIdAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        foreach (var folder in room.Folders.OfType<FolderDto>())
        {
            landed.AddRange(await ListFolderFilesAsync(folder.Id));
        }

        landed.Select(file => file.Title).Should().NotContain($"{title}.docx");

        // 403 is what this endpoint answers for every other target it may not write to; if the fix lands as a 400
        // instead, this line is the one to update.
        result.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "the export must refuse what the room refuses instead of reporting success and losing the answer");
    }
}
