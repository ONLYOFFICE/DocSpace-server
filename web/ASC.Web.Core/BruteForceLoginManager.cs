﻿// (c) Copyright Ascensio System SIA 2009-2025
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Web.Core;

[Scope]
public class BruteForceLoginManager(
    IHttpContextAccessor httpContextAccessor,
    SettingsManager settingsManager,
    UserManager userManager,
    IFusionCache hybridCache,
    SetupInfo setupInfo,
    Recaptcha recaptcha, 
    IDistributedLockProvider distributedLockProvider)
{
    public async Task<(bool Result, bool ShowRecaptcha)> IncrementAsync(string key, string requestIp, bool throwException, string exceptionMessage = null)
    {
        var blockCacheKey = GetBlockCacheKey(key, requestIp);
        
        if (await GetFromCache<string>(blockCacheKey) != null)
        {
            if (throwException)
            {
                throw new BruteForceCredentialException(exceptionMessage);
            }

            return (false, true);
        }

        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetLockKey(requestIp, key)))
        {
            if (await GetFromCache<string>(blockCacheKey) != null)
            {
                throw new BruteForceCredentialException(exceptionMessage);
            }

            var historyCacheKey = GetHistoryCacheKey(key, requestIp);
            var settings = new LoginSettingsWrapper(await settingsManager.LoadAsync<LoginSettings>());
            var history = await GetFromCache<List<DateTime>>(historyCacheKey) ?? [];

            var now = DateTime.UtcNow;
            var checkTime = now.Subtract(settings.CheckPeriod);

            history = history.Where(item => item > checkTime).ToList();
            history.Add(now);

            var showRecaptcha = history.Count > settings.AttemptCount - 1;

            if (history.Count > settings.AttemptCount)
            {
                await SetToCache(blockCacheKey, "block", settings.BlockTime);
                await hybridCache.RemoveAsync(historyCacheKey);

                if (throwException)
                {
                    throw new BruteForceCredentialException(exceptionMessage);
                }

                return (false, showRecaptcha);
            }

            await SetToCache(historyCacheKey, history, settings.CheckPeriod);

            return (true, showRecaptcha);
        }
    }

    public async Task DecrementAsync(string key, string requestIp)
    {
        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetLockKey(requestIp, key)))
        {
            var settings = new LoginSettingsWrapper(await settingsManager.LoadAsync<LoginSettings>());
            var historyCacheKey = GetHistoryCacheKey(key, requestIp);
            var history = await GetFromCache<List<DateTime>>(historyCacheKey) ?? [];

            if (history.Count > 0)
            {
                history.RemoveAt(history.Count - 1);
            }

            await SetToCache(historyCacheKey, history, settings.CheckPeriod);
        }
    }

    public async Task<UserInfo> AttemptAsync(string login, RecaptchaType recaptchaType, string recaptchaResponse, UserInfo user)
    {
        var requestIp = MessageSettings.GetIP(httpContextAccessor.HttpContext?.Request);
        var secretEmail = SetupInfo.IsSecretEmail(login);

        var recaptchaPassed = secretEmail || await CheckRecaptchaAsync(recaptchaType, recaptchaResponse, requestIp);

        var blockCacheKey = GetBlockCacheKey(login, requestIp);

        if (!recaptchaPassed && await GetFromCache<string>(blockCacheKey) != null)
        {
            throw new BruteForceCredentialException();
        }

        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetLockKey(requestIp, login)))
        {
            if (!recaptchaPassed && await GetFromCache<string>(blockCacheKey) != null)
            {
                throw new BruteForceCredentialException();
            }

            string historyCacheKey = null;
            var now = DateTime.UtcNow;
            LoginSettingsWrapper settings = null;
            List<DateTime> history = null;

            if (!recaptchaPassed)
            {
                historyCacheKey = GetHistoryCacheKey(login, requestIp);

                settings = new LoginSettingsWrapper(await settingsManager.LoadAsync<LoginSettings>());
                var checkTime = now.Subtract(settings.CheckPeriod);

                history = await GetFromCache<List<DateTime>>(historyCacheKey) ?? [];
                history = history.Where(item => item > checkTime).ToList();
                history.Add(now);

                if (history.Count > settings.AttemptCount)
                {
                    await SetToCache(blockCacheKey, "block", settings.BlockTime);
                    await hybridCache.RemoveAsync(historyCacheKey);
                    throw new BruteForceCredentialException();
                }

                await SetToCache(historyCacheKey, history, settings.CheckPeriod);
            }

            if (user == null || !userManager.UserExists(user))
            {
                throw new AuthenticationException("user not found");
            }

            if (recaptchaPassed)
            {
                return user;
            }

            history.RemoveAt(history.Count - 1);

            await SetToCache(historyCacheKey, history, settings.CheckPeriod);
        }

        return user;
    }

    private async Task<bool> CheckRecaptchaAsync(RecaptchaType recaptchaType, string recaptchaResponse, string requestIp)
    {
        var recaptchaPassed = false;

        var validate = !string.IsNullOrEmpty(recaptchaResponse);

        validate = recaptchaType is RecaptchaType.hCaptcha
            ? validate && !string.IsNullOrEmpty(setupInfo.HcaptchaPublicKey) && !string.IsNullOrEmpty(setupInfo.HcaptchaPrivateKey)
            : validate && !string.IsNullOrEmpty(setupInfo.RecaptchaPublicKey) && !string.IsNullOrEmpty(setupInfo.RecaptchaPrivateKey);

        if (validate)
        {
            recaptchaPassed = await recaptcha.ValidateRecaptchaAsync(recaptchaType, recaptchaResponse, requestIp);

            if (!recaptchaPassed)
            {
                throw new RecaptchaException();
            }
        }

        return recaptchaPassed;
    }

    private async Task<T> GetFromCache<T>(string key)
    {
        return await hybridCache.GetOrDefaultAsync<T>(key);
    }

    private async Task SetToCache<T>(string key, T value, TimeSpan expirationPeriod)
    {
        await hybridCache.SetAsync(key, value, expirationPeriod);
    }

    private static string GetBlockCacheKey(string login, string requestIp) => $"loginblock/{login.ToLowerInvariant()}/{requestIp}";

    private static string GetHistoryCacheKey(string login, string requestIp) => $"loginsec/{login.ToLowerInvariant()}/{requestIp}";

    private static string GetLockKey(string ip, string key) => $"brut_force_{ip}_{key}";
}
