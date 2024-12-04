// (c) Copyright Ascensio System SIA 2009-2024
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

    public async Task ChangeQuotaUsedValueAsync(string featureId, object value)
    {
        var room = GetQuotaRoom();

        await MakeRequest("change-quota-used-value", new { room, featureId, value });
    }

    public async Task ChangeCustomQuotaUsedValueAsync(int tenantId, string customQuotaFeature, bool enableQuota, long usedSpace, long quotaLimit, List<Guid> userIds)
    {
        var room = $"{tenantId}-QUOTA";

        await MakeRequest("change-user-quota-used-value", new { room, customQuotaFeature, enableQuota, usedSpace, quotaLimit, userIds });
    }

    public async Task ChangeQuotaFeatureValue(string featureId, object value)
    {
        var room = GetQuotaRoom();

        await MakeRequest("change-quota-feature-value", new { room, featureId, value });
    }

    public async Task ChangeInvitationLimitValue(int value)
    {
        var room = GetQuotaRoom();

        await MakeRequest("change-invitation-limit-value", new { room, value });
    }

    public async Task LogoutSession(Guid userId, int loginEventId = 0)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await MakeRequest("logout-session", new { room = $"{tenantId}-{userId}", loginEventId });
    }

    private string GetQuotaRoom()
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        return $"{tenantId}-quota";
    }
}
