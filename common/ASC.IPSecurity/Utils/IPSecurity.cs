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

using ASC.Core.Users;

namespace ASC.IPSecurity;

[Scope]
public class IPSecurity(
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor,
    AuthContext authContext,
    TenantManager tenantManager,
    IPRestrictionsService iPRestrictionsService,
    UserManager userManager,
    SettingsManager settingsManager,
    ILogger<IPSecurity> logger)
{
    private string _currentIpForTest;
    private string CurrentIpForTest
    {
        get
        {
            return _currentIpForTest ??= configuration["ipsecurity:test"];
        }
    }

    private string _myNetworks;
    private string MyNetworks
    {
        get
        {
            return _myNetworks ??= configuration["ipsecurity:mynetworks"];
        }
    }

    private bool? _ipSecurityEnabled;
    private bool IpSecurityEnabled
    {
        get
        {
            if (_ipSecurityEnabled.HasValue)
            {
                return _ipSecurityEnabled.Value;
            }
            
            var hideSettings = (configuration["web:hide-settings"] ?? "").Split(',', ';', ' ');
            _ipSecurityEnabled = !hideSettings.Contains("IpSecurity", StringComparer.CurrentCultureIgnoreCase);
            return _ipSecurityEnabled.Value;
        }
    }
    
    
    public async Task<bool> VerifyAsync()
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();

        if (!IpSecurityEnabled)
        {
            return true;
        }
        
        var enable = (await settingsManager.LoadAsync<IPRestrictionsSettings>()).Enable;

        if (!enable)
        {
            return true;
        }
        
        if (httpContextAccessor?.HttpContext == null)
        {
            return true;
        }

        if (tenant == null || authContext.CurrentAccount.ID == tenant.OwnerId && !authContext.IsFromInvite())
        {
            return true;
        }

        string requestIps = null;
        try
        {
            var restrictions = (await iPRestrictionsService.GetAsync(tenant.Id)).ToList();

            if (restrictions.Count == 0)
            {
                return true;
            }

            requestIps = CurrentIpForTest;

            if (string.IsNullOrWhiteSpace(requestIps))
            {
                requestIps = httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString();
            }

            var ips = string.IsNullOrWhiteSpace(requestIps)
                          ? Array.Empty<string>()
                          : requestIps.Split([",", " "], StringSplitOptions.RemoveEmptyEntries);

            var isDocSpaceAdmin = await userManager.IsUserInGroupAsync(authContext.CurrentAccount.ID, Constants.GroupAdmin.ID);

            if (ips.Any(requestIp => restrictions.Exists(restriction => (!restriction.ForAdmin || isDocSpaceAdmin) && IPAddressRange.MatchIPs(requestIp, restriction.Ip))))
            {
                return true;
            }
            
            if (IsMyNetwork(ips))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            logger.ErrorCantVerifyRequest(requestIps ?? "", tenant, ex);

            return false;
        }

        logger.InformationRestricted(requestIps, tenant, httpContextAccessor.HttpContext.Request.GetDisplayUrl());

        return false;
    }



    private bool IsMyNetwork(string[] ips)
    {
        try
        {
            if (!string.IsNullOrEmpty(MyNetworks))
            {
                var myNetworkIps = MyNetworks.Split([",", " "], StringSplitOptions.RemoveEmptyEntries);

                if (ips.Any(requestIp => myNetworkIps.Any(ipAddress => IPAddressRange.MatchIPs(requestIp, ipAddress))))
                {
                    return true;
                }
            }

            var hostAddresses = Dns.GetHostAddresses(Dns.GetHostName());

            var localIPs = new List<IPAddress> { IPAddress.IPv6Loopback, IPAddress.Loopback };

            localIPs.AddRange(hostAddresses.Where(ip => ip.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6));

            foreach (var ipAddress in localIPs)
            {
                if (ips.Contains(ipAddress.ToString()))
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            logger.ErrorCantVerifyLocalNetWork(string.Join(",", ips), ex);
        }

        return false;
    }
}
