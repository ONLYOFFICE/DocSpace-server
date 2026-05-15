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

namespace ASC.Core.Notify.Jabber;

[Scope]
public class JabberServiceClient(UserManager userManager, AuthContext authContext, TenantManager tenantManager)
{
    private static readonly TimeSpan _timeout = TimeSpan.FromMinutes(2);
    private static DateTime _lastErrorTime;

    private static bool IsServiceProbablyNotAvailable()
    {
        return _lastErrorTime != default && _lastErrorTime + _timeout > DateTime.Now;
    }

    public bool SendMessage(int tenantId, string from, string to, string text, string subject)
    {
        if (IsServiceProbablyNotAvailable())
        {
            return false;
        }

        using var service = GetService();
        try
        {
            service.SendMessage(tenantId, from, to, text, subject);
            return true;
        }
        catch (Exception error)
        {
            ProcessError(error);
        }

        return false;
    }

    public string GetVersion()
    {
        using var service = GetService();
        try
        {
            return service.GetVersion();
        }
        catch (Exception error)
        {
            ProcessError(error);
        }

        return null;
    }

    public async Task<int> GetNewMessagesCountAsync()
    {
        const int result = 0;
        if (IsServiceProbablyNotAvailable())
        {
            return result;
        }

        await using var service = GetService();
        try
        {
            return service.GetNewMessagesCount(GetCurrentTenantId(), await GetCurrentUserNameAsync());
        }
        catch (Exception error)
        {
            ProcessError(error);
        }

        return result;
    }

    public async Task<byte> AddXmppConnectionAsync(string connectionId, byte state)
    {
        byte result = 4;
        if (IsServiceProbablyNotAvailable())
        {
            throw new Exception();
        }

        await using var service = GetService();
        try
        {
            result = service.AddXmppConnection(connectionId, await GetCurrentUserNameAsync(), state, GetCurrentTenantId());
        }
        catch (Exception error)
        {
            ProcessError(error);
        }

        return result;
    }

    public async Task<byte> RemoveXmppConnectionAsync(string connectionId)
    {
        const byte result = 4;
        if (IsServiceProbablyNotAvailable())
        {
            return result;
        }

        await using var service = GetService();
        try
        {
            return service.RemoveXmppConnection(connectionId, await GetCurrentUserNameAsync(), GetCurrentTenantId());
        }
        catch (Exception error)
        {
            ProcessError(error);
        }

        return result;
    }

    public async Task<byte> GetStateAsync(string userName)
    {
        const byte defaultState = 0;

        try
        {
            if (IsServiceProbablyNotAvailable())
            {
                return defaultState;
            }

            await using var service = GetService();

            return service.GetState(GetCurrentTenantId(), userName);
        }
        catch (Exception error)
        {
            ProcessError(error);
        }

        return defaultState;
    }

    public async Task<byte> SendStateAsync(byte state)
    {
        try
        {
            if (IsServiceProbablyNotAvailable())
            {
                throw new Exception();
            }

            await using var service = GetService();

            return service.SendState(GetCurrentTenantId(), await GetCurrentUserNameAsync(), state);
        }
        catch (Exception error)
        {
            ProcessError(error);
        }

        return 4;
    }

    public async Task<Dictionary<string, byte>> GetAllStatesAsync()
    {
        Dictionary<string, byte> states = null;
        try
        {
            if (IsServiceProbablyNotAvailable())
            {
                throw new Exception();
            }

            await using var service = GetService();
            states = service.GetAllStates(GetCurrentTenantId(), await GetCurrentUserNameAsync());
        }
        catch (Exception error)
        {
            ProcessError(error);
        }

        return states;
    }

    public async Task<MessageClass[]> GetRecentMessagesAsync(string to, int id)
    {
        MessageClass[] messages = null;
        try
        {
            if (IsServiceProbablyNotAvailable())
            {
                throw new Exception();
            }

            await using var service = GetService();
            messages = service.GetRecentMessages(GetCurrentTenantId(), await GetCurrentUserNameAsync(), to, id);
        }
        catch (Exception error)
        {
            ProcessError(error);
        }

        return messages;
    }

    public async Task PingAsync(byte state)
    {
        try
        {
            if (IsServiceProbablyNotAvailable())
            {
                throw new Exception();
            }

            await using var service = GetService();
            service.Ping(authContext.CurrentAccount.ID.ToString(), GetCurrentTenantId(), await GetCurrentUserNameAsync(), state);
        }
        catch (Exception error)
        {
            ProcessError(error);
        }
    }

    private int GetCurrentTenantId()
    {
        return tenantManager.GetCurrentTenantId();
    }

    private async Task<string> GetCurrentUserNameAsync()
    {
        return (await userManager.GetUsersAsync(authContext.CurrentAccount.ID)).UserName;
    }

    private static void ProcessError(Exception error)
    {
        if (error is FaultException)
        {
            throw error;
        }
        if (error is CommunicationException or TimeoutException)
        {
            _lastErrorTime = DateTime.Now;
        }

        throw error;
    }

    private JabberServiceClientWcf GetService()
    {
        return new JabberServiceClientWcf();
    }
}