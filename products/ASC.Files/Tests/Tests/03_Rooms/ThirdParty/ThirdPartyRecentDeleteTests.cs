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

namespace ASC.Files.Tests.Tests._03_Rooms.ThirdParty;
/// <summary>
/// Removing a file of a third-party (Nextcloud) room from the caller's Recent section through
/// <c>DELETE api/2.0/files/recent</c>, the request the web client sends. Skipped unless Nextcloud is
/// configured in the environment.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Requires", "Nextcloud")]
[Collection("Nextcloud")]
public class ThirdPartyRecentDeleteTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    /// <remarks>
    /// Bug 84528: the call answered 204 and the file stayed in Recent, because only numeric file ids
    /// were passed on and the third-party ones were dropped.
    /// </remarks>
    [Trait("Bug", "84528")]
    [Fact]
    public async Task DeleteRecent_ThirdPartyFile_RemovesFileFromRecent()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, work) = await CreateWorkFolder("TP Recent Delete");
        var file = await CreateThirdPartyFile(work.Id, $"{UniqueTitle("TP Recent Delete")}.docx");

        await _filesApi.AddFileToRecentAsync(file.Id, TestContext.Current.CancellationToken);
        (await PollRecentTitlesUntil(t => t.Contains(file.Title))).Should().Contain(file.Title);

        // Act
        await _filesApi.DeleteRecentAsync(
            new BaseBatchRequestDto(fileIds: [new BaseBatchRequestDtoAllOfFileIds(file.Id)]),
            TestContext.Current.CancellationToken);

        // Assert
        var recent = await PollRecentTitlesUntil(t => !t.Contains(file.Title));
        recent.Should().NotContain(file.Title);
    }

    /// <remarks>
    /// Bug 84528: once third-party ids were passed on, a file deleted in the storage must not fail the
    /// whole call. The storage no longer finds the file, so its entry is not removed, but the call
    /// still succeeds and the section keeps listing without it.
    /// </remarks>
    [Trait("Bug", "84528")]
    [Fact]
    public async Task DeleteRecent_ThirdPartyFileMissingFromStorage_Succeeds()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, work) = await CreateWorkFolder("TP Recent Delete Missing");
        var file = await CreateThirdPartyFile(work.Id, $"{UniqueTitle("TP Recent Delete Missing")}.docx");

        await _filesApi.AddFileToRecentAsync(file.Id, TestContext.Current.CancellationToken);
        (await PollRecentTitlesUntil(t => t.Contains(file.Title))).Should().Contain(file.Title);

        await DeleteThroughAnotherConnection(file.Id);

        // Act
        await _filesApi.DeleteRecentAsync(
            new BaseBatchRequestDto(fileIds: [new BaseBatchRequestDtoAllOfFileIds(file.Id)]),
            TestContext.Current.CancellationToken);

        // Assert
        var recent = await PollRecentTitlesUntil(t => !t.Contains(file.Title));
        recent.Should().NotContain(file.Title);
    }
}
