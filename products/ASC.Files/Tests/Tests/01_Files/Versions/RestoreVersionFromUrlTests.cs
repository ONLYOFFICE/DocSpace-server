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

namespace ASC.Files.Tests.Tests._01_Files.Versions;

/// <summary>
/// Restoring a file version from an address through <c>restoreversion</c> with <c>url</c>: an address that is not an
/// http or https one is refused with 400, an address that does not answer or answers with an error is a failure of the
/// server behind it, a 502. <c>EntryManager.SaveEditingAsync</c> raised plain exceptions for all three, and a 500 came back. A version
/// the file does not have is 404.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "Files")]
public class RestoreVersionFromUrlTests(
    AspireAppFixture fixture)
    : VersionsTestBase(fixture)
{
    [Fact]
    public async Task RestoreFileVersion_UrlNotAnAddress_Returns400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest Restore From Bad Url.docx");
        var versionBefore = await CurrentVersion(file.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.RestoreFileVersionAsync(file.Id, version: 1, url: "not an address", cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await CurrentVersion(file.Id)).Should().Be(versionBefore, "a refused restore stores no new version");
    }

    /// <summary>
    /// An absolute address of another scheme passed the address check and made the HTTP client throw a
    /// NotSupportedException, which came back as 415, a code the action does not declare.
    /// </summary>
    [Theory]
    [InlineData("ftp://127.0.0.1/autotest.docx")]
    [InlineData("file:///etc/hosts")]
    public async Task RestoreFileVersion_UrlOfAnotherScheme_Returns400(string url)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest Restore From Other Scheme.docx");
        var versionBefore = await CurrentVersion(file.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.RestoreFileVersionAsync(file.Id, version: 1, url: url, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await CurrentVersion(file.Id)).Should().Be(versionBefore, "a refused restore stores no new version");
    }

    [Fact]
    public async Task RestoreFileVersion_UrlRefusesConnection_Returns502()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest Restore From Closed Port.docx");
        var versionBefore = await CurrentVersion(file.Id);

        // Act - nothing listens on port 1 of the host the Files service runs on, so the connection is refused.
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.RestoreFileVersionAsync(file.Id, version: 1, url: "http://127.0.0.1:1/autotest.docx", cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(502);
        (await CurrentVersion(file.Id)).Should().Be(versionBefore, "a refused restore stores no new version");
    }

    [Fact]
    public async Task RestoreFileVersion_UrlAnswersWithError_Returns502()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest Restore From Missing Url.docx");
        var versionBefore = await CurrentVersion(file.Id);

        // An API route that does not exist: whatever the portal answers for it (404, or 401 to an anonymous request),
        // it is never a success.
        var missing = new Uri(_filesClient.BaseAddress!, "/api/2.0/files/autotest-missing-content").ToString();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.RestoreFileVersionAsync(file.Id, version: 1, url: missing, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(502);
        (await CurrentVersion(file.Id)).Should().Be(versionBefore, "a refused restore stores no new version");
    }

    /// <summary>
    /// A version the file does not have: the call read it with no check and failed with a NullReferenceException, a
    /// 500. It is now 404, as the restore without <c>url</c> answers it, and the address is never fetched.
    /// </summary>
    [Fact]
    public async Task RestoreFileVersion_UrlWithMissingVersion_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest Restore Missing Version.docx");
        var versionBefore = await CurrentVersion(file.Id);

        // Act - the address does not matter: the version is looked up first.
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.RestoreFileVersionAsync(file.Id, version: versionBefore + 10, url: "http://127.0.0.1:1/autotest.docx", cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent!.ToString().Should().Contain("The required file was not found");
        (await CurrentVersion(file.Id)).Should().Be(versionBefore, "a refused restore stores no new version");
    }

    private async Task<int> CurrentVersion(int fileId)
    {
        return (await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response.@Version;
    }
}
