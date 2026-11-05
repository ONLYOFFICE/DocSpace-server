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

namespace ASC.Notify.Tests.Invites;

/// <summary>
/// The room letters of the Files module print the room's name. These are mostly push bodies, which
/// <c>LetterTestBase</c> cannot render, so what reaches the letter is read off the tags the action's own
/// <c>Init</c> sets: a private room is named neutrally, and an ordinary one keeps its title.
/// </summary>
public class PrivateRoomTitleInLettersTests
{
    private const string SecretTitle = "Autotest secret room title";

    public static TheoryData<string> Actions =>
    [
        nameof(InvitedToRoomNotifyAction),
        nameof(RoomUpdateAccessForUserNotifyAction),
        nameof(DocumentsUploadedToRoomNotifyAction),
        nameof(DocumentUploadedToRoomNotifyAction),
        nameof(DocumentCreatedInRoomNotifyAction),
        nameof(FolderCreatedInRoomNotifyAction)
    ];

    private static async Task<Dictionary<string, string?>> TagsOf(string actionName, bool isPrivate)
    {
        var stack = await TestContext.Current.GetFixture<LetterStackFixture>()
            ?? throw new InvalidOperationException($"No {nameof(LetterStackFixture)} in the test context.");

        using var scope = await LetterScope.OpenAsync(stack, CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));
        var services = scope.Services;

        var room = services.GetRequiredService<Folder<int>>();
        room.Title = SecretTitle;
        room.SettingsPrivate = isPrivate;

        var file = services.GetRequiredService<File<int>>();
        file.Title = "Encrypted.docx";

        var folder = services.GetRequiredService<Folder<int>>();
        folder.Title = "Subfolder";

        NotifyAction action;
        switch (actionName)
        {
            case nameof(InvitedToRoomNotifyAction):
                var invited = services.GetRequiredService<InvitedToRoomNotifyAction>();
                invited.Init(room);
                action = invited;
                break;
            case nameof(RoomUpdateAccessForUserNotifyAction):
                var access = services.GetRequiredService<RoomUpdateAccessForUserNotifyAction>();
                access.Init(room, "Viewer");
                action = access;
                break;
            case nameof(DocumentsUploadedToRoomNotifyAction):
                var uploadedMany = services.GetRequiredService<DocumentsUploadedToRoomNotifyAction>();
                uploadedMany.Init(room, 2);
                action = uploadedMany;
                break;
            case nameof(DocumentUploadedToRoomNotifyAction):
                var uploaded = services.GetRequiredService<DocumentUploadedToRoomNotifyAction>();
                uploaded.Init(room, file);
                action = uploaded;
                break;
            case nameof(DocumentCreatedInRoomNotifyAction):
                var created = services.GetRequiredService<DocumentCreatedInRoomNotifyAction>();
                created.Init(room, file);
                action = created;
                break;
            case nameof(FolderCreatedInRoomNotifyAction):
                var folderCreated = services.GetRequiredService<FolderCreatedInRoomNotifyAction>();
                folderCreated.Init(room, folder);
                action = folderCreated;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(actionName), actionName, null);
        }

        return action.Tags.ToDictionary(tag => tag.Tag, tag => tag.Value?.ToString());
    }

    [Fact]
    public async Task InvitationLetters_WithNoTitle_NameTheRoomNeutrally()
    {
        var stack = await TestContext.Current.GetFixture<LetterStackFixture>()
            ?? throw new InvalidOperationException($"No {nameof(LetterStackFixture)} in the test context.");

        using var scope = await LetterScope.OpenAsync(stack, CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));

        var room = scope.Services.GetRequiredService<SaasRoomInviteNotifyAction>();
        room.Init(scope.Culture.Name, null, scope.PortalUrl);

        var agent = scope.Services.GetRequiredService<SaasAgentInviteNotifyAction>();
        agent.Init(scope.Culture.Name, null, scope.PortalUrl);

        var existingAgent = scope.Services.GetRequiredService<SaasAgentInviteExistingUserNotifyAction>();
        existingAgent.Init(scope.Recipient, null, scope.PortalUrl);

        foreach (var tags in new[] { room.Tags, agent.Tags, existingAgent.Tags })
        {
            tags.Single(tag => tag.Tag == CommonTags.Message).Value.Should().Be(WebstudioNotifyPatternResource.PrivateRoomTitle);
        }
    }

    [Theory]
    [MemberData(nameof(Actions))]
    public async Task PrivateRoom_IsNamedNeutrally(string actionName)
    {
        var tags = await TagsOf(actionName, isPrivate: true);

        tags.Should().ContainKey(NotifyConstants.RoomTitle).WhoseValue.Should().Be(FilesPatternResource.PrivateRoomTitle);
        tags.Values.Should().NotContain(SecretTitle);
    }

    [Theory]
    [MemberData(nameof(Actions))]
    public async Task OrdinaryRoom_KeepsItsTitle(string actionName)
    {
        var tags = await TagsOf(actionName, isPrivate: false);

        tags.Should().ContainKey(NotifyConstants.RoomTitle).WhoseValue.Should().Be(SecretTitle);
    }
}
