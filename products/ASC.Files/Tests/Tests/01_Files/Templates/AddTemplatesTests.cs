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

namespace ASC.Files.Tests.Tests._01_Files.Templates;

/// <summary>
/// <c>POST /files/templates</c> - adding one or more files to the caller's personal template
/// list. The endpoint neither validates that a file id exists nor rejects being called twice
/// with the same id; it always reports success.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Templates")]
public class AddTemplatesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task AddTemplates_SingleFile_Succeeds()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Templates Single Room");
        var file = await CreateFile("Autotest Templates Single File", room.Id);

        // Act
        var result = await _filesApi.AddTemplatesAsync(
            new TemplatesRequestDto([file.Id]), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeTrue();
    }

    [Fact]
    public async Task AddTemplates_MultipleFiles_Succeeds()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Templates Multi Room");
        var file1 = await CreateFile("Autotest Templates Multi File 1", room.Id);
        var file2 = await CreateFile("Autotest Templates Multi File 2", room.Id);

        // Act
        var result = await _filesApi.AddTemplatesAsync(
            new TemplatesRequestDto([file1.Id, file2.Id]), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeTrue();
    }

    [Fact]
    public async Task AddTemplates_SameFileTwice_IsIdempotent()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Templates Idempotent Room");
        var file = await CreateFile("Autotest Templates Idempotent File", room.Id);

        await _filesApi.AddTemplatesAsync(new TemplatesRequestDto([file.Id]), TestContext.Current.CancellationToken);

        // Act
        var result = await _filesApi.AddTemplatesAsync(
            new TemplatesRequestDto([file.Id]), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeTrue();
    }

    [Fact]
    public async Task AddTemplates_EmptyFileIds_Succeeds()
    {
        // Act
        var result = await _filesApi.AddTemplatesAsync(
            new TemplatesRequestDto([]), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeTrue();
    }

    /// <summary>
    /// Sent raw: the generated client drops the <c>Content-Type</c> header together with the body, so
    /// a bodyless typed call is refused by ASP.NET with 415 before it reaches the controller. Only a
    /// hand-built request can express "no body at all" the way a real caller would.
    /// </summary>
    [Fact]
    public async Task AddTemplates_NoBody_Succeeds()
    {
        // Act
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await _filesClient.PostAsync("api/2.0/files/templates", content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// The endpoint does not check that the file actually exists - it reports success regardless.
    /// </summary>
    [Fact]
    public async Task AddTemplates_NonExistentFileId_Succeeds()
    {
        // Act
        var result = await _filesApi.AddTemplatesAsync(
            new TemplatesRequestDto([999999999]), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeTrue();
    }
}
