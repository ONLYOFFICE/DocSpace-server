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

namespace ASC.Web.Api.Tests.Tests._08_OAuth2.ClientManagement;

/// <summary>
/// Who may change a client: a guest refused with its own signature (unlike the <c>*_Guest_ThrowsForbidden</c>
/// cases, which send none), an administrator acting on another member's client with the change read back,
/// and the scope and activation-body validation.
/// </summary>
[Trait("Category", "OAuth2")]
public class ClientWriteAccessTests(
    AspireAppFixture fixture)
    : ClientManagementTestBase(fixture)
{
    [Fact]
    public async Task CreateClient_GuestOwnSignature_ThrowsForbidden()
    {
        // Arrange
        var guest = await InviteGuest();
        await ApplySignatureAsync(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.CreateClientAsync(
                ValidCreateClientRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateClient_GuestOwnSignature_ThrowsForbidden()
    {
        // Arrange
        var created = await CreateClientAsAsync();
        await ApplySignatureAsync(await InviteGuest());

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.UpdateClientAsync(
                created.ClientId, ValidUpdateClientRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteClient_GuestOwnSignature_ThrowsForbidden()
    {
        // Arrange
        var created = await CreateClientAsAsync();
        await ApplySignatureAsync(await InviteGuest());

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.DeleteClientAsync(created.ClientId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task RegenerateSecret_GuestOwnSignature_ThrowsForbidden()
    {
        // Arrange
        var created = await CreateClientAsAsync();
        await ApplySignatureAsync(await InviteGuest());

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.RegenerateSecretAsync(created.ClientId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ChangeActivation_GuestOwnSignature_ThrowsForbidden()
    {
        // Arrange
        var created = await CreateClientAsAsync();
        await ApplySignatureAsync(await InviteGuest());

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.ChangeActivationAsync(
                created.ClientId, new ChangeClientActivationRequest(status: false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteUserClients_GuestOwnSignature_ThrowsForbidden()
    {
        // Arrange
        await ApplySignatureAsync(await InviteGuest());

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.DeleteUserClientsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteTenantClients_GuestOwnSignature_ThrowsForbidden()
    {
        // Arrange
        await ApplySignatureAsync(await InviteGuest());

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.DeleteTenantClientsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateClient_DocSpaceAdminOnUsersClient_StoresTheNewFields()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        var created = await CreateClientAsAsync(user);
        await ApplySignatureAsync(await InviteContact(EmployeeType.DocSpaceAdmin));

        // Act
        await _clientManagementApi.UpdateClientAsync(
            created.ClientId,
            ValidUpdateClientRequest(
                name: "Renamed By Admin",
                description: "Changed description",
                allowedOrigins: ["https://changed.example.com"],
                redirectUris: ["https://changed.example.com/callback"],
                scopes: ["files:read"]),
            TestContext.Current.CancellationToken);

        // Assert
        var stored = await _clientQueryingApi.GetClientAsync(created.ClientId, TestContext.Current.CancellationToken);
        stored.Name.Should().Be("Renamed By Admin");
        stored.Description.Should().Be("Changed description");
        stored.AllowedOrigins.Should().Equal("https://changed.example.com");
        stored.RedirectUris.Should().Equal("https://changed.example.com/callback");
        stored.Scopes.Should().Equal("files:read");
    }

    [Fact]
    public async Task ChangeActivation_DocSpaceAdminOnUsersClient_StoresEnabledFlag()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        var created = await CreateClientAsAsync(user);
        await ApplySignatureAsync(await InviteContact(EmployeeType.DocSpaceAdmin));

        // Act
        await _clientManagementApi.ChangeActivationAsync(
            created.ClientId, new ChangeClientActivationRequest(status: false), TestContext.Current.CancellationToken);
        var disabled = await _clientQueryingApi.GetClientAsync(created.ClientId, TestContext.Current.CancellationToken);

        await _clientManagementApi.ChangeActivationAsync(
            created.ClientId, new ChangeClientActivationRequest(status: true), TestContext.Current.CancellationToken);
        var enabled = await _clientQueryingApi.GetClientAsync(created.ClientId, TestContext.Current.CancellationToken);

        // Assert
        disabled.Enabled.Should().BeFalse();
        enabled.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task RegenerateSecret_DocSpaceAdminOnUsersClient_StoresTheNewSecret()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        var created = await CreateClientAsAsync(user);
        await ApplySignatureAsync(await InviteContact(EmployeeType.DocSpaceAdmin));

        // Act
        var regenerated = await _clientManagementApi.RegenerateSecretAsync(created.ClientId, TestContext.Current.CancellationToken);

        // Assert
        var stored = await _clientQueryingApi.GetClientAsync(created.ClientId, TestContext.Current.CancellationToken);
        regenerated.ClientSecret.Should().NotBe(created.ClientSecret);
        stored.ClientSecret.Should().Be(regenerated.ClientSecret);
    }

    [Fact]
    public async Task DeleteClient_DocSpaceAdminOnUsersClient_RemovesTheClient()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        var created = await CreateClientAsAsync(user);
        await ApplySignatureAsync(await InviteContact(EmployeeType.DocSpaceAdmin));

        // Act
        await _clientManagementApi.DeleteClientAsync(created.ClientId, TestContext.Current.CancellationToken);

        // Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientQueryingApi.GetClientAsync(created.ClientId, TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteUserClients_DocSpaceAdmin_KeepsOtherMembersClients()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        var usersClient = await CreateClientAsAsync(user);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var adminsClient = await CreateClientAsAsync(admin);

        // Act
        await _clientManagementApi.DeleteUserClientsAsync(TestContext.Current.CancellationToken);

        // Assert
        var ids = await ListClientIdsAsync();
        ids.Should().Contain(usersClient.ClientId);
        ids.Should().NotContain(adminsClient.ClientId);
    }

    [Fact]
    public async Task CreateClient_UnknownScope_ThrowsValidationError()
    {
        // Arrange
        await ApplySignatureAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.CreateClientAsync(
                ValidCreateClientRequest(scopes: ["no-such:scope"]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    /// <remarks>
    /// Raw HTTP: <c>status</c> is a required constructor parameter of <see cref="ChangeClientActivationRequest"/>,
    /// so the typed client cannot send a body without it.
    /// </remarks>
    [Fact]
    public async Task ChangeActivation_BodyWithoutStatus_ThrowsValidationError()
    {
        // Arrange
        var created = await CreateClientAsAsync();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        using var response = await _identityClient.PatchAsync(
            $"api/2.0/clients/{created.ClientId}/activation", content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteMember_UserWithClient_DeletesTheirClients()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        var usersClient = await CreateClientAsAsync(user);

        await _peopleClient.Authenticate(Owner);
        await _userStatusApi.UpdateUserStatusAsync(
            EmployeeStatus.Terminated, new UpdateMembersRequestDto([user.Id], false), TestContext.Current.CancellationToken);

        // Act
        await _profilesApi.DeleteMemberAsync(user.Id.ToString(), TestContext.Current.CancellationToken);

        // Assert
        await ApplySignatureAsync();
        var ids = await ListClientIdsAsync();
        ids.Should().NotContain(usersClient.ClientId);
    }

    [Fact]
    public async Task StartReassign_WithProfileDeletion_DeletesTheUsersClients()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        var usersClient = await CreateClientAsAsync(user);

        await _peopleClient.Authenticate(Owner);
        await _userStatusApi.UpdateUserStatusAsync(
            EmployeeStatus.Terminated, new UpdateMembersRequestDto([user.Id], false), TestContext.Current.CancellationToken);
        var userDataApi = new UserDataApi(
            _peopleClient, new Configuration { BasePath = _peopleClient.BaseAddress!.ToString().TrimEnd('/') });

        // Act
        await userDataApi.StartReassignAsync(
            new StartReassignRequestDto(user.Id, Owner.Id, deleteProfile: true), TestContext.Current.CancellationToken);

        var deadline = DateTime.UtcNow.AddSeconds(60);
        var completed = false;

        while (!completed && DateTime.UtcNow < deadline)
        {
            await Task.Delay(1_000, TestContext.Current.CancellationToken);
            completed = (await userDataApi.GetReassignProgressAsync(user.Id, TestContext.Current.CancellationToken)).Response.IsCompleted;
        }

        // Assert
        completed.Should().BeTrue("the reassign operation should finish within the deadline");
        await ApplySignatureAsync();
        var ids = await ListClientIdsAsync();
        ids.Should().NotContain(usersClient.ClientId);
    }

    /// <summary>
    /// The ids of the clients the current signature sees. Raw JSON for the reason given on
    /// <c>ClientQueryingTestBase</c>: the generated page model cannot read the null cursors.
    /// </summary>
    private async Task<List<string?>> ListClientIdsAsync()
    {
        using var response = await _identityClient.GetAsync("api/2.0/clients?limit=50", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, "{0}", body);

        using var json = JsonDocument.Parse(body);

        return json.RootElement.GetProperty("data").EnumerateArray()
            .Select(c => c.GetProperty("client_id").GetString())
            .ToList();
    }
}
