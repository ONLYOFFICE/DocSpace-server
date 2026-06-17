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

namespace ASC.ApiSystem.Controllers;

[Scope]
[ApiController]
[Route("[controller]")]
public class PortalController(
        ILogger<PortalController> logger,
        SettingsManager settingsManager,
        CommonMethods commonMethods,
        HostedSolution hostedSolution,
        CoreSettings coreSettings,
        CoreBaseSettings coreBaseSettings,
        QuotaUsageManager quotaUsageManager,
        AccountLinker accountLinker,
        DocumentServiceLicense documentServiceLicense,
        CsvFileHelper csvFileHelper,
        CsvFileUploader csvFileUploader,
        PortalRegistrationService portalRegistrationService,
        UserFormatter userFormatter,
        LoginProfileTransport loginProfileTransport)
    : ControllerBase
{

    #region For TEST api

    /// <remarks>
    /// Test API.
    /// </remarks>
    /// <summary>Test API.</summary>
    /// <path>apisystem/portal/test</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [SwaggerResponse(200, "Portal api works")]
    [HttpGet("test")]
    [AllowAnonymous]
    public IActionResult Check()
    {
        return Ok(new
        {
            value = "Portal api works"
        });
    }

    #endregion

    #region API methods

    /// <remarks>
    /// Registers a new portal with the parameters specified in the request.
    /// </remarks>
    /// <summary>
    /// Register a portal
    /// </summary>
    /// <path>apisystem/portal/register</path>
    [Tags("Portal")]
    [SwaggerResponse(200, "Ok", typeof(IActionResult))]
    [HttpPost("register")]
    //[AllowCrossSiteJson]
    [Authorize(AuthenticationSchemes = "auth:allowskip:registerportal,auth:portal,auth:portalbasic")]
    public async ValueTask<IActionResult> RegisterAsync(TenantModel model)
    {
        if (model == null)
        {
            return BadRequest(new ErrorDto
            {
                Error = "params",
                Message = "Model is null"
            });
        }

        var modelError = ValidateModelState(ModelState);
        if (modelError != null)
        {
            return BadRequest(modelError);
        }

        var (response, error, statusCode) = await portalRegistrationService.HandleRegisterAsync(model);
        return error != null ? StatusCode(statusCode, error) : Ok(response);
    }


    /// <remarks>
    /// Registers a new portal by email with the parameters specified in the request.
    /// </remarks>
    /// <summary>
    /// Register a portal by email
    /// </summary>
    /// <path>apisystem/portal/registerbyemail</path>
    [Tags("Portal")]
    [SwaggerResponse(200, "Ok", typeof(IActionResult))]
    [HttpPost("registerbyemail")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default")]
    public async ValueTask<IActionResult> RegisterByEmailAsync(TenantModel model)
    {
        if (model == null)
        {
            return BadRequest(new ErrorDto
            {
                Error = "params",
                Message = "Model is null"
            });
        }

        var modelError = ValidateModelState(ModelState);
        if (modelError != null)
        {
            return BadRequest(modelError);
        }

        var (response, error, statusCode) = await portalRegistrationService.HandleRegisterByEmailAsync(model);
        return error != null ? StatusCode(statusCode, error) : Ok(response);
    }


    /// <remarks>
    /// Registers a new portal and immediately configures the specified OAuth provider, so the owner can sign in
    /// without a password. Supports any provider registered in the system.
    /// Provider keys are matched to the consumer's managed keys by conventional suffix (clientId, clientSecret,
    /// baseUrl, redirectUrl/redirectUri).
    /// </remarks>
    /// <summary>
    /// Provision a portal with an OAuth provider
    /// </summary>
    /// <path>apisystem/portal/provision</path>
    [Tags("Portal")]
    [SwaggerResponse(200, "Ok", typeof(IActionResult))]
    [HttpPost("provision")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:registerportal")]
    public async ValueTask<IActionResult> ProvisionAsync(ProvisionPortalRequestDto model)
    {
        if (model == null)
        {
            return BadRequest(new ErrorDto
            {
                Error = "params",
                Message = "Model is null"
            });
        }

        var modelError = ValidateModelState(ModelState);
        if (modelError != null)
        {
            return BadRequest(modelError);
        }

        var (response, error, statusCode) = await portalRegistrationService.HandleProvisionAsync(model);
        return error != null ? StatusCode(statusCode, error) : Ok(response);
    }

    /// <remarks>
    /// Deletes a portal with a name specified in the request.
    /// </remarks>
    /// <summary>
    /// Remove a portal
    /// </summary>
    /// <path>apisystem/portal/remove</path>
    [Tags("Portal")]
    [SwaggerResponse(200, "Ok", typeof(IActionResult))]
    [HttpDelete("remove")]
    [AllowCrossSiteJson]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default,auth:portal,auth:portalbasic")]
    public async Task<IActionResult> RemoveAsync([FromQuery] TenantModel model)
    {
        if (!coreBaseSettings.Standalone)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorDto
            {
                Error = "error",
                Message = "Method for server edition only."
            });
        }

        var (succ, tenant) = await commonMethods.TryGetTenantAsync(model);
        if (!succ)
        {
            logger.ErrorModelWithoutTenant();

            return BadRequest(new ErrorDto
            {
                Error = "portalNameEmpty",
                Message = "PortalName is required"
            });
        }

        if (tenant == null)
        {
            logger.ErrorTenantNotFound();

            return BadRequest(new ErrorDto
            {
                Error = "portalNameNotFound",
                Message = "Portal not found"
            });
        }

        var isLastFullAccessSpace = true;

        var activeTenants = await hostedSolution.GetTenantsAsync(default(DateTime));

        foreach (var t in activeTenants.Where(t => t.Id != tenant.Id))
        {
            var settings = await settingsManager.LoadAsync<TenantAccessSpaceSettings>(t.Id);
            if (!settings.LimitedAccessSpace)
            {
                isLastFullAccessSpace = false;
                break;
            }
        }

        if (isLastFullAccessSpace)
        {
            return BadRequest(new ErrorDto
            {
                Error = "error",
                Message = "The last full access space cannot be deleted."
            });
        }

        var wizardSettings = await settingsManager.LoadAsync<WizardSettings>(tenant.Id);

        if (!wizardSettings.Completed)
        {
            await hostedSolution.RemoveTenantAsync(tenant);
        }
        else
        {
            await commonMethods.SendRemoveInstructions(commonMethods.GetRequestScheme(), tenant);
        }

        return Ok(new
        {
            tenant = await commonMethods.ToTenantResponseDto(tenant),
            removed = !wizardSettings.Completed
        });
    }

    /// <remarks>
    /// Changes a portal activation status with a value specified in the request.
    /// </remarks>
    /// <summary>
    /// Change a portal status
    /// </summary>
    /// <path>apisystem/portal/status</path>
    [Tags("Portal")]
    [SwaggerResponse(200, "Ok", typeof(IActionResult))]
    [HttpPut("status")]
    [AllowCrossSiteJson]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default,auth:portal,auth:portalbasic")]
    public async Task<IActionResult> ChangeStatusAsync(TenantModel model)
    {
        if (!coreBaseSettings.Standalone)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorDto
            {
                Error = "error",
                Message = "Method for server edition only."
            });
        }

        var (succ, tenant) = await commonMethods.TryGetTenantAsync(model);
        if (!succ)
        {
            logger.ErrorModelWithoutTenant();

            return BadRequest(new ErrorDto
            {
                Error = "portalNameEmpty",
                Message = "PortalName is required"
            });
        }

        if (tenant == null)
        {
            logger.ErrorTenantNotFound();

            return BadRequest(new ErrorDto
            {
                Error = "portalNameNotFound",
                Message = "Portal not found"
            });
        }

        var active = model.Status;

        if (active != TenantStatus.Active)
        {
            active = TenantStatus.Suspended;
        }

        tenant.SetStatus(active);

        await hostedSolution.SaveTenantAsync(tenant);

        return Ok(new
        {
            tenant = await commonMethods.ToTenantResponseDto(tenant)
        });
    }

    /// <remarks>
    /// Checks if the specified name is available to create a portal.
    /// </remarks>
    /// <summary>
    /// Validate the portal name
    /// </summary>
    /// <path>apisystem/portal/validateportalname</path>
    [Tags("Portal")]
    [SwaggerResponse(200, "Ok", typeof(IActionResult))]
    [HttpPost("validateportalname")]
    [AllowCrossSiteJson]
    [AllowAnonymous]
    public async ValueTask<IActionResult> CheckExistingNamePortalAsync(TenantModel model)
    {
        if (model == null)
        {
            return BadRequest(new ErrorDto
            {
                Error = "portalNameEmpty",
                Message = "PortalName is required"
            });
        }

        var (exists, error) = await portalRegistrationService.CheckExistingNamePortalAsync((model.PortalName ?? "").Trim());

        if (!exists)
        {
            return BadRequest(error);
        }

        return Ok(new
        {
            message = "portalNameReadyToRegister"
        });
    }

    /// <remarks>
    /// Returns a list of all the portals registered for the user with the email address specified in the request.
    /// </remarks>
    /// <summary>
    /// Get portals
    /// </summary>
    /// <path>apisystem/portal/get</path>
    [Tags("Portal")]
    [SwaggerResponse(200, "Ok", typeof(IActionResult))]
    [HttpGet("get")]
    [AllowCrossSiteJson]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default,auth:portal,auth:portalbasic")]
    public async Task<IActionResult> GetPortalsAsync([FromQuery] TenantModel model, [FromQuery] bool statistics)
    {
        if (!coreBaseSettings.Standalone)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorDto
            {
                Error = "error",
                Message = "Method for server edition only."
            });
        }

        try
        {
            var tenants = (await commonMethods.GetTenantsAsync(model))
                .Distinct()
                .Where(t => t.Status == TenantStatus.Active)
                .OrderBy(t => t.Id);

            var result = new List<TenantResponseDto>();

            var owners = statistics
                ? (await hostedSolution.FindUsersAsync(tenants.Select(t => t.OwnerId))).Select(owner => new TenantOwnerDto
                {
                    Id = owner.Id,
                    Email = owner.Email,
                    DisplayName = userFormatter.GetUserName(owner)
                })
                : null;

            foreach (var t in tenants)
            {
                if (statistics)
                {
                    var quotaUsage = await quotaUsageManager.Get(t);
                    var owner = owners.FirstOrDefault(o => o.Id == t.OwnerId);
                    var wizardSettings = await settingsManager.LoadAsync<WizardSettings>(t.Id);

                    result.Add(await commonMethods.ToTenantResponseDto(t, quotaUsage, owner, wizardSettings));
                }
                else
                {
                    result.Add(await commonMethods.ToTenantResponseDto(t));
                }
            }

            return Ok(new
            {
                tenants = result
            });
        }
        catch (Exception ex)
        {
            logger.ErrorGetPortals(ex);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto
            {
                Error = "error",
                Message = ex.Message
            });
        }
    }

    /// <remarks>
    /// Signs in to the portal with the parameters specified in the request.
    /// </remarks>
    /// <summary>
    /// Sign in to the portal
    /// </summary>
    /// <path>apisystem/portal/signin</path>
    [Tags("Portal")]
    [SwaggerResponse(200, "Ok", typeof(IActionResult))]
    [HttpPost("signin")]
    [AllowCrossSiteJson]
    [AllowAnonymous]
    public async Task<IActionResult> SignInToPortalAsync(TenantModel model)
    {
        try
        {
            var clientIp = commonMethods.GetClientIp();

            if (commonMethods.CheckMuchRegistration(model, clientIp))
            {
                if (string.IsNullOrEmpty(model.RecaptchaResponse))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ErrorDto
                    {
                        Error = "tooMuchAttempts",
                        Message = "Too much attempts already"
                    });
                }

                var error = await portalRegistrationService.GetRecaptchaErrorAsync(model, clientIp);

                if (error != null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, error);
                }
            }

            if (!string.IsNullOrEmpty(model.ThirdPartyProfile))
            {
                try
                {
                    var profile = await loginProfileTransport.FromPureTransport(model.ThirdPartyProfile);
                    if (profile != null && string.IsNullOrEmpty(profile.AuthorizationError))
                    {
                        var tenantWrappersByProfile = await GetTenantsByThirdPartyProfileAsync(profile);
                        return Ok(new
                        {
                            tenants = tenantWrappersByProfile
                        });
                    }
                }
                catch (Exception e)
                {
                    logger.ErrorWithThirdPartyProfile(e);
                }
            }

            var tenants = await commonMethods.GetTenantsAsync(model.Email, model.PasswordHash);

            var scheme = commonMethods.GetRequestScheme();

            var tenantWrappers = from tenant in tenants
                let domain = tenant.GetTenantDomain(coreSettings)
                let portalName = $"{scheme}{Uri.SchemeDelimiter}{domain}"
                let portalLink = commonMethods.CreateReference(tenant.Id, scheme, domain, model.Email)
                select new TenantWrapper(portalName, portalLink);

            return Ok(new
            {
                tenants = tenantWrappers
            });
        }
        catch (Exception ex)
        {
            logger.ErrorSignInToPortal(ex);

            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorDto
            {
                Error = "error",
                Message = ex.Message
            });
        }
    }

    private record TenantWrapper(string PortalName, string PortalLink);

    private async Task<List<TenantWrapper>> GetTenantsByThirdPartyProfileAsync(LoginProfile profile)
    {
        var result = new List<TenantWrapper>();
        if (profile == null)
        {
            return result;
        }

        var linkedProfiles = await accountLinker.GetLinkedObjectsByHashIdAsync(profile.HashId);
        var userIds = new HashSet<Guid>();
        foreach (var profileId in linkedProfiles)
        {
            if (Guid.TryParse(profileId, out var userId))
            {
                userIds.Add(userId);
            }
        }

        if (!string.IsNullOrEmpty(profile.EMail))
        {
            var byEmail = await hostedSolution.FindUsersAsync(profile.EMail, EmployeeActivationStatus.Activated);
            foreach (var userInfo in byEmail)
            {
                userIds.Add(userInfo.Id);
            }
        }

        var users = (await hostedSolution.FindUsersAsync(userIds))
            .Where(u => u.Status is EmployeeStatus.Active)
            .DistinctBy(u => u.TenantId)
            .ToDictionary(k => k.TenantId, v => v);

        var tenants = await hostedSolution.GetTenantsAsync(users.Keys.ToList());

        var scheme = commonMethods.GetRequestScheme();

        foreach (var tenant in tenants)
        {
            if (!users.TryGetValue(tenant.Id, out var user))
            {
                continue;
            }
            var domain = tenant.GetTenantDomain(coreSettings);
            var portalName = $"{scheme}{Uri.SchemeDelimiter}{domain}";
            var portalLink = commonMethods.CreateReference(tenant.Id, scheme, domain, user.Email);
            result.Add(new TenantWrapper(portalName, portalLink));
        }

        return result;
    }

    /// <remarks>
    /// Returns an Document Server license quota.
    /// </remarks>
    /// <summary>
    /// Get an Document Server license quota
    /// </summary>
    /// <path>apisystem/portal/licensequota</path>
    [Tags("Portal")]
    [SwaggerResponse(200, "Ok", typeof(IActionResult))]
    [HttpGet("licensequota")]
    [AllowCrossSiteJson]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default,auth:portal,auth:portalbasic")]
    public async Task<IActionResult> GetDocumentServerLicenseQuotaAsync([FromQuery] bool useCache = true)
    {
        if (!coreBaseSettings.Standalone)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorDto
            {
                Error = "error",
                Message = "Method for server edition only."
            });
        }

        var (userQuota, license) = await documentServiceLicense.GetLicenseQuotaAsync(useCache);

        userQuota ??= [];

        var totalUsers = userQuota.Count;
        var portalUsers = userQuota.Count(u => Guid.TryParse(u.Key, out _));
        var externalUsers = totalUsers - portalUsers;
        var licenseTypeByUsers = license is { DSConnections: 0, DSUsersCount: > 0 };

        return Ok(new
        {
            userQuota,
            license,
            totalUsers,
            portalUsers,
            externalUsers,
            licenseTypeByUsers
        });
    }

    /// <remarks>
    /// Generates the Document Server license quota report.
    /// </remarks>
    /// <summary>
    /// Generate the Document Server license quota report
    /// </summary>
    /// <path>apisystem/portal/licensequota/report</path>
    [Tags("Portal")]
    [SwaggerResponse(200, "URL to the xlsx report file", typeof(IActionResult))]
    [HttpPost("licensequota/report")]
    [AllowCrossSiteJson]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default,auth:portal,auth:portalbasic")]
    public async Task<IActionResult> CreateDocumentServerLicenseQuotaReport([FromQuery] bool useCache = true)
    {
        if (!coreBaseSettings.Standalone)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorDto
            {
                Error = "error",
                Message = "Method for server edition only."
            });
        }

        var reportName = string.Format(Resource.DocumentServerLicenseQuotaReportName + ".csv", DateTime.UtcNow.ToShortDateString());

        var (userQuota, _) = await documentServiceLicense.GetLicenseQuotaAsync(useCache);

        if (userQuota == null)
        {
            return Ok(null);
        }

        var userIds = userQuota
            .Select(row => Guid.TryParse(row.Key, out var guid) ? (Guid?)guid : null)
            .Where(g => g.HasValue)
            .Select(g => g.Value);

        var users = await hostedSolution.FindUsersAsync(userIds);

        var csvRows = new List<DocumentServerLicenseQuotaRow>();

        foreach (var row in userQuota)
        {
            var user = users.FirstOrDefault(u => u.Id.ToString() == row.Key);
            if (user != null)
            {
                csvRows.Add(new DocumentServerLicenseQuotaRow(user.Id.ToString(), user.FirstName, user.LastName, user.Email, row.Value));
            }
            else
            {
                csvRows.Add(new DocumentServerLicenseQuotaRow(row.Key, null, null, null, row.Value));
            }
        }

        await using var stream = csvFileHelper.CreateFile(csvRows, new DocumentServerLicenseQuotaRowMap());

        var result = await csvFileUploader.UploadFile(stream, reportName);

        return Ok(new
        {
            result
        });
    }

    [NonAction]
    public ErrorDto ValidateModelState(ModelStateDictionary modelState)
    {
        if (!modelState.IsValid)
        {
            var messages = new List<string>();
            foreach (var k in modelState.Keys)
            {
                messages.Add(modelState[k].Errors.FirstOrDefault()?.ErrorMessage ?? "Unknown error");
            }

            return new ErrorDto { Error = "params", Message = JsonSerializer.Serialize(messages.ToArray()) };
        }

        return null;
    }

    private record DocumentServerLicenseQuotaRow(string Id, string FirstName, string LastName, string Email, DateTime Date);

    private class DocumentServerLicenseQuotaRowMap : ClassMap<DocumentServerLicenseQuotaRow>
    {
        public DocumentServerLicenseQuotaRowMap()
        {
            Map(item => item.Date).TypeConverter<CsvFileHelper.CsvDateTimeConverter>();

            Map(item => item.Id).Name(Resource.DocumentServerLicenseQuotaId);
            Map(item => item.FirstName).Name(Resource.DocumentServerLicenseQuotaFirstName);
            Map(item => item.LastName).Name(Resource.DocumentServerLicenseQuotaLastName);
            Map(item => item.Email).Name(Resource.DocumentServerLicenseQuotaEmail);
            Map(item => item.Date).Name(Resource.DocumentServerLicenseQuotaDate);
        }
    }

    #endregion
}
