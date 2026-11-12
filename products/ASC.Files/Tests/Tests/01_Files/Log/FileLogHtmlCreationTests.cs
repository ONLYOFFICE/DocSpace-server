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

namespace ASC.Files.Tests.Tests._01_Files.Log;

/// <summary>GET /files/file/{id}/log for files created through <c>POST /files/:folderId/html</c>.</summary>
[Trait("Category", "Files")]
public class FileLogHtmlCreationTests(
    AspireAppFixture fixture)
    : FileLogTestBase(fixture)
{
    /// <summary>
    /// A docx created through createFile has its FileCreated entry at once, but an HTML file created
    /// through createHtmlFile had an empty log (still empty 26 s later): the content was stored through the uploader,
    /// which writes no audit entry, and nothing after it did. Fixed by recording the file as created (and sending the
    /// socket event) after the content is stored.
    /// </summary>
    [Fact]
    [Trait("Bug", "84773")]
    public async Task GetFileHistory_HtmlFileCreatedThroughCreateHtmlFile_HasCreationEntry()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Log Html " + Guid.NewGuid().ToString()[..8]);
        var file = (await _filesApi.CreateHtmlFileAsync(
            room.Id, new CreateTextOrHtmlFileRequest("Autotest Log Html", "<p/>"), TestContext.Current.CancellationToken)).Response;

        // Act
        var history = await PollFileHistoryAsync(file.Id, TimeSpan.FromSeconds(20));

        // Assert
        history.Should().NotBeEmpty();
    }

    /// <summary>
    /// BUG 84773, text-file side: createTextFile stores its content through the same path as createHtmlFile and
    /// left the log just as empty. Fixed together with the HTML case.
    /// </summary>
    [Fact]
    [Trait("Bug", "84773")]
    public async Task GetFileHistory_TextFileCreatedThroughCreateTextFile_HasCreationEntry()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Log Text " + Guid.NewGuid().ToString()[..8]);
        var file = (await _filesApi.CreateTextFileAsync(
            room.Id, new CreateTextOrHtmlFileRequest("Autotest Log Text", "plain text"), TestContext.Current.CancellationToken)).Response;

        // Act
        var history = await PollFileHistoryAsync(file.Id, TimeSpan.FromSeconds(20));

        // Assert
        history.Should().NotBeEmpty();
    }
}
