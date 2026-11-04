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


namespace ASC.Files.Tests.Tests._03_Rooms.AiHub;

/// <summary>
/// A guest has no AI, so a guest is never added to an AI project: not by a direct invitation, not by email and not
/// through the project's invitation link.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class AiProjectGuestInviteTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Fact]
    public async Task DirectInvite_Guest_Returns403AndGuestIsNotAdded()
    {
        // Arrange
        var projectId = await CreateProjectAsync();
        var guest = await InviteGuest();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await ShareRoomAsync(projectId, guest.Id, FileShare.Read));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("The role is not available for this user type");
        (await GetMemberAccessAsync(projectId, guest.Id)).Should().BeNull();
    }

    [Fact]
    public async Task EmailInvite_Returns403()
    {
        // Arrange
        var projectId = await CreateProjectAsync();
        var request = new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Email = $"guest-{Guid.NewGuid():N}@example.com", Access = FileShare.Read }],
            Notify = false
        };

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _roomsApi.SetRoomSecurityAsync(projectId, request, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("The role is not available for this user type");
    }

    [Fact]
    public async Task InvitationLink_ExistingGuest_IsRefusedAndNotAdded()
    {
        // Arrange
        var projectId = await CreateProjectAsync();
        var link = (await _roomsApi.SetRoomLinkAsync(
            projectId,
            new RoomLinkRequest(access: FileShare.RoomManager, title: "Project link", linkType: LinkType.Invitation),
            TestContext.Current.CancellationToken)).Response;
        var guest = await InviteGuest();

        // Act
        await _webApiClient.Authenticate(guest);
        var fullLink = await _webApiClient.GetAsync(new Uri(link.SharedLink.ShareLink).PathAndQuery, TestContext.Current.CancellationToken);
        var key = HttpUtility.ParseQueryString(fullLink.RequestMessage?.RequestUri?.Query!)["key"];
        var confirm = (await _authenticationApi.CheckConfirmAsync(
            new CheckConfirmRequestDto(key!, uiD: Owner.Id, type: ConfirmType.LinkInvite),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        confirm.Result.Should().Be(ValidationResult.QuotaFailed);
        (await GetMemberAccessAsync(projectId, guest.Id)).Should().BeNull();
    }

    private async Task<FileShare?> GetMemberAccessAsync(int projectId, Guid userId)
    {
        await _filesClient.Authenticate(Owner);
        var shares = (await _roomsApi.GetRoomSecurityInfoAsync(projectId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return shares.FirstOrDefault(s => s.SubjectType == SubjectType.User && s.SharedToUser?.Id == userId)?.Access;
    }
}
