// (c) Copyright Ascensio System SIA 2009-2026
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
public class SettingsController(
        ILogger<SettingsController> logger,
        CommonMethods commonMethods,
        CoreSettings coreSettings)
    : ControllerBase
{

    #region For TEST api

    /// <remarks>
    /// Test API.
    /// </remarks>
    /// <summary>Test API.</summary>
    /// <path>apisystem/settings/test</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [SwaggerResponse(200, "Settings api works")]
    [HttpGet("test")]
    [AllowAnonymous]
    public IActionResult Check()
    {
        return Ok(new
        {
            value = "Settings api works"
        });
    }

    #endregion

    #region API methods

    /// <remarks>
    /// Returns the portal settings by the parameters specified in the request.
    /// </remarks>
    /// <summary>
    /// Get settings
    /// </summary>
    /// <path>apisystem/settings/get</path>
    [Tags("Settings")]
    [SwaggerResponse(200, "Settings", typeof(IActionResult))]
    [HttpGet("get")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default,auth:portal,auth:portalbasic")]
    public async Task<IActionResult> GetSettingsAsync([FromQuery] SettingsModel model)
    {
        var (succ, tenantId, error) = await GetTenantAsync(model);
        if (!succ)
        {
            return BadRequest(error);
        }

        if (string.IsNullOrEmpty(model.Key))
        {
            return BadRequest(new ErrorDto
            {
                Error = "params",
                Message = "Key is required"
            });
        }

        var settings = await coreSettings.GetSettingAsync(model.Key, tenantId);

        return Ok(new
        {
            settings
        });
    }

    /// <remarks>
    /// Saves the settings specified in the request for the current portal.
    /// </remarks>
    /// <summary>
    /// Save settings
    /// </summary>
    /// <path>apisystem/settings/save</path>
    [Tags("Settings")]
    [SwaggerResponse(200, "Settings", typeof(IActionResult))]
    [HttpPost("save")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default,auth:portal,auth:portalbasic")]
    public async Task<IActionResult> SaveSettingsAsync([FromBody] SettingsModel model)
    {
        var (succ, tenantId, error) = await GetTenantAsync(model);
        if (!succ)
        {
            return BadRequest(error);
        }

        if (string.IsNullOrEmpty(model.Key))
        {
            return BadRequest(new ErrorDto
            {
                Error = "params",
                Message = "Key is required"
            });
        }

        if (string.IsNullOrEmpty(model.Value))
        {
            return BadRequest(new ErrorDto
            {
                Error = "params",
                Message = "Value is empty"
            });
        }

        if (model.Key.Equals("BaseDomain", StringComparison.InvariantCultureIgnoreCase))
        {
            if (Uri.CheckHostName(model.Value) != UriHostNameType.Dns)
            {
                return BadRequest(new ErrorDto
                {
                    Error = "params",
                    Message = "BaseDomain is not valid"
                });
            }
        }

        logger.DebugSaveSetting(model.Key, model.Value, tenantId);

        await coreSettings.SaveSettingAsync(model.Key, model.Value, tenantId);

        var settings = await coreSettings.GetSettingAsync(model.Key, tenantId);

        return Ok(new
        {
            settings
        });
    }

    /// <remarks>
    /// Checks the domain with the name specified in the request.
    /// </remarks>
    /// <summary>
    /// Check the domain name
    /// </summary>
    /// <path>apisystem/settings/checkdomain</path>
    [Tags("Settings")]
    [SwaggerResponse(200, "True if success", typeof(IActionResult))]
    [HttpPost("checkdomain")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default,auth:portal,auth:portalbasic")]
    public async Task<IActionResult> CheckDomain([FromBody] DomainModel model)
    {
        if (model == null || string.IsNullOrEmpty(model.HostName))
        {
            return BadRequest(new ErrorDto
            {
                Error = "hostNameEmpty",
                Message = "HostName is required"
            });
        }

        if (Uri.CheckHostName(model.HostName) != UriHostNameType.Dns)
        {
            return BadRequest(new ErrorDto
            {
                Error = "hostNameInvalid",
                Message = "HostName is not valid"
            });
        }

        try
        {
            var currentHostIps = await commonMethods.GetHostIpsAsync();

            var hostIps = (await Dns.GetHostAddressesAsync(model.HostName)).Select(ip => ip.ToString());

            return Ok(new
            {
                value = currentHostIps.Any(ip => hostIps.Contains(ip))
            });
        }
        catch (Exception ex)
        {
            logger.ErrorCheckDomain(model.HostName, ex);

            return Ok(new
            {
                value = false
            });
        }
    }

    #endregion

    #region private methods

    private async Task<(bool, int, ErrorDto)> GetTenantAsync(SettingsModel model)
    {
        ErrorDto error;
        var tenantId = -1;

        if (model == null)
        {
            error = new ErrorDto
            {
                Error = "portalNameEmpty",
                Message = "PortalName is required"
            };

            logger.ErrorModelIsNull();

            return (false, tenantId, error);
        }

        if (model.TenantId is -1)
        {
            tenantId = model.TenantId.Value;
            return (true, tenantId, null);
        }

        var (success, tenant) = await commonMethods.TryGetTenantAsync(model);
        if (!success)
        {
            error = new ErrorDto
            {
                Error = "portalNameEmpty",
                Message = "PortalName is required"
            };

            logger.ErrorModelWithoutTenant();

            return (false, tenantId, error);
        }

        if (tenant == null)
        {
            error = new ErrorDto
            {
                Error = "portalNameNotFound",
                Message = "Portal not found"
            };

            logger.ErrorTenantNotFound();

            return (false, tenantId, error);
        }

        tenantId = tenant.Id;
        return (true, tenantId, null);
    }

    #endregion
}
