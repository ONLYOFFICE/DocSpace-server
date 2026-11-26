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

namespace ASC.Files.Tests.Tests._06_Operations.Ids;

/// <summary>
/// <c>copyas</c> (<c>destFolderId</c>), the two file-creation calls (<c>templateId</c>) and the default template
/// setting (<c>selectedFile</c>) take an id as a JSON value: a number for a portal item, a string for a third-party
/// one. A number that is not a 32-bit integer - a fraction, an exponent, a value beyond the range - can name nothing,
/// and it used to end as 500: the handlers read it with <c>JsonElement.GetInt32</c>, which threw a
/// <c>FormatException</c>. It is now refused with 400, like the batch ids of <see cref="BatchIdNumberRangeTests"/>.
/// </summary>
/// <remarks>
/// Sent raw: the generated client types these ids as an integer or a string, so it cannot carry a fraction or an
/// exponent.
/// </remarks>
[Trait("Category", "Operations")]
public class JsonIdNumberRangeTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    public static TheoryData<string> Numbers() => ["1.5", "2147483648", "1e3"];

    [Theory]
    [MemberData(nameof(Numbers))]
    public async Task CopyFileAs_DestFolderIdNotInt32_IsBadRequest(string number)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest copyas source.docx", Owner);

        // Act
        using var response = await Send("POST", $"api/2.0/files/file/{file.Id}/copyas",
            $$"""{"destTitle":"Autotest copy.docx","destFolderId":{{number}}}""");

        // Assert
        await AssertInvalidId(response, number);
    }

    [Theory]
    [MemberData(nameof(Numbers))]
    public async Task CreateFile_TemplateIdNotInt32_IsBadRequest(string number)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var folderId = await GetUserFolderIdAsync(Owner);

        // Act
        using var response = await Send("POST", $"api/2.0/files/{folderId}/file",
            $$"""{"title":"Autotest from template.docx","templateId":{{number}}}""");

        // Assert
        await AssertInvalidId(response, number);
    }

    [Theory]
    [MemberData(nameof(Numbers))]
    public async Task CreateFileInMyDocuments_TemplateIdNotInt32_IsBadRequest(string number)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await Send("POST", "api/2.0/files/@my/file",
            $$"""{"title":"Autotest from template.docx","templateId":{{number}}}""");

        // Assert
        await AssertInvalidId(response, number);
    }

    /// <summary>
    /// <c>.docx</c> is in the built-in template set: for an extension outside it the call returns before it reads the
    /// id, and the case would prove nothing.
    /// </summary>
    [Theory]
    [MemberData(nameof(Numbers))]
    public async Task SetDefaultTemplate_SelectedFileNotInt32_IsBadRequest(string number)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await Send("PUT", "api/2.0/files/settings/defaulttemplate",
            $$"""{"fileExtension":".docx","selectedFile":{{number}}}""");

        // Assert
        await AssertInvalidId(response, number);
    }

    private static async Task AssertInvalidId(HttpResponseMessage response, string number)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ErrorMessageOf(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Be($"Invalid ID: {number}");
    }

    private async Task<HttpResponseMessage> Send(string method, string route, string body)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), route)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
