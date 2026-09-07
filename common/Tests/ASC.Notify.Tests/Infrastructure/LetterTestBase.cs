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

namespace ASC.Notify.Tests.Infrastructure;

/// <summary>
/// Shared harness for letter tests. A derived class says how the production action's <c>Init</c> is
/// called and what the text must contain — and gets two tests per culture from
/// <see cref="LetterCultures"/>:
///
/// <list type="bullet">
/// <item><see cref="Letter_Renders"/> — renders the letter, runs the checks that hold for every letter
/// plus the letter's own, and saves the HTML next to the test binaries for a browser.</item>
/// <item><see cref="Letter_IsDeliveredToMailPit"/> — additionally delivers it to MailPit, prints the
/// message URL, and asserts how much of the letter's markup real mail clients support.</item>
/// </list>
///
/// The tag values come from the action itself. A test used to restate them — the orange button, the
/// footer flavour, the top image, whether the signature is a table row — which was a copy of <c>Init</c>
/// free to fall out of step with it silently: the test stayed green while rendering a letter the sending
/// code no longer produces.
///
/// That is why this harness needs the whole stack (<see cref="LetterStackFixture"/>): <c>Init</c> resolves
/// links against the current tenant and shortens them through the database, so there is no calling it
/// without a portal.
/// </summary>
/// <typeparam name="TAction">The notify action that sends this letter in production.</typeparam>
public abstract class LetterTestBase<TAction> where TAction : NotifyAction
{
    private static async ValueTask<LetterStackFixture> GetStackAsync()
    {
        return await TestContext.Current.GetFixture<LetterStackFixture>()
            ?? throw new InvalidOperationException(
                $"No stack in the test context. {nameof(LetterStackFixture)} is registered with "
                + "[assembly: AssemblyFixture] and starts before any letter test runs.");
    }

    /// <summary>
    /// How the sending code calls this letter's <c>Init</c>. This is the whole of what a letter test has
    /// to say about its tags: there is no <c>Init</c> on <see cref="INotifyAction"/> — every action
    /// declares its own signature — so the call itself is the one thing that cannot be shared.
    /// </summary>
    protected abstract Task InitAsync(TAction action, LetterScope scope);

    /// <summary>
    /// What this letter must carry in every culture — the links, the button target, the images. Wording
    /// belongs in <see cref="AssertDefaultCultureText"/>, since another culture may carry a translation.
    /// Optional: a letter whose <c>Init</c> shortens its link has nothing stable to assert on, because
    /// the short key is minted by the database on every call.
    /// </summary>
    protected virtual void AssertContent(RenderedLetter letter, LetterScope scope) { }

    /// <summary>
    /// The wording of the default-culture resources, checked only for
    /// <see cref="LetterCultures.DefaultCultureName"/>.
    /// </summary>
    protected virtual void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope) { }

    /// <summary>
    /// The smallest share of mail clients that must fully render this letter's markup, as MailPit's
    /// html-check scores it against the caniemail support matrix. The rest is nearly all *partial*
    /// support — the CSS that every table-based email leans on — and under 3% is unsupported outright.
    ///
    /// Every letter is built from the same <c>HtmlMaster</c> template and they measure 78.5% to 91.5%,
    /// so this floor sits just below the lowest of them: low enough that a caniemail data update does
    /// not turn the suite red on its own, high enough that markup which costs a letter more than a few
    /// points has to be justified. A letter overrides it only when it genuinely needs markup the others
    /// do not, and the override says why.
    /// </summary>
    protected virtual double MinimumHtmlSupport => 75;

    [Theory]
    [MemberData(nameof(LetterCultures.All), MemberType = typeof(LetterCultures))]
    public async Task Letter_Renders(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);

        var stack = await GetStackAsync();

        using var scope = await LetterScope.OpenAsync(stack, culture);

        var (action, pattern, tags) = await PrepareAsync(scope);

        var letter = await LetterPreview.RenderAsync(pattern, tags, culture);

        letter.ShouldHavePortablePattern();
        letter.ShouldHaveNoUnresolvedTags($"the tags {typeof(TAction).Name}.Init sets");
        letter.ShouldHaveRenderedLinks();

        letter.ShouldHaveTopImage(tags);
        letter.ShouldHaveSignature(tags);

        AssertContent(letter, scope);

        if (cultureName == LetterCultures.DefaultCultureName)
        {
            AssertDefaultCultureText(letter, scope);
        }

        await SaveForReviewAsync(letter, action.ID, culture);
    }

    [Theory]
    [MemberData(nameof(LetterCultures.All), MemberType = typeof(LetterCultures))]
    public async Task Letter_IsDeliveredToMailPit(string cultureName)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var culture = CultureInfo.GetCultureInfo(cultureName);

        var stack = await GetStackAsync();

        using var scope = await LetterScope.OpenAsync(stack, culture);

        var (action, pattern, tags) = await PrepareAsync(scope);

        var letter = await LetterPreview.RenderAsync(pattern, tags, culture);

        // A unique address per run, so the assertion below finds this letter and not one left over from
        // an earlier run, another culture or the portal itself.
        var address = $"{action.ID}-{cultureName}-{Guid.NewGuid():N}@preview.onlyoffice.com";

        var inbox = stack.Inbox;

        await inbox.SendAsync(address, letter.Subject, letter.Body, EmbeddedAttachments(tags), cancellationToken);

        var delivered = await inbox.WaitForMessageAsync(address, TimeSpan.FromSeconds(30), cancellationToken);

        delivered.Should().NotBeNull("the letter should show up in the MailPit inbox");

        // Trimmed, because a mail header cannot carry surrounding whitespace and none of it survives
        // transport. A couple of translations end their subject with a space, and the reader never sees
        // the difference — asserting on it would only test MIME, not the letter.
        delivered!.Subject.Should().Be(letter.Subject.Trim());

        var check = await inbox.CheckHtmlAsync(delivered.Id, cancellationToken);

        Write($"Letter delivered to {address}");
        Write($"Open it in MailPit: {inbox.GetMessageUrl(delivered)}");
        Write($"HTML support: {check.Total.Supported:F1}% supported, {check.Total.Partial:F1}% partial, "
            + $"{check.Total.Unsupported:F1}% unsupported "
            + $"({check.Total.Tests} tests over {check.Total.Nodes} nodes)");

        check.Total.Supported.Should().BeGreaterThanOrEqualTo(MinimumHtmlSupport, DescribeWarnings(check));
    }

    /// <summary>
    /// Resolves the action, runs the letter's own <c>Init</c> and assembles what the renderer needs.
    /// </summary>
    private async Task<(TAction Action, IPattern Pattern, List<ITagValue> Tags)> PrepareAsync(LetterScope scope)
    {
        var action = scope.Services.GetRequiredService<TAction>();

        await InitAsync(action, scope);

        var pattern = action.Patterns.Find(p => p.SenderName == ASC.Core.Configuration.Constants.NotifyEMailSenderSysName)
            ?? throw new InvalidOperationException(
                $"Action '{action.ID}' carries no email pattern, so there is no letter to render.");

        var tags = await BuildCommonTagsAsync(scope, action);

        return (action, pattern, tags);
    }

    /// <summary>
    /// Everything the letter substitutes, assembled the way the engine assembles it: the action's own
    /// tags, then the ones <c>NotifyTransferRequest</c> appends to every request on its way out — the
    /// portal paths, the author, the external links, the branding and the letter logo.
    ///
    /// It runs the real transfer step rather than restating what it produces. That list is nineteen tags
    /// long and reads white-label settings, the tenant logo and the notification image folder; a copy of
    /// it here would be a second implementation, free to drift from the one that fills real letters.
    /// </summary>
    private static async Task<List<ITagValue>> BuildCommonTagsAsync(LetterScope scope, INotifyAction action)
    {
        var services = scope.Services;

        // The recipient the engine would have resolved. Only the address matters to the transfer step;
        // the letter is delivered by the test itself.
        var recipient = new DirectRecipient(scope.Recipient.Id.ToString(), scope.DisplayName, [scope.Recipient.Email]);

        var request = new NotifyRequest(
            services.GetRequiredService<ILoggerFactory>(),
            services.GetRequiredService<StudioNotifyHelper>().NotifySource,
            action,
            null,
            recipient);

        // Init's tags first, exactly as NotifyClientImpl.CreateRequest adds them before the engine runs
        // any INotifyEngineAction — which is what lets the transfer step see a letter's TopGif and leave
        // its logo alone.
        request.Arguments.AddRange(action.Tags ?? []);

        await services.GetRequiredService<ASC.Web.Studio.Core.Notify.NotifyTransferRequest>().BeforeTransferRequestAsync(request);

        return request.Arguments;
    }

    /// <summary>
    /// What the transfer step attached for this letter to reference by content id. Empty for a letter
    /// that carries its own top image, since then no logo is attached.
    /// </summary>
    private static IReadOnlyCollection<NotifyMessageAttachment> EmbeddedAttachments(List<ITagValue> tags)
    {
        return tags.Find(tag => tag.Tag == CommonTags.EmbeddedAttachments)?.Value
            as NotifyMessageAttachment[] ?? [];
    }

    /// <summary>
    /// What the letter uses that mail clients struggle with, worst first. Without it a failure reads
    /// "expected at least 90, found 84" and says nothing about which markup to stop using.
    /// </summary>
    private static string DescribeWarnings(HtmlCheck check)
    {
        var worst = (check.Warnings ?? [])
            .OrderByDescending(warning => (warning.Score.Unsupported + warning.Score.Partial) * warning.Score.Found)
            .Take(5)
            .Select(warning => $"{Environment.NewLine}  - {warning.Slug} ({warning.Category}): "
                + $"unsupported in {warning.Score.Unsupported:F1}% of clients, "
                + $"partial in {warning.Score.Partial:F1}%, used by {warning.Score.Found} node(s)")
            .ToArray();

        return worst.Length == 0
            ? "MailPit reported no offending markup, so the letter is below the bar for another reason"
            : "MailPit blames this markup:" + string.Concat(worst);
    }

    /// <summary>A resource string in the recipient's culture — button captions, the signature, …</summary>
    protected static string Resource(string key, CultureInfo culture)
    {
        return WebstudioNotifyPatternResource.ResourceManager.GetString(key, culture)
            ?? throw new InvalidOperationException($"Resource key '{key}' is missing.");
    }

    /// <summary>
    /// Drops the rendered letter next to the test binaries, so it can be opened in a browser when MailPit
    /// is not running.
    /// </summary>
    private static async Task SaveForReviewAsync(RenderedLetter letter, string letterId, CultureInfo culture)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "letter-preview");
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"{letterId}.{culture.Name}.html");

        await File.WriteAllTextAsync(path, letter.Body, TestContext.Current.CancellationToken);

        Write($"Subject: {letter.Subject}");
        Write($"Rendered letter: {path}");
    }

    private static void Write(string message)
    {
        TestContext.Current.TestOutputHelper?.WriteLine(message);
    }
}
