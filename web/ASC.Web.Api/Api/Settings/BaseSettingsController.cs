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

///<summary>
/// Portal settings API.
///</summary>
///<name>settings</name>
[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("settings")]
public class BaseSettingsController(IFusionCache fusionCache, WebItemManager webItemManager) : ControllerBase
{
    //private const int ONE_THREAD = 1;

    //private static readonly DistributedTaskQueue quotaTasks = new DistributedTaskQueue("quotaOperations", ONE_THREAD);
    //private static DistributedTaskQueue LDAPTasks { get; } = new DistributedTaskQueue("ldapOperations");
    //private static DistributedTaskQueue SMTPTasks { get; } = new DistributedTaskQueue("smtpOperations");

    internal readonly WebItemManager WebItemManager = webItemManager;
    private readonly int _maxCount = 10;
    private readonly int _expirationMinutes = 2;

    internal async Task CheckCache(string baseKey)
    {
        var key = HttpContext.Connection.RemoteIpAddress + baseKey;
        var countFromCache = await fusionCache.TryGetAsync<int>(key);
        var count = countFromCache.HasValue ? countFromCache.Value : 0;
        if (count > _maxCount)
        {
            throw new Exception(Resource.ErrorRequestLimitExceeded);
        }

        await fusionCache.SetAsync(key, count + 1, TimeSpan.FromMinutes(_expirationMinutes));
    }

    internal string GetProductName(Guid productId)
    {
        var product = WebItemManager[productId];
        if (productId == Guid.Empty)
        {
            return "All";
        }

        return product != null ? product.Name : productId.ToString();
    }
}