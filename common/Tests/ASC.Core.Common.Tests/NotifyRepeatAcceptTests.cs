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
/// The question the sending code asks before it builds what only a new letter needs - a confirmation link, an audit
/// record, a spent invitation: a letter already sent, or a request for it accepted a moment ago, is refused, and an
/// accepted request books the letter for a moment so that a burst of calls builds it once.
/// </summary>
/// <remarks>
/// Each test gets a cache of its own; the cache is memory-only and the lock in-memory.
/// </remarks>
public class NotifyRepeatAcceptTests
{
    private const int TenantId = 1;
    private const string EmailSender = "email.sender";
    private const string Recipient = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";
    private const string RoomId = "folder_10";

    [Fact]
    public async Task LetterAlreadySent_IsRefused()
    {
        var service = CreateService();
        var action = new TestAction("portal_deactivate", tags: ["LinkAddress"]);

        // The engine passes an empty object id for a letter without an object, the sending code passes none.
        await service.IsDuplicateAsync(Request(action, "", [new TagValue("LinkAddress", "owner@example.com")]));
        var accepted = await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, null, ["owner@example.com"]);

        accepted.Should().BeFalse();
    }

    [Fact]
    public async Task SecondRequestRightAfterTheFirst_IsRefused()
    {
        // The engine has not sent the first letter yet - the queue is slow, or the letter never reaches it - and still
        // the second call must not build another link.
        var service = CreateService();
        var action = new TestAction("profile_delete");

        var first = await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, RoomId, []);
        var second = await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, RoomId, []);

        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    [Fact]
    public async Task AcceptedRequest_IsNotHeldBackByTheEngine()
    {
        var service = CreateService();
        var action = new TestAction("profile_delete");

        await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, RoomId, []);
        var letter = await service.IsDuplicateAsync(Request(action, RoomId));

        letter.Should().BeFalse("the request was accepted so that this letter goes out");
    }

    [Fact]
    public async Task AfterTheAcceptance_ARequestIsAcceptedAgain()
    {
        // The engine never marked the letter (its delivery failed, or it never reached the engine), so once the
        // acceptance runs out the letter may be asked for again.
        var service = CreateService(accepted: "00:00:01");
        var action = new TestAction("profile_delete");

        await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, RoomId, []);
        await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);
        var again = await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, RoomId, []);

        again.Should().BeTrue();
    }

    [Fact]
    public async Task SentLetter_StaysRefusedAfterTheAcceptanceRunsOut()
    {
        // The acceptance and the engine's mark share the key, so the mark has to replace the acceptance with its own
        // life rather than run out with it.
        var service = CreateService(accepted: "00:00:01");
        var action = new TestAction("profile_delete");

        await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, RoomId, []);
        await service.IsDuplicateAsync(Request(action, RoomId));
        await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);
        var again = await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, RoomId, []);

        again.Should().BeFalse();
    }

    [Fact]
    public async Task FailedDelivery_TakesTheAcceptanceBack()
    {
        var service = CreateService();
        var action = new TestAction("profile_delete");

        await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, RoomId, []);
        var request = Request(action, RoomId);
        await service.IsDuplicateAsync(request);
        await service.ReleaseAsync(request);
        var again = await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, RoomId, []);

        again.Should().BeTrue("nobody got the letter, so asking for it again must not wait for the acceptance to run out");
    }

    [Fact]
    public async Task LetterSentByAnotherNode_IsHeldBackDespiteTheAcceptanceInMemory()
    {
        // Two nodes share the distributed level and the lock. Node B accepted the request and holds the acceptance in
        // its own memory; node A sends the letter first. B's engine must see A's mark in the distributed level rather
        // than its own stale acceptance.
        var distributed = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var locks = new InMemoryLockProvider();
        var nodeA = CreateService(cache: CreateNodeCache(distributed), locks: locks);
        var nodeB = CreateService(cache: CreateNodeCache(distributed), locks: locks);
        var action = new TestAction("profile_delete");

        await nodeB.TryAcceptAsync(TenantId, action, EmailSender, Recipient, RoomId, []);
        var sentByA = await nodeA.IsDuplicateAsync(Request(action, RoomId));
        var sentByB = await nodeB.IsDuplicateAsync(Request(action, RoomId));

        sentByA.Should().BeFalse();
        sentByB.Should().BeTrue();
    }

    [Fact]
    public async Task AnotherAddress_IsAcceptedSeparately()
    {
        var service = CreateService();
        var action = new TestAction("portal_deactivate", tags: ["LinkAddress"]);

        await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, null, ["owner@example.com"]);
        var otherAddress = await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, null, ["new@example.com"]);

        otherAddress.Should().BeTrue();
    }

    [Fact]
    public async Task LetterNotOptedIn_IsAlwaysAccepted()
    {
        // The mark is left while a letter of the same name still drops repeats; asked about now that it does not, the
        // letter goes on, every time.
        var service = CreateService();
        await service.IsDuplicateAsync(Request(new TestAction("change_password_v115"), null));
        var action = new TestAction("change_password_v115", suppressRepeats: false);

        var first = await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, null, []);
        var second = await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, null, []);

        first.Should().BeTrue();
        second.Should().BeTrue();
    }

    [Fact]
    public async Task WithoutEveryKeyTagValue_IsAlwaysAccepted()
    {
        // Marks lie under exactly the keys a request without every value would read, so only the check of the values
        // can keep that request from being refused.
        var cache = new FusionCache(new FusionCacheOptions());
        var service = CreateService(cache: cache);
        var action = new TestAction("user_role_changed", tags: ["AccessLevel"]);
        await cache.SetAsync(NotifyDeduplicationService.GetCacheKey(TenantId, action.ID, EmailSender, Recipient, RoomId, []), true, TimeSpan.FromMinutes(1), token: TestContext.Current.CancellationToken);
        await cache.SetAsync(NotifyDeduplicationService.GetCacheKey(TenantId, action.ID, EmailSender, Recipient, RoomId, [""]), true, TimeSpan.FromMinutes(1), token: TestContext.Current.CancellationToken);

        var noValues = await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, RoomId, []);
        var emptyValue = await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, RoomId, [""]);

        noValues.Should().BeTrue();
        emptyValue.Should().BeTrue();
    }

    private static NotifyDeduplicationService CreateService(string? accepted = null, IFusionCache? cache = null, InMemoryLockProvider? locks = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["core:notify:deduplication:accepted"] = accepted
            })
            .Build();

        return new NotifyDeduplicationService(
            configuration,
            cache ?? new FusionCache(new FusionCacheOptions()),
            locks ?? new InMemoryLockProvider(),
            NullLogger<NotifyDeduplicationService>.Instance);
    }

    private static FusionCache CreateNodeCache(IDistributedCache distributed)
    {
        var cache = new FusionCache(new FusionCacheOptions());
        cache.SetupDistributedCache(distributed, new FusionCacheSystemTextJsonSerializer());

        return cache;
    }

    private static NotifyRequest Request(INotifyAction action, string? objectId, List<ITagValue>? tags = null)
    {
        return new NotifyRequest(NullLoggerFactory.Instance, new TestNotifySource(), action, objectId, new DirectRecipient(Recipient, null))
        {
            _tenantId = TenantId,
            CurrentSender = EmailSender,
            Arguments = tags ?? []
        };
    }

    private sealed class TestAction(string id, bool suppressRepeats = true, string[]? tags = null) : INotifyAction
    {
        public string ID => id;
        public List<Pattern> Patterns => [];
        public List<ITagValue> Tags { get; set; } = [];
        public bool SuppressRepeats => suppressRepeats;
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
