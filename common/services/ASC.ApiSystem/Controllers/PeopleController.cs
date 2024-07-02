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

namespace ASC.ApiSystem.Controllers;

[Scope]
[ApiController]
[Route("[controller]")]
public class PeopleController(
        ILogger<PeopleController> option,
        HostedSolution hostedSolution,
        UserFormatter userFormatter,
        ICache cache,
        CoreSettings coreSettings,
        CommonLinkUtility commonLinkUtility,
        CommonMethods commonMethods)
    : ControllerBase
{
    #region For TEST api

    [HttpGet("test")]
    public IActionResult Check()
    {
        return Ok(new
        {
            value = "Portal api works"
        });
    }

    #endregion

    #region API methods

    [HttpPost("find")]
    [AllowCrossSiteJson]
    public async Task<IActionResult> FindAsync(FindPeopleModel model)
    {
        var sw = Stopwatch.StartNew();
        var userIds = model.UserIds ?? new List<Guid>();

        var users = await hostedSolution.FindUsersAsync(userIds);

        var result = await users.ToAsyncEnumerable().SelectAwait(async user => new
        {
            id = user.Id,
            name = userFormatter.GetUserName(user),
            email = user.Email,

            link = await GetUserProfileLinkAsync(user)
        }).ToListAsync();

        option.LogDebug("People find {0} / {1}; Elapsed {2} ms", result.Count, userIds.Count(), sw.ElapsedMilliseconds);
        sw.Stop();

        return Ok(new
        {
            result
        });
    }

    #endregion

    #region private methods

    private async Task<string> GetTenantDomainAsync(int tenantId)
    {
        var domain = cache.Get<string>(tenantId.ToString());
        if (string.IsNullOrEmpty(domain))
        {
            var tenant = await hostedSolution.GetTenantAsync(tenantId);
            domain = tenant.GetTenantDomain(coreSettings);
            cache.Insert(tenantId.ToString(), domain, TimeSpan.FromMinutes(10));
        }
        return domain;
    }

    private async Task<string> GetUserProfileLinkAsync(UserInfo user)
    {
        var scheme = commonMethods.GetRequestScheme();
        var tenantDomain = await GetTenantDomainAsync(user.TenantId);
        var profileUrl = await commonLinkUtility.GetUserProfileAsync(user.Id);
        return string.Format("{0}{1}{2}{3}",
                             scheme,
                             Uri.SchemeDelimiter,
                             tenantDomain,
                             profileUrl.TrimStart('~'));
    }

    #endregion
}
