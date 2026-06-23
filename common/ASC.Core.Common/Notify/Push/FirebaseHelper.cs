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

namespace ASC.Core.Common.Notify.Push;

[Scope]
public class FirebaseHelper(AuthContext authContext,
    UserManager userManager,
    TenantManager tenantManager,
    IConfiguration configuration,
    ILogger<FirebaseHelper> logger,
    FirebaseDao firebaseDao,
    CacheFirebaseDao cacheFirebaseDao,
    IFusionCache cache)
{
    public async Task SendMessageAsync(NotifyMessage msg)
    {
        var receiver = msg.Reciever;

        await InitializeFirebaseAsync();

        if (FirebaseApp.DefaultInstance == null)
        {
            return;
        }

        await tenantManager.SetCurrentTenantAsync(msg.TenantId);

        var userId = await cache.GetOrDefaultAsync<Guid>(GetCacheKey(msg.TenantId, receiver));

        if (Equals(userId, Guid.Empty))
        {
            var user = await userManager.GetUserByUserNameAsync(receiver);
            if (user == null)
            {
                return;
            }
            userId = user.Id;
            await cache.SetAsync(GetCacheKey(msg.TenantId, receiver), user.Id);
        }

        var fireBaseUsers = await cacheFirebaseDao.GetSubscribedUserDeviceTokensAsync(userId, msg.TenantId, PushConstants.PushDocAppName);

        var messages = fireBaseUsers.Select(fb => CreateFirebaseMessage(fb, msg)).ToList();
        await SendMessagesAsync(userId, msg.TenantId, messages);

    }

    private static readonly SemaphoreSlim _firebaseInitLock = new(initialCount: 1, maxCount: 1);
    private static bool _firebaseInitialized;
    private static GoogleCredential _credentials;
    private async Task InitializeFirebaseAsync()
    {
        if (_firebaseInitialized)
        {
            return;
        }

        await _firebaseInitLock.WaitAsync();
        try
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                var credential = GetFirebaseCredentials();
                FirebaseApp.Create(new AppOptions
                {
                    Credential = credential
                });
                _firebaseInitialized = true;
            }
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            throw;
        }
        finally
        {
            _firebaseInitLock.Release();
        }
    }

    private GoogleCredential GetFirebaseCredentials()
    {
        if (_credentials != null)
        {
            return _credentials;
        }
        var apiKey = new FirebaseApiKey(configuration);

        var serviceCredential = new ServiceAccountCredential(
            new ServiceAccountCredential.Initializer(apiKey.ClientEmail)
            {
                ProjectId = apiKey.ProjectId
            }
            .FromPrivateKey(apiKey.PrivateKey.Replace("\\n", "\n"))
        );

        _credentials = GoogleCredential.FromServiceAccountCredential(serviceCredential);

        return _credentials;
    }

    private FirebaseAdminMessaging.Message CreateFirebaseMessage(FireBaseUser user, NotifyMessage msg)
    {
        return new FirebaseAdminMessaging.Message
        {
            Data = new Dictionary<string, string> { { "data", msg.Data } },
            Token = user.FirebaseDeviceToken,
            Notification = new FirebaseAdminMessaging.Notification
            {
                Body = msg.Content
            }
        };
    }
    private async Task SendMessagesAsync(Guid userId, int tenantId, List<FirebaseAdminMessaging.Message> messages)
    {

        var tasks = messages.Select(async message =>
        {
            try
            {
                await FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance.SendAsync(message);
            }
            catch (FirebaseAdmin.Messaging.FirebaseMessagingException ex)
            {
                if (ex.MessagingErrorCode is FirebaseAdmin.Messaging.MessagingErrorCode.InvalidArgument or FirebaseAdmin.Messaging.MessagingErrorCode.Unregistered)
                {
                    await HandleInvalidTokenAsync(userId, tenantId, message.Token);
                }
                logger.ErrorWithException(ex);
            }
            catch (Exception ex)
            {
                logger.ErrorWithException(ex);
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task HandleInvalidTokenAsync(Guid userId, int tenantId, string token)
    {
        try
        {
            await firebaseDao.DeleteInvalidTokenAsync(userId, tenantId, token);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }
    }

    public async Task<FireBaseUser> RegisterUserDeviceAsync(string fbDeviceToken, bool isSubscribed, string application)
    {
        var userId = authContext.CurrentAccount.ID;
        var tenantId = tenantManager.GetCurrentTenantId();

        return await firebaseDao.RegisterUserDeviceAsync(userId, tenantId, fbDeviceToken, isSubscribed, application);
    }

    public async Task<FireBaseUser> UpdateUserAsync(string fbDeviceToken, bool isSubscribed, string application)
    {
        var userId = authContext.CurrentAccount.ID;
        var tenantId = tenantManager.GetCurrentTenantId();

        return await firebaseDao.UpdateUserAsync(userId, tenantId, fbDeviceToken, isSubscribed, application);
    }

    private static string GetCacheKey(int tenantId, string receiver)
    {
        return tenantId + receiver;
    }
}