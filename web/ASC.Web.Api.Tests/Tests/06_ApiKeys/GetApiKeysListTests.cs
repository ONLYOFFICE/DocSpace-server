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
/// GET /api/2.0/keys — visibility differs by role: Owner and DocSpaceAdmin see every user's keys,
/// RoomAdmin and User see only their own. Each case has a different arrange/assertion shape (who
/// gets invited, who is expected to be visible), so these stay four <see cref="Fact"/>s rather than
/// a forced <see cref="Theory"/>.
/// </summary>
[Trait("Category", "ApiKeys")]
public class GetApiKeysListTests(
    AspireAppFixture fixture)
    : ApiKeysTestBase(fixture)
{
    [Fact]
    public async Task GetApiKeys_Owner_SeesKeysOfAllUsers()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        var user = await InviteMember(EmployeeType.User);

        await _peopleClient.Authenticate(Owner);
        var ownerKey = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Owner Key"), TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(admin);
        var adminKey = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Admin Key"), TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(user);
        var userKey = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest User Key"), TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(Owner);

        // Act
        var keys = await _apiKeysApi.GetApiKeysAsync(TestContext.Current.CancellationToken);

        // Assert
        var ids = keys.Response.Select(k => k.Id);
        ids.Should().Contain([ownerKey.Response.Id, adminKey.Response.Id, userKey.Response.Id]);
    }

    [Fact]
    public async Task GetApiKeys_DocSpaceAdmin_SeesKeysOfAllUsers()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        var user = await InviteMember(EmployeeType.User);

        await _peopleClient.Authenticate(Owner);
        var ownerKey = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Owner Key"), TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(user);
        var userKey = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest User Key"), TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(admin);

        // Act
        var keys = await _apiKeysApi.GetApiKeysAsync(TestContext.Current.CancellationToken);

        // Assert
        var ids = keys.Response.Select(k => k.Id);
        ids.Should().Contain([ownerKey.Response.Id, userKey.Response.Id]);
    }

    [Fact]
    public async Task GetApiKeys_RoomAdmin_SeesOnlyOwnKeys()
    {
        // Arrange
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);

        await _peopleClient.Authenticate(Owner);
        var ownerKey = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Owner Key"), TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(roomAdmin);
        var roomAdminKey = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest RoomAdmin Key"), TestContext.Current.CancellationToken);

        // Act
        var keys = await _apiKeysApi.GetApiKeysAsync(TestContext.Current.CancellationToken);

        // Assert
        var ids = keys.Response.Select(k => k.Id).ToList();
        ids.Should().Contain(roomAdminKey.Response.Id);
        ids.Should().NotContain(ownerKey.Response.Id);
    }

    [Fact]
    public async Task GetApiKeys_User_SeesOnlyOwnKeys()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);

        await _peopleClient.Authenticate(Owner);
        var ownerKey = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Owner Key"), TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(user);
        var userKey = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest User Key"), TestContext.Current.CancellationToken);

        // Act
        var keys = await _apiKeysApi.GetApiKeysAsync(TestContext.Current.CancellationToken);

        // Assert
        var ids = keys.Response.Select(k => k.Id).ToList();
        ids.Should().Contain(userKey.Response.Id);
        ids.Should().NotContain(ownerKey.Response.Id);
    }
}
