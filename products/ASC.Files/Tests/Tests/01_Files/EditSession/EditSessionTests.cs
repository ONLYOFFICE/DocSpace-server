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

namespace ASC.Files.Tests.Tests._01_Files.EditSession;

[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class EditSessionTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    public static TheoryData<bool> FileLocationIsRoomOrMyDocuments =>
    [
        true,
        false
    ];

    /// <summary>
    /// A single request checked against every field the response carries: id, created, expired,
    /// location and bytes_total, for a file living either in a room or in "My Documents".
    /// </summary>
    [Theory]
    [MemberData(nameof(FileLocationIsRoomOrMyDocuments))]
    public async Task CreateEditSession_ValidFile_ReturnsFullSession(bool inRoom)
    {
        await _filesClient.Authenticate(Owner);

        var file = inRoom
            ? await CreateFile("Autotest Edit Session File", (await CreateCustomRoom("Autotest Edit Session Room")).Id)
            : await CreateFileInMy("Autotest Edit Session My File", Owner);

        const long fileSize = 5120;
        var beforeRequest = DateTime.UtcNow;

        var session = (await _filesApi.CreateEditSessionAsync(file.Id, fileSize, TestContext.Current.CancellationToken)).Response;

        session.Success.Should().BeTrue();
        session.Data.Should().NotBeNull();
        session.Data.Id.Should().NotBeNullOrEmpty();
        session.Data.Created.Should().BeOnOrAfter(beforeRequest.AddSeconds(-1));
        session.Data.Expired.Should().BeAfter(DateTime.UtcNow);
        session.Data.Location.Should().NotBeNullOrEmpty();
        session.Data.BytesTotal.Should().Be(fileSize);
    }

    [Fact]
    public async Task CreateEditSession_WithoutFileSize_ReturnsSuccess()
    {
        await _filesClient.Authenticate(Owner);

        var file = await CreateFileInMy("Autotest Edit Session No Size", Owner);

        var session = (await _filesApi.CreateEditSessionAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        session.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CreateEditSession_NonExistentFile_NotFound()
    {
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CreateEditSessionAsync(999999999, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }
}
