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

namespace ASC.Notify.Tests;

/// <summary>
/// Who a periodic letter reaches once it has decided that today is its day. This is the step between the
/// two suites that already exist — <see cref="PeriodicLetterScheduleTests"/> asks a letter *whether* it
/// goes out and the letter tests render *what* it says, and neither touches the delivery in between: the
/// recipient groups, the owner and the payer unioned in without duplicates, and the unsubscribe.
///
/// It needs the stack rather than a context alone, because every one of those answers comes out of the
/// portal — <c>StudioNotifyHelper</c> reads the groups from the database and the subscription from the
/// subscription provider. What it does not need is a mail server: the letters are handed to a client
/// that writes down who it was asked to send to and sends nothing.
/// </summary>
public class PeriodicLetterDeliveryTests
{
    private static readonly string _senderName = ASC.Core.Configuration.Constants.NotifyEMailSenderSysName;

    private static async ValueTask<LetterStackFixture> GetStackAsync()
    {
        return await TestContext.Current.GetFixture<LetterStackFixture>()
            ?? throw new InvalidOperationException(
                $"No stack in the test context. {nameof(LetterStackFixture)} is registered with "
                + "[assembly: AssemblyFixture] and starts before any letter test runs.");
    }

    private static Task<LetterScope> OpenScopeAsync(LetterStackFixture stack)
    {
        return LetterScope.OpenAsync(stack, CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));
    }

    /// <summary>
    /// Runs the letter's real <c>SendAsync</c> against the portal and returns who it asked for, by id.
    /// The portal state is the one every letter test renders against: which letter goes out today is
    /// <see cref="PeriodicLetterScheduleTests"/>'s question, and <c>SendAsync</c> never asks it.
    /// </summary>
    private static async Task<List<Guid>> RecipientsOfAsync<TAction>(LetterScope scope)
        where TAction : BasePeriodicNotifyAction
    {
        var action = scope.Services.GetRequiredService<TAction>();
        var client = new RecordingNotifyClient();

        await action.SendAsync(PeriodicLetterContexts.Paid(scope.Tenant), client, _senderName);

        return client.Sent.ConvertAll(sent => Guid.Parse(sent.Recipient.ID));
    }

    /// <summary>
    /// A letter addressed to nobody but the owner reaches the owner. The owner is not one of the groups
    /// <c>GetRecipientsAsync</c> knows about, so without <c>ToOwner</c> unioning them in the letter goes
    /// nowhere at all — and the tariff warnings are the ones nobody else may see.
    /// </summary>
    [Fact]
    public async Task ToOwner_ReachesTheOwnerWhenNoGroupWasAskedFor()
    {
        var stack = await GetStackAsync();

        using var scope = await OpenScopeAsync(stack);

        var recipients = await RecipientsOfAsync<SaasAdminWarningAfterThreeMonthsV1NotifyAction>(scope);

        recipients.Should().Contain(stack.Portal.Owner.Id);
    }

    /// <summary>
    /// The owner is put into <c>GroupAdmin</c> at tenant creation, so a letter that asks for the admins
    /// *and* the owner asks for the same person twice. Sending it twice is the reader's problem, and it
    /// is one <c>DistinctBy</c> away — which is why it is worth a test that would notice that going
    /// missing.
    /// </summary>
    [Fact]
    public async Task ToOwner_OnTopOfToAdmins_DoesNotDeliverTwice()
    {
        var stack = await GetStackAsync();

        using var scope = await OpenScopeAsync(stack);

        var recipients = await RecipientsOfAsync<SaasAdminHandyAppsV1NotifyAction>(scope);

        recipients.Should().Contain(stack.Portal.Owner.Id)
            .And.OnlyHaveUniqueItems("the owner is an admin already, and nobody may get the same letter twice");
    }

    /// <summary>
    /// Billing knows of no payer on a portal that was never bought, and <c>ToPayer</c> has to survive
    /// that: <c>GetUserByEmailAsync</c> answers with <c>LostUser</c> rather than null, and adding
    /// <em>that</em> to the recipients would address the letter to nobody.
    /// </summary>
    [Fact]
    public async Task ToPayer_AddsNobodyWhenBillingKnowsOfNobody()
    {
        var stack = await GetStackAsync();

        using var scope = await OpenScopeAsync(stack);

        var recipients = await RecipientsOfAsync<SaasOwnerPaymentWarningGracePeriodExpiredNotifyAction>(scope);

        recipients.Should().Contain(stack.Portal.Owner.Id)
            .And.NotContain(ASC.Core.Users.Constants.LostUser.Id);
    }

    /// <summary>
    /// The letter leaves <c>SendAsync</c> carrying the recipient's culture as a tag. That tag is not
    /// decoration: <c>SendNoticeToAsync</c> only queues the request, and by the time
    /// <c>NotifyEngine</c> resolves the pattern it is on its own thread with its own ambient culture —
    /// <c>NotifyRequest.GetCulture</c> reads this tag to put the right one back. Lose it and every
    /// periodic letter silently renders in whatever culture the sender process happens to be in.
    /// </summary>
    [Fact]
    public async Task TheLetterCarriesTheRecipientsCultureAsATag()
    {
        var stack = await GetStackAsync();

        using var scope = await OpenScopeAsync(stack);

        var action = scope.Services.GetRequiredService<SaasAdminWarningAfterThreeMonthsV1NotifyAction>();
        var client = new RecordingNotifyClient();

        await action.SendAsync(PeriodicLetterContexts.Paid(scope.Tenant), client, _senderName);

        client.Sent.Should().NotBeEmpty();

        var culture = action.Tags.Should().ContainSingle(tag => tag.Tag == CommonTags.Culture).Subject;

        // The owner as the database has them, not scope.Recipient: SendAsync resolves its own
        // recipients, and the scope's copy carries the culture the test asked for.
        var owner = await scope.Services.GetRequiredService<UserManager>().GetUsersAsync(stack.Portal.Owner.Id);

        var expected = owner.CultureName is { Length: > 0 } ? owner.GetCulture() : scope.Tenant.GetCulture();

        culture.Value.Should().Be(expected.Name);
    }

    /// <summary>
    /// A recipient who is subscribed gets both kinds of letter. That is the half of
    /// <c>RequiresSubscription</c> this stack can answer: the other half — that an unsubscribed recipient
    /// is dropped from the marketing letter and kept on the payment notice — is asserted in
    /// <c>PeriodicLetterScheduleTests.OnlyTheMarketingLettersHonourTheUnsubscribe</c> instead, over the
    /// flag itself.
    /// </summary>
    /// <remarks>
    /// Not over an actual unsubscribe, deliberately. <c>StudioNotifyHelper.SubscribeToNotifyAsync(…,
    /// false)</c> writes to the database, but <c>IsSubscribedToNotifyAsync</c> keeps answering
    /// <c>true</c> in this host: the subscription store is an in-memory cache invalidated over the
    /// backplane, and <see cref="LetterHost"/> runs with Redis and RabbitMQ switched off. A test that
    /// unsubscribed here would be measuring that, not the letter.
    /// </remarks>
    [Fact]
    public async Task BothKindsOfLetterReachASubscribedRecipient()
    {
        var stack = await GetStackAsync();

        using var scope = await OpenScopeAsync(stack);

        var helper = scope.Services.GetRequiredService<StudioNotifyHelper>();
        var periodic = scope.Services.GetRequiredService<PeriodicNotifyAction>();
        var owner = stack.Portal.Owner.Id;

        (await helper.IsSubscribedToNotifyAsync(owner, periodic))
            .Should().BeTrue("a portal owner is subscribed to the periodic notifications to begin with");

        (await RecipientsOfAsync<SaasAdminHandyAppsV1NotifyAction>(scope))
            .Should().Contain(owner, "the marketing letter asks the subscription and is told yes");

        (await RecipientsOfAsync<SaasOwnerPaymentWarningGracePeriodExpiredNotifyAction>(scope))
            .Should().Contain(owner, "the payment notice does not ask at all");
    }

    /// <summary>
    /// Stands in for the notify client the tariff job registers. It records what it was asked to send and
    /// sends nothing: the point here is the recipient list, and rendering the letter is what the letter
    /// tests are for.
    /// </summary>
    private sealed class RecordingNotifyClient : INotifyClient
    {
        public List<(INotifyAction Action, IRecipient Recipient)> Sent { get; } = [];

        public Task SendNoticeToAsync(INotifyAction action, IRecipient recipient, string senderNames)
        {
            Sent.Add((action, recipient));

            return Task.CompletedTask;
        }

        // Nothing else is reachable from BasePeriodicNotifyAction.SendAsync. A call here means the
        // sending code changed and this stand-in stopped standing in for it.
        public void AddInterceptor(ISendInterceptor interceptor) => throw NotUsed();

        public Task SendNoticeAsync(INotifyAction action, string objectID, IRecipient recipient, bool checkSubscription) => throw NotUsed();

        public Task SendNoticeAsync(INotifyAction action, string objectID, IRecipient recipient) => throw NotUsed();

        public Task SendNoticeAsync(INotifyAction action, string objectID, IRecipient recipient, string senderNames) => throw NotUsed();

        public Task SendNoticeAsync(INotifyAction action, string objectID, IRecipient[] recipient, string senderNames) => throw NotUsed();

        public Task SendNoticeToAsync(INotifyAction action, string objectID, IRecipient[] recipients, string[] senderNames, bool checkSubsciption) => throw NotUsed();

        public Task SendNoticeToAsync(INotifyAction action, IRecipient[] recipients, string[] senderNames) => throw NotUsed();

        private static NotSupportedException NotUsed([CallerMemberName] string member = "")
        {
            return new NotSupportedException(
                $"A periodic letter reached INotifyClient.{member}, which it never used to. Either the "
                + "sending code changed, or this stand-in is being asked the wrong thing.");
        }
    }
}
