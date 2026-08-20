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
/// One test's view of the portal: a service scope with the tenant set, the owner authenticated and the
/// culture in place — the state <c>StudioNotifyWorker.OnMessageAsync</c> puts a scope into before it
/// hands the action to the notify client. A notify action reads all three, so getting this wrong does
/// not throw, it silently renders the wrong letter.
/// </summary>
public sealed class LetterScope : IDisposable
{
    private readonly IServiceScope _scope;

    private LetterScope(IServiceScope scope, Tenant tenant, UserInfo recipient, string portalUrl, CultureInfo culture)
    {
        _scope = scope;
        Tenant = tenant;
        Recipient = recipient;
        PortalUrl = portalUrl;
        Culture = culture;
    }

    public IServiceProvider Services => _scope.ServiceProvider;

    public Tenant Tenant { get; }

    /// <summary>
    /// Who the letter is for: the portal owner as the database has them, but carrying the culture under
    /// test. The clone is never saved — <c>NotifyAction.GetCulture</c> reads
    /// <see cref="UserInfo.CultureName"/> off the argument, so a culture costs nothing and no two tests
    /// can tread on each other through the user table.
    /// </summary>
    public UserInfo Recipient { get; }

    /// <summary>
    /// Where the portal answers, resolved the way the sending code resolves it rather than assumed.
    /// Letters must be asserted against this: it is the same value the links inside them are built from,
    /// so a test that hard-codes an address instead can disagree with the letter it is reading.
    /// </summary>
    public string PortalUrl { get; }

    public CultureInfo Culture { get; }

    public static async Task<LetterScope> OpenAsync(LetterStackFixture fixture, CultureInfo culture)
    {
        var scope = fixture.Host.CreateScope();

        try
        {
            var services = scope.ServiceProvider;

            // Stands in for the request the sending code would have had. Being a loopback address, it
            // sends ServerRootPath to the tenant's own domain for the host — which resolves back to
            // `localhost` because the base domain is `localhost`, so the port set here is what survives.
            services.GetRequiredService<CommonLinkUtility>().ServerUri = LetterEnvironment.PortalUrl;

            var tenant = await services.GetRequiredService<TenantManager>()
                .SetCurrentTenantAsync(fixture.Portal.TenantId);

            // The author tags come from the authenticated account; without this the letter is written by
            // nobody in particular and NotifyTransferRequest leaves the author blank.
            await services.GetRequiredService<SecurityContext>()
                .AuthenticateMeWithoutCookieAsync(tenant.Id, tenant.OwnerId);

            // AFTER setting the tenant, which overwrites the ambient culture with the tenant's own. The
            // other order renders every letter in English and fails nothing.
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            var portalUrl = services.GetRequiredService<CommonLinkUtility>().GetFullAbsolutePath("").TrimEnd('/');

            AssertPortalUrl(portalUrl, fixture.Portal);

            var owner = await services.GetRequiredService<UserManager>().GetUsersAsync(tenant.OwnerId);

            var recipient = (UserInfo)owner.Clone();
            recipient.CultureName = culture.Name;

            return new LetterScope(scope, tenant, recipient, portalUrl, culture);
        }
        catch
        {
            scope.Dispose();

            throw;
        }
    }

    /// <summary>
    /// The address the stack was set up for must be the one the portal actually resolves to. This is
    /// load-bearing rather than cosmetic: the notification image folder is configured from it, and the
    /// URL shortener refuses outright to shorten a link whose host is not the portal's own — so a
    /// mismatch would otherwise surface as a missing image or an <c>ArgumentException</c> from deep
    /// inside <c>Init</c>, far from the cause.
    /// </summary>
    private static void AssertPortalUrl(string resolved, LetterPortal portal)
    {
        if (string.Equals(resolved, portal.Url, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"The portal resolves to '{resolved}', but the stack was set up for '{portal.Url}'. That is "
            + $"what Tenant.GetTenantDomain makes of the alias '{portal.Alias}': check whether "
            + "`core:base-domain` still is `localhost`, or whether the tenant carries a mapped domain.");
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}
