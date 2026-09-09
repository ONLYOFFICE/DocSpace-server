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

namespace ASC.Files.Tests.Tests._03_Rooms;

/// <summary>
/// Shared setup for the room permission suites. The tests are split across several classes on
/// purpose: xUnit runs the tests of one class sequentially, so a single large class would
/// serialise the whole suite.
/// </summary>
public abstract class RoomsPermissionsTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>Creates a room, turns it into a template with the given visibility and returns its id.</summary>
    protected async Task<int> CreateTemplate(string title, bool isPublic)
    {
        var room = await CreateCustomRoom($"{title} Source");

        await _roomsApi.CreateRoomTemplateAsync(
            new RoomTemplateDto(room.Id, title, @public: isPublic),
            TestContext.Current.CancellationToken);

        return await WaitForRoomTemplate();
    }

    /// <summary>
    /// Polls the "create room from template" status until it completes, then returns the new room id.
    /// </summary>
    protected async Task<int> WaitForRoomFromTemplate()
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var status = (await _roomsApi.GetRoomCreatingStatusAsync(TestContext.Current.CancellationToken)).Response;

            if (status is { IsCompleted: true })
            {
                return status.RoomId;
            }

            if (DateTime.UtcNow >= deadline)
            {
                // The deadline ends the loop with an assertion carrying the last status. Cancelling
                // the call itself would kill the test with TaskCanceledException instead.
                status.Should().NotBeNull("the room creation status must be reported within 30 seconds");
                status.IsCompleted.Should().BeTrue(
                    "the room must be created from the template within 30 seconds (progress {0}, error '{1}')", status.Progress, status.Error);
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>Returns the titles of all rooms visible to the current user.</summary>
    protected async Task<List<string>> GetRoomTitles()
    {
        var list = (await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        return list.Folders.ConvertAll(f => f.Title);
    }

    /// <summary>Returns the titles of all templates visible to the current user.</summary>
    protected async Task<List<string>> GetTemplateTitles()
    {
        var list = (await _roomsApi.GetRoomsFolderAsync(
            searchArea: SearchArea.Templates,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        return list.Folders.ConvertAll(f => f.Title);
    }

    /// <summary>
    /// Creates a room owned by the caller holding one pending invited user, which is what the resend
    /// endpoint targets.
    /// </summary>
    protected async Task<FolderDtoInteger> CreateRoomWithPendingUser()
    {
        var room = await CreateCustomRoom("Autotest Resend Perm");

        var pendingUser = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, pendingUser, FileShare.Read);

        return room;
    }

    /// <summary>The external room link used by the link-permission tests.</summary>
    protected static RoomLinkRequest BuildExternalLink()
    {
        return new RoomLinkRequest(
            access: FileShare.Read,
            linkType: LinkType.External,
            title: "Autotest Perm Link",
            denyDownload: false);
    }

    /// <summary>Builds a single-member room invitation with notifications turned off.</summary>
    protected static RoomInvitationRequest BuildInvitation(User user, FileShare access)
    {
        return new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = access }],
            Notify = false
        };
    }

    /// <summary>
    /// Sends a raw DELETE /api/2.0/files/tags with an arbitrary JSON body, bypassing the typed SDK
    /// so that malformed payloads can be tested.
    /// </summary>
    protected async Task<HttpResponseMessage> SendRawTagsDelete(string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "api/2.0/files/tags")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>Creates a tag and attaches it to a freshly created room, so the tag has a link.</summary>
    protected async Task SeedLinkedTag(string tagName)
    {
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagName), TestContext.Current.CancellationToken);

        var room = await CreateCustomRoom("Autotest HasLinks Access Room");

        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);
    }

}
