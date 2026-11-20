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

namespace ASC.Notify.Tests.Lifecycle;

/// <summary>
/// The owner's confirmation links of the portal lifecycle, for real: the suite signs them in-process the way the
/// letters carry them (<see cref="CommonLinkUtility.GetConfirmationEmailUrl"/>), and Web.Api suspends and continues
/// the portal on them. The delete link cannot be followed here: the deletion removes the portal's OAuth clients
/// through the identity service, which the letter stack does not run (500); the Web.Api suite covers the same
/// removal path through <c>deleteportalimmediately</c>. Each case registers a fresh portal; the one the letter tests share is never
/// touched.
/// </summary>
public class PortalConfirmLinkTests
{
    /// <summary>The query of the link a letter carries for the owner of <paramref name="tenantId"/>.</summary>
    private static async Task<string> LinkAsync(LetterStackFixture stack, int tenantId, ConfirmType type)
    {
        using var scope = stack.Host.CreateScope();

        scope.ServiceProvider.GetRequiredService<CommonLinkUtility>().ServerUri = LetterEnvironment.PublishedUrl;
        await scope.ServiceProvider.GetRequiredService<TenantManager>().SetCurrentTenantAsync(tenantId);

        var tenant = await scope.ServiceProvider.GetRequiredService<TenantManager>().GetTenantAsync(tenantId);
        var owner = await scope.ServiceProvider.GetRequiredService<UserManager>().GetUsersAsync(tenant.OwnerId);
        var link = scope.ServiceProvider.GetRequiredService<CommonLinkUtility>().GetConfirmationEmailUrl(owner.Email, type);

        return new Uri(link).Query.TrimStart('?');
    }

    /// <summary>The portal's alias, status and status date as the database has them, past every cache.</summary>
    private static async Task<(string Alias, TenantStatus Status, DateTime? StatusChanged)> ReadTenantAsync(LetterStackFixture stack, int tenantId)
    {
        await using var connection = await stack.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = "select alias, status, statuschanged from tenants_tenants where id = @id";
        command.Parameters.AddWithValue("@id", tenantId);

        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        (await reader.ReadAsync(TestContext.Current.CancellationToken)).Should().BeTrue("the row stays until the removal worker purges it");

        return (reader.GetString(0), (TenantStatus)reader.GetInt32(1), reader.IsDBNull(2) ? null : reader.GetDateTime(2));
    }

    /// <summary>Calls <paramref name="request"/> with <paramref name="confirm"/> as its link, and returns its status.</summary>
    private static async Task<HttpStatusCode> WithLinkAsync(LetterPortalClients portal, string confirm, Func<CancellationToken, Task> request)
    {
        portal.WebApiHttpClient.DefaultRequestHeaders.Remove("confirm");
        portal.WebApiHttpClient.DefaultRequestHeaders.Add("confirm", confirm);

        try
        {
            return await StatusOfAsync(request);
        }
        finally
        {
            portal.WebApiHttpClient.DefaultRequestHeaders.Remove("confirm");
        }
    }

    /// <summary>The status <paramref name="request"/> answers: 200 for a success, the <see cref="ApiException"/>'s otherwise.</summary>
    private static async Task<HttpStatusCode> StatusOfAsync(Func<CancellationToken, Task> request)
    {
        try
        {
            await request(TestContext.Current.CancellationToken);

            return HttpStatusCode.OK;
        }
        catch (ApiException exception)
        {
            return (HttpStatusCode)exception.ErrorCode;
        }
    }

    [Fact]
    public async Task SuspendLink_SuspendsThePortal_AndContinueLinkBringsItBack()
    {
        var stack = await LetterStackFixture.CurrentAsync();

        using var portal = await stack.CreatePortalAsync(TestContext.Current.CancellationToken);

        var suspend = await LinkAsync(stack, portal.TenantId, ConfirmType.PortalSuspend);
        var resume = await LinkAsync(stack, portal.TenantId, ConfirmType.PortalContinue);

        (await WithLinkAsync(portal, suspend, portal.PortalSettingsApi.SuspendPortalAsync)).Should().Be(HttpStatusCode.OK);

        var suspended = await ReadTenantAsync(stack, portal.TenantId);
        suspended.Status.Should().Be(TenantStatus.Suspended);

        (await StatusOfAsync(portal.CommonSettingsApi.GetSupportedCulturesAsync)).Should().Be(HttpStatusCode.NotFound, "a suspended portal answers 404");
        (await StatusOfAsync(portal.CommonSettingsApi.GetPortalColorThemeAsync)).Should().Be(HttpStatusCode.OK, "the colour theme is allowed while suspended");
        (await WithLinkAsync(portal, suspend, portal.PortalSettingsApi.SuspendPortalAsync)).Should().Be(HttpStatusCode.NotFound, "suspending is not allowed on a suspended portal");

        (await WithLinkAsync(portal, resume, portal.PortalSettingsApi.ContinuePortalAsync)).Should().Be(HttpStatusCode.OK);

        (await ReadTenantAsync(stack, portal.TenantId)).Status.Should().Be(TenantStatus.Active);
        (await StatusOfAsync(portal.CommonSettingsApi.GetSupportedCulturesAsync)).Should().Be(HttpStatusCode.OK);

        // The same suspend link works again once the portal is active, and sets the status anew.
        await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);

        (await WithLinkAsync(portal, suspend, portal.PortalSettingsApi.SuspendPortalAsync)).Should().Be(HttpStatusCode.OK);

        var again = await ReadTenantAsync(stack, portal.TenantId);
        again.Status.Should().Be(TenantStatus.Suspended);
        again.StatusChanged.Should().BeAfter(suspended.StatusChanged!.Value);

        (await WithLinkAsync(portal, resume, portal.PortalSettingsApi.ContinuePortalAsync)).Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LinkOfAnotherType_IsRefused()
    {
        var stack = await LetterStackFixture.CurrentAsync();

        using var portal = await stack.CreatePortalAsync(TestContext.Current.CancellationToken);

        var resume = await LinkAsync(stack, portal.TenantId, ConfirmType.PortalContinue);
        var suspend = await LinkAsync(stack, portal.TenantId, ConfirmType.PortalSuspend);

        (await WithLinkAsync(portal, resume, portal.PortalSettingsApi.SuspendPortalAsync)).Should().Be(HttpStatusCode.Forbidden, "a continue link does not suspend");
        (await WithLinkAsync(portal, suspend, portal.PortalSettingsApi.DeletePortalAsync)).Should().Be(HttpStatusCode.Forbidden, "a suspend link does not delete");

        (await ReadTenantAsync(stack, portal.TenantId)).Status.Should().Be(TenantStatus.Active);
    }

    [Fact]
    public async Task ForgedKey_IsRefused()
    {
        var stack = await LetterStackFixture.CurrentAsync();

        using var portal = await stack.CreatePortalAsync(TestContext.Current.CancellationToken);

        var suspend = await LinkAsync(stack, portal.TenantId, ConfirmType.PortalSuspend);
        var forged = Regex.Replace(suspend, "key=[^&]+", "key=0000000000000.ABCDEFGHIJKLMNOPQRSTUVWXYZ");

        (await WithLinkAsync(portal, forged, portal.PortalSettingsApi.SuspendPortalAsync)).Should().Be(HttpStatusCode.Unauthorized);

        (await ReadTenantAsync(stack, portal.TenantId)).Status.Should().Be(TenantStatus.Active);
    }
}
