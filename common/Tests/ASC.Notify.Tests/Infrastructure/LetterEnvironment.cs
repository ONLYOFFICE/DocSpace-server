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
/// Everything a letter needs from its surroundings, resolved the way the locally running Aspire stack
/// resolves it: the portal address, the notification image folder, the branding text and the external
/// resource links. Letter tests must not hard-code any of this — a link that differs from what the
/// portal actually produces makes the preview lie.
/// </summary>
internal static class LetterEnvironment
{
    /// <summary>
    /// Where the Aspire stack publishes the portal (OpenResty, <c>Constants.RestyPort</c> in the
    /// AppHost). Override with <c>PORTAL_URL</c> to preview against another host.
    /// </summary>
    public static string PortalUrl { get; } =
        Environment.GetEnvironmentVariable("PORTAL_URL")?.TrimEnd('/') is { Length: > 0 } url
            ? url
            : "http://localhost:8092";

    public static string PortalHost { get; } = new Uri(PortalUrl).Host;

    /// <summary>
    /// The default branding text. Letters must never spell the product name out: they carry
    /// <c>${LetterLogoText}</c> so white-labelled portals send their own name, and this is the value
    /// that placeholder is resolved with (see <c>TextileStyler.GetLogoText</c>).
    /// </summary>
    public static string LogoText => BaseWhiteLabelSettings.DefaultLogoText;

    /// <summary>
    /// The buildtools configuration of the local stack, when it is available next to the repository:
    /// <c>externalresources.json</c> for the site/helpcenter/social links and <c>appsettings.json</c>
    /// for <c>web:images</c>. Absent files simply fall back to the defaults below.
    /// </summary>
    public static IConfiguration Configuration { get; } = BuildConfiguration();

    public static ExternalResourceSettingsHelper ExternalResources { get; } = new(Configuration);

    /// <summary>The site the signature links to — <c>CommonLinkUtility.GetSiteLink</c>.</summary>
    public static string SiteUrl { get; } = Fallback(ExternalResources.Site.GetDefaultRegionalDomain(), "https://www.onlyoffice.com");

    /// <summary>The help center — <c>CommonLinkUtility.GetHelpLinkAsync</c>, i.e. the `__HelpLink` tag.</summary>
    public static string HelpUrl { get; } = Fallback(ExternalResources.Helpcenter.GetDefaultRegionalDomain(), "https://helpcenter.onlyoffice.com");

    /// <summary>The support desk — <c>CommonLinkUtility.GetSupportLinkAsync</c>, i.e. the `__SupportLink` tag.</summary>
    public static string SupportUrl { get; } = Fallback(ExternalResources.Support.GetDefaultRegionalDomain(), "https://helpdesk.onlyoffice.com");

    /// <summary>The `__SalesEmail` tag — <c>CommonLinkUtility.GetSalesEmail</c>.</summary>
    public static string SalesEmail { get; } = Fallback(ExternalResources.Common.GetDefaultRegionalFullEntry("paymentemail"), "sales@onlyoffice.com");

    /// <summary>The `__SupportEmail` tag — <c>CommonLinkUtility.GetSupportEmail</c>.</summary>
    public static string SupportEmail { get; } = Fallback(ExternalResources.Common.GetDefaultRegionalFullEntry("supportemail"), "support@onlyoffice.com");

    /// <summary>
    /// What <c>StudioNotifyHelper.GetNotificationImageUrl</c> returns for an empty file name, i.e. the
    /// value of the <c>ImagePath</c> tag: <c>web:notification:image:path</c> when configured, the
    /// portal's own image folder otherwise — which is the case in the local stack.
    /// </summary>
    public static string NotificationImagePath { get; } = BuildNotificationImagePath();

    /// <summary>A single notification image, e.g. <c>configure_docspace.gif</c>.</summary>
    public static string NotificationImageUrl(string fileName)
    {
        return $"{NotificationImagePath}/{fileName}";
    }

    /// <summary>A portal link, the equivalent of <c>CommonLinkUtility.GetFullAbsolutePath("~/...")</c>.</summary>
    public static string PortalLink(string relativePath)
    {
        return $"{PortalUrl}/{relativePath.TrimStart('~', '/')}".TrimEnd('/');
    }

    /// <summary>
    /// A regional external resource, resolved for the recipient's culture exactly like the sending code
    /// does (<c>externalResourceSettingsHelper.Helpcenter.GetRegionalDomain(culture)</c> and friends).
    /// </summary>
    public static string ExternalDomain(ExternalResource resource, CultureInfo culture, string fallback)
    {
        return Fallback(resource.GetRegionalDomain(culture), fallback);
    }

    public static string ExternalEntry(ExternalResource resource, string key, CultureInfo culture, string fallback)
    {
        return Fallback(resource.GetRegionalFullEntry(key, culture), fallback);
    }

    private static string Fallback(string? value, string fallback)
    {
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    private static string BuildNotificationImagePath()
    {
        var configured = Configuration["web:notification:image:path"];

        if (!string.IsNullOrEmpty(configured))
        {
            return configured.TrimEnd('/');
        }

        var images = Configuration["web:images"] ?? "static/images";

        return $"{PortalUrl}/{images.Trim('~', '/')}/notifications";
    }

    private static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Any value works: only the unsubscribe link is encrypted with it, and the letters pass
                // RecipientSubscriptionConfigURL, which short-circuits that path.
                ["core:machinekey"] = "letter-preview",
                ["web:images"] = "static/images"
            });

        foreach (var fileName in new[] { "appsettings.json", "externalresources.json" })
        {
            var path = FindConfigFile(fileName);

            if (path != null)
            {
                builder.AddJsonFile(path, optional: true);
            }
        }

        return builder.Build();
    }

    /// <summary>
    /// Looks for a buildtools config file: under <c>pathToConf</c> when the variable is set, otherwise
    /// in <c>buildtools/config</c> next to the repository — the layout every local checkout has.
    /// </summary>
    private static string? FindConfigFile(string fileName)
    {
        var pathToConf = Environment.GetEnvironmentVariable("pathToConf");

        if (!string.IsNullOrEmpty(pathToConf) && File.Exists(Path.Combine(pathToConf, fileName)))
        {
            return Path.Combine(pathToConf, fileName);
        }

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
