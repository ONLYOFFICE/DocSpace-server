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

namespace ASC.Files.Tests.Tests._01_Files.Forms.BuiltinDb;

/// <summary>
/// <c>GET /files/builtinDb/connectionString</c> hands out the password of an account that reads every form submission
/// of the portal, past the rights the caller has on the rooms, so a token that may only read is refused even when it
/// belongs to the owner. The DocSpace.API.SDK 4.1.0 does not carry the endpoint yet, so it is called over raw HTTP.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Forms")]
public class BuiltinDbConnectionScopeTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    public static TheoryData<string> ReadOnlyScopes => ["files:read", "*:read"];

    [Theory]
    [MemberData(nameof(ReadOnlyScopes))]
    public async Task ApiKeyThatMayOnlyRead_IsRefused(string scope)
    {
        var key = await CreateApiKeyAsync(scope);

        using var response = await GetConnectionAsync(key);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ApiKeyThatMayWriteFiles_GetsThrough()
    {
        var key = await CreateApiKeyAsync("files:write");

        using var response = await GetConnectionAsync(key);

        // 202 while the portal's own database is being prepared, 200 once it is ready.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);
    }

    private async Task<string> CreateApiKeyAsync(string scope)
    {
        await _peopleClient.Authenticate(Owner);
        var created = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto($"forms db {scope}", [scope]), TestContext.Current.CancellationToken);

        return created.Response.Key;
    }

    private async Task<HttpResponseMessage> GetConnectionAsync(string apiKey)
    {
        _filesClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        return await new RawApiClient(_filesClient).GetAsync("api/2.0/files/builtinDb/connectionString", TestContext.Current.CancellationToken);
    }
}
