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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Owner;

/// <summary>
/// PUT /api/2.0/settings/owner with a valid owner change link, refused for the account the body names or for the
/// address the link carries. Each refusal was a plain <c>Exception</c> that nothing maps, so it came back as 500. The
/// link is what POST settings/owner mails, computed here from the stack's machine key, as
/// <c>SendRemoveInstructionsTests</c> computes its key.
/// </summary>
[Trait("Category", "Settings")]
public class UpdatePortalOwnerRefusalTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UpdatePortalOwner_UnknownOwnerId_Returns404()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        // Act
        var exception = await UpdateOwnerWithLinkAsync(admin, Guid.NewGuid());

        // Assert
        exception.ErrorCode.Should().Be(404);
        await OwnerShouldNotChangeAsync();
    }

    [Fact]
    public async Task UpdatePortalOwner_GuestOwnerId_Returns403()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var guest = await InviteGuest();

        // Act
        var exception = await UpdateOwnerWithLinkAsync(admin, guest.Id);

        // Assert
        exception.ErrorCode.Should().Be(403);
        await OwnerShouldNotChangeAsync();
    }

    [Fact]
    public async Task UpdatePortalOwner_DisabledOwnerId_Returns403()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var disabled = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(Owner);
        await _userStatusApi.UpdateUserStatusAsync(
            EmployeeStatus.Terminated, new UpdateMembersRequestDto([disabled.Id], false), TestContext.Current.CancellationToken);

        // Act
        var exception = await UpdateOwnerWithLinkAsync(admin, disabled.Id);

        // Assert
        exception.ErrorCode.Should().Be(403);
        await OwnerShouldNotChangeAsync();
    }

    /// <summary>
    /// The confirm scheme checks the plain <c>email</c> of the link when there is one, the action read only
    /// <c>encemail</c>: a link whose <c>encemail</c> names another member passed the scheme and was refused by the action.
    /// </summary>
    [Fact]
    public async Task UpdatePortalOwner_EncryptedAddressNotTheOwners_Returns403()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        // Act
        var exception = await UpdateOwnerWithLinkAsync(admin, admin.Id, encryptedEmail: admin.Email);

        // Assert
        exception.ErrorCode.Should().Be(403);
        await OwnerShouldNotChangeAsync();
    }

    /// <summary>
    /// Calls the action anonymously with the link POST settings/owner would mail for <paramref name="linkFor"/>, asking
    /// to make <paramref name="ownerId"/> the owner, and returns the refusal.
    /// </summary>
    private async Task<ApiException> UpdateOwnerWithLinkAsync(User linkFor, Guid ownerId, string? encryptedEmail = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["core:machinekey"] = Fixture.GetAppHostSetting("core:machinekey") })
            .Build();
        var machineKeys = new ASC.Security.Cryptography.MachinePseudoKeys(configuration);
        var provider = new ASC.Security.Cryptography.EmailValidationKeyProvider(
            machineKeys,
            null!,
            configuration,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ASC.Security.Cryptography.EmailValidationKeyProvider>.Instance);

        // The key the server issues: the owner's address, the confirmation type and the id of the new owner.
        var key = provider.GetEmailKey(Owner.Email + "PortalOwnerChange" + linkFor.Id, TenantId);
        var confirm = $"type=PortalOwnerChange&key={Uri.EscapeDataString(key)}&uid={linkFor.Id}&email={Uri.EscapeDataString(Owner.Email)}";
        if (encryptedEmail is not null)
        {
            var encrypted = new ASC.Security.Cryptography.InstanceCrypto(machineKeys).Encrypt(encryptedEmail);
            confirm += $"&encemail={Uri.EscapeDataString(encrypted)}";
        }

        await _webApiClient.Authenticate(null);
        _webApiClient.DefaultRequestHeaders.Add("confirm", confirm);
        try
        {
            return await Assert.ThrowsAsync<ApiException>(
                async () => await _ownerApi.UpdatePortalOwnerAsync(
                    new OwnerIdSettingsRequestDto(ownerId), TestContext.Current.CancellationToken));
        }
        finally
        {
            _webApiClient.DefaultRequestHeaders.Remove("confirm");
        }
    }

    private async Task OwnerShouldNotChangeAsync()
    {
        await _webApiClient.Authenticate(Owner);
        var settings = await _commonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken);

        settings.Response.OwnerId.Should().Be(Owner.Id);
    }
}
