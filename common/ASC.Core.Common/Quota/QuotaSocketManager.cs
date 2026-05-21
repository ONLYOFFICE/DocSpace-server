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

using System.Threading.Channels;

namespace ASC.Core.Common.Quota;
public class QuotaSocketManager(
    ITariffService tariffService,
    TenantManager tenantManager,
    ChannelWriter<SocketData> channelWriter,
    MachinePseudoKeys machinePseudoKeys,
    IConfiguration configuration)
    : SocketServiceClient(tariffService, tenantManager, channelWriter, machinePseudoKeys, configuration)
{
    protected override string Hub => "files";

    public async Task ChangeQuotaUsedValueAsync<T>(string featureId, T value)
    {
        var room = GetQuotaRoom();

        await MakeRequest("change-quota-used-value", new { room, featureId, value });
    }

    public async Task ChangeCustomQuotaUsedValueAsync(int tenantId, string customQuotaFeature, bool enableQuota, long usedSpace, long quotaLimit, List<Guid> userIds)
    {
        var room = $"{tenantId}-QUOTA";

        await MakeRequest("change-user-quota-used-value", new { room, customQuotaFeature, enableQuota, usedSpace, quotaLimit, userIds });
    }

    public async Task ChangeQuotaFeatureValueAsync<T>(string featureId, T value)
    {
        var room = GetQuotaRoom();

        await MakeRequest("change-quota-feature-value", new { room, featureId, value });
    }

    public async Task ChangeInvitationLimitValue(int value)
    {
        var room = GetQuotaRoom();

        await MakeRequest("change-invitation-limit-value", new { room, value });
    }

    public async Task ChangeWebPlugin(string webPluginName, bool enabled)
    {
        var room = GetQuotaRoom();

        await MakeRequest("change-web-plugin", new { room, webPluginName, enabled });
    }

    public async Task TopUpWallet(bool auto)
    {
        var room = GetQuotaRoom();

        await MakeRequest("top-up-wallet", new { room, auto });
    }

    public async Task TopUpAiAsync(bool auto)
    {
        var room = GetQuotaRoom();

        await MakeRequest("top-up-ai", new { room, auto });
    }

    public async Task LogoutSession(Guid userId, int loginEventId = 0, string redirectUrl = null)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await MakeRequest("logout-session", new { room = $"{tenantId}-{userId}", loginEventId, redirectUrl });
    }

    public async Task ChangeAiConfigAsync()
    {
        var room = GetQuotaRoom();

        await MakeRequest("change-ai-config", new { room });
    }

    public async Task EncryptionProgressAsync(int percentage, string error)
    {
        await MakeRequest("encryption-progress", new { room = "storage-encryption", percentage, error }, tenantId: -1);
    }

    public async Task UserQuotaExceededAsync(Guid userId)
    {
        await QuotaExceededAsync(QuotaScope.User, userId);
    }

    public async Task RoomQuotaExceededAsync(int roomId)
    {
        await QuotaExceededAsync(QuotaScope.Room, roomId);
    }

    public async Task TenantQuotaExceededAsync()
    {
        await QuotaExceededAsync(QuotaScope.Tenant, _tenantManager.GetCurrentTenantId());
    }

    private async Task QuotaExceededAsync(QuotaScope scope, Guid entityId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        var room = scope switch
        {
            QuotaScope.User => $"{tenantId}-user-{entityId}-quota",
            _ => throw new ArgumentException("Invalid scope for Guid entityId", nameof(scope))
        };

        await MakeRequest(
            "quota_exceeded",
            new
            {
                room,
                scope = scope.ToString().ToLower(),
                id = entityId
            });
    }

    private async Task QuotaExceededAsync(QuotaScope scope, int entityId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        var room = scope switch
        {
            QuotaScope.Room => $"{tenantId}-room-{entityId}-quota",
            QuotaScope.Tenant => $"{tenantId}-tenant-quota",
            _ => throw new ArgumentException("Invalid scope for int entityId", nameof(scope))
        };

        await MakeRequest(
            "quota_exceeded",
            new
            {
                room,
                scope = scope.ToString().ToLower(),
                id = entityId
            });
    }
    private string GetQuotaRoom()
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        return $"{tenantId}-quota";
    }

    private enum QuotaScope
    {
        [Description("User")]
        User,

        [Description("Room")]
        Room,

        [Description("Tenant")]
        Tenant
    }
}
