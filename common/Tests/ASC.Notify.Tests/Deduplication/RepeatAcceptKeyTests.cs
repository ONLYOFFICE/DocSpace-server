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
/// (<c>StudioNotifyService.TryAccept*</c> and the methods that ask it themselves); the engine marks the letter when it
/// sends it, from what the letter carries. Both have to arrive at the same key. Otherwise the API accepts a repeat the
/// engine then drops, after a link, an audit record or an invitation has been spent on it, or keeps refusing a letter
/// the engine has never seen.
///
/// Each case runs the sending code as production runs it and catches the letter on its way to the notify service; the
/// call fails right after the letter left, so the acceptance is taken back. The letter then goes the way the notify
/// service takes it: <c>StudioNotifyWorker</c> makes the request with the client <c>WorkContext</c> registers, and the
/// request's interceptors run where the engine runs them before a channel sends. The repeat asked for afterwards can
/// then be refused by nothing but the engine's mark. The engine's channels themselves do not run: the letter host is
/// built but never started, so no sender is registered.
/// </summary>
public class RepeatAcceptKeyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RoomInvite_IsRefusedByTheEngineMark(bool isAgent)
    {
        var email = UniqueEmail();
        var roomId = $"folder_{Guid.NewGuid():N}";

        await AssertTheEngineMarkRefusesTheRepeatAsync(
            async (service, scope) =>
            {
                await using var acceptance = await service.TryAcceptEmailRoomInviteAsync(email, roomId, EmployeeType.User, isAgent);
                acceptance.Accepted.Should().BeTrue();

                await service.SendEmailRoomInviteAsync(email, roomId, "Room", scope.PortalUrl, EmployeeType.User, isAgent);
                acceptance.Complete();
            },
            async (service, _) =>
            {
                await using var acceptance = await service.TryAcceptEmailRoomInviteAsync(email, roomId, EmployeeType.User, isAgent);

                return acceptance.Accepted;
            });
    }

    [Fact]
    public async Task DocSpaceInvite_IsRefusedByTheEngineMark()
    {
        var email = UniqueEmail();

        await AssertTheEngineMarkRefusesTheRepeatAsync(
            async (service, scope) =>
            {
                await using var acceptance = await service.TryAcceptDocSpaceInviteAsync(email, EmployeeType.RoomAdmin);
                acceptance.Accepted.Should().BeTrue();

                await service.SendDocSpaceInviteAsync(email, scope.PortalUrl, EmployeeType.RoomAdmin);
                acceptance.Complete();
            },
            async (service, _) =>
            {
                await using var acceptance = await service.TryAcceptDocSpaceInviteAsync(email, EmployeeType.RoomAdmin);

                return acceptance.Accepted;
            });
    }

    [Fact]
    public async Task EmailActivation_IsRefusedByTheEngineMark()
    {
        var email = UniqueEmail();

        await AssertTheEngineMarkRefusesTheRepeatAsync(
            (service, scope) => service.SendEmailActivationInstructionsAsync(scope.Recipient, email),
            (service, scope) => service.SendEmailActivationInstructionsAsync(scope.Recipient, email));
    }

    [Fact]
    public async Task ProfileDeletion_IsRefusedByTheEngineMark()
    {
        var email = UniqueEmail();

        await AssertTheEngineMarkRefusesTheRepeatAsync(
            (service, scope) => service.SendMsgProfileDeletionAsync(WithEmail(scope.Recipient, email)),
            (service, scope) => service.SendMsgProfileDeletionAsync(WithEmail(scope.Recipient, email)));
    }

    [Fact]
    public async Task PortalDeactivation_IsRefusedByTheEngineMark()
    {
        await AssertTheEngineMarkRefusesTheRepeatAsync(
            async (service, scope) =>
            {
                await using var acceptance = await service.TryAcceptPortalDeactivationAsync(scope.Tenant);
                acceptance.Accepted.Should().BeTrue();

                await service.SendMsgPortalDeactivationAsync(scope.Tenant, scope.PortalUrl, scope.PortalUrl);
                acceptance.Complete();
            },
            async (service, scope) =>
            {
                await using var acceptance = await service.TryAcceptPortalDeactivationAsync(scope.Tenant);

                return acceptance.Accepted;
            });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PortalDeletion_IsRefusedByTheEngineMark(bool checkActivation)
    {
        // The two ways differ in the recipient: the owner as a user, or the owner's address alone.
        await AssertTheEngineMarkRefusesTheRepeatAsync(
            async (service, scope) =>
            {
                await using var acceptance = await service.TryAcceptPortalDeletionAsync(scope.Tenant, checkActivation);
                acceptance.Accepted.Should().BeTrue();

                await service.SendMsgPortalDeletionAsync(scope.Tenant, scope.PortalUrl, false, checkActivation);
                acceptance.Complete();
            },
            async (service, scope) =>
            {
                await using var acceptance = await service.TryAcceptPortalDeletionAsync(scope.Tenant, checkActivation);

                return acceptance.Accepted;
            });
    }

    private static async Task AssertTheEngineMarkRefusesTheRepeatAsync(
        Func<StudioNotifyService, LetterScope, Task> send,
        Func<StudioNotifyService, LetterScope, Task<bool>> askAgain)
    {
        var stack = await LetterStackFixture.CurrentAsync();

        using var scope = await LetterScope.OpenAsync(stack, CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));

        // The real sending code, publishing to a bus that keeps the letter and then fails, as a broker could: the
        // acceptance of the request is taken back, so it cannot be what refuses the repeat below.
        var bus = new RecordingEventBus { FailAfterRecording = true };
        var helper = ActivatorUtilities.CreateInstance<StudioNotifyServiceHelper>(scope.Services, bus);
        var service = ActivatorUtilities.CreateInstance<StudioNotifyService>(scope.Services, helper);

        await Assert.ThrowsAsync<InvalidOperationException>(() => send(service, scope));
        bus.FailAfterRecording = false;

        var letter = bus.Published.OfType<NotifyItemIntegrationEvent>().Should().ContainSingle().Subject;

        await PassThroughTheEngineAsync(scope, letter);

        var accepted = await askAgain(service, scope);

        accepted.Should().BeFalse($"the engine marked {letter.Action.Id} under the key the sending code reads");
    }

    // What the notify service does with a letter up to the moment a channel would send it.
    private static async Task PassThroughTheEngineAsync(LetterScope scope, NotifyItemIntegrationEvent letter)
    {
        await scope.Services.GetRequiredService<StudioNotifyWorker>().OnMessageAsync(letter);

        var queue = scope.Services.GetRequiredService<ChannelReader<NotifyRequest>>();
        var requests = new List<NotifyRequest>();

        while (queue.TryRead(out var request))
        {
            requests.Add(request);
        }

        requests.Should().NotBeEmpty("StudioNotifyWorker queues a request for every recipient of the letter");

        using var engineScope = scope.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();

        foreach (var request in requests)
        {
            request._interceptors.Should().Contain(i => i is DuplicateMessageInterceptor, "the notify client WorkContext registers carries the repeat check");

            foreach (var sender in request._senderNames)
            {
                // as NotifyEngine does for each channel before it hands the letter over
                request.CurrentSender = sender;

                var prevented = await request.Intercept(InterceptorPlace.MessageSend, engineScope);

                prevented.Should().BeFalse("the first letter goes out, and the repeat check marks it");
            }
        }
    }

    private static UserInfo WithEmail(UserInfo user, string email)
    {
        var copy = (UserInfo)user.Clone();
        copy.Email = email;

        return copy;
    }

    private static string UniqueEmail()
    {
        return $"repeat-{Guid.NewGuid():N}@preview.onlyoffice.com";
    }
}
