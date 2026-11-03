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

namespace ASC.AI.Tests.Tests.AttachmentTests;

/// <summary>
/// <c>/api/2.0/ai/attachments/*</c> with the portal AI switch off (<c>PUT /api/2.0/settings/ai-access</c>,
/// <c>enabled: false</c>). Every route that reaches the AI service is behind the switch: the save routes, the reads, the
/// link and the deletes answer 403 once the change has propagated to the AI service, and the authentication check still runs
/// first (an unauthenticated 401 does not turn into a 403).
/// </summary>
/// <remarks>
/// The switch propagates, and propagation is not instant: a 200 can still slip through after the first 403. So a gated route
/// is polled for a short run of refusals before the rest of the calls are required to be refused too. The mutating versions
/// aim at an id that does not exist, so the calls that slip through cannot destroy a draft the test still needs. The
/// "AI Tools wallet service not paid for" case of the TypeScript suite needs the AI gateway's wallet and is not ported.
/// </remarks>
[Trait("Category", "AiDisabled")]
[Trait("Feature", "AI/Attachments")]
public class AttachmentAiDisabledTests(AspireAppFixture fixture) : AttachmentTestBase(fixture)
{
    /// <summary>Consecutive refusals that count as "the switch has taken effect"; one is not enough mid-propagation.</summary>
    private const int SettleStreak = 3;

    /// <summary>Calls required to stay refused afterwards.</summary>
    private const int RefusedAttempts = 12;

    /// <summary>Calls used to judge whether a route answers one status or several.</summary>
    private const int ConsistencyAttempts = 12;

    /// <summary>How long the flipped setting is given to propagate.</summary>
    private static readonly TimeSpan _settleTimeout = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(150);

    private async Task<List<int>> CollectStatusesAsync(Func<Task<int>> call, int attempts)
    {
        var statuses = new List<int>();

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            statuses.Add(await call());
            await Task.Delay(_pollInterval, TestContext.Current.CancellationToken);
        }

        return statuses;
    }

    /// <summary>
    /// Asserts a route stays refused: polls until it has answered 403 <see cref="SettleStreak"/> times in a row, then requires
    /// the next <see cref="RefusedAttempts"/> calls to be refused too. Without the poll the propagation window alone fails
    /// the test; without the run afterwards a route that refuses only half the calls would look correctly gated.
    /// </summary>
    private async Task ExpectRefusedOnceSettledAsync(Func<Task<int>> call, string label)
    {
        var seen = new List<int>();
        var streak = 0;
        var deadline = DateTime.UtcNow + _settleTimeout;

        while (true)
        {
            var status = await call();
            seen.Add(status);
            streak = status == 403 ? streak + 1 : 0;

            if (streak >= SettleStreak || DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(_pollInterval, TestContext.Current.CancellationToken);
        }

        streak.Should().Be(SettleStreak, $"{label} was never refused {SettleStreak} times in a row within {_settleTimeout.TotalSeconds} s: {string.Join(",", seen)}");

        var after = await CollectStatusesAsync(call, RefusedAttempts);

        after.Where(status => status != 403).Should().BeEmpty($"{label} answered these once the refusal had settled: {string.Join(",", after)}");
    }

    [Fact]
    public async Task LinkToMessage_WhenAiAccessIsDisabled_Returns403()
    {
        var (threadId, messageId) = await ThreadWithMessageAsync();
        var draftId = await SaveDraftAsync(Owner, "Autotest before-off.docx");

        await SetPortalAiAccessAsync(false);

        await ExpectRefusedOnceSettledAsync(async () => (await LinkAsync([draftId], messageId, threadId)).Status, "link-to-message");
    }

    [Fact]
    public async Task Delete_WhenAiAccessIsDisabled_Returns403()
    {
        // Aimed at an id that does not exist. The route answers `{success:true}` for unknown ids just as it does for real
        // ones, so the gate is measured exactly the same way — but the calls that slip through during propagation cannot
        // destroy anything, which they would if a real draft were the target.
        var survivor = await SaveDraftAsync(Owner, "Autotest bystander.docx");

        await SetPortalAiAccessAsync(false);

        await ExpectRefusedOnceSettledAsync(async () => (await DeleteAsync(MissingId)).Status, "delete");

        // Turning the switch back on shows the refused calls left the real draft alone.
        await SetPortalAiAccessAsync(true);
        await ExpectStoredAsync(survivor, "a draft that was never targeted");
    }

    [Fact]
    public async Task DeleteMany_WhenAiAccessIsDisabled_Returns403()
    {
        await SetPortalAiAccessAsync(false);

        await ExpectRefusedOnceSettledAsync(async () => (await DeleteManyAsync(MissingId)).Status, "delete-many");
    }

    [Fact]
    public async Task SaveFile_AnonymousWhenAiAccessIsDisabled_StillGets401()
    {
        // The auth check runs before the AI gate, so turning the feature off does not turn an unauthenticated 401 into a 403.
        await SetPortalAiAccessAsync(false);
        await ActAsAsync(null);

        var outcome = await SaveFileAsync(SaveRequest("1"));

        outcome.Status.Should().Be(401);
    }

    [Fact]
    public async Task SaveFile_TheFileRoutesAreGatedWhenAiAccessIsDisabled()
    {
        // No requirement says how far the switch reaches into /ai/attachments/*, so this is recorded as plain behaviour.
        // The file is uploaded while AI is still on, so the 403 below cannot be the upload.
        var fileId = await BackingFileIdAsync(Owner, "Autotest off.docx", "created with AI off");

        await SetPortalAiAccessAsync(false);
        await ActAsAsync(Owner);

        var refused = await AttachByIdAsync(fileId);

        refused.Status.Should().Be(403);
        refused.Error.Should().Be("Forbidden");
    }

    [Fact]
    public async Task SaveFilesMany_TheBatchRouteIsGatedTheSameWayAsTheSingleOne()
    {
        var fileId = await BackingFileIdAsync(Owner, "off-batch.docx", "x");

        await SetPortalAiAccessAsync(false);
        await ActAsAsync(Owner);

        var refused = await SaveFilesManyAsync([fileId]);

        refused.Status.Should().Be(403);
        refused.Error.Should().Be("Forbidden");
    }

    [Fact]
    [Trait("Bug", "82759")]
    public async Task Get_AnswersOneStatusForOnePortalState()
    {
        // The claim was never "it should be 403". The scope of the switch is not defined for the read routes, so either
        // answer could be the intended one — but not both, and `get` used to alternate indefinitely, so whether a draft
        // was readable on an AI-disabled portal was decided per request. Which of the two it settles on is left to the
        // assertion rather than pinned.
        var draftId = await SaveDraftAsync(Owner, "Autotest before-off.docx");

        await SetPortalAiAccessAsync(false);

        var statuses = await CollectStatusesAsync(async () => (await GetAsync(draftId)).Status, ConsistencyAttempts);

        // The route answered at all, so the disagreement below is about the gate and not about a dead endpoint.
        statuses.Should().Contain(status => status == 200 || status == 403);
        statuses.Distinct().Should().HaveCount(1, $"get answered {string.Join(",", statuses)} for one unchanged portal state");
    }

    [Fact]
    [Trait("Bug", "82766")]
    public async Task GetMany_AnswersOneStatusForOnePortalState()
    {
        var draftId = await SaveDraftAsync(Owner, "Autotest before-off.docx");

        await SetPortalAiAccessAsync(false);

        var statuses = await CollectStatusesAsync(async () => (await GetManyAsync(draftId)).Status, ConsistencyAttempts);

        statuses.Should().Contain(status => status == 200 || status == 403);
        statuses.Distinct().Should().HaveCount(1, $"get-many answered {string.Join(",", statuses)} for one unchanged portal state");
    }

    [Fact]
    public async Task SuggestedQuestions_WhenAiAccessIsDisabled_Returns403()
    {
        var draftId = await SaveDraftAsync(Owner, "Autotest before-off suggested questions.docx");

        // Control: answered while the switch is on.
        (await StatusOfAsync(() => Attachments.AiAttachmentsGetSuggestedQuestionsAsync(draftId, TestContext.Current.CancellationToken))).Should().Be(200);

        await SetPortalAiAccessAsync(false);

        await ExpectRefusedOnceSettledAsync(
            () => StatusOfAsync(() => Attachments.AiAttachmentsGetSuggestedQuestionsAsync(draftId, TestContext.Current.CancellationToken)),
            "suggested-questions");
    }
}
