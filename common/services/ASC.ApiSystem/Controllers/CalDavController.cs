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
[ApiExplorerSettings(IgnoreApi = true)]
public class CalDavController(CommonMethods commonMethods,
        EmailValidationKeyProvider emailValidationKeyProvider,
        CoreSettings coreSettings,
        CommonConstants commonConstants,
        InstanceCrypto instanceCrypto,
        ILogger<CalDavController> logger,
        IHttpClientFactory httpClientFactory)
    : ControllerBase
{
    #region For TEST api

    /// <summary>
    /// Test api
    /// </summary>
    /// <path>apisystem/caldav/test</path>
    [SwaggerResponse(200, "CalDav api works")]
    [HttpGet("test")]
    public IActionResult Check()
    {
        return Ok(new
        {
            value = "CalDav api works"
        });
    }

    #endregion

    #region API methods

    /// <summary>
    /// Changes to storage
    /// </summary>
    /// <path>apisystem/caldav/change_to_storage</path>
    [Tags("CalDav")]
    [SwaggerResponse(200, "Ok", typeof(IActionResult))]
    [HttpGet("change_to_storage")]
    public async Task<IActionResult> СhangeOfCalendarStorageAsync(string change)
    {
        var (succ, tenant, error) = await GetTenantAsync(change);
        if (!succ)
        {
            return BadRequest(error);
        }

        try
        {
            var scheme = commonMethods.GetRequestScheme();
            var validationKey = emailValidationKeyProvider.GetEmailKey(tenant.Id, change + ConfirmType.Auth);

            await SendToApi(scheme, tenant, "calendar/change_to_storage", new Dictionary<string, string> { { "change", change }, { "key", validationKey } });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error change_to_storage");

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "apiError",
                message = ex.Message
            });
        }

        return Ok();
    }

    /// <summary>
    /// Delete
    /// </summary>
    /// <path>apisystem/caldav/caldav_delete_event</path>
    [Tags("CalDav")]
    [SwaggerResponse(200, "Ok", typeof(IActionResult))]
    [HttpGet("caldav_delete_event")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default")]
    public async Task<IActionResult> CaldavDeleteEventAsync(string eventInfo)
    {
        var (succ, tenant, error) = await GetTenantAsync(eventInfo);
        if (!succ)
        {
            return BadRequest(error);
        }

        try
        {
            var scheme = commonMethods.GetRequestScheme();
            var validationKey = emailValidationKeyProvider.GetEmailKey(tenant.Id, eventInfo + ConfirmType.Auth);

            await SendToApi(scheme, tenant, "calendar/caldav_delete_event", new Dictionary<string, string> { { "eventInfo", eventInfo }, { "key", validationKey } });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error caldav_delete_event");

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "apiError",
                message = ex.Message
            });
        }

        return Ok();
    }

    /// <summary>
    /// Caldav authenticated
    /// </summary>
    /// <path>apisystem/caldav/is_caldav_authenticated</path>
    [Tags("CalDav")]
    [SwaggerResponse(200, "Ok", typeof(IActionResult))]
    [HttpPost("is_caldav_authenticated")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default")]
    public async Task<IActionResult> IsCaldavAuthenticatedAsync(UserPassword userPassword)
    {
        if (userPassword == null || string.IsNullOrEmpty(userPassword.User) || string.IsNullOrEmpty(userPassword.Password))
        {
            logger.LogError("CalDav authenticated data is null");

            return BadRequest(new
            {
                value = "false",
                error = "portalNameEmpty",
                message = "Argument is required"
            });
        }

        var (succ, email, tenant, error) = await GetUserDataAsync(userPassword.User);
        if (!succ)
        {
            return BadRequest(error);
        }

        try
        {
            logger.LogInformation(string.Format("Caldav auth user: {0}, tenant: {1}", email, tenant.Id));

            if (await instanceCrypto.EncryptAsync(email) == userPassword.Password)
            {
                return Ok(new
                {
                    value = "true"
                });
            }

            var validationKey = emailValidationKeyProvider.GetEmailKey(tenant.Id, email + userPassword.Password + ConfirmType.Auth);

            var authData = $"userName={HttpUtility.UrlEncode(email)}&password={HttpUtility.UrlEncode(userPassword.Password)}&key={HttpUtility.UrlEncode(validationKey)}";

            var scheme = commonMethods.GetRequestScheme();

            await SendToApi(scheme, tenant, "authentication/login", null, WebRequestMethods.Http.Post, authData);

            return Ok(new
            {
                value = "true"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Caldav authenticated");

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                value = "false",
                message = ex.Message
            });
        }
    }

    #endregion

    #region private methods

    private async Task<(bool, Tenant, object)> GetTenantAsync(string calendarParam)
    {
        object error;

        if (string.IsNullOrEmpty(calendarParam))
        {
            logger.LogError("calendarParam is empty");

            error = new
            {
                value = "false",
                error = "portalNameEmpty",
                message = "Argument is required"
            };

            return (false, null, error);
        }

        logger.LogInformation($"CalDav calendarParam: {calendarParam}");

        var userParam = calendarParam.Split('/')[0];
        (var succ, _, var tenant, error) = await GetUserDataAsync(userParam);

        return (succ, tenant, error);
    }

    private async Task<(bool, string, Tenant, object)> GetUserDataAsync(string userParam)
    {
        object error;

        if (string.IsNullOrEmpty(userParam))
        {
            logger.LogError("userParam is empty");

            error = new
            {
                value = "false",
                error = "portalNameEmpty",
                message = "Argument is required"
            };

            return (false, null, null, error);
        }

        var userData = userParam.Split('@');

        if (userData.Length < 3)
        {
            logger.LogError($"Error Caldav username: {userParam}");

            error = new
            {
                value = "false",
                error = "portalNameEmpty",
                message = "PortalName is required"
            };

            return (false, null, null, error);
        }

        var email = string.Join("@", userData[0], userData[1]);

        var tenantName = userData[2];

        var baseUrl = coreSettings.BaseDomain;

        if (!string.IsNullOrEmpty(baseUrl) && tenantName.EndsWith("." + baseUrl, StringComparison.InvariantCultureIgnoreCase))
        {
            tenantName = tenantName.Replace("." + baseUrl, "");
        }

        logger.LogInformation($"CalDav: user:{userParam} tenantName:{tenantName}");

        var tenantModel = new TenantModel { PortalName = tenantName };

        var (succ, tenant) = await commonMethods.TryGetTenantAsync(tenantModel);
        if (!succ)
        {
            logger.LogError("Model without tenant");

            error = new
            {
                value = "false",
                error = "portalNameEmpty",
                message = "PortalName is required"
            };

            return (false, email, tenant, error);
        }

        if (tenant == null)
        {
            logger.LogError("Tenant not found " + tenantName);

            error = new
            {
                value = "false",
                error = "portalNameNotFound",
                message = "Portal not found"
            };

            return (false, email, null, error);
        }

        return (true, email, tenant, null);
    }

    private async Task SendToApi(
        string requestUriScheme,
        Tenant tenant,
        string path,
        IEnumerable<KeyValuePair<string, string>> args = null,
        string httpMethod = WebRequestMethods.Http.Get,
        string data = null)
    {
        var query = args == null
                        ? null
                        : string.Join("&", args.Select(arg => HttpUtility.UrlEncode(arg.Key) + "=" + HttpUtility.UrlEncode(arg.Value)).ToArray());

        var url = $"{requestUriScheme}{Uri.SchemeDelimiter}{tenant.GetTenantDomain(coreSettings)}{commonConstants.WebApiBaseUrl}{path}{(string.IsNullOrEmpty(query) ? "" : "?" + query)}";

        logger.LogInformation($"CalDav: SendToApi: {url}");

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(url),
            Method = new HttpMethod(httpMethod)
        };
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

        var httpClient = httpClientFactory.CreateClient();

        if (data != null)
        {
            request.Content = new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded");
        }

        await httpClient.SendAsync(request);
    }

    #endregion

    public class UserPassword
    {
        public string User { get; set; }
        public string Password { get; set; }
    }
}
