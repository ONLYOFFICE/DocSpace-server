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
/// Shared harness for letter tests. A derived class only describes ONE letter — its id, its pattern,
/// the tags the sending code sets for it and what its text must say — and gets two tests per culture
/// from <see cref="LetterCultures"/>:
///
/// <list type="bullet">
/// <item><see cref="Letter_Renders"/> — renders the letter, runs the checks that hold for every letter
/// plus the letter's own, and saves the HTML next to the test binaries for a browser.</item>
/// <item><see cref="Letter_IsDeliveredToMailPit"/> — additionally delivers it to the MailPit the test
/// run started (<see cref="MailPitFixture"/>), prints the message URL, and asserts how much of the
/// letter's markup real mail clients support.</item>
/// </list>
///
/// Everything about the surroundings (portal address, image folder, external links, branding) comes from
/// <see cref="LetterEnvironment"/> — a letter test must not hard-code URLs.
/// </summary>
public abstract class LetterTestBase
{
    /// <summary>
    /// What an unrecognised textile link looks like once the styler has run: the closing quotation mark
    /// of the caption, the colon, the opening one of the address — straight or curled — and the address
    /// itself. A rendered link never keeps that shape, since it becomes an <c>&lt;a href&gt;</c>.
    /// </summary>
    private static readonly Regex _unrenderedLink =
        new("""(&#8221;|&#8220;|")\s*:\s*(&#8220;|&#8221;|")(https?://|mailto:)""", RegexOptions.Compiled);

    /// <summary>
    /// The inbox started for the whole assembly. Read from the ambient test context rather than taken
    /// through a constructor: xUnit hands assembly fixtures to test classes as constructor arguments,
    /// and threading one through would mean a constructor on every letter test that only passes it on.
    /// </summary>
    private static async ValueTask<MailPitInbox> GetInboxAsync()
    {
        var fixture = await TestContext.Current.GetFixture<MailPitFixture>();

        return fixture?.Inbox
            ?? throw new InvalidOperationException(
                $"No MailPit inbox in the test context. {nameof(MailPitFixture)} is registered with "
                + "[assembly: AssemblyFixture] and starts before any letter test runs.");
    }

    /// <summary>Action id / resource key suffix, e.g. <c>saas_admin_handy_apps_v1</c>.</summary>
    protected abstract string LetterId { get; }

    /// <summary>The pattern under test: the <c>subject_*</c> and <c>pattern_*</c> pair.</summary>
    protected abstract IPattern Pattern { get; }

    /// <summary>
    /// The tags the sending code sets for this letter specifically (the orange button, <c>URL1</c>…),
    /// mirroring its block in <c>StudioPeriodicNotify</c>. The tags every letter gets are added by the
    /// harness.
    /// </summary>
    protected abstract IEnumerable<ITagValue> BuildLetterTags(CultureInfo culture);

    /// <summary>
    /// What this letter must carry in every culture — the links, the button target, the images. Wording
    /// belongs in <see cref="AssertDefaultCultureText"/>, since another culture may carry a translation.
    /// </summary>
    protected abstract void AssertContent(RenderedLetter letter, CultureInfo culture);

    /// <summary>
    /// The wording of the default-culture resources, checked only for
    /// <see cref="LetterCultures.DefaultCultureName"/>.
    /// </summary>
    protected virtual void AssertDefaultCultureText(RenderedLetter letter) { }

    /// <summary>
    /// The top image the sending code sets for this letter (<c>topGif = studioNotifyHelper.GetNotificationImageUrl(...)</c>),
    /// or <c>null</c> when it sets none and the letter logo is shown instead. When set, the harness both
    /// passes it in and asserts it survived into the letter.
    /// </summary>
    protected virtual string? TopGif => null;

    /// <summary>The first name the letter greets, i.e. the value of the <c>UserName</c> tag.</summary>
    protected virtual string RecipientName => "FirstName";

    /// <summary>
    /// Who triggered the notification, i.e. the value of the <c>__AuthorName</c> tag — the inviter in
    /// the room and agent letters. Filled for every letter by <c>NotifyConfiguration</c> in production.
    /// </summary>
    protected virtual string AuthorName => "AuthorName";

    /// <summary>
    /// Footer flavour, as chosen in <c>BasePeriodicNotifyAction.Init</c>: <c>common</c> for an
    /// owner/admin recipient, <c>social</c> for everybody else.
    /// </summary>
    protected virtual string Footer => "common";

    /// <summary>Whether <c>$TrulyYours</c> is a top-level table row (true for the HTML letters).</summary>
    protected virtual bool TrulyYoursAsTableRow => true;

    /// <summary>
    /// Whether the letter signs off at all. A handful of short transactional letters — the password
    /// setup link, for one — deliberately carry no <c>$TrulyYours</c>, so the signature check is skipped
    /// for them.
    /// </summary>
    protected virtual bool HasSignature => true;

    /// <summary>
    /// Which resource the sending code signs off with. Almost every letter uses
    /// <c>TrulyYoursText</c>; the backup ones pass <c>BestRegardsText</c> instead.
    /// </summary>
    protected virtual string SignatureKey => "TrulyYoursText";

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

        var letter = await LetterPreview.RenderAsync(Pattern, BuildTags(culture), culture);

        AssertPatternIsPortable();
        AssertNoUnresolvedTags(letter);
        AssertLinksRendered(letter);
        AssertTopImage(letter);
        AssertSignature(letter, culture);

        AssertContent(letter, culture);

        if (cultureName == LetterCultures.DefaultCultureName)
        {
            AssertDefaultCultureText(letter);
        }

        await SaveForReviewAsync(letter, culture);
    }

    [Theory]
    [MemberData(nameof(LetterCultures.All), MemberType = typeof(LetterCultures))]
    public async Task Letter_IsDeliveredToMailPit(string cultureName)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var culture = CultureInfo.GetCultureInfo(cultureName);
        var letter = await LetterPreview.RenderAsync(Pattern, BuildTags(culture), culture);

        // A unique address per run, so the assertion below finds this letter and not one left over from
        // an earlier run, another culture or the portal itself.
        var address = $"{LetterId}-{cultureName}-{Guid.NewGuid():N}@preview.onlyoffice.com";

        var inbox = await GetInboxAsync();

        await inbox.SendAsync(address, letter.Subject, letter.Body, cancellationToken);

        var delivered = await inbox.WaitForMessageAsync(address, TimeSpan.FromSeconds(30), cancellationToken);

        delivered.Should().NotBeNull("the letter should show up in the MailPit inbox");

        // Trimmed, because a mail header cannot carry surrounding whitespace and none of it survives
        // transport. A couple of translations end their subject with a space (zh-CN, for two), and the
        // reader never sees the difference — asserting on it would only test MIME, not the letter.
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

    /// <summary>The orange button, with its caption taken from the resource key the sending code uses.</summary>
    /// <summary>
    /// The orange button. <paramref name="tag"/> names it — letters that carry two buttons render the
    /// second one under its own tag, e.g. <c>$OrangeButtonPwd</c>.
    /// </summary>
    protected static ITagValue OrangeButton(string captionKey, CultureInfo culture, string url, string tag = "OrangeButton")
    {
        return TagValues.OrangeButton(Resource(captionKey, culture), url, tag);
    }

    /// <summary>
    /// The tags every letter gets: from <c>BasePeriodicNotifyAction.Init</c> (user name, button,
    /// signature, images) and from <c>NotifyConfiguration</c> (portal paths, branding, footer settings).
    /// </summary>
    private List<ITagValue> BuildTags(CultureInfo culture)
    {
        var tags = new List<ITagValue>
        {
            new TagValue(CommonTags.Culture, culture.Name),
            new TagValue(CommonTags.UserName, RecipientName),
            TagValues.TrulyYours(LetterEnvironment.SiteUrl, Resource(SignatureKey, culture), TrulyYoursAsTableRow),

            new TagValue(CommonTags.TopGif, TopGif ?? string.Empty),
            new TagValue(CommonTags.ImagePath, LetterEnvironment.NotificationImagePath),
            new TagValue(CommonTags.LetterLogoText, LetterEnvironment.LogoText),

            new TagValue(CommonTags.Footer, Footer),
            new TagValue(CommonTags.MailWhiteLabelSettings, new MailWhiteLabelSettings().GetDefault()),

            new TagValue(CommonTags.VirtualRootPath, LetterEnvironment.PortalUrl),
            new TagValue(CommonTags.VirtualRootHost, LetterEnvironment.PortalHost),
            new TagValue(CommonTags.RecipientSubscriptionConfigURL, LetterEnvironment.PortalLink("unsubscribe")),

            new TagValue(CommonTags.AuthorName, AuthorName),
            new TagValue(CommonTags.HelpLink, LetterEnvironment.HelpUrl),
            new TagValue(CommonTags.SupportLink, LetterEnvironment.SupportUrl),
            new TagValue(CommonTags.SiteLink, LetterEnvironment.SiteUrl),
            new TagValue(CommonTags.SalesEmail, LetterEnvironment.SalesEmail),
            new TagValue(CommonTags.SupportEmail, LetterEnvironment.SupportEmail)
        };

        tags.AddRange(BuildLetterTags(culture));

        return tags;
    }

    /// <summary>
    /// Nothing environment-specific may be baked into the pattern text:
    /// <list type="bullet">
    /// <item>the product name is <c>${LetterLogoText}</c>, so a white-labelled portal sends its own
    /// branding;</item>
    /// <item>links arrive as tags (<c>$URL1</c>, <c>${__VirtualRootPath}</c>, …) whose values the sending
    /// code resolves from <c>externalresources.json</c> and from the portal address.</item>
    /// </list>
    /// </summary>
    private void AssertPatternIsPortable()
    {
        foreach (var text in new[] { Pattern.Subject(), Pattern.Body() })
        {
            text.Should().NotContain(LetterEnvironment.LogoText,
                $"the letter must carry ${{{CommonTags.LetterLogoText}}} instead of the product name, so "
                + "white-labelled portals send their own branding");

            text.Should().NotContain("http://", "links must come from tags, not be hard-coded in the pattern")
                .And.NotContain("https://", "links must come from tags, not be hard-coded in the pattern");
        }
    }

    /// <summary>
    /// Every tag the pattern references must have been given a value: a forgotten one leaves the raw
    /// <c>$Tag</c> / <c>${Tag}</c> in the letter, which is exactly what the reader would see.
    /// </summary>
    private void AssertNoUnresolvedTags(RenderedLetter letter)
    {
        var tags = new NVelocityPatternFormatter().GetTags(Pattern);

        tags.Should().NotBeEmpty("every letter substitutes at least the user name; an empty list means the "
            + "pattern was not found and this check would silently pass");

        foreach (var tag in tags)
        {
            var because = $"tag '{tag}' has no value in {GetType().Name}.BuildLetterTags";

            letter.Subject.Should().NotContain($"${tag}", because).And.NotContain($"${{{tag}}}", because);
            letter.Body.Should().NotContain($"${tag}", because).And.NotContain($"${{{tag}}}", because);
        }
    }

    /// <summary>
    /// A textile link is written <c>"caption":"address"</c> and is recognised only when nothing is glued
    /// to it. A particle in Japanese or Korean, a case suffix in Finnish or Azerbaijani, a hyphen in
    /// Armenian — any of them is enough for the parser to walk past, and it then leaves the caption, the
    /// colon and the address standing in the text, merely curling the quotation marks. The letter still
    /// renders and still contains the address, so a check that the address is present passes while the
    /// reader is looking at <c>“Help Center”:”https://…”</c>.
    /// </summary>
    private static void AssertLinksRendered(RenderedLetter letter)
    {
        var raw = _unrenderedLink.Matches(letter.Body)
            .Select(match => match.Value)
            .ToArray();

        raw.Should().BeEmpty("a textile link was not recognised and its address is printed to the reader; "
            + "the translation most likely glues a particle or a suffix to the closing quotation mark — "
            + "put a space there");
    }

    /// <summary>The signature the harness passes in, in the recipient's culture, plus the site it links to.</summary>
    private void AssertSignature(RenderedLetter letter, CultureInfo culture)
    {
        if (!HasSignature)
        {
            return;
        }

        var signature = Resource(SignatureKey, culture)
            .Replace("${" + CommonTags.LetterLogoText + "}", LetterEnvironment.LogoText);

        letter.Body.Should().Contain(signature).And.Contain(LetterEnvironment.SiteUrl);
    }

    private void AssertTopImage(RenderedLetter letter)
    {
        if (TopGif != null)
        {
            letter.Body.Should().Contain(TopGif, "the top image the sending code sets must reach the letter");
        }
        else
        {
            letter.Body.Should().Contain("mail_logo.png", "without a top image the letter logo is shown instead");
        }
    }

    /// <summary>
    /// Drops the rendered letter next to the test binaries, so it can be opened in a browser when MailPit
    /// is not running.
    /// </summary>
    private async Task SaveForReviewAsync(RenderedLetter letter, CultureInfo culture)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "letter-preview");
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"{LetterId}.{culture.Name}.html");

        await File.WriteAllTextAsync(path, letter.Body, TestContext.Current.CancellationToken);

        Write($"Subject: {letter.Subject}");
        Write($"Rendered letter: {path}");
    }

    private static void Write(string message)
    {
        TestContext.Current.TestOutputHelper?.WriteLine(message);
    }
}
