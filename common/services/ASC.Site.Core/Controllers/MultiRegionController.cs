// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Site.Core.Controllers;

[Scope]
[ApiController]
[Route("[controller]")]
public class MultiRegionController(
        CommonConstants commonConstants,
        MultiRegionPrivider multiRegionPrivider,
        LoginProfileTransport loginProfileTransport,
        CommonLinkUtility commonLinkUtility,
        PasswordHasher passwordHasher,
        ILogger<MultiRegionController> logger)
    : ControllerBase
{
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

            var tenant = await multiRegionPrivider.FindTenantByDomainAsync(domain);

            if (tenant == null)
            {
                return null;
            }

            var portalUrl = GetAbsolutePortalUrl(commonConstants.BaseDomain, tenant.Alias, tenant.MappedDomain);

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
    public async Task<IEnumerable<TenantLinksDto>> FindByEmail(FindByEmailRequestDto inDto)
    {
        try
        {
            if (string.IsNullOrEmpty(inDto?.Email))
            {
                return null;
            }

            var tenantUsers = await multiRegionPrivider.FindTenantsByEmailAsync(inDto.Email);

            var tenantLinks = tenantUsers
                .Select(tenantUser =>
                {
                    var portalUrl = GetAbsolutePortalUrl(commonConstants.BaseDomain, tenantUser.TenantAlias, tenantUser.TenantMappedDomain);
                    var authUrl = GetRelativeAuthUrl(tenantUser.TenantId, tenantUser.UserEmail, false);
                    return new TenantLinksDto(portalUrl, authUrl);
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
    public async Task<IEnumerable<TenantLinksDto>> FindByEmailPassword(FindByEmailPasswordRequestDto inDto)
    {
        try
        {
            if (string.IsNullOrEmpty(inDto?.Email) || (string.IsNullOrEmpty(inDto.Password) && string.IsNullOrEmpty(inDto.PasswordHash)))
            {
                return null;
            }

            var passwordHash = inDto.PasswordHash ?? passwordHasher.GetClientPassword(inDto.Password);

            var tenantUsers = await multiRegionPrivider.FindTenantsByEmailPasswordAsync(inDto.Email, passwordHash);

            var tenantLinks = tenantUsers
                .Select(tenantUser =>
                {
                    var portalUrl = GetAbsolutePortalUrl(commonConstants.BaseDomain, tenantUser.TenantAlias, tenantUser.TenantMappedDomain);
                    var authUrl = GetRelativeAuthUrl(tenantUser.TenantId, tenantUser.UserEmail, false);
                    return new TenantLinksDto(portalUrl, authUrl);
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
    public async Task<IEnumerable<TenantLinksDto>> FindBySocial(FindBySocialRequestDto inDto)
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

            var tenantUsers = await multiRegionPrivider.FindTenantsBySocialAsync(loginProfile);

            var tenantLinks = tenantUsers
                .Select(tenantUser =>
                {
                    var portalUrl = GetAbsolutePortalUrl(commonConstants.BaseDomain, tenantUser.TenantAlias, tenantUser.TenantMappedDomain);
                    var authUrl = GetRelativeAuthUrl(tenantUser.TenantId, tenantUser.UserEmail, true);
                    return new TenantLinksDto(portalUrl, authUrl);
                });

            return tenantLinks;
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
            throw;
        }
    }


    private string GetRelativeAuthUrl(int tenantId, string email, bool social = false)
    {
        var authLink = commonLinkUtility.GetConfirmationUrlRelative(tenantId, email, ConfirmType.Auth);
        var socialParameters = social ? "&social=true" : "";

        return $"/{authLink}{socialParameters}";
    }

    private string GetAbsolutePortalUrl(string baseDomain, string tenantAlias, string tenantMappedDomain)
    {
        return string.IsNullOrEmpty(tenantMappedDomain) ? $"https://{tenantAlias}.{baseDomain}" : $"https://{tenantMappedDomain}";
    }
}
