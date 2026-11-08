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


namespace ASC.Notify.Tests.Deduplication;

/// <summary>
/// The sending code asks whether a letter is a repeat before it builds the letter, from values of its own
/// (<c>StudioNotifyService.TryAccept*</c> and the methods that ask it themselves); the engine asks again when it
/// sends the letter, from what the letter carries. Both have to arrive at the same key. Otherwise the API accepts
/// a repeat the engine then drops, after a link, an audit record or an invitation has been spent on it, or keeps
/// refusing a letter the engine has never seen.
///
/// Each case runs the sending code as production runs it, catches the letter on its way to the notify service and
/// checks that the acceptance lies under the key the engine makes of that letter. How the engine turns a request
/// into the key is covered by <c>NotifyDeduplicationServiceTests</c>; this class covers the six letters the API
/// asks about first, whose two sides are written apart and can drift.
/// </summary>
public class RepeatAcceptKeyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RoomInvite_IsAcceptedUnderTheEngineKey(bool isAgent)
    {
        await AssertAcceptedUnderTheEngineKeyAsync(async (service, scope) =>
        {
            var email = UniqueEmail();
            var roomId = $"folder_{Guid.NewGuid():N}";

            await using var acceptance = await service.TryAcceptEmailRoomInviteAsync(email, roomId, EmployeeType.User, isAgent);
            acceptance.Accepted.Should().BeTrue();

            await service.SendEmailRoomInviteAsync(email, roomId, "Room", scope.PortalUrl, EmployeeType.User, isAgent);
            acceptance.Complete();
        });
    }

    [Fact]
    public async Task DocSpaceInvite_IsAcceptedUnderTheEngineKey()
    {
        await AssertAcceptedUnderTheEngineKeyAsync(async (service, scope) =>
        {
            var email = UniqueEmail();

            await using var acceptance = await service.TryAcceptDocSpaceInviteAsync(email, EmployeeType.RoomAdmin);
            acceptance.Accepted.Should().BeTrue();

            await service.SendDocSpaceInviteAsync(email, scope.PortalUrl, EmployeeType.RoomAdmin);
            acceptance.Complete();
        });
    }

    [Fact]
    public async Task EmailActivation_IsAcceptedUnderTheEngineKey()
    {
        await AssertAcceptedUnderTheEngineKeyAsync(async (service, scope) =>
        {
            (await service.SendEmailActivationInstructionsAsync(scope.Recipient, UniqueEmail())).Should().BeTrue();
        });
    }

    [Fact]
    public async Task ProfileDeletion_IsAcceptedUnderTheEngineKey()
    {
        await AssertAcceptedUnderTheEngineKeyAsync(async (service, scope) =>
        {
            var user = (UserInfo)scope.Recipient.Clone();
            user.Email = UniqueEmail();

            (await service.SendMsgProfileDeletionAsync(user)).Should().BeTrue();
        });
    }

    [Fact]
    public async Task PortalDeactivation_IsAcceptedUnderTheEngineKey()
    {
        await AssertAcceptedUnderTheEngineKeyAsync(async (service, scope) =>
        {
            await using var acceptance = await service.TryAcceptPortalDeactivationAsync(scope.Tenant);
            acceptance.Accepted.Should().BeTrue();

            await service.SendMsgPortalDeactivationAsync(scope.Tenant, scope.PortalUrl, scope.PortalUrl);
            acceptance.Complete();
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PortalDeletion_IsAcceptedUnderTheEngineKey(bool checkActivation)
    {
        // The two ways differ in the recipient: the owner as a user, or the owner's address alone.
        await AssertAcceptedUnderTheEngineKeyAsync(async (service, scope) =>
        {
            await using var acceptance = await service.TryAcceptPortalDeletionAsync(scope.Tenant, checkActivation);
            acceptance.Accepted.Should().BeTrue();

            await service.SendMsgPortalDeletionAsync(scope.Tenant, scope.PortalUrl, false, checkActivation);
            acceptance.Complete();
        });
    }

    private static async Task AssertAcceptedUnderTheEngineKeyAsync(Func<StudioNotifyService, LetterScope, Task> send)
    {
        var stack = await LetterStackFixture.CurrentAsync();

        using var scope = await LetterScope.OpenAsync(stack, CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));

        // The real sending code, publishing to a bus that keeps the letter instead of handing it to the notify service.
        var bus = new RecordingEventBus();
        var helper = ActivatorUtilities.CreateInstance<StudioNotifyServiceHelper>(scope.Services, bus);
        var service = ActivatorUtilities.CreateInstance<StudioNotifyService>(scope.Services, helper);

        await send(service, scope);

        var letter = bus.Published.OfType<NotifyItemIntegrationEvent>().Should().ContainSingle().Subject;

        // The action as StudioNotifyWorker resolves it before handing the letter to the engine. The worker asks
        // Type.GetType from ASC.Web.Core, which holds the actions, so the name is looked up there.
        var actionType = typeof(StudioNotifyService).Assembly.GetType(letter.Action.NotifyActionType, throwOnError: true)!;
        var action = (INotifyAction)scope.Services.GetRequiredService(actionType);
        action.SuppressRepeats.Should().BeTrue($"{action.ID} is a letter the API asks about before it builds it");

        letter.SenderNames.Should().NotBeEmpty();
        letter.Recipients.Should().NotBeEmpty().And.OnlyContain(r => !r.IsGroup);

        var cache = scope.Services.GetRequiredService<IFusionCache>();

        foreach (var sender in letter.SenderNames)
        {
            foreach (var recipient in letter.Recipients)
            {
                var key = EngineKey(letter, action, sender, recipient.Id);
                var acceptance = await cache.TryGetAsync<bool>(NotifyDeduplicationService.GetAcceptedKey(key), token: TestContext.Current.CancellationToken);
                var mark = await cache.TryGetAsync<bool>(key, token: TestContext.Current.CancellationToken);

                acceptance.HasValue.Should().BeTrue($"the API accepted {action.ID} for {recipient.Id} through {sender} beside the key the engine reads");
                mark.HasValue.Should().BeFalse("the letter is only accepted, the engine has not sent it");
            }
        }
    }

    // What the engine keys a letter on once StudioNotifyWorker has made a request of it: the tenant, the action, the
    // channel, the recipient, the object, and the values of the action's DeduplicationTags among the letter's tags.
    private static string EngineKey(NotifyItemIntegrationEvent letter, INotifyAction action, string sender, string recipientId)
    {
        var values = action.DeduplicationTags.Select(tag => letter.Tags.Find(t => t.Key == tag)?.Value);

        return NotifyDeduplicationService.GetCacheKey(letter.TenantId, action.ID, sender, recipientId, letter.ObjectId, values);
    }

    private static string UniqueEmail()
    {
        return $"repeat-{Guid.NewGuid():N}@preview.onlyoffice.com";
    }
}
