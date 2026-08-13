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
/// The "Discover 4 handy apps" letter (<c>saas_admin_handy_apps_v1</c>), sent in SaaS on day 2 after
/// portal registration to the owner and the DocSpace admins.
///
/// <see cref="Letter_HasNoUnresolvedTags"/> renders it and checks the result — it needs nothing but the
/// resources and runs in every test pass. <see cref="Letter_IsDeliveredToMailPit"/> additionally drops
/// the letter into the MailPit inbox of the local Aspire stack so it can be reviewed in a mail client;
/// it is skipped when MailPit is not running.
/// </summary>
public class SaasAdminHandyAppsLetterTests
{
    private const string PortalUrl = "https://oftx4rgi.onlyoffice.com";
    private const string SiteUrl = "https://www.onlyoffice.com";
    private const string LogoText = "ONLYOFFICE";
    private const string RecipientName = "FirstName";

    /// <summary>Culture of the recipient — change it to preview a translated letter.</summary>
    private const string CultureName = "en-US";

    private static readonly IPattern _pattern = new EmailPattern(
        () => WebstudioNotifyPatternResource.subject_saas_admin_handy_apps_v1,
        () => WebstudioNotifyPatternResource.pattern_saas_admin_handy_apps_v1);

    [Fact]
    public async Task Letter_HasNoUnresolvedTags()
    {
        var culture = CultureInfo.GetCultureInfo(CultureName);

        var letter = await LetterPreview.RenderAsync(_pattern, BuildTags(culture), culture);

        letter.Subject.Should().Be($"Discover 4 handy apps inside your {LogoText}");

        letter.Body.Should().Contain($"{LogoText} Files")
            .And.Contain($"{LogoText} Rooms")
            .And.Contain($"{LogoText} Forms")
            .And.Contain($"{LogoText} AI Agents")
            .And.Contain($"Go to your {LogoText}")
            .And.Contain(PortalUrl)
            .And.Contain("Truly Yours");

        // Every tag the pattern uses must have been substituted: a missing TagValue would leave the
        // raw "$Tag" / "${Tag}" in the letter, which is exactly what a reader would see.
        letter.Body.Should().NotContain("$UserName")
            .And.NotContain("$OrangeButton")
            .And.NotContain("$TrulyYours")
            .And.NotContain("${" + CommonTags.LetterLogoText + "}");

        await SaveForReviewAsync(letter);
    }

    /// <summary>
    /// Also drops the rendered letter next to the test binaries, so it can be opened in a browser when
    /// MailPit is not running.
    /// </summary>
    private static async Task SaveForReviewAsync(RenderedLetter letter)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "letter-preview");
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, "saas_admin_handy_apps_v1.html");

        await File.WriteAllTextAsync(path, letter.Body, TestContext.Current.CancellationToken);

        TestContext.Current.TestOutputHelper?.WriteLine($"Subject: {letter.Subject}");
        TestContext.Current.TestOutputHelper?.WriteLine($"Rendered letter: {path}");
    }

    [Fact]
    public async Task Letter_IsDeliveredToMailPit()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var endpoint = await MailPitEndpoint.ResolveAsync(cancellationToken);

        if (endpoint == null)
        {
            Assert.Skip("MailPit is not running. Start the stack with "
                + "`dotnet run --project common/ASC.AppHost --launch-profile development`, "
                + "or point MAILPIT_SMTP=host:port and MAILPIT_HTTP=http://host:port at an existing instance.");

            return;
        }

        var culture = CultureInfo.GetCultureInfo(CultureName);
        var letter = await LetterPreview.RenderAsync(_pattern, BuildTags(culture), culture);

        // A unique address per run, so the assertion below finds this letter and not one left over
        // from an earlier run or from the portal itself.
        var address = $"handy-apps-{Guid.NewGuid():N}@preview.onlyoffice.io";

        var inbox = new MailPitInbox(endpoint);

        await inbox.SendAsync(address, letter.Subject, letter.Body, cancellationToken);

        var delivered = await inbox.WaitForMessageAsync(address, TimeSpan.FromSeconds(15), cancellationToken);

        delivered.Should().NotBeNull("the letter should show up in the MailPit inbox");
        delivered!.Subject.Should().Be(letter.Subject);

        TestContext.Current.TestOutputHelper?.WriteLine($"Letter delivered to {address}");
        TestContext.Current.TestOutputHelper?.WriteLine($"Open it in MailPit: {inbox.GetMessageUrl(delivered)}");
    }

    /// <summary>
    /// The tag values the letter is rendered with. In production they come from
    /// <c>BasePeriodicNotifyAction.Init</c> (called by <c>StudioPeriodicNotify.SendSaasLettersAsync</c>)
    /// and from <c>NotifyConfiguration</c>; here they are sample data, kept in the same shape.
    /// </summary>
    private static List<ITagValue> BuildTags(CultureInfo culture)
    {
        return
        [
            new TagValue(CommonTags.Culture, culture.Name),
            new TagValue(CommonTags.UserName, RecipientName),
            TagValues.OrangeButton(GetString("ButtonGoToDocSpace", culture), PortalUrl),
            TagValues.TrulyYours(SiteUrl, GetString("TrulyYoursText", culture), true),

            // The letter has no top image of its own, so the tenant letter logo is rendered instead —
            // the same as in production, where TopGif stays empty for this letter.
            new TagValue(CommonTags.TopGif, string.Empty),
            new TagValue(CommonTags.ImagePath, "https://static.onlyoffice.com/media/newsletters"),
            new TagValue(CommonTags.LetterLogoText, LogoText),

            // "common" is what an owner/admin recipient gets (see BasePeriodicNotifyAction.Init).
            new TagValue(CommonTags.Footer, "common"),
            new TagValue(CommonTags.MailWhiteLabelSettings, new MailWhiteLabelSettings().GetDefault()),

            new TagValue(CommonTags.VirtualRootPath, PortalUrl),
            new TagValue(CommonTags.VirtualRootHost, new Uri(PortalUrl).Host),
            new TagValue(CommonTags.RecipientSubscriptionConfigURL, $"{PortalUrl}/unsubscribe")
        ];
    }

    private static string GetString(string key, CultureInfo culture)
    {
        return WebstudioNotifyPatternResource.ResourceManager.GetString(key, culture)
            ?? throw new InvalidOperationException($"Resource key '{key}' is missing.");
    }
}
