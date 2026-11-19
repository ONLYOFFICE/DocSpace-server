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
/// The batch endpoints take their ids as JSON values: a number for a portal item, a string for a third-party one.
/// A number that is not a 32-bit integer - a fraction, an exponent, a value beyond the range - can name nothing, and
/// it used to end as 500: <c>FileOperationsManager.GetIds</c> read it with <c>JsonElement.GetInt32</c>, which threw a
/// <c>FormatException</c>. So did an id object of <c>bulkdownload</c> without <c>key</c> or <c>value</c>
/// (<c>KeyNotFoundException</c>). Both are now refused with 400.
/// </summary>
/// <remarks>
/// Sent raw: the generated client types an id as an integer or a string, so it cannot carry a fraction, an exponent
/// or an object.
/// </remarks>
[Trait("Category", "Operations")]
public class BatchIdNumberRangeTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    public static TheoryData<string, string, string, string> Requests()
    {
        var data = new TheoryData<string, string, string, string>();
        string[] numbers = ["1.5", "2147483648", "1e3"];

        foreach (var number in numbers)
        {
            data.Add("PUT", "api/2.0/files/fileops/bulkdownload", $$"""{"fileIds":[{{number}}]}""", number);
            data.Add("PUT", "api/2.0/files/fileops/bulkdownload", $$"""{"fileConvertIds":[{"key":{{number}},"value":".pdf"}]}""", number);
            data.Add("PUT", "api/2.0/files/fileops/markasread", $$"""{"fileIds":[{{number}}]}""", number);
            data.Add("PUT", "api/2.0/files/fileops/delete", $$"""{"fileIds":[{{number}}]}""", number);
            data.Add("POST", "api/2.0/files/favorites", $$"""{"fileIds":[{{number}}]}""", number);
            data.Add("DELETE", "api/2.0/files/recent", $$"""{"fileIds":[{{number}}]}""", number);
            data.Add("POST", "api/2.0/files/share", $$"""{"fileIds":[{{number}}]}""", number);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Requests))]
    public async Task BatchRequest_NumericIdNotInt32_IsBadRequest(string method, string route, string body, string number)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await Send(method, route, body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ErrorMessageOf(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Be($"Invalid ID: {number}");
    }

    [Fact]
    public async Task BulkDownload_IdObjectWithoutKey_IsBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await Send("PUT", "api/2.0/files/fileops/bulkdownload", """{"fileIds":[{"value":".pdf"}]}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ErrorMessageOf(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Be("""An ID object must have "key" and "value": {"value":".pdf"}""");
    }

    private async Task<HttpResponseMessage> Send(string method, string route, string body)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), route)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>The <c>error.message</c> of an error body, or an empty string when it has none.</summary>
    private static string ErrorMessageOf(string body)
    {
        using var json = JsonDocument.Parse(string.IsNullOrEmpty(body) ? "{}" : body);

        return json.RootElement.TryGetProperty("error", out var error)
            && error.ValueKind == JsonValueKind.Object
            && error.TryGetProperty("message", out var message)
            && message.ValueKind == JsonValueKind.String
                ? message.GetString() ?? ""
                : "";
    }
}
