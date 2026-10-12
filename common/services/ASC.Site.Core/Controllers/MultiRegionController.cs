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

namespace ASC.Site.Core.Controllers;

[Scope]
[ApiController]
[Route("[controller]")]
public class MultiRegionController(
        CommonConstants commonConstants,
        MultiRegionProvider multiRegionProvider,
        TenantDomainValidator tenantDomainValidator,
        HostedSolution hostedSolution,
        LoginProfileTransport loginProfileTransport,
        CommonLinkUtility commonLinkUtility,
        PasswordHasher passwordHasher,
        ApiSystemHelper apiSystemHelper,
        ILogger<MultiRegionController> logger)
    : ControllerBase
{
    [HttpPost("validatealias")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default")]
    public async Task<ValidateAliasResponseDto> ValidateAlias(ValidateAliasRequestDto inDto)
    {
        try
        {
            if (!apiSystemHelper.ApiCacheEnable)
            {
                throw new InvalidOperationException("ApiCache is not enabled.");
            }

            var alias = inDto?.Alias?.Trim()?.ToLowerInvariant();

            if (string.IsNullOrEmpty(alias))
            {
                throw new ArgumentException("Alias is empty.");
            }

            tenantDomainValidator.ValidateDomainLength(alias);

            tenantDomainValidator.ValidateDomainCharacters(alias);

            var forbidden = await hostedSolution.IsForbiddenDomainAsync(alias);

            var sameAliasTenants = forbidden ? [alias] : await apiSystemHelper.FindTenantsInCacheAsync(alias);

            if (sameAliasTenants != null)
            {
                throw new ArgumentException("Address busy.");
            }

            return new ValidateAliasResponseDto(true, null);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);

            return new ValidateAliasResponseDto(false, ex.Message);
        }
    }


    [HttpPost("findbydomain")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default")]
    public async Task<string> FindByDomain(FindByDomainRequestDto inDto)
    {
        try
        {
            if (string.IsNullOrEmpty(inDto?.Domain))
            {
                return null;
            }

            var domain = inDto.Domain.ToLowerInvariant();

            if (!string.IsNullOrEmpty(commonConstants.BaseDomain) && domain.EndsWith(commonConstants.BaseDomain))
            {
                domain = domain.Replace(commonConstants.BaseDomain, "").TrimEnd('.');
            }

            var tenant = await multiRegionProvider.FindTenantByDomainAsync(domain);

            if (tenant == null)
            {
                return null;
            }

            var portalUrl = GetPortalDomain(commonConstants.BaseDomain, tenant.Alias, tenant.MappedDomain);

            return portalUrl;
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
            throw;
        }
    }

    [HttpPost("findbyemail")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default")]
    public async Task<IEnumerable<TenantLinksResponseDto>> FindByEmail(FindByEmailRequestDto inDto)
    {
        try
        {
            if (string.IsNullOrEmpty(inDto?.Email))
            {
                return null;
            }

            var tenantUsers = await multiRegionProvider.FindTenantsByEmailAsync(inDto.Email);

            var tenantLinks = tenantUsers
                .Select(tenantUser =>
                {
                    var portalDomain = GetPortalDomain(commonConstants.BaseDomain, tenantUser.TenantAlias, tenantUser.TenantMappedDomain);
                    var authPath = GetAuthPath(tenantUser.TenantId, tenantUser.UserEmail, false);
                    return new TenantLinksResponseDto(portalDomain, authPath);
                });

            return tenantLinks;
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
            throw;
        }
    }

    [HttpPost("findbyemailpassword")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default")]
    public async Task<IEnumerable<TenantLinksResponseDto>> FindByEmailPassword(FindByEmailPasswordRequestDto inDto)
    {
        try
        {
            if (string.IsNullOrEmpty(inDto?.Email) || (string.IsNullOrEmpty(inDto.Password) && string.IsNullOrEmpty(inDto.PasswordHash)))
            {
                return null;
            }

            var passwordHash = inDto.PasswordHash ?? passwordHasher.GetClientPassword(inDto.Password);

            var tenantUsers = await multiRegionProvider.FindTenantsByEmailPasswordAsync(inDto.Email, passwordHash);

            var tenantLinks = tenantUsers
                .Select(tenantUser =>
                {
                    var portalDomain = GetPortalDomain(commonConstants.BaseDomain, tenantUser.TenantAlias, tenantUser.TenantMappedDomain);
                    var authPath = GetAuthPath(tenantUser.TenantId, tenantUser.UserEmail, false);
                    return new TenantLinksResponseDto(portalDomain, authPath);
                });

            return tenantLinks;
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
            throw;
        }
    }

    [HttpPost("findbysocial")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default")]
    public async Task<FindBySocialResponseDto> FindBySocial(FindBySocialRequestDto inDto)
    {
        try
        {
            if (string.IsNullOrEmpty(inDto?.Transport))
            {
                return null;
            }

            var loginProfile = await loginProfileTransport.FromPureTransport(inDto.Transport);

            if (loginProfile == null)
            {
                return null;
            }

            var tenantUsers = await multiRegionProvider.FindTenantsBySocialAsync(loginProfile);

            var tenantLinks = tenantUsers
                .Select(tenantUser =>
                {
                    var portalDomain = GetPortalDomain(commonConstants.BaseDomain, tenantUser.TenantAlias, tenantUser.TenantMappedDomain);
                    var authPath = GetAuthPath(tenantUser.TenantId, tenantUser.UserEmail, true);
                    return new TenantLinksResponseDto(portalDomain, authPath);
                });

            return new FindBySocialResponseDto(loginProfile.EMail, tenantLinks);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
            throw;
        }
    }

    [HttpPost("resetpassword")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default")]
    public async Task<IEnumerable<TenantLinksResponseDto>> ResetPassword(FindByEmailRequestDto inDto)
    {
        try
        {
            if (string.IsNullOrEmpty(inDto?.Email))
            {
                return null;
            }

            var tenantUsers = await multiRegionProvider.FindTenantsByEmailAsync(inDto.Email);

            var result = new List<TenantLinksResponseDto>();

            foreach (var tenantUser in tenantUsers)
            {
                var passwordStamp = await multiRegionProvider.GetUserPasswordStampAsync(tenantUser.TenantRegion, tenantUser.TenantId, tenantUser.UserId);
                var portalDomain = GetPortalDomain(commonConstants.BaseDomain, tenantUser.TenantAlias, tenantUser.TenantMappedDomain);
                var passwordPath = GetPasswordPath(tenantUser.TenantId, tenantUser.UserEmail, tenantUser.UserId, passwordStamp.ToString("s"));
                result.Add(new TenantLinksResponseDto(portalDomain, passwordPath));
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
            throw;
        }
    }

    private string GetAuthPath(int tenantId, string email, bool social = false)
    {
        var authLink = commonLinkUtility.GetConfirmationUrlRelative(tenantId, email, ConfirmType.Auth);
        var socialParameters = social ? "&social=true" : "";

        return $"/{authLink}{socialParameters}";
    }

    private string GetPasswordPath(int tenantId, string email, Guid userID, string hash)
    {
        var passwordLink = commonLinkUtility.GetConfirmationUrlRelative(tenantId, email, ConfirmType.PasswordChange, hash, userID);

        return $"/{passwordLink}";
    }

    private string GetPortalDomain(string baseDomain, string tenantAlias, string tenantMappedDomain)
    {
        return string.IsNullOrEmpty(tenantMappedDomain) ? $"https://{tenantAlias}.{baseDomain}" : $"https://{tenantMappedDomain}";
    }
}
