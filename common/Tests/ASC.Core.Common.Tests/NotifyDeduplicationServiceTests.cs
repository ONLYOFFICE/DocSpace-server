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

namespace ASC.Core.Common.Tests;

/// <summary>
/// The repeat check every letter passes right before it is sent: the same letter about the same entity reaches the
/// same recipient through the same channel once per period, while anything that tells two letters apart lets the
/// second one through.
/// </summary>
/// <remarks>
/// Each test gets a cache of its own, so no mark leaks from one test into another. The cache is memory-only and the
/// lock in-memory: what is checked here is which letters count as the same, not how Redis spreads the mark.
/// </remarks>
public class NotifyDeduplicationServiceTests
{
    private const int TenantId = 1;
    private const string EmailSender = "email.sender";
    private const string TelegramSender = "telegram.sender";
    private const string Recipient = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";
    private const string RoomId = "folder_10";

    [Fact]
    public async Task RepeatOfTheSameLetter_IsSuppressed()
    {
        var service = CreateService();
        var action = new TestAction("saas_room_invite_existing_user");

        var first = await service.IsDuplicateAsync(Request(action, RoomId));
        var second = await service.IsDuplicateAsync(Request(action, RoomId));

        first.Should().BeFalse();
        second.Should().BeTrue();
    }

    [Fact]
    public async Task ParallelRepeats_LetExactlyOneThrough()
    {
        var service = CreateService();
        var action = new TestAction("saas_room_invite_existing_user");

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => service.IsDuplicateAsync(Request(action, RoomId))));

        results.Count(duplicate => !duplicate).Should().Be(1);
    }

    [Fact]
    public async Task RenamedRoom_IsStillTheSameLetter()
    {
        var service = CreateService();
        var action = new TestAction("saas_room_invite_existing_user");

        await service.IsDuplicateAsync(Request(action, RoomId, tags: [new TagValue("RoomTitle", "Before")]));
        var afterRename = await service.IsDuplicateAsync(Request(action, RoomId, tags: [new TagValue("RoomTitle", "After")]));

        afterRename.Should().BeTrue("a title is free text the caller controls and never part of the key");
    }

    [Fact]
    public async Task AnotherRoom_IsNotSuppressed()
    {
        var service = CreateService();
        var action = new TestAction("saas_room_invite_existing_user");

        await service.IsDuplicateAsync(Request(action, RoomId));
        var otherRoom = await service.IsDuplicateAsync(Request(action, "folder_11"));

        otherRoom.Should().BeFalse();
    }

    [Fact]
    public async Task AnotherRecipient_IsNotSuppressed()
    {
        var service = CreateService();
        var action = new TestAction("saas_room_invite_existing_user");

        await service.IsDuplicateAsync(Request(action, RoomId));
        var otherRecipient = await service.IsDuplicateAsync(Request(action, RoomId, recipientId: "7c9e6679-7425-40de-944b-e07fc1f90ae7"));

        otherRecipient.Should().BeFalse();
    }

    [Fact]
    public async Task RecipientAddressInAnotherCase_IsTheSameRecipient()
    {
        var service = CreateService();
        var action = new TestAction("saas_room_invite");

        await service.IsDuplicateAsync(Request(action, RoomId, recipientId: "guest@example.com"));
        var sameAddress = await service.IsDuplicateAsync(Request(action, RoomId, recipientId: "Guest@Example.com"));

        sameAddress.Should().BeTrue();
    }

    [Fact]
    public async Task AnotherChannel_IsNotSuppressed()
    {
        var service = CreateService();
        var action = new TestAction("ShareDocument");

        await service.IsDuplicateAsync(Request(action, "file_5", sender: EmailSender));
        var telegram = await service.IsDuplicateAsync(Request(action, "file_5", sender: TelegramSender));

        telegram.Should().BeFalse();
    }

    [Fact]
    public async Task AnotherTenant_IsNotSuppressed()
    {
        var service = CreateService();
        var action = new TestAction("saas_room_invite_existing_user");

        await service.IsDuplicateAsync(Request(action, RoomId));
        var otherTenant = await service.IsDuplicateAsync(Request(action, RoomId, tenantId: TenantId + 1));

        otherTenant.Should().BeFalse();
    }

    [Fact]
    public async Task AnotherAction_IsNotSuppressed()
    {
        var service = CreateService();

        await service.IsDuplicateAsync(Request(new TestAction("saas_room_invite_existing_user"), RoomId));
        var otherAction = await service.IsDuplicateAsync(Request(new TestAction("user_role_changed"), RoomId));

        otherAction.Should().BeFalse();
    }

    [Fact]
    public async Task DiscriminatorValue_TellsLettersApart()
    {
        var service = CreateService();
        var action = new TestAction("user_type_changed", tags: ["UserType"]);

        var toAdmin = await service.IsDuplicateAsync(Request(action, null, tags: [new TagValue("UserType", "RoomAdmin")]));
        var toUser = await service.IsDuplicateAsync(Request(action, null, tags: [new TagValue("UserType", "User")]));
        var toAdminAgain = await service.IsDuplicateAsync(Request(action, null, tags: [new TagValue("UserType", "RoomAdmin")]));
        var toUserAgain = await service.IsDuplicateAsync(Request(action, null, tags: [new TagValue("UserType", "User")]));

        toAdmin.Should().BeFalse();
        toUser.Should().BeFalse();
        toAdminAgain.Should().BeTrue("switching back and forth yields at most one letter per state");
        toUserAgain.Should().BeTrue();
    }

    [Fact]
    public async Task ActionThatOptsOut_IsNeverSuppressed()
    {
        var service = CreateService();
        var action = new TestAction("for_admin_notify", period: TimeSpan.Zero);

        var first = await service.IsDuplicateAsync(Request(action, null));
        var second = await service.IsDuplicateAsync(Request(action, null));

        first.Should().BeFalse();
        second.Should().BeFalse();
    }

    [Fact]
    public async Task ZeroConfiguredPeriod_TurnsTheCheckOff()
    {
        var service = CreateService(period: "00:00:00");
        var action = new TestAction("saas_room_invite_existing_user");

        await service.IsDuplicateAsync(Request(action, RoomId));
        var second = await service.IsDuplicateAsync(Request(action, RoomId));

        second.Should().BeFalse();
    }

    [Fact]
    public async Task AfterThePeriod_TheLetterGoesOutAgain()
    {
        var service = CreateService(period: "00:00:01");
        var action = new TestAction("saas_room_invite_existing_user");

        await service.IsDuplicateAsync(Request(action, RoomId));
        await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);
        var afterPeriod = await service.IsDuplicateAsync(Request(action, RoomId));

        afterPeriod.Should().BeFalse();
    }

    [Fact]
    public async Task PeriodLongerThanTheInvitationVisitWindow_IsCappedByIt()
    {
        // An hour asked for by both the configuration and the action, a visit window of one second: an invitation
        // opened once is dead after a second, so a resend may not stay suppressed for longer than that.
        var service = CreateService(period: "01:00:00", visitInterval: "00:00:01");
        var action = new TestAction("saas_room_invite", period: TimeSpan.FromHours(1));

        await service.IsDuplicateAsync(Request(action, RoomId));
        await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);
        var afterVisitWindow = await service.IsDuplicateAsync(Request(action, RoomId));

        afterVisitWindow.Should().BeFalse();
    }

    private static NotifyDeduplicationService CreateService(string? period = null, string? visitInterval = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["core:notify:deduplication:period"] = period,
                ["visit:validinterval"] = visitInterval
            })
            .Build();

        return new NotifyDeduplicationService(
            configuration,
            new FusionCache(new FusionCacheOptions()),
            new InMemoryLockProvider(),
            NullLogger<NotifyDeduplicationService>.Instance);
    }

    private static NotifyRequest Request(
        INotifyAction action,
        string? objectId,
        string recipientId = Recipient,
        string sender = EmailSender,
        int tenantId = TenantId,
        List<ITagValue>? tags = null)
    {
        return new NotifyRequest(NullLoggerFactory.Instance, new TestNotifySource(), action, objectId, new DirectRecipient(recipientId, null))
        {
            _tenantId = tenantId,
            CurrentSender = sender,
            Arguments = tags ?? []
        };
    }

    private sealed class TestAction(string id, TimeSpan? period = null, string[]? tags = null) : INotifyAction
    {
        public string ID => id;
        public List<Pattern> Patterns => [];
        public List<ITagValue> Tags { get; set; } = [];
        public TimeSpan? DeduplicationPeriod => period;
        public string[] DeduplicationTags => tags ?? [];
    }

    private sealed class TestNotifySource : INotifySource
    {
        public string Id => "test";

        public IRecipientProvider GetRecipientsProvider()
        {
            throw new NotSupportedException();
        }

        public ISubscriptionProvider GetSubscriptionProvider()
        {
            throw new NotSupportedException();
        }
    }
}
