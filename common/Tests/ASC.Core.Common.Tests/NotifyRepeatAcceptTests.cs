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
/// accepted request books the letter for a moment so that a burst of calls builds it once. The booking lives on a key
/// of its own and is taken back when the sending code fails before it hands the letter over.
/// </summary>
/// <remarks>
/// Each test gets a cache of its own; the cache is memory-only and the lock in-memory, except where two nodes share a
/// distributed level.
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
        await using var acceptance = await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, null, ["owner@example.com"]);

        acceptance.Accepted.Should().BeFalse();
    }

    [Fact]
    public async Task SecondRequestRightAfterTheFirst_IsRefused()
    {
        // The engine has not sent the first letter yet - the queue is slow, or the letter never reaches it - and still
        // the second call must not build another link.
        var service = CreateService();
        var action = new TestAction("profile_delete");

        var first = await AcceptAsync(service, action);
        var second = await AcceptAsync(service, action);

        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    [Fact]
    public async Task AcceptedRequest_IsNotHeldBackByTheEngine()
    {
        var service = CreateService();
        var action = new TestAction("profile_delete");

        await AcceptAsync(service, action);
        var letter = await service.IsDuplicateAsync(Request(action, RoomId));

        letter.Should().BeFalse("the request was accepted so that this letter goes out");
    }

    [Fact]
    public async Task AfterTheAcceptance_ARequestIsAcceptedAgain()
    {
        // The engine never marked the letter (it never reached the engine), so once the acceptance runs out the letter
        // may be asked for again.
        var service = CreateService(accepted: "00:00:01");
        var action = new TestAction("profile_delete");

        await AcceptAsync(service, action);
        await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);
        var again = await AcceptAsync(service, action);

        again.Should().BeTrue();
    }

    [Fact]
    public async Task SentLetter_StaysRefusedAfterTheAcceptanceRunsOut()
    {
        var service = CreateService(accepted: "00:00:01");
        var action = new TestAction("profile_delete");

        await AcceptAsync(service, action);
        await service.IsDuplicateAsync(Request(action, RoomId));
        await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);
        var again = await AcceptAsync(service, action);

        again.Should().BeFalse();
    }

    [Fact]
    public async Task LateAcceptance_LeavesTheSentMarkInPlace()
    {
        // A request reads no mark; before it records its acceptance, another node sends the letter and marks it. The
        // acceptance written after that must not take the place of the mark, or the letter could go out again once the
        // acceptance runs out.
        var distributed = new HookedDistributedCache();
        var locks = new InMemoryLockProvider();
        var nodeA = CreateService(cache: CreateNodeCache(distributed), locks: locks);
        var nodeB = CreateService(accepted: "00:00:01", cache: CreateNodeCache(distributed), locks: locks);
        var action = new TestAction("profile_delete");
        var mark = NotifyDeduplicationService.GetCacheKey(TenantId, action.ID, EmailSender, Recipient, RoomId, []);

        distributed.AfterFirstRead(mark, async () => await nodeA.IsDuplicateAsync(Request(action, RoomId)));

        var late = await AcceptAsync(nodeB, action);
        await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);
        var again = await AcceptAsync(nodeB, action);

        late.Should().BeTrue("the request read no mark before the other node sent the letter");
        again.Should().BeFalse("the letter went out within the period");
    }

    [Fact]
    public async Task LetterSentByAnotherNode_IsHeldBackDespiteTheAcceptanceInMemory()
    {
        // Two nodes share the distributed level and the lock. Node B accepted the request and holds the acceptance in
        // its own memory; node A sends the letter first. B's engine must see A's mark rather than let its own letter go.
        var distributed = new HookedDistributedCache();
        var locks = new InMemoryLockProvider();
        var nodeA = CreateService(cache: CreateNodeCache(distributed), locks: locks);
        var nodeB = CreateService(cache: CreateNodeCache(distributed), locks: locks);
        var action = new TestAction("profile_delete");

        await AcceptAsync(nodeB, action);
        var sentByA = await nodeA.IsDuplicateAsync(Request(action, RoomId));
        var sentByB = await nodeB.IsDuplicateAsync(Request(action, RoomId));

        sentByA.Should().BeFalse();
        sentByB.Should().BeTrue();
    }

    [Fact]
    public async Task FailedDelivery_TakesTheAcceptanceBack()
    {
        var service = CreateService();
        var action = new TestAction("profile_delete");

        await AcceptAsync(service, action);
        var request = Request(action, RoomId);
        await service.IsDuplicateAsync(request);
        await service.ReleaseAsync(request);
        var again = await AcceptAsync(service, action);

        again.Should().BeTrue("nobody got the letter, so asking for it again must not wait for the acceptance to run out");
    }

    [Fact]
    public async Task AcceptanceNotCompleted_IsTakenBack()
    {
        // The sending code failed before it handed the letter over - the link, the audit record or the publishing
        // threw - so the repeat right after the failure must build the letter rather than be refused for one never sent.
        var service = CreateService();
        var action = new TestAction("profile_delete");

        await using (var failed = await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, RoomId, []))
        {
            failed.Accepted.Should().BeTrue();
        }

        var again = await AcceptAsync(service, action);

        again.Should().BeTrue();
    }

    [Fact]
    public async Task CompletedAcceptance_IsKept()
    {
        var service = CreateService();
        var action = new TestAction("profile_delete");

        await using (var sent = await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, RoomId, []))
        {
            sent.Complete();
        }

        var again = await AcceptAsync(service, action);

        again.Should().BeFalse();
    }

    [Fact]
    public async Task ZeroAcceptance_StillRefusesALetterAlreadySent()
    {
        // A zero acceptance turns off holding back a burst, not the check of a letter the engine already sent.
        var service = CreateService(accepted: "00:00:00");
        var action = new TestAction("profile_delete");

        await service.IsDuplicateAsync(Request(action, RoomId));
        var again = await AcceptAsync(service, action);

        again.Should().BeFalse();
    }

    [Fact]
    public async Task ZeroAcceptance_RecordsNothing()
    {
        var service = CreateService(accepted: "00:00:00");
        var action = new TestAction("profile_delete");

        var first = await AcceptAsync(service, action);
        var second = await AcceptAsync(service, action);

        first.Should().BeTrue();
        second.Should().BeTrue();
    }

    [Fact]
    public async Task AcceptanceOfAnotherRequest_IsNotTakenBack()
    {
        // A request failed long after its acceptance ran out; meanwhile another request was accepted and its letter went
        // out. The late failure must leave the second acceptance alone, or the next call builds the letter again.
        var service = CreateService(accepted: "00:00:01");
        var action = new TestAction("profile_delete");

        var failed = await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, RoomId, []);
        await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);
        var sent = await AcceptAsync(service, action);
        await failed.DisposeAsync();
        var again = await AcceptAsync(service, action);

        failed.Accepted.Should().BeTrue();
        sent.Should().BeTrue("the first acceptance had run out");
        again.Should().BeFalse("the late failure took back only its own acceptance");
    }

    [Fact]
    public async Task ReleaseANodeMissed_DoesNotHoldBackTheNextLetter()
    {
        // Node B saw the mark while suppressing a repeat; node A's delivery then failed and took the mark back, and the
        // message telling B so never came (there is no backplane here). B must not keep refusing from its memory.
        var distributed = new HookedDistributedCache();
        var locks = new InMemoryLockProvider();
        var nodeA = CreateService(cache: CreateNodeCache(distributed), locks: locks);
        var nodeB = CreateService(cache: CreateNodeCache(distributed), locks: locks);
        var action = new TestAction("profile_delete");

        var failed = Request(action, RoomId);
        await nodeA.IsDuplicateAsync(failed);
        var suppressed = await nodeB.IsDuplicateAsync(Request(action, RoomId));
        await nodeA.ReleaseAsync(failed);
        var next = await nodeB.IsDuplicateAsync(Request(action, RoomId));

        suppressed.Should().BeTrue();
        next.Should().BeFalse("nobody got the letter, and the mark is gone from the distributed level");
    }

    [Fact]
    public async Task ReleaseANodeMissed_DoesNotHoldBackTheNextRequest()
    {
        var distributed = new HookedDistributedCache();
        var locks = new InMemoryLockProvider();
        var nodeA = CreateService(cache: CreateNodeCache(distributed), locks: locks);
        var nodeB = CreateService(cache: CreateNodeCache(distributed), locks: locks);
        var action = new TestAction("profile_delete");

        var failed = Request(action, RoomId);
        await nodeA.IsDuplicateAsync(failed);
        var refused = await AcceptAsync(nodeB, action);
        await nodeA.ReleaseAsync(failed);
        var next = await AcceptAsync(nodeB, action);

        refused.Should().BeFalse();
        next.Should().BeTrue("nobody got the letter, and the mark is gone from the distributed level");
    }

    [Fact]
    public async Task AnotherAddress_IsAcceptedSeparately()
    {
        var service = CreateService();
        var action = new TestAction("portal_deactivate", tags: ["LinkAddress"]);

        await AcceptAsync(service, action, objectId: null, "owner@example.com");
        var otherAddress = await AcceptAsync(service, action, objectId: null, "new@example.com");

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

        var first = await AcceptAsync(service, action, objectId: null);
        var second = await AcceptAsync(service, action, objectId: null);

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

        var noValues = await AcceptAsync(service, action);
        var emptyValue = await AcceptAsync(service, action, RoomId, "");

        noValues.Should().BeTrue();
        emptyValue.Should().BeTrue();
    }

    // Asks, and hands the letter over when accepted, as the sending code does on success.
    private static async Task<bool> AcceptAsync(NotifyDeduplicationService service, INotifyAction action, string? objectId = RoomId, params string[] values)
    {
        await using var acceptance = await service.TryAcceptAsync(TenantId, action, EmailSender, Recipient, objectId, values);
        acceptance.Complete();

        return acceptance.Accepted;
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

    /// <summary>
    /// A distributed level shared by the nodes of a test, which can let something happen right after a node read a
    /// key and before it acts on what it read.
    /// </summary>
    private sealed class HookedDistributedCache : IDistributedCache
    {
        private readonly MemoryDistributedCache _inner = new(Options.Create(new MemoryDistributedCacheOptions()));
        private string? _hookedKey;
        private Func<Task>? _hook;

        public void AfterFirstRead(string key, Func<Task> hook)
        {
            _hookedKey = key;
            _hook = hook;
        }

        public byte[]? Get(string key)
        {
            return _inner.Get(key);
        }

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            var value = await _inner.GetAsync(key, token);

            // FusionCache adds a prefix of its own to the keys it stores on this level
            if (_hookedKey is not null && key.EndsWith(_hookedKey, StringComparison.Ordinal) && Interlocked.Exchange(ref _hook, null) is { } hook)
            {
                await hook();
            }

            return value;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _inner.Set(key, value, options);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            return _inner.SetAsync(key, value, options, token);
        }

        public void Refresh(string key)
        {
            _inner.Refresh(key);
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            return _inner.RefreshAsync(key, token);
        }

        public void Remove(string key)
        {
            _inner.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            return _inner.RemoveAsync(key, token);
        }
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
