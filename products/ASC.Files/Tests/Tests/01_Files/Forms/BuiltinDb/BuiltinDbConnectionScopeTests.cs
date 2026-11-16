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
/// belongs to the owner; so is one that may write only rooms or accounts. The DocSpace.API.SDK 4.1.0 does not carry the
/// endpoint yet, so it is called over raw HTTP (<see cref="BuiltinDbApi"/>).
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Forms")]
public class BuiltinDbConnectionScopeTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    // Scopes that read files, read everything, or write something other than files; each with a read its scope allows.
    public static TheoryData<string, string> ScopesWithoutFilesWrite => new()
    {
        { "files:read", MyDocuments },
        { "*:read", MyDocuments },
        { "rooms:write", Rooms },
        { "accounts:write", People }
    };

    public static TheoryData<string> ScopesWithFilesWrite => ["files:write", "*:write"];

    private const string MyDocuments = "my documents";
    private const string Rooms = "rooms";
    private const string People = "people";

    [Theory]
    [MemberData(nameof(ScopesWithoutFilesWrite))]
    public async Task ApiKeyThatCannotWriteFiles_IsRefused(string scope, string allowedRead)
    {
        var key = await CreateApiKeyAsync(scope);
        _filesClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        _peopleClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        var cancellationToken = TestContext.Current.CancellationToken;

        // The key works where its scope allows (the SDK throws on anything but 2xx)...
        Task read = allowedRead switch
        {
            MyDocuments => _foldersApi.GetMyFolderAsync(cancellationToken: cancellationToken),
            Rooms => _roomsApi.GetRoomsFolderAsync(cancellationToken: cancellationToken),
            _ => _profilesApi.GetAllProfilesAsync(cancellationToken: cancellationToken)
        };
        await read;

        // ...so the refusal here is the scope's, not the right's: the owner has the right, whose refusal says "Access denied".
        using var refused = await BuiltinDbApi.GetConnectionAsync(_filesClient);
        refused.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await refused.Content.ReadAsStringAsync(cancellationToken)).Should().NotContain("Access denied");
    }

    [Theory]
    [MemberData(nameof(ScopesWithFilesWrite))]
    public async Task ApiKeyThatMayWriteFiles_GetsTheConnection(string scope)
    {
        var key = await CreateApiKeyAsync(scope);
        _filesClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

        var connection = await BuiltinDbApi.GetReadyAsync(_filesClient);

        connection.Database.Should().Be($"forms_t{TenantId}");
    }

    private async Task<string> CreateApiKeyAsync(string scope)
    {
        await _peopleClient.Authenticate(Owner);
        var created = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto($"forms db {scope}", [scope]), TestContext.Current.CancellationToken);

        return created.Response.Key;
    }
}
