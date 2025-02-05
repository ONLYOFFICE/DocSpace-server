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
public class SettingsController(CommonMethods commonMethods,
        CoreSettings coreSettings,
        ILogger<SettingsController> option)
    : ControllerBase
{
    private CommonMethods CommonMethods { get; } = commonMethods;
    private CoreSettings CoreSettings { get; } = coreSettings;
    private ILogger<SettingsController> Log { get; } = option;

    #region For TEST api

    /// <summary>
    /// Test api
    /// </summary>
    /// <path>apisystem/settings/test</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [SwaggerResponse(200, "Settings api works")]
    [HttpGet("test")]
    public IActionResult Check()
    {
        return Ok(new
        {
            value = "Settings api works"
        });
    }

    #endregion

    #region API methods

    /// <summary>
    /// Gets settings
    /// </summary>
    /// <path>apisystem/settings</path>
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
            return BadRequest(new
            {
                error = "params",
                message = "Key is required"
            });
        }

        var settings = await CoreSettings.GetSettingAsync(model.Key, tenantId);

        return Ok(new
        {
            settings
        });
    }

    /// <summary>
    /// Saves settings
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
            return BadRequest(new
            {
                error = "params",
                message = "Key is required"
            });
        }

        if (string.IsNullOrEmpty(model.Value))
        {
            return BadRequest(new
            {
                error = "params",
                message = "Value is empty"
            });
        }

        if (model.Key.Equals("BaseDomain", StringComparison.InvariantCultureIgnoreCase))
        {
            if (Uri.CheckHostName(model.Value) != UriHostNameType.Dns)
            {
                return BadRequest(new
                {
                    error = "params",
                    message = "BaseDomain is not valid"
                });
            }
        }

        Log.LogDebug("Set {0} value {1} for {2}", model.Key, model.Value, tenantId.ToString());

        await CoreSettings.SaveSettingAsync(model.Key, model.Value, tenantId);

        var settings = await CoreSettings.GetSettingAsync(model.Key, tenantId);

        return Ok(new
        {
            settings
        });
    }

    /// <summary>
    /// Checks domain
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
            return BadRequest(new
            {
                error = "hostNameEmpty",
                message = "HostName is required"
            });
        }

        if (Uri.CheckHostName(model.HostName) != UriHostNameType.Dns)
        {
            return BadRequest(new
            {
                error = "hostNameInvalid",
                message = "HostName is not valid"
            });
        }

        try
        {
            var currentHostIps = await CommonMethods.GetHostIpsAsync();

            var hostIps = (await Dns.GetHostAddressesAsync(model.HostName)).Select(ip => ip.ToString());

            return Ok(new
            {
                value = currentHostIps.Any(ip => hostIps.Contains(ip))
            });
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "checkdomain " + model.HostName);

            return Ok(new
            {
                value= false
            });
        }
    }

    #endregion

    #region private methods

    private async Task<(bool, int, object)> GetTenantAsync(SettingsModel model)
    {
        object error;
        var tenantId = -1;

        if (model == null)
        {
            error = new
            {
                error = "portalNameEmpty",
                message = "PortalName is required"
            };

            Log.LogError("Model is null");

            return (false, tenantId, error);
        }

        if (model.TenantId is -1)
        {
            tenantId = model.TenantId.Value;
            return (true, tenantId, null);
        }

        var (success, tenant) = await CommonMethods.TryGetTenantAsync(model);
        if (!success)
        {
            error = new
            {
                error = "portalNameEmpty",
                message = "PortalName is required"
            };

            Log.LogError("Model without tenant");

            return (false, tenantId, error);
        }

        if (tenant == null)
        {
            error = new
            {
                error = "portalNameNotFound",
                message = "Portal not found"
            };

            Log.LogError("Tenant not found");

            return (false, tenantId, error);
        }

        tenantId = tenant.Id;
        return (true, tenantId, null);
    }

    #endregion
}
