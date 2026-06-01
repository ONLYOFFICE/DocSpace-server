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

namespace ASC.Web.Api.Controllers.Settings;

[DefaultRoute("tips")]
[ApiExplorerSettings(IgnoreApi = true)]
public class TipsController(
    ILoggerFactory loggerFactory,
    AuthContext authContext,
    StudioNotifyHelper studioNotifyHelper,
    SettingsManager settingsManager,
    WebItemManager webItemManager,
    SetupInfo setupInfo,
    IFusionCache fusionCache,
    IHttpClientFactory clientFactory,
    TenantManager tenantManager,
    IServiceProvider serviceProvider)
    : BaseSettingsController(fusionCache, webItemManager)
{
    private readonly ILogger _log = loggerFactory.CreateLogger("ASC.Api");

    /// <remarks>
    /// Updates the user interface tip settings with the parameters specified in the request.
    /// </remarks>
    /// <summary>Update the tip settings</summary>
    /// <path>api/2.0/settings/tips</path>
    [Tags("Settings / Tips")]
    [SwaggerResponse(200, "Updated tip settings", typeof(TipsSettings))]
    [HttpPut("")]
    public async Task<TipsSettings> UpdateTipsSettings(TipsRequestDto inDto)
    {
        var settings = new TipsSettings { Show = inDto.Show };
        await settingsManager.SaveForCurrentUserAsync(settings);

        if (!inDto.Show && !string.IsNullOrEmpty(setupInfo.TipsAddress))
        {
            try
            {
                var tenant = tenantManager.GetCurrentTenant();
                using var request = new HttpRequestMessage();
                request.RequestUri = new Uri($"{setupInfo.TipsAddress}/tips/deletereaded");

                var data = new NameValueCollection
                {
                    ["userId"] = authContext.CurrentAccount.ID.ToString(),
                    ["tenantId"] = tenant.Id.ToString(CultureInfo.InvariantCulture)
                };
                var body = JsonSerializer.Serialize(data);//todo check
                request.Content = new StringContent(body);
#pragma warning disable CA2000
                var httpClient = clientFactory.CreateClient();
#pragma warning restore CA2000
                using var response = await httpClient.SendAsync(request);

            }
            catch (Exception e)
            {
                _log.ErrorWithException(e);
            }
        }

        return settings;
    }

    /// <remarks>
    /// Updates the tip subscription.
    /// </remarks>
    /// <summary>Update the tip subscription</summary>
    /// <path>api/2.0/settings/tips/change/subscription</path>
    [Tags("Settings / Tips")]
    [SwaggerResponse(200, "Boolean value: true if the user is subscribed to the tips", typeof(bool))]
    [HttpPut("change/subscription")]
    public async Task<bool> UpdateTipsSubscription()
    {
        var recipient = await studioNotifyHelper.ToRecipientAsync(authContext.CurrentAccount.ID);

        var isSubscribe = await studioNotifyHelper.IsSubscribedToNotifyAsync(recipient,  serviceProvider.GetService<PeriodicNotifyAction>());

        await studioNotifyHelper.SubscribeToNotifyAsync(recipient, serviceProvider.GetService<PeriodicNotifyAction>(), !isSubscribe);

        return !isSubscribe;
    }

    /// <remarks>
    /// Checks if the current user is subscribed to the tips or not.
    /// </remarks>
    /// <summary>Check the tip subscription</summary>
    /// <path>api/2.0/settings/tips/subscription</path>
    [Tags("Settings / Tips")]
    [SwaggerResponse(200, "Boolean value: true if the user is subscribed to the tips", typeof(bool))]
    [HttpGet("subscription")]
    public async Task<bool> GetTipsSubscription()
    {
        return await studioNotifyHelper.IsSubscribedToNotifyAsync(authContext.CurrentAccount.ID, serviceProvider.GetService<PeriodicNotifyAction>());
    }
}
