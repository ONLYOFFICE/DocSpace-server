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
/// A Recent entry whose third-party file is gone from the storage must not break opening another
/// document: the editor configuration lists the caller's Recent files, and the storage answers for
/// the missing one with nothing. Skipped unless Nextcloud is configured in the environment.
/// </summary>
/// <remarks>
/// The file is removed through a room on a second connection to the same account, so the Recent mark made
/// through the room's connection stays: deleting through the room would take the mark with it.
/// Each connection caches the storage's answers for a minute, so the test waits that out before
/// opening the document.
/// </remarks>
[Trait("Category", "Rooms")]
[Trait("Requires", "Nextcloud")]
[Collection("Nextcloud")]
public class ThirdPartyRecentMissingFileTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    /// <remarks>
    /// Bug 84528: the editor configuration answered 500 (NullReferenceException) for a document of
    /// any editable type while Recent held a WebDAV file deleted in the storage, because the
    /// provider's null reached the type filter of the third-party listing.
    /// </remarks>
    [Trait("Bug", "84528")]
    [Fact]
    public async Task OpenEditFile_RecentHoldsThirdPartyFileMissingFromStorage_OpensDocument()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, work) = await CreateWorkFolder("TP Recent Missing");
        var missing = await CreateThirdPartyFile(work.Id, $"{UniqueTitle("TP Recent Missing")}.docx");

        await _filesApi.AddFileToRecentAsync(missing.Id, TestContext.Current.CancellationToken);
        (await PollRecentTitlesUntil(t => t.Contains(missing.Title))).Should().Contain(missing.Title);

        await DeleteThroughAnotherConnection(missing.Id);

        var document = await CreateFileInMy("Autotest Recent Missing Document.docx", Owner);
        var spreadsheet = await CreateFileInMy("Autotest Recent Missing Spreadsheet.xlsx", Owner);

        // Without this wait the room's connection still answers from its cache with the deleted
        // file and the editor opens fine, so the test would pass while the defect is there.
        await WaitForProviderCacheToExpire();

        // Act
        var failures = new List<string>();

        foreach (var file in new[] { document, spreadsheet })
        {
            try
            {
                await _filesApi.OpenEditFileAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);
            }
            catch (ApiException e)
            {
                failures.Add($"{file.Title}: {e.ErrorCode} {e.ErrorContent}");
            }
        }

        // Assert
        failures.Should().BeEmpty();
    }
}
