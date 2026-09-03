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

namespace ASC.Files.Tests.Tests._04_Security.Ssrf;

/// <summary>
/// GET /filehandler.ashx?action=create&amp;fileuri=... fetches the given URL server-side and saves
/// the response body as a file in the caller's "My Documents" folder. The handler is not part of the
/// OpenAPI document (<c>[ApiExplorerSettings(IgnoreApi = true)]</c>-style HTTP handler), so it is
/// called over raw HTTP through <c>_filesClient</c>.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Ssrf")]
public class FileHandlerSsrfTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task CreateFile_LoopbackFileUri_DoesNotCreateFile()
    {
        // Arrange
        var title = $"ssrf-loopback-{Guid.NewGuid():N}.txt";

        // Act
        await RequestCreateFromUri("http://127.0.0.1:9999/ssrf-canary", title);

        // Assert
        var myFolder = (await _foldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        myFolder.Files.Should().NotContain(f => f.Title == title);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "82548")]
    public async Task CreateFile_LinkLocalMetadataFileUri_ShouldBeRejected()
    {
        // Arrange
        var title = $"ssrf-imds-{Guid.NewGuid():N}.txt";

        // Act
        await RequestCreateFromUri("http://169.254.169.254/latest/meta-data/", title);

        // Assert
        var myFolder = (await _foldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        myFolder.Files.Should().NotContain(f => f.Title == title,
            "the server must not fetch link-local / cloud metadata addresses on behalf of the caller");
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "82548")]
    public async Task CreateFile_InternalClusterServiceFileUri_ShouldBeRejected()
    {
        // Arrange
        var title = $"ssrf-k8s-{Guid.NewGuid():N}.txt";

        // Act
        await RequestCreateFromUri("http://files.docspace.svc.cluster.local:5050/health", title);

        // Assert
        var myFolder = (await _foldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        myFolder.Files.Should().NotContain(f => f.Title == title,
            "the server must not fetch internal/cluster-only addresses on behalf of the caller");
    }

    /// <summary>
    /// <c>filehandler.ashx</c> is a raw HTTP handler with no OpenAPI document, so it has no SDK
    /// signature; the SDK carve-out in the tests rule applies. <c>response=message</c> makes the
    /// handler answer with an <c>ok:</c>/<c>error:</c> text body instead of a redirect, which is all
    /// that is needed here - the assertion is on whether the file was created, not on this response.
    /// </summary>
    private async Task RequestCreateFromUri(string fileUri, string title)
    {
        var query = "filehandler.ashx?action=create" +
            $"&fileuri={Uri.EscapeDataString(fileUri)}" +
            $"&title={Uri.EscapeDataString(title)}" +
            "&response=message";

        using var response = await _filesClient.GetAsync(query, TestContext.Current.CancellationToken);
    }
}
