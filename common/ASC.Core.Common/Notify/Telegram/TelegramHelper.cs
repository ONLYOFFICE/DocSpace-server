// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Core.Common.Notify;

[Scope]
public class TelegramHelper(ConsumerFactory consumerFactory,
    TelegramDao telegramDao,
    TelegramServiceClient telegramServiceClient,
    IHttpClientFactory httpClientFactory,
    ILogger<TelegramHelper> logger)
{
    public enum RegStatus
    {
        NotRegistered,
        Registered,
        AwaitingConfirmation
    }

    public string RegisterUser(Guid userId, int tenantId)
    {
        var token = GenerateToken(userId);

        telegramServiceClient.RegisterUser(userId.ToString(), tenantId, token);

        return GetLink(token);
    }

    public void SendMessage(NotifyMessage msg)
    {
        telegramServiceClient.SendMessage(msg);
    }

    public bool CreateClient(int tenantId, string token, int tokenLifespan, string proxy)
    {
        var client = InitClient(token, proxy);
        if (TestingClient(client))
        {
            telegramServiceClient.CreateOrUpdateClient(tenantId, token, tokenLifespan, proxy);

            return true;
        }

        return false;
    }

    public async Task<RegStatus> UserIsConnectedAsync(Guid userId, int tenantId)
    {
        if (await telegramDao.GetUserAsync(userId, tenantId) != null)
        {
            return RegStatus.Registered;
        }

        return IsAwaitingRegistration(userId, tenantId) ? RegStatus.AwaitingConfirmation : RegStatus.NotRegistered;
    }

    public string CurrentRegistrationLink(Guid userId, int tenantId)
    {
        var token = GetCurrentToken(userId, tenantId);
        return string.IsNullOrEmpty(token) ? string.Empty : GetLink(token);
    }

    public void DisableClient(int tenantId)
    {
        telegramServiceClient.DisableClient(tenantId);
    }

    public async Task DisconnectAsync(Guid userId, int tenantId)
    {
        await telegramDao.DeleteAsync(userId, tenantId);
    }

    private bool IsAwaitingRegistration(Guid userId, int tenantId)
    {
        return GetCurrentToken(userId, tenantId) != null;
    }

    private string GetCurrentToken(Guid userId, int tenantId)
    {
        return telegramServiceClient.RegistrationToken(userId.ToString(), tenantId);
    }

    private string GenerateToken(Guid userId)
    {
        var id = userId.ToByteArray();
        var d = BitConverter.GetBytes(DateTime.Now.Ticks);

        var buf = id.Concat(d).ToArray();

        using var sha = SHA256.Create();

        return Convert.ToBase64String(sha.ComputeHash(buf))
            .Replace('+', '-').Replace('/', '_').Replace("=", ""); // make base64 url safe
    }

    private string GetLink(string token)
    {
        var tgProvider = (ITelegramLoginProvider)consumerFactory.GetByKey("telegram");
        var botname = tgProvider?.TelegramBotName;
        if (string.IsNullOrEmpty(botname))
        {
            return null;
        }

        return $"t.me/{botname}?start={token}";
    }

    public bool TestingClient(TelegramBotClient telegramBotClient)
    {
        try
        {
            if (!telegramBotClient.TestApiAsync().GetAwaiter().GetResult())
            {
                return false;
            }
        }
        catch (Exception e)
        {
            logger.DebugCouldNotTest(e);

            return false;
        }

        return true;
    }

    public TelegramBotClient InitClient(string token, string proxy)
    {
        if (string.IsNullOrEmpty(proxy))
        {
            return new TelegramBotClient(token);
        }

        var httpClient = httpClientFactory.CreateClient();

        httpClient.BaseAddress = new Uri(proxy);

        return new TelegramBotClient(token, httpClient);
    }
}
