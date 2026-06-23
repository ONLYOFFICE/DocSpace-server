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

namespace ASC.Web.Api.Core;

[Scope]
public class DnsSettings(PermissionContext permissionContext,
    TenantManager tenantManager,
    CoreBaseSettings coreBaseSettings,
    CoreSettings coreSettings,
    MessageService messageService,
    CspSettingsHelper cspSettingsHelper)
{
    public async Task<string> SaveDnsSettingsAsync(string dnsName, bool enableDns)
    {
        if (!coreBaseSettings.Standalone)
        {
            throw new NotSupportedException();
        }

        if (!SetupInfo.IsVisibleSettings<DnsSettings>())
        {
            throw new Exception(Resource.ErrorNotAllowedOption);
        }

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();

        dnsName = dnsName?.Trim().ToLowerInvariant();

        if (!enableDns || string.IsNullOrEmpty(dnsName))
        {
            dnsName = null;
        }

        if (dnsName == null || await CheckCustomDomainAsync(tenant.Alias, dnsName))
        {
            var oldDomain = tenant.GetTenantDomain(coreSettings);

            tenant.MappedDomain = dnsName;
            await tenantManager.SaveTenantAsync(tenant);
            messageService.Send(MessageAction.DnsSettingsUpdated, oldDomain, [dnsName]);
            await cspSettingsHelper.RenameDomainAsync(oldDomain, tenant.GetTenantDomain(coreSettings));
            return null;
        }

        throw new Exception(Resource.ErrorNotCorrectTrustedDomain);
    }

    private async Task<bool> CheckCustomDomainAsync(string alias, string domain)
    {
        if (string.IsNullOrEmpty(domain))
        {
            return false;
        }
        var tenantBaseDomain = TenantBaseDomain;

        if (domain.Equals($"{alias}{tenantBaseDomain}", StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(tenantBaseDomain) &&
            (domain.EndsWith(tenantBaseDomain, StringComparison.InvariantCultureIgnoreCase) || domain.Equals(tenantBaseDomain.TrimStart('.'), StringComparison.InvariantCultureIgnoreCase)))
        {
            return false;
        }

        if (Uri.TryCreate(domain.Contains(Uri.SchemeDelimiter) ? domain : Uri.UriSchemeHttp + Uri.SchemeDelimiter + domain, UriKind.Absolute, out var test))
        {
            try
            {
                await tenantManager.CheckTenantAddressAsync(test.Host);
            }
            catch (TenantTooShortException ex)
            {
                var minLength = ex.MinLength;
                var maxLength = ex.MaxLength;
                if (minLength > 0 && maxLength > 0)
                {
                    throw new TenantTooShortException(string.Format(Resource.ErrorTenantTooShortFormat, minLength, maxLength));
                }

                throw new TenantTooShortException(Resource.ErrorTenantTooShort);
            }
            catch (TenantIncorrectCharsException)
            {
            }
            return true;
        }
        return false;
    }

    protected string TenantBaseDomain =>
        string.IsNullOrEmpty(coreSettings.BaseDomain)
            ? string.Empty
            : $".{coreSettings.BaseDomain}";
}
