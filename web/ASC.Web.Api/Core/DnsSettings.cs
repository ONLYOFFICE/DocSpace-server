﻿// (c) Copyright Ascensio System SIA 2009-2024
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
            await cspSettingsHelper.RenameDomain(oldDomain, tenant.GetTenantDomain(coreSettings));
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

    protected string TenantBaseDomain
    {
        get
        {
            return string.IsNullOrEmpty(coreSettings.BaseDomain)
                       ? string.Empty
                       : $".{coreSettings.BaseDomain}";
        }
    }
}
