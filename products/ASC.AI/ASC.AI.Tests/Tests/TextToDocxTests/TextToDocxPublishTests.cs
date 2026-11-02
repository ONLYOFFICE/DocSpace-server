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

[Trait("Category", "API")]
[Trait("Feature", "AI/TextToDocx")]
public class TextToDocxPublishTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Publish_MyDocuments_ReturnsAccepted()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();

        var response = await _aiExport.AiExportTextToDocxWithHttpInfoAsync(BuildRequest(folderId), TestContext.Current.CancellationToken);

        // The conversion runs in the background; the route only accepts the job.
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        response.Data.Success.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Publish_NonPositiveFolderId_ReturnsBadRequest(int folderId)
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _aiExport.AiExportTextToDocxAsync(BuildRequest(folderId), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task Publish_NonexistentFolderId_ReturnsNotFound()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _aiExport.AiExportTextToDocxAsync(BuildRequest(999999999), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    private static AiExportTextToDocxRequest BuildRequest(int folderId) =>
        new(
            title: $"document-{Guid.NewGuid():N}",
            content: "# Heading\n\nGenerated content.",
            folderId: new AiExportTextToDocxRequestFolderId((decimal)folderId));
}
