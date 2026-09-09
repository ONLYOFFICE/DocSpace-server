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

namespace ASC.AI.Tests.Tests.PromptFolderStorageTests;

[Trait("Category", "Validation")]
[Trait("Feature", "AI/PromptFolders")]
public class PromptFolderNameLengthTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const int MaxNameLength = 255;

    [Fact]
    public async Task Create_NameAtLimit_Succeeds()
    {
        var name = new string('a', MaxNameLength);

        using var response = await _ai.PostAsync(
            PromptFoldersPath,
            new { name },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await _ai.ReadAsync<PromptFolderDto>(response, TestContext.Current.CancellationToken);
        created.Name.Should().Be(name);
    }

    [Theory]
    [InlineData(MaxNameLength + 1)]
    [InlineData(MaxNameLength + 2)]
    [InlineData(1000)]
    [InlineData(1024)]
    [InlineData(1025)]
    [InlineData(5000)]
    public async Task Create_NameOverLimit_Returns400(int length)
    {
        using var response = await _ai.PostAsync(
            PromptFoldersPath,
            new { name = new string('a', length) },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Rename_NameAtLimit_PersistsFullName()
    {
        var created = await CreatePromptFolderAsync("original");
        var name = new string('a', MaxNameLength);

        using var response = await _ai.PutAsync(
            $"{PromptFoldersPath}/{created.Id}",
            new { name },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ReadPromptFolderAsync(created.Id)).Name.Should().Be(name);
    }

    [Theory]
    [InlineData(MaxNameLength + 1)]
    [InlineData(MaxNameLength + 2)]
    [InlineData(1000)]
    [InlineData(1024)]
    [InlineData(1025)]
    [InlineData(5000)]
    public async Task Rename_NameOverLimit_Returns400(int length)
    {
        var created = await CreatePromptFolderAsync("original");

        using var response = await _ai.PutAsync(
            $"{PromptFoldersPath}/{created.Id}",
            new { name = new string('a', length) },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// The rename path writes through <c>ExecuteUpdateAsync</c>, which bypasses the change
    /// tracker and therefore the <c>SaveChangesAsync</c> validation the create path relies on.
    /// Without request-level validation MySQL silently truncated the name to the column width.
    /// </summary>
    [Fact]
    public async Task Rename_NameOverLimit_LeavesStoredNameUntouched()
    {
        var created = await CreatePromptFolderAsync("original");

        using var response = await _ai.PutAsync(
            $"{PromptFoldersPath}/{created.Id}",
            new { name = new string('a', 5000) },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadPromptFolderAsync(created.Id)).Name.Should().Be("original");
    }
}
