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

namespace ASC.Notify.Tests.Rooms;

/// <summary>
/// Who the Files notify client queues a room letter for, on the letter host's real services: the room deleted
/// letter, the form submitted / received letters and their push, the form filling letters and the recipient filter
/// of the room pushes. The rooms and forms are entries in memory (the stack runs no Files service), the members are
/// real accounts of the stack's portal, and their rooms-activity subscription and mute list are stored the way
/// <c>setNotificationSettings</c> and <c>setRoomsNotificationStatus</c> store them. A case reads the requests the
/// client handed to the engine (<see cref="NotifyQueue"/>) and keeps only those the engine's own subscription check
/// would let through.
/// </summary>
[Collection(nameof(NotifyQueueCollection))]
public class RoomLetterRecipientsTests
{
    private static readonly string[] _emailAndTelegram =
        [ConfigurationConstants.NotifyEMailSenderSysName, ConfigurationConstants.NotifyTelegramSenderSysName];

    [Fact]
    public async Task RoomRemoved_ReachesOwnerAndManagers_SkipsActorGroupsAndOtherRoles()
    {
        using var scope = await OpenScopeAsync();

        var deleter = await CreateMemberAsync(scope);
        var owner = await CreateMemberAsync(scope);
        var manager = await CreateMemberAsync(scope);
        var editor = await CreateMemberAsync(scope);
        var room = NewRoom(scope, FolderType.CustomRoom);

        await Client(scope).SendRoomRemovedAsync(room,
        [
            new AceWrapper { Id = deleter.Id, Access = FileShare.RoomManager },
            new AceWrapper { Id = owner.Id, Access = FileShare.Read, Owner = true },
            new AceWrapper { Id = manager.Id, Access = FileShare.RoomManager },
            new AceWrapper { Id = editor.Id, Access = FileShare.Editing },
            new AceWrapper { Id = Guid.NewGuid(), Access = FileShare.RoomManager, SubjectGroup = true }
        ], deleter.Id);

        var letters = await DeliveredAsync(scope, room.UniqID);

        letters.Select(r => r.Recipient.ID).Should().BeEquivalentTo([owner.Id.ToString(), manager.Id.ToString()]);
        letters.Should().AllSatisfy(r =>
        {
            r.NotifyAction.Should().BeOfType<RoomRemovedNotifyAction>();
            r._senderNames.Should().BeEquivalentTo(_emailAndTelegram);
        });
    }

    [Fact]
    public async Task RoomRemoved_AiAgent_SendsTheAgentVariant()
    {
        using var scope = await OpenScopeAsync();

        var deleter = await CreateMemberAsync(scope);
        var manager = await CreateMemberAsync(scope);
        var agent = NewRoom(scope, FolderType.AiRoom);

        await Client(scope).SendRoomRemovedAsync(agent, [new AceWrapper { Id = manager.Id, Access = FileShare.RoomManager }], deleter.Id);

        var letter = (await DeliveredAsync(scope, agent.UniqID)).Should().ContainSingle().Subject;

        letter.NotifyAction.Should().BeOfType<AgentRemovedNotifyAction>();
        letter.Recipient.ID.Should().Be(manager.Id.ToString());
    }

    /// <summary>N1: the subscription is checked for the deleter, not for the manager the letter goes to.</summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task RoomRemoved_ManagerWithRoomsActivityOff_GetsNoLetter()
    {
        using var scope = await OpenScopeAsync();

        var deleter = await CreateMemberAsync(scope);
        var manager = await CreateMemberAsync(scope);
        await SwitchRoomsActivityOffAsync(scope, manager);
        var room = NewRoom(scope, FolderType.CustomRoom);

        await Client(scope).SendRoomRemovedAsync(room, [new AceWrapper { Id = manager.Id, Access = FileShare.RoomManager }], deleter.Id);

        (await DeliveredAsync(scope, room.UniqID)).Should().BeEmpty("the manager switched room activity off");
    }

    /// <summary>N1: the mute list is read for the deleter, not for the manager the letter goes to.</summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task RoomRemoved_ManagerWhoMutedTheRoom_GetsNoLetter()
    {
        using var scope = await OpenScopeAsync();

        var deleter = await CreateMemberAsync(scope);
        var manager = await CreateMemberAsync(scope);
        var room = NewRoom(scope, FolderType.CustomRoom);
        await MuteAsync(scope, manager, room);

        await Client(scope).SendRoomRemovedAsync(room, [new AceWrapper { Id = manager.Id, Access = FileShare.RoomManager }], deleter.Id);

        (await DeliveredAsync(scope, room.UniqID)).Should().BeEmpty("the manager muted the room");
    }

    /// <summary>N1: a deleter who switched room activity off silences the letter for every manager.</summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task RoomRemoved_DeleterWithRoomsActivityOff_StillReachesSubscribedManager()
    {
        using var scope = await OpenScopeAsync();

        var deleter = await CreateMemberAsync(scope);
        await SwitchRoomsActivityOffAsync(scope, deleter);
        var manager = await CreateMemberAsync(scope);
        var room = NewRoom(scope, FolderType.CustomRoom);

        await Client(scope).SendRoomRemovedAsync(room, [new AceWrapper { Id = manager.Id, Access = FileShare.RoomManager }], deleter.Id);

        (await DeliveredAsync(scope, room.UniqID)).Select(r => r.Recipient.ID)
            .Should().BeEquivalentTo([manager.Id.ToString()], "the manager's own subscription is on");
    }

    [Fact]
    public async Task FormSubmitted_FillerAndCreatorGetTheirLetters_AndBothOnePush()
    {
        using var scope = await OpenScopeAsync();

        var creator = await CreateMemberAsync(scope);
        var filler = await CreateMemberAsync(scope);
        var (room, original, filled) = NewForm(scope, creator.Id, filler.Id);

        await Client(scope).SendFormSubmittedAsync(room, original, filled);

        var requests = await DeliveredAsync(scope, filled.UniqID);

        var letters = requests.Where(r => !IsPush(r)).ToList();
        letters.Should().HaveCount(2);
        letters.Should().ContainSingle(r => r.NotifyAction is FormSubmittedNotifyAction && r.Recipient.ID == filler.Id.ToString());
        letters.Should().ContainSingle(r => r.NotifyAction is FormReceivedNotifyAction && r.Recipient.ID == creator.Id.ToString());
        letters.Should().AllSatisfy(r => r._senderNames.Should().BeEquivalentTo(_emailAndTelegram));

        requests.Where(IsPush).Select(r => r.Recipient.ID)
            .Should().BeEquivalentTo([creator.Id.ToString(), filler.Id.ToString()]);
    }

    [Fact]
    public async Task FormSubmitted_FillerIsTheCreator_QueuesNothing()
    {
        using var scope = await OpenScopeAsync();

        var creator = await CreateMemberAsync(scope);
        var (room, original, filled) = NewForm(scope, creator.Id, creator.Id);

        await Client(scope).SendFormSubmittedAsync(room, original, filled);

        (await NotifyQueue.TakeAsync(scope.Services, filled.UniqID)).Should().BeEmpty();
    }

    [Fact]
    public async Task FormSubmitted_GuestFiller_IsNamedByTheGuestLabel()
    {
        using var scope = await OpenScopeAsync();

        var creator = await CreateMemberAsync(scope);
        var (room, original, filled) = NewForm(scope, creator.Id, ConfigurationConstants.Guest.ID);

        await Client(scope).SendFormSubmittedAsync(room, original, filled);

        var received = (await NotifyQueue.TakeAsync(scope.Services, filled.UniqID))
            .Should().ContainSingle(r => r.NotifyAction is FormReceivedNotifyAction).Subject;

        var guestLabel = AuditReportResource.ResourceManager.GetString("GuestAccount", CultureInfo.GetCultureInfo(scope.Tenant.Language));

        received.Arguments.Should().ContainSingle(a => a.Tag == CommonTags.FromUserName).Which.Value.Should().Be(guestLabel);
    }

    /// <summary>N2: the form letters skip the rooms-activity and mute checks every other room letter makes.</summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task FormSubmitted_RecipientsWithRoomsActivityOffOrMuted_GetNoLetter()
    {
        using var scope = await OpenScopeAsync();

        var creator = await CreateMemberAsync(scope);
        var filler = await CreateMemberAsync(scope);
        var (room, original, filled) = NewForm(scope, creator.Id, filler.Id);
        await SwitchRoomsActivityOffAsync(scope, filler);
        await MuteAsync(scope, creator, room);

        await Client(scope).SendFormSubmittedAsync(room, original, filled);

        (await DeliveredAsync(scope, filled.UniqID)).Where(r => !IsPush(r))
            .Should().BeEmpty("the filler switched room activity off and the creator muted the room");
    }

    /// <summary>N8: the submitted push goes to both parties as direct recipients, past the filter of every other room push.</summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task FormSubmitted_Push_SkipsRecipientsWithRoomsActivityOffOrMuted()
    {
        using var scope = await OpenScopeAsync();

        var creator = await CreateMemberAsync(scope);
        var filler = await CreateMemberAsync(scope);
        var (room, original, filled) = NewForm(scope, creator.Id, filler.Id);
        await SwitchRoomsActivityOffAsync(scope, filler);
        await MuteAsync(scope, creator, room);

        await Client(scope).SendFormSubmittedAsync(room, original, filled);

        (await DeliveredAsync(scope, filled.UniqID)).Where(IsPush)
            .Should().BeEmpty("the filler switched room activity off and the creator muted the room");
    }

    [Theory]
    [InlineData(typeof(FormStartedFillingNotifyAction))]
    [InlineData(typeof(YourTurnFormFillingNotifyAction))]
    [InlineData(typeof(FormWasCompletelyFilledNotifyAction))]
    [InlineData(typeof(StoppedFormFillingNotifyAction))]
    public async Task FormFilling_EveryRecipientGetsTheLetterByEmailAndTelegram(Type actionType)
    {
        using var scope = await OpenScopeAsync();

        var actor = await CreateMemberAsync(scope);
        var first = await CreateMemberAsync(scope);
        var second = await CreateMemberAsync(scope);
        var (room, original, _) = NewForm(scope, actor.Id, actor.Id);

        await Client(scope).SendFormFillingEvent(room, original, [first.Id, second.Id], actionType, actor.Id);

        var letters = await DeliveredAsync(scope, room.UniqID);

        letters.Select(r => r.Recipient.ID).Should().BeEquivalentTo([first.Id.ToString(), second.Id.ToString()]);
        letters.Should().AllSatisfy(r =>
        {
            r.NotifyAction.Should().BeOfType(actionType);
            r._senderNames.Should().BeEquivalentTo(_emailAndTelegram);
        });
    }

    /// <summary>N2: the form filling letters skip the rooms-activity and mute checks.</summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task FormFilling_RecipientsWithRoomsActivityOffOrMuted_GetNoLetter()
    {
        using var scope = await OpenScopeAsync();

        var actor = await CreateMemberAsync(scope);
        var unsubscribed = await CreateMemberAsync(scope);
        var muting = await CreateMemberAsync(scope);
        var (room, original, _) = NewForm(scope, actor.Id, actor.Id);
        await SwitchRoomsActivityOffAsync(scope, unsubscribed);
        await MuteAsync(scope, muting, room);

        await Client(scope).SendFormFillingEvent(room, original, [unsubscribed.Id, muting.Id], typeof(YourTurnFormFillingNotifyAction), actor.Id);

        (await DeliveredAsync(scope, room.UniqID)).Should().BeEmpty("one recipient switched room activity off, the other muted the room");
    }

    [Fact]
    public async Task ShareFolder_ReachesOnlyRecipientsWithRoomsActivityOn()
    {
        using var scope = await OpenScopeAsync();

        var subscribed = await CreateMemberAsync(scope);
        var unsubscribed = await CreateMemberAsync(scope);
        await SwitchRoomsActivityOffAsync(scope, unsubscribed);
        var room = NewRoom(scope, FolderType.CustomRoom);

        await Client(scope).SendShareNoticeAsync(room,
            new Dictionary<Guid, FileShare> { [subscribed.Id] = FileShare.Read, [unsubscribed.Id] = FileShare.Editing }, "message");

        var letter = (await DeliveredAsync(scope, room.UniqID)).Should().ContainSingle().Subject;

        letter.NotifyAction.Should().BeOfType<ShareFolderNotifyAction>();
        letter.Recipient.ID.Should().Be(subscribed.Id.ToString());
    }

    [Fact]
    public async Task RoomPushRecipients_SkipActorUnsubscribedAndMuted()
    {
        using var scope = await OpenScopeAsync();

        var actor = await CreateMemberAsync(scope);
        var member = await CreateMemberAsync(scope);
        var unsubscribed = await CreateMemberAsync(scope);
        var muting = await CreateMemberAsync(scope);
        var room = NewRoom(scope, FolderType.CustomRoom);
        await SwitchRoomsActivityOffAsync(scope, unsubscribed);
        await MuteAsync(scope, muting, room);

        var client = Client(scope);

        var recipients = await client.GetNotifiableUsersAsync([actor.Id, member.Id, unsubscribed.Id, muting.Id], room, actor.Id);

        recipients.Select(r => r.ID).Should().BeEquivalentTo([member.Id.ToString()]);

        var (_, form, _) = NewForm(scope, actor.Id, actor.Id);

        (await client.GetNotifiableUsersAsync([member.Id], form, actor.Id)).Should().BeEmpty("only a room has room pushes");
    }

    private static async Task<LetterScope> OpenScopeAsync()
    {
        var stack = await LetterStackFixture.CurrentAsync();

        return await LetterScope.OpenAsync(stack, CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));
    }

    private static FilesNotifyClient Client(LetterScope scope)
    {
        return scope.Services.GetRequiredService<FilesNotifyClient>();
    }

    private static bool IsPush(NotifyRequest request)
    {
        return request._senderNames.SequenceEqual([ConfigurationConstants.NotifyPushSenderSysName]);
    }

    private static async Task<List<NotifyRequest>> DeliveredAsync(LetterScope scope, string objectId)
    {
        var delivered = new List<NotifyRequest>();

        foreach (var request in await NotifyQueue.TakeAsync(scope.Services, objectId))
        {
            if (await NotifyQueue.PassesEngineSubscriptionCheckAsync(scope.Services, request))
            {
                delivered.Add(request);
            }
        }

        return delivered;
    }

    private static async Task<UserInfo> CreateMemberAsync(LetterScope scope)
    {
        var tag = Guid.NewGuid().ToString("N")[..12];

        var user = new UserInfo
        {
            FirstName = "Room",
            LastName = $"Member{tag}",
            UserName = $"room.member.{tag}",
            Email = $"room.member.{tag}@letters.test",
            Status = EmployeeStatus.Active,
            ActivationStatus = EmployeeActivationStatus.Activated
        };

        return await scope.Services.GetRequiredService<UserManager>().SaveUserInfo(user, EmployeeType.RoomAdmin, paidUserQuotaCheck: false);
    }

    private static async Task SwitchRoomsActivityOffAsync(LetterScope scope, UserInfo user)
    {
        await scope.Services.GetRequiredService<StudioNotifyHelper>()
            .SubscribeToNotifyAsync(user.Id, scope.Services.GetRequiredService<RoomsActivityNotifyAction>(), false);
    }

    private static async Task MuteAsync(LetterScope scope, UserInfo user, Folder<int> room)
    {
        await scope.Services.GetRequiredService<SettingsManager>()
            .SaveAsync(new RoomsNotificationSettings { DisabledRooms = [room.Id.ToString()] }, user.Id);
    }

    private static Folder<int> NewRoom(LetterScope scope, FolderType type)
    {
        var room = scope.Services.GetRequiredService<Folder<int>>();

        room.Id = NewId();
        room.Title = $"Room {room.Id}";
        room.FolderType = type;
        room.RootFolderType = FolderType.VirtualRooms;
        room.ParentId = NewId();

        return room;
    }

    private static (Folder<int> Room, File<int> Original, File<int> Filled) NewForm(LetterScope scope, Guid creator, Guid filler)
    {
        var room = NewRoom(scope, FolderType.FillingFormsRoom);

        return (room, NewFile(scope, room, creator, "form.pdf"), NewFile(scope, room, filler, "form - filled.pdf"));
    }

    private static File<int> NewFile(LetterScope scope, Folder<int> room, Guid author, string title)
    {
        var file = scope.Services.GetRequiredService<File<int>>();

        file.Id = NewId();
        file.Title = title;
        file.ParentId = room.Id;
        file.RootFolderType = FolderType.VirtualRooms;
        file.CreateBy = author;

        return file;
    }

    private static int NewId()
    {
        return Random.Shared.Next(1_000_000_000, int.MaxValue);
    }
}
