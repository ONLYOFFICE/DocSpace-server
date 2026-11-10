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
/// GET /api/2.0/keys/@self — authenticated with the API key itself (<c>Authorization: Bearer
/// &lt;key&gt;</c>) rather than a user token, for every role that can create a key.
/// </summary>
[Trait("Category", "ApiKeys")]
public class GetOwnApiKeyTests(
    AspireAppFixture fixture)
    : ApiKeysTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(ApiKeyActorData.AllRoles), MemberType = typeof(ApiKeyActorData))]
    public async Task GetApiKey_ByRole_ReturnsOwnKeyInfo(ApiKeyActor actor)
    {
        // Arrange
        await AuthenticateAsAsync(actor);
        var created = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Self Key"), TestContext.Current.CancellationToken);

        // Act
        var result = await AsApiKeyAsync(created.Response.Key, () =>
            _apiKeysApi.GetApiKeyWithHttpInfoAsync(TestContext.Current.CancellationToken));

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        var key = result.Data.Response;
        key.Id.Should().Be(created.Response.Id);
        key.Name.Should().Be(created.Response.Name);
        key.Key.Should().MatchRegex("^sk-\\*{3}");
        key.KeyPostfix.Should().Be(created.Response.Key[^4..]);
        key.Permissions.Should().BeEmpty();
        key.IsActive.Should().BeTrue();
        result.Data.Count.Should().Be(1);
        result.Data.Links.Should().ContainSingle(link => link.Action == "GET");
    }

    /// <summary>
    /// Bug 84486: a request authenticated by a regular user token instead of an API key used to fail with a
    /// 500; the endpoint now answers 400 when the request carries no API key.
    /// </summary>
    [Trait("Bug", "84486")]
    [Fact]
    public async Task GetApiKey_UserToken_ReturnsBadRequest()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _apiKeysApi.GetApiKeyAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }
}
