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

using CallContext = ASC.Common.Notify.Engine.CallContext;

namespace ASC.Api.Core.Middleware;

[Scope]
public class ProductSecurityFilter(ILogger<ProductSecurityFilter> logger,
        WebItemSecurity webItemSecurity,
        AuthContext authContext)
    : IAsyncResourceFilter
{
    private static readonly Dictionary<string, Guid> _products;

    static ProductSecurityFilter()
    {
        var blog = new Guid("6a598c74-91ae-437d-a5f4-ad339bd11bb2");
        var bookmark = new Guid("28b10049-dd20-4f54-b986-873bc14ccfc7");
        var forum = new Guid("853b6eb9-73ee-438d-9b09-8ffeedf36234");
        var news = new Guid("3cfd481b-46f2-4a4a-b55c-b8c0c9def02c");
        var wiki = new Guid("742cf945-cbbc-4a57-82d6-1600a12cf8ca");
        var photo = new Guid("9d51954f-db9b-4aed-94e3-ed70b914e101");

        _products = new Dictionary<string, Guid>
                {
                    { "blog", blog },
                    { "bookmark", bookmark },
                    { "event", news },
                    { "forum", forum },
                    { "photo", photo },
                    { "wiki", wiki },
                    { "birthdays", WebItemManager.BirthdaysProductID },
                    { "community", WebItemManager.CommunityProductID },
                    { "crm", WebItemManager.CRMProductID },
                    { "files", WebItemManager.DocumentsProductID },
                    { "project", WebItemManager.ProjectsProductID },
                    { "calendar", WebItemManager.CalendarProductID },
                    { "mail", WebItemManager.MailProductID }
                };
    }


    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        if (!authContext.IsAuthenticated)
        {
            await next();
            return;
        }

        if (context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
        {
            var pid = FindProduct(controllerActionDescriptor);
            if (pid != Guid.Empty)
            {
                if (CallContext.GetData("asc.web.product_id") == null)
                {
                    CallContext.SetData("asc.web.product_id", pid);
                }

                if (!await webItemSecurity.IsAvailableForMeAsync(pid))
                {
                    context.Result = new StatusCodeResult((int)HttpStatusCode.Forbidden);
                    logger.WarningPaymentRequired(controllerActionDescriptor.ControllerName, authContext.CurrentAccount.ID);
                    return;
                }
            }
        }
        await next();
    }

    private static Guid FindProduct(ControllerActionDescriptor method)
    {
        if (method == null || string.IsNullOrEmpty(method.ControllerName))
        {
            return Guid.Empty;
        }

        var name = method.ControllerName.ToLower();
        if (name == "community")
        {
            var url = method.MethodInfo.GetCustomAttribute<HttpMethodAttribute>().Template;
            if (!string.IsNullOrEmpty(url))
            {
                var module = url.Split('/')[0];
                if (_products.TryGetValue(module, out var communityProduct))
                {
                    return communityProduct;
                }
            }
        }

        return _products.GetValueOrDefault(name);
    }
}