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

namespace ASC.Files.Tests.Tests._01_Files.Recent;

[Trait("Category", "Features")]
[Trait("Feature", "Recent")]
public class DeleteRecentTests(
    AspireAppFixture fixture)
    : RecentTestBase(fixture)
{
    [Fact]
    public async Task DeleteRecent_OwnerFile_RemovesItFromRecentSection()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Delete Recent File.docx", Owner);
        await _filesApi.AddFileToRecentAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);
        await PollRecentUntil(r => r.Files.Any(f => f.Title == file.Title));

        // Act
        await _filesApi.DeleteRecentAsync(new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken);

        // Assert
        var recent = await PollRecentUntil(r => r.Files.All(f => f.Title != file.Title));
        recent.Files.Should().NotContain(f => f.Title == file.Title);
    }

    [Fact]
    public async Task DeleteRecent_MultipleFiles_RemovesAllInOneRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file1 = await CreateFileInMy("Autotest Delete Recent File 1.docx", Owner);
        var file2 = await CreateFileInMy("Autotest Delete Recent File 2.docx", Owner);
        await _filesApi.AddFileToRecentAsync(file1.Id, cancellationToken: TestContext.Current.CancellationToken);
        await _filesApi.AddFileToRecentAsync(file2.Id, cancellationToken: TestContext.Current.CancellationToken);
        await PollRecentUntil(r => r.Files.Any(f => f.Title == file1.Title) && r.Files.Any(f => f.Title == file2.Title));

        // Act
        await _filesApi.DeleteRecentAsync(new BaseBatchRequestDto { FileIds = [new(file1.Id), new(file2.Id)] }, TestContext.Current.CancellationToken);

        // Assert
        var recent = await PollRecentUntil(r => r.Files.All(f => f.Title != file1.Title && f.Title != file2.Title));
        recent.Files.Should().NotContain(f => f.Title == file1.Title);
        recent.Files.Should().NotContain(f => f.Title == file2.Title);
    }

    [Fact]
    public async Task DeleteRecent_FileNotInRecent_IsIdempotent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Delete Recent Not Added.docx", Owner);

        // Act & Assert - the SDK call returns Task<NoContentResultWrapper> with no status to assert
        // beyond a clean completion; a non-2xx would throw ApiException.
        await _filesApi.DeleteRecentAsync(new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken);
    }
}
