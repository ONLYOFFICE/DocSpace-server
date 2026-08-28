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

namespace ASC.Web.Api.Tests.Tests._06_ApiKeys;

/// <summary>
/// POST /api/2.0/keys — a key name carrying HTML/script content is accepted for every role, but
/// it is HTML-encoded on the way in, so nothing that renders it later (the expiry notification
/// email, most of all) can be made to execute it. Before bug 82910 was fixed the name round-tripped
/// verbatim; the encoding itself is pinned by the <c>Permissions/CreateApiKeyPermissionsTests</c>
/// "Bug 82910" cases.
/// </summary>
[Trait("Category", "ApiKeys")]
public class CreateApiKeySpecialCharacterNameTests(
    AspireAppFixture fixture)
    : ApiKeysTestBase(fixture)
{
    private const string SpecialName = "<script>alert('xss')</script>";
    private const string EncodedName = "&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;";

    [Theory]
    [MemberData(nameof(ApiKeyActorData.AllRoles), MemberType = typeof(ApiKeyActorData))]
    public async Task CreateApiKey_ByRole_AllowsSpecialCharactersInName(ApiKeyActor actor)
    {
        // Arrange
        await AuthenticateAsAsync(actor);

        // Act
        var result = await _apiKeysApi.CreateApiKeyWithHttpInfoAsync(
            new CreateApiKeyRequestDto(SpecialName), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Response.Id.Should().NotBeEmpty();
        result.Data.Response.Name.Should().Be(EncodedName);
    }
}
