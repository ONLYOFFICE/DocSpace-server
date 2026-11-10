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

namespace ASC.Files.Tests.Tests._01_Files.Create;

/// <summary>
/// <c>createFile</c> without a template id copies the blank document of the template store's
/// <c>new/&lt;culture&gt;/</c> folder that matches the caller's culture. The expected bytes are the
/// template files of the repository (<c>common/resources/DocStore/new</c>), which the stack serves.
/// </summary>
[Trait("Category", "Files")]
[Trait("Feature", "Files")]
public class NewFileTemplateCultureTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    public static TheoryData<string, string> Cultures => new()
    {
        { "de", "de-DE" },
        { "en-US", "en-US" },
        { "ru", "ru-RU" }
    };

    [Theory]
    [MemberData(nameof(Cultures))]
    public async Task CreateFile_Docx_CopiesTheTemplateOfTheCallersCulture(string culture, string templateFolder)
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);
        await _profilesApi.UpdateMemberCultureAsync(
            Owner.Id.ToString(), new UpdateMemberCultureRequest(culture), TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
        var folderId = await GetUserFolderIdAsync(Owner);

        // Act
        var file = (await _filesApi.CreateFileAsync(
            folderId, new CreateFileRequest("Autotest Culture Template.docx"), TestContext.Current.CancellationToken)).Response;

        // Assert
        var url = (await _filesApi.GetPresignedUriAsync(file.Id, TestContext.Current.CancellationToken)).Response;
        var content = await _filesClient.GetByteArrayAsync(url, TestContext.Current.CancellationToken);
        var expected = await System.IO.File.ReadAllBytesAsync(TemplatePath(templateFolder, "new.docx"), TestContext.Current.CancellationToken);

        Convert.ToHexString(SHA256.HashData(content)).Should().Be(
            Convert.ToHexString(SHA256.HashData(expected)), $"culture {culture} should get the {templateFolder} template");
    }

    private static string TemplatePath(string folder, string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "common", "resources", "DocStore", "new")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the test runs from inside the server repository");

        return Path.Combine(directory!.FullName, "common", "resources", "DocStore", "new", folder, fileName);
    }
}
