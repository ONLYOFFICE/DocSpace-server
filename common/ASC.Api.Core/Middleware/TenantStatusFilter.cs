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

namespace ASC.Api.Core.Middleware;

[Scope]
public class TenantStatusFilter(ILogger<TenantStatusFilter> logger, TenantManager tenantManager)
    : IAsyncResourceFilter
{
    private readonly string[] _passthroughtRequestEndings = [
        "preparation-portal", "portal", "getrestoreprogress", "cancelrestore", "settings", "settings.json",
        "colortheme", "colortheme.json", "logos", "logos.json", "build", "build.json", "@self", "@self.json",
        "encryption/progress", "rebranding/additional", "rebranding/company"
    ]; //TODO add or update when "preparation-portal" will be done


    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var tenant = tenantManager.GetCurrentTenant(false);
        if (tenant == null)
        {
            context.Result = new StatusCodeResult((int)HttpStatusCode.NotFound);
            logger.WarningTenantNotFound();
            return;
        }

        if (tenant.Status is TenantStatus.RemovePending or TenantStatus.Suspended)
        {
            if (tenant.Status == TenantStatus.Suspended &&
                context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor &&
                controllerActionDescriptor.EndpointMetadata.OfType<AllowSuspendedAttribute>().Any())
            {
                await next();
                return;
            }

            context.Result = new StatusCodeResult((int)HttpStatusCode.NotFound);
            logger.WarningTenantIsNotRemoved(tenant.Id);
            return;
        }

        if (tenant.Status is TenantStatus.Transfering or TenantStatus.Restoring or TenantStatus.Encryption)
        {
            if (_passthroughtRequestEndings.Any(path => context.HttpContext.Request.Path.ToString().EndsWith(path, StringComparison.InvariantCultureIgnoreCase)))
            {
                await next();
                return;
            }

            context.Result = new StatusCodeResult((int)HttpStatusCode.Forbidden);
            logger.WarningTenantStatus(tenant.Id, tenant.Status);
            return;
        }
        await next();
    }
}