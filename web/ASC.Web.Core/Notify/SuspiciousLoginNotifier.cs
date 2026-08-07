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

namespace ASC.Web.Studio.Core.Notify;

[Singleton]
public class SuspiciousLoginNotifierConfiguration(IConfiguration configuration)
{
    public int HistoryLimit =>
        field != default ? field : field = configuration.GetValue<int?>("web:suspiciousLogin:historyLimit") ?? 30;

    public int FailThreshold =>
        field != default ? field : field = configuration.GetValue<int?>("web:suspiciousLogin:failThreshold") ?? 3;

    public int SuspiciousScore =>
        field != default ? field : field = configuration.GetValue<int?>("web:suspiciousLogin:suspiciousScore") ?? 2;

    public TimeSpan FailWindow =>
        field != default ? field : field = TimeSpan.FromHours(configuration.GetValue<double?>("web:suspiciousLogin:failWindowHours") ?? 24);

    public TimeSpan FreshLoginWindow =>
        field != default ? field : field = TimeSpan.FromMinutes(configuration.GetValue<double?>("web:suspiciousLogin:freshLoginWindowMinutes") ?? 5);
}

[Scope]
public partial class SuspiciousLoginNotifier(
    IDbContextFactory<MessagesContext> messagesContextFactory,
    TenantManager tenantManager,
    UserManager userManager,
    GeolocationHelper geolocationHelper,
    StudioNotifyService studioNotifyService,
    SuspiciousLoginNotifierConfiguration configuration,
    ILogger<SuspiciousLoginNotifier> logger)
{
    private static readonly int[] _successActions =
    [
        (int)MessageAction.LoginSuccess,
        (int)MessageAction.LoginSuccessViaSocialAccount,
        (int)MessageAction.LoginSuccessViaSms,
        (int)MessageAction.LoginSuccessViaApi,
        (int)MessageAction.LoginSuccessViaSocialApp,
        (int)MessageAction.LoginSuccessViaApiSms,
        (int)MessageAction.LoginSuccessViaSSO,
        (int)MessageAction.LoginSuccessViaApiSocialAccount,
        (int)MessageAction.LoginSuccesViaTfaApp,
        (int)MessageAction.LoginSuccessViaApiTfa,
        (int)MessageAction.LoginSuccessViaOAuth,
        (int)MessageAction.LoginSuccessViaPassword,
        (int)MessageAction.AuthLinkActivated
    ];

    private static readonly int[] _failActions =
    [
        (int)MessageAction.LoginFailInvalidCombination,
        (int)MessageAction.LoginFailSocialAccountNotFound,
        (int)MessageAction.LoginFailDisabledProfile,
        (int)MessageAction.LoginFail,
        (int)MessageAction.LoginFailViaSms,
        (int)MessageAction.LoginFailViaApi,
        (int)MessageAction.LoginFailViaApiSms,
        (int)MessageAction.LoginFailViaApiTfa,
        (int)MessageAction.LoginFailViaApiSocialAccount,
        (int)MessageAction.LoginFailViaTfaApp,
        (int)MessageAction.LoginFailViaSSO,
        (int)MessageAction.LoginFailIpSecurity,
        (int)MessageAction.LoginFailBruteForce,
        (int)MessageAction.LoginFailRecaptcha
    ];

    private static readonly Regex _versionToken = VersionRegex();
    private static readonly Regex _whitespace = WhitespaceRegex();

    public async Task CheckAsync(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return;
            }

            var user = await userManager.GetUsersAsync(userId);
            if (user == null || user.Id == Guid.Empty || string.IsNullOrEmpty(user.Email))
            {
                return;
            }

            var tenantId = tenantManager.GetCurrentTenantId();

            await using var messagesContext = await messagesContextFactory.CreateDbContextAsync();

            var recentSuccess = await messagesContext.LoginEvents
                .Where(e => e.TenantId == tenantId
                    && e.UserId == userId
                    && e.Action.HasValue
                    && _successActions.Contains(e.Action.Value))
                .OrderByDescending(e => e.Id)
                .Take(configuration.HistoryLimit)
                .ToListAsync();

            if (recentSuccess.Count == 0)
            {
                return;
            }

            var current = recentSuccess[0];

            if (DateTime.UtcNow - current.Date > configuration.FreshLoginWindow)
            {
                return;
            }

            var baseline = recentSuccess.Skip(1).ToList();

            if (baseline.Count == 0)
            {
                return;
            }

            // Signal 1: OS + browser pair not seen among previous successful logins
            var currentDeviceKey = DeviceKey(current);
            var newDevice = baseline.All(e => DeviceKey(e) != currentDeviceKey);

            // Signal 2: login from a country the account has not been used from before
            var currentGeo = await geolocationHelper.GetGeolocationAsync(current.Ip);
            var currentCountry = currentGeo[0];
            var newCountry = false;
            if (!string.IsNullOrEmpty(currentCountry))
            {
                var seenAnyCountry = false;
                var knownCountry = false;
                foreach (var e in baseline)
                {
                    var country = (await geolocationHelper.GetGeolocationAsync(e.Ip))[0];
                    if (string.IsNullOrEmpty(country))
                    {
                        continue;
                    }
                    seenAnyCountry = true;
                    if (string.Equals(country, currentCountry, StringComparison.OrdinalIgnoreCase))
                    {
                        knownCountry = true;
                        break;
                    }
                }
                newCountry = seenAnyCountry && !knownCountry;
            }

            // Signal 3: several failed login attempts recently
            var failSince = DateTime.UtcNow - configuration.FailWindow;
            var failCount = await messagesContext.LoginEvents
                .Where(e => e.TenantId == tenantId
                    && e.Date >= failSince
                    && (e.UserId == userId || e.Login == user.Email)
                    && e.Action.HasValue
                    && _failActions.Contains(e.Action.Value))
                .CountAsync();
            var manyFails = failCount >= configuration.FailThreshold;

            var score =
                (newDevice ? 1 : 0) +
                (newCountry ? 1 : 0) +
                (manyFails ? 1 : 0);

            if (score < configuration.SuspiciousScore)
            {
                return;
            }

            var loginEvent = new BaseEvent
            {
                IP = current.Ip,
                Browser = current.Browser,
                Platform = current.Platform,
                Date = current.Date,
                Country = currentCountry,
                City = currentGeo[1]
            };

            await studioNotifyService.SendSuspiciousLoginAsync(user, loginEvent);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }
    }

    private static string DeviceKey(DbLoginEvent e)
    {
        return Normalize(e.Browser) + "|" + Normalize(e.Platform);
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        value = _versionToken.Replace(value, " ");
        value = _whitespace.Replace(value, " ");

        return value.Trim().ToLowerInvariant();
    }

    [GeneratedRegex(@"\b\d[\d.]*\b", RegexOptions.Compiled)]
    private static partial Regex VersionRegex();
    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}
