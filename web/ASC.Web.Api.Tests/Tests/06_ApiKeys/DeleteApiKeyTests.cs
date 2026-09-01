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
/// DELETE /api/2.0/keys/{keyId} — every role deletes its own key, and a DocSpaceAdmin also deletes
/// the portal owner's key (both are admin-like roles for API key management).
/// </summary>
[Trait("Category", "ApiKeys")]
public class DeleteApiKeyTests(
    AspireAppFixture fixture)
    : ApiKeysTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(ApiKeyActorData.AllRoles), MemberType = typeof(ApiKeyActorData))]
    public async Task DeleteApiKey_ByRole_DeletesOwnKey(ApiKeyActor actor)
    {
        // Arrange
        await AuthenticateAsAsync(actor);
        var created = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Delete Key"), TestContext.Current.CancellationToken);

        // Act
        var result = await _apiKeysApi.DeleteApiKeyWithHttpInfoAsync(created.Response.Id, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Response.Should().BeTrue();
        result.Data.Count.Should().Be(1);
        result.Data.Links.Should().ContainSingle(link => link.Action == "DELETE" && link.Href.Contains(created.Response.Id.ToString()));
    }

    [Fact]
    public async Task DeleteApiKey_DocSpaceAdmin_DeletesOwnersKey()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);

        await _peopleClient.Authenticate(Owner);
        var created = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Owner Key"), TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(admin);

        // Act
        var result = await _apiKeysApi.DeleteApiKeyWithHttpInfoAsync(created.Response.Id, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Response.Should().BeTrue();
        result.Data.Count.Should().Be(1);
        result.Data.Links.Should().ContainSingle(link => link.Action == "DELETE" && link.Href.Contains(created.Response.Id.ToString()));
    }
}
