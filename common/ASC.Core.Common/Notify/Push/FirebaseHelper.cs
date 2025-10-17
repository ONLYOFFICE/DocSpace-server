// (c) Copyright Ascensio System SIA 2009-2025
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
    protected readonly UserManager _userManager = userManager;

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
            var user = await _userManager.GetUserByUserNameAsync(receiver);
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
    private static string _credentials;
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
                var credentials = GetFirebaseCredentials();
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromJson(credentials)
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

    private string GetFirebaseCredentials()
    {
        if (_credentials == null)
        {
            var apiKey = new FirebaseApiKey(configuration);
            _credentials = JsonSerializer.Serialize(apiKey);
        }
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
                if (ex.MessagingErrorCode == FirebaseAdmin.Messaging.MessagingErrorCode.InvalidArgument ||
                    ex.MessagingErrorCode == FirebaseAdmin.Messaging.MessagingErrorCode.Unregistered)
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