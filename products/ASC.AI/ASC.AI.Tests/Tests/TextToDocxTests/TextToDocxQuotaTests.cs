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
/// The export writes a real document into real storage — the one write path that does not go through the Files API.
/// Ported from messages.spec.ts, "AI Messages - text-to-docx and the room storage quota".
/// </summary>
[Trait("Category", "API")]
[Trait("Feature", "AI/TextToDocx")]
public class TextToDocxQuotaTests(AspireAppFixture fixture) : TextToDocxPortTestBase(fixture)
{
    /// <summary>
    /// A room whose quota is exhausted answers <c>402 "Room space quota exceeded"</c> through the Files API — and
    /// 202 <c>{"success":true}</c> here, with nothing written: the queue accepts work it cannot do and no one ever
    /// hears about it. The export has to answer the way the Files API answers for the same room.
    /// </summary>
    [Fact]
    [Trait("Bug", "83071")]
    public async Task Export_IntoRoomOutOfQuota_AnswersLikeTheFilesApi()
    {
        await WebApiHttpClient.Authenticate(Owner);
        await SettingsQuotaApi.SaveRoomQuotaSettingsAsync(
            new QuotaSettingsRequestDto(enableQuota: true, defaultQuota: new QuotaSettingsRequestDtoDefaultQuota(1024)),
            TestContext.Current.CancellationToken);

        var roomId = await CreateRoomOfTypeAsync(RoomType.CustomRoom, "Autotest Export Quota Room");

        var rooms = (await FilesQuotaApi.UpdateRoomsQuotaAsync(
            new UpdateRoomsQuotaRequestDto(roomIds: [new DuplicateRequestDtoAllOfFileIds(roomId)], quota: 1024),
            TestContext.Current.CancellationToken)).Response;
        rooms[0].QuotaLimit.Should().Be(1024);

        // 1 KB is under the size of an empty document, so the room is out of quota from the start. That refusal is
        // the premise of the test and the answer the export is measured against.
        var oracle = await Assert.ThrowsAsync<ApiException>(async () =>
            await FilesApi.CreateFileAsync(roomId, new CreateFileRequest($"Oracle {RandomSuffix()}"), TestContext.Current.CancellationToken));
        oracle.ErrorCode.Should().Be(402, "the room has to be out of quota for the Files API before the export can be compared against it");
        oracle.ErrorContent?.ToString().Should().Contain("quota exceeded");

        var title = $"Exported quota {RandomSuffix()}";
        var result = await TryExportAsync(BuildExport(title, "The assistant said hello.", roomId));

        // The quota does hold — nothing is written past it. This half stays true after the fix.
        await WaitForExportToSettleAsync();
        (await ListFolderFilesAsync(roomId)).Select(file => file.Title).Should().NotContain(
            $"{title}.docx",
            "a room that is out of quota must not gain a document");

        ((int)result.StatusCode).Should().Be(
            oracle.ErrorCode,
            "the export has to answer the way the Files API answers for the same room, instead of accepting a job it silently abandons");
    }
}
