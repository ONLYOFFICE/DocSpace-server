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

/// <summary>The rendered letter, exactly as the SMTP sender would hand it to MailKit.</summary>
internal sealed record RenderedLetter(string Subject, string Body);

/// <summary>
/// Renders a letter the way the notify engine does, but without a portal: pattern text from the
/// resources, tag substitution through <see cref="NVelocityPatternFormatter"/>, then the email styler
/// (<see cref="TextileStyler"/>) which wraps the body into the <c>HtmlMaster</c> template and builds
/// the top logo and the footer.
///
/// This is the same order as <c>NotifyEngine.CreateNoticeMessageAsync</c> plus the
/// <c>${LetterLogoText}</c> pass that <c>NotifyConfiguration</c>'s WhiteLabelInterceptor applies to
/// the finished body. What it does NOT reproduce is where the tag VALUES come from — in production
/// they are built by the action's <c>Init</c> from live tenant data; here the test supplies them.
/// </summary>
internal static class LetterPreview
{
    public static async Task<RenderedLetter> RenderAsync(IPattern pattern, IReadOnlyCollection<ITagValue> tags, CultureInfo culture)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;

        // The pattern and subject are resolved through ResourceManager, i.e. from the *current UI
        // culture* — the same way the engine renders in the recipient's culture.
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        try
        {
            var recipient = new DirectRecipient(Guid.NewGuid().ToString(), "Letter preview", ["preview@onlyoffice.com"]);
            var message = new NoticeMessage(recipient, null, null, pattern);

            message.AddArgument([.. tags]);

            new NVelocityPatternFormatter().FormatMessage(message, message.Arguments);

            await CreateStyler().ApplyFormatingAsync(message);

            var logoText = tags.FirstOrDefault(t => t.Tag == CommonTags.LetterLogoText)?.Value as string ?? string.Empty;

            return new RenderedLetter(ReplaceLogoText(message.Subject, logoText), ReplaceLogoText(message.Body, logoText));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    /// <summary>
    /// Mirrors the WhiteLabelInterceptor: <c>${LetterLogoText}</c> is replaced on the finished body,
    /// which is why it also works inside tag values (the orange button caption, the signature).
    /// </summary>
    private static string ReplaceLogoText(string text, string logoText)
    {
        return string.IsNullOrEmpty(text) || string.IsNullOrEmpty(logoText)
            ? text
            : text.Replace("${" + CommonTags.LetterLogoText + "}", logoText);
    }

    private static TextileStyler CreateStyler()
    {
        var configuration = BuildConfiguration();

        return new TextileStyler(
            new CoreBaseSettings(configuration),
            configuration,
            new InstanceCrypto(new MachinePseudoKeys(configuration)),
            new ExternalResourceSettingsHelper(configuration));
    }

    /// <summary>
    /// The styler needs the <c>externalresources</c> section for the footer and social links. It lives
    /// in the buildtools config next to the repository, so it is picked up when present and simply
    /// left out otherwise (the letter still renders, with empty footer links).
    /// </summary>
    private static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Any value works: only the unsubscribe link is encrypted with it, and the preview
                // passes RecipientSubscriptionConfigURL, which short-circuits that path.
                ["core:machinekey"] = "letter-preview"
            });

        var externalResources = FindExternalResourcesConfig();

        if (externalResources != null)
        {
            builder.AddJsonFile(externalResources, optional: true);
        }

        return builder.Build();
    }

    private static string? FindExternalResourcesConfig()
    {
        const string fileName = "externalresources.json";

        var pathToConf = Environment.GetEnvironmentVariable("pathToConf");

        if (!string.IsNullOrEmpty(pathToConf) && File.Exists(Path.Combine(pathToConf, fileName)))
        {
            return Path.Combine(pathToConf, fileName);
        }

        // From bin/Debug/<tfm> up to the docspace root, where buildtools/ sits next to server/.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "buildtools", "config", fileName);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
