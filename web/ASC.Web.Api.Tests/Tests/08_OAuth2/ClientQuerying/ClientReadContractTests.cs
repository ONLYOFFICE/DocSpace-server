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

namespace ASC.Web.Api.Tests.Tests._08_OAuth2.ClientQuerying;

/// <summary>
/// The read contract the published documents promise: an administrator's single read, what the info form
/// leaves out, list order and keyset paging, the guest's consents and scopes with their own signature, and
/// the developer-tools restriction carried by the signature.
/// </summary>
[Trait("Category", "OAuth2")]
public class ClientReadContractTests(
    AspireAppFixture fixture)
    : ClientQueryingTestBase(fixture)
{
    [Fact]
    public async Task GetClient_DocSpaceAdminOnUsersClient_ReturnsTheClient()
    {
        // Arrange
        await ApplySignatureAsync(await InviteContact(EmployeeType.User));
        var clientId = await CreateOAuthClientAsync("Autotest Admin Reads");
        await ApplySignatureAsync(await InviteContact(EmployeeType.DocSpaceAdmin));

        // Act
        var client = await _clientQueryingApi.GetClientAsync(clientId, TestContext.Current.CancellationToken);

        // Assert
        client.ClientId.Should().Be(clientId);
        client.Name.Should().Be("Autotest Admin Reads");
    }

    /// <remarks>Raw JSON: the assertion is about fields absent on the wire, which a typed model cannot show.</remarks>
    [Fact]
    public async Task GetClientInfo_OwnClient_OmitsSecretRedirectUrisAndAllowedOrigins()
    {
        // Arrange
        await ApplySignatureAsync();
        var clientId = await CreateOAuthClientAsync("Autotest Info Fields");

        // Act
        var info = await GetPageAsync($"api/2.0/clients/{clientId}/info");

        // Assert
        info.GetProperty("client_id").GetString().Should().Be(clientId);
        info.TryGetProperty("client_secret", out _).Should().BeFalse();
        info.TryGetProperty("redirect_uris", out _).Should().BeFalse();
        info.TryGetProperty("allowed_origins", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetClients_ThreeClients_ListsNewestFirst()
    {
        // Arrange
        await ApplySignatureAsync();
        var first = await CreateOAuthClientAsync("Autotest Order 1");
        var second = await CreateOAuthClientAsync("Autotest Order 2");
        var third = await CreateOAuthClientAsync("Autotest Order 3");

        // Act
        var page = await GetPageAsync("api/2.0/clients?limit=50");

        // Assert
        var ids = page.GetProperty("data").EnumerateArray().Select(c => c.GetProperty("client_id").GetString());
        ids.Should().Equal(third, second, first);
    }

    [Fact]
    public async Task GetClients_CursorOfFirstPage_ReturnsTheOlderClient()
    {
        // Arrange
        await ApplySignatureAsync();
        var older = await CreateOAuthClientAsync("Autotest Cursor 1");
        var newer = await CreateOAuthClientAsync("Autotest Cursor 2");
        var firstPage = await GetPageAsync("api/2.0/clients?limit=1");
        var last = firstPage.GetProperty("data")[0];
        var lastId = last.GetProperty("client_id").GetString();
        var lastCreatedOn = Uri.EscapeDataString(last.GetProperty("created_on").GetString()!);

        // Act
        var secondPage = await GetPageAsync($"api/2.0/clients?limit=1&last_client_id={lastId}&last_created_on={lastCreatedOn}");

        // Assert
        lastId.Should().Be(newer);
        secondPage.GetProperty("data").EnumerateArray().Select(c => c.GetProperty("client_id").GetString())
            .Should().Equal(older);
    }

    [Fact]
    public async Task GetClients_NoLimit_DefaultsTo30()
    {
        // Arrange
        await ApplySignatureAsync();

        // Act
        var page = await GetPageAsync("api/2.0/clients");

        // Assert
        page.GetProperty("limit").GetInt32().Should().Be(30);
    }

    [Theory]
    [InlineData("api/2.0/clients?limit=0")]
    [InlineData("api/2.0/clients?limit=51")]
    [InlineData("api/2.0/clients?limit=10&last_created_on=not-a-date")]
    [InlineData("api/2.0/clients/info?limit=51")]
    [InlineData("api/2.0/clients/consents?limit=0")]
    [InlineData("api/2.0/clients/consents?limit=51")]
    public async Task ListClients_OutOfRangePaging_ReturnsBadRequest(string path)
    {
        // Arrange
        await ApplySignatureAsync();

        // Act
        using var response = await _identityClient.GetAsync(path, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <remarks>
    /// Raw JSON, as in <see cref="GetConsentsTests"/>: the typed call cannot read an empty page (null cursor),
    /// and the SDK reports that deserialization failure as a 500 although the server answered 200.
    /// </remarks>
    [Fact]
    public async Task GetConsents_GuestOwnSignature_ReturnsEmptyList()
    {
        // Arrange
        await ApplySignatureAsync(await InviteGuest());

        // Act
        var page = await GetPageAsync("api/2.0/clients/consents?limit=50");

        // Assert
        page.GetProperty("data").EnumerateArray().Should().BeEmpty();
    }

    [Fact]
    public async Task GetScopes_GuestOwnSignature_ReturnsAlphabeticalCatalogueWithOpenidLast()
    {
        // Arrange
        await ApplySignatureAsync(await InviteGuest());

        // Act
        var scopes = await _scopeManagementApi.GetScopesAsync(TestContext.Current.CancellationToken);

        // Assert
        var names = scopes.Select(s => s.Name).ToList();
        names.Should().HaveCountGreaterThan(1);
        names[^1].Should().Be("openid");
        names.SkipLast(1).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task CreateClient_DevToolsRestricted_MemberIsRefusedAsGuest(EmployeeType type)
    {
        // Arrange
        await RestrictDevToolsAsync();
        await ApplySignatureAsync(await InviteContact(type));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.CreateClientAsync(NewClientRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateClient_DevToolsRestricted_DocSpaceAdminStillCreates()
    {
        // Arrange
        await RestrictDevToolsAsync();
        await ApplySignatureAsync(await InviteContact(EmployeeType.DocSpaceAdmin));

        // Act
        var clientId = await CreateOAuthClientAsync();

        // Assert
        clientId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateClient_SignatureIssuedBeforeRestriction_StillCreates()
    {
        // Arrange — the signature carries the setting as it was when the signature was issued.
        var user = await InviteContact(EmployeeType.User);
        await ApplySignatureAsync(user);
        await RestrictDevToolsAsync();

        // Act
        var clientId = await CreateOAuthClientAsync();

        // Assert
        clientId.Should().NotBeNullOrEmpty();
    }

    private async Task RestrictDevToolsAsync()
    {
        await _webApiClient.Authenticate(Owner);
        var securityApi = new DocSpace.API.SDK.Api.Security.AccessToDevToolsApi(
            _webApiClient, new Configuration { BasePath = _webApiClient.BaseAddress!.ToString().TrimEnd('/') });
        await securityApi.SetTenantDevToolsAccessSettingsAsync(
            new TenantDevToolsAccessSettingsRequestDto(true), TestContext.Current.CancellationToken);
    }
}
