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

using System.Text.Json;

using ASC.Core.Common;
using ASC.FederatedLogin;
using ASC.FederatedLogin.Profile;
using ASC.Files.Core.Helpers;
using ASC.Files.Core.Utils;
using ASC.Web.Api.Core;

using CsvHelper.Configuration;

namespace ASC.ApiSystem.Controllers;

[Scope]
[ApiController]
[Route("[controller]")]
public class PortalController(
        IConfiguration configuration,
        TenantManager tenantManager,
        SettingsManager settingsManager,
        ApiSystemHelper apiSystemHelper,
        CommonMethods commonMethods,
        HostedSolution hostedSolution,
        CoreSettings coreSettings,
        TenantDomainValidator tenantDomainValidator,
        UserFormatter userFormatter,
        CommonConstants commonConstants,
        ILogger<PortalController> option,
        TimeZonesProvider timeZonesProvider,
        PasswordHasher passwordHasher,
        CspSettingsHelper cspSettingsHelper,
        CoreBaseSettings coreBaseSettings,
        QuotaUsageManager quotaUsageManager,
        PasswordSettingsManager passwordSettingsManager,
        LoginProfileTransport loginProfileTransport,
        AccountLinker accountLinker,
        DocumentServiceLicense documentServiceLicense,
        CsvFileHelper csvFileHelper,
        CsvFileUploader csvFileUploader,
        ShortUrl shortUrl)
    : ControllerBase
{
    private readonly char[] _alphabetArray = Enumerable.Range('a', 26).Union(Enumerable.Range('0', 10)).Select(x => (char)x).ToArray();
    private const string DefaultPrefix = "docspace";
    private const int DefaultRandomLength = 6;

    #region For TEST api

    /// <summary>
    /// Test API.
    /// </summary>
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

    /// <summary>
    /// Registers a new portal with the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Register a portal
    /// </short>
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
            return BadRequest(new
            {
                error = "portalNameEmpty",
                message = "PortalName is required"
            });
        }

        if (!ModelState.IsValid)
        {
            List<string> message = [];

            foreach (var k in ModelState.Keys)
            {
                message.Add(ModelState[k].Errors.FirstOrDefault().ErrorMessage);
            }

            return BadRequest(new
            {
                error = "params",
                message = JsonSerializer.Serialize(message.ToArray())
            });
        }

        var sw = Stopwatch.StartNew();

        if (string.IsNullOrEmpty(model.PasswordHash))
        {
            if (!CheckPasswordPolicy(model.Password, out var error1))
            {
                sw.Stop();
                return BadRequest(error1);
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                model.PasswordHash = passwordHasher.GetClientPassword(model.Password);
            }

        }
        model.FirstName = (model.FirstName ?? "").Trim();
        model.LastName = (model.LastName ?? "").Trim();

        if (!CheckValidName(model.FirstName + model.LastName, out var error))
        {
            sw.Stop();

            return BadRequest(error);
        }

        model.PortalName = (model.PortalName ?? "").Trim();
        (var exists, error) = await CheckExistingNamePortalAsync(model.PortalName);

        if (!exists)
        {
            sw.Stop();

            return BadRequest(error);
        }

        option.LogDebug("PortalName = {0}; Elapsed ms. CheckExistingNamePortal: {1}", model.PortalName, sw.ElapsedMilliseconds);

        var clientIP = commonMethods.GetClientIp();

        if (commonMethods.CheckMuchRegistration(model, clientIP, sw))
        {
            return BadRequest(new
            {
                error = "tooMuchAttempts",
                message = "Too much attempts already"
            });
        }

        error = await GetRecaptchaError(model, clientIP, sw);

        if (error != null)
        {
            return BadRequest(error);
        }

        var language = model.Language ?? string.Empty;

        var tz = timeZonesProvider.GetCurrentTimeZoneInfo(language);

        option.LogDebug("PortalName = {0}; Elapsed ms. TimeZonesProvider.GetCurrentTimeZoneInfo: {1}", model.PortalName, sw.ElapsedMilliseconds);

        if (!string.IsNullOrEmpty(model.TimeZoneName))
        {
            tz = TimeZoneConverter.GetTimeZone(model.TimeZoneName.Trim(), false) ?? tz;

            option.LogDebug("PortalName = {0}; Elapsed ms. TimeZonesProvider.OlsonTimeZoneToTimeZoneInfo: {1}", model.PortalName, sw.ElapsedMilliseconds);
        }

        var lang = timeZonesProvider.GetCurrentCulture(language);

        option.LogDebug("PortalName = {0}; model.Language = {1}, resultLang.DisplayName = {2}", model.PortalName, language, lang.DisplayName);

        var info = new TenantRegistrationInfo
        {
            Name = configuration["web:portal-name"] ?? "",
            Address = model.PortalName,
            Culture = lang,
            FirstName = model.FirstName,
            LastName = model.LastName,
            PasswordHash = string.IsNullOrEmpty(model.PasswordHash) ? null : model.PasswordHash,
            Email = (model.Email ?? "").Trim(),
            TimeZoneInfo = tz,
            MobilePhone = string.IsNullOrEmpty(model.Phone) ? null : model.Phone.Trim(),
            Industry = (TenantIndustry)model.Industry,
            Spam = model.Spam,
            Calls = model.Calls,
            HostedRegion = model.Region,
            LimitedAccessSpace = model.LimitedAccessSpace
        };

        if (!string.IsNullOrEmpty(model.AffiliateId))
        {
            info.AffiliateId = model.AffiliateId;
        }

        if (!string.IsNullOrEmpty(model.PartnerId))
        {
            info.PartnerId = model.PartnerId;
        }

        if (!string.IsNullOrEmpty(model.Campaign))
        {
            info.Campaign = model.Campaign;
        }

        Tenant t;
        try
        {
            /****REGISTRATION!!!*****/

            t = await hostedSolution.RegisterTenantAsync(info);

            tenantManager.SetCurrentTenant(t);

            await cspSettingsHelper.SaveAsync(null);

            if (!coreBaseSettings.Standalone && apiSystemHelper.ApiCacheEnable)
            {
                t.PaymentId = await coreSettings.GetKeyAsync(t.Id);

                await apiSystemHelper.AddTenantToCacheAsync(t.GetTenantDomain(coreSettings), model.AWSRegion);

                option.LogDebug("PortalName = {0}; Elapsed ms. CacheController.AddTenantToCache: {1}", model.PortalName, sw.ElapsedMilliseconds);
            }

            /*********/

            option.LogDebug("PortalName = {0}; Elapsed ms. HostedSolution.RegisterTenant: {1}", model.PortalName, sw.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            sw.Stop();

            option.LogError(e, "");

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "registerNewTenantError",
                message = e.Message,
                stacktrace = e.StackTrace
            });
        }

        var trialQuota = configuration["quota:id"];
        if (!string.IsNullOrEmpty(trialQuota))
        {
            if (int.TryParse(trialQuota, out var trialQuotaId))
            {
                var dueDate = DateTime.MaxValue;
                if (int.TryParse(configuration["quota:due"], out var dueTrial))
                {
                    dueDate = DateTime.UtcNow.AddDays(dueTrial);
                }

                var tariff = new Tariff
                {
                    Quotas = [new Quota(trialQuotaId, 1)],
                    DueDate = dueDate
                };
                await hostedSolution.SetTariffAsync(t.Id, tariff);
            }
        }

        var isFirst = true;
        string sendCongratulationsAddress = null;

        var scheme = commonMethods.GetRequestScheme();

        if (!string.IsNullOrEmpty(model.PasswordHash))
        {
            sendCongratulationsAddress = await commonMethods.SendCongratulations(scheme, t, model.SkipWelcome);
            isFirst = sendCongratulationsAddress != null;
        }
        else if (coreBaseSettings.Standalone)
        {
            try
            {
                /* set wizard not completed*/
                tenantManager.SetCurrentTenant(t);

                var settings = await settingsManager.LoadAsync<WizardSettings>();

                settings.Completed = false;

                await settingsManager.SaveAsync(settings);
            }
            catch (Exception e)
            {
                option.LogError(e, "RegisterAsync");
            }
        }

        var reference = commonMethods.CreateReference(t.Id, scheme, t.GetTenantDomain(coreSettings), info.Email, isFirst);
        option.LogDebug("PortalName = {0}; Elapsed ms. CreateReferenceByCookie...: {1}", model.PortalName, sw.ElapsedMilliseconds);

        sw.Stop();

        return Ok(new
        {
            reference,
            tenant = commonMethods.ToTenantWrapper(t),
            referenceWelcome = sendCongratulationsAddress
        });
    }


    /// <summary>
    /// Registers a new portal by email with the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Register a portal by email
    /// </short>
    /// <path>apisystem/portal/registerbyemail</path>
    [Tags("Portal")]
    [SwaggerResponse(200, "Ok", typeof(IActionResult))]
    [HttpPost("registerbyemail")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default")]
    public async ValueTask<IActionResult> RegisterByEmailAsync(TenantModel model)
    {
        if (model == null)
        {
            return BadRequest(new
            {
                error = "params",
                message = "Model is null"
            });
        }

        if (!ModelState.IsValid)
        {
            List<string> message = [];

            foreach (var k in ModelState.Keys)
            {
                message.Add(ModelState[k].Errors.FirstOrDefault().ErrorMessage);
            }

            return BadRequest(new
            {
                error = "params",
                message = JsonSerializer.Serialize(message)
            });
        }

        LoginProfile loginProfile = null;
        if (!string.IsNullOrEmpty(model.ThirdPartyProfile))
        {
            try
            {
                var profile = await loginProfileTransport.FromPureTransport(model.ThirdPartyProfile);
                if (profile != null && string.IsNullOrEmpty(profile.AuthorizationError))
                {
                    loginProfile = profile;
                    if (!string.IsNullOrEmpty(loginProfile.EMail))
                    {
                        model.Email = loginProfile.EMail;
                    }
                    if (!string.IsNullOrEmpty(loginProfile.FirstName))
                    {
                        model.FirstName = loginProfile.FirstName;
                    }
                    if (!string.IsNullOrEmpty(loginProfile.LastName))
                    {
                        model.LastName = loginProfile.LastName;
                    }
                }
            }
            catch (Exception e)
            {
                option.LogError(e, e.Message);
            }
        }

        if (string.IsNullOrEmpty(model.Email))
        {
            return BadRequest(new
            {
                error = "emailEmpty",
                message = "Email is required"
            });
        }

        var sw = Stopwatch.StartNew();

        if (string.IsNullOrEmpty(model.PasswordHash))
        {
            if (string.IsNullOrEmpty(model.Password))
            {
                model.Password = Guid.NewGuid().ToString();
            }
            else
            {
                if (!CheckPasswordPolicy(model.Password, out var error1))
                {
                    sw.Stop();
                    return BadRequest(error1);
                }
            }

            model.PasswordHash = passwordHasher.GetClientPassword(model.Password);
        }

        model.FirstName = (model.FirstName ?? "").Trim();
        model.LastName = (model.LastName ?? "").Trim();

        var fullName = model.FirstName + model.LastName;
        object error = null;

        if (string.IsNullOrEmpty(fullName) || !CheckValidName(fullName, out error))
        {
            model.FirstName = "Administrator";
            model.LastName = "";

            if (error != null)
            {
                option.LogDebug("CheckValidName failed: {0}; Elapsed ms.: {1}", fullName, sw.ElapsedMilliseconds);
            }
        }

        var prefix = configuration["web:alias:prefix"] ?? DefaultPrefix;
        var randomLength = int.Parse(configuration["web:alias:random-length"] ?? DefaultRandomLength.ToString());

        if (prefix.Length + randomLength > tenantDomainValidator.MaxLength || prefix.Length + randomLength < tenantDomainValidator.MinLength)
        {
            prefix = DefaultPrefix;
            randomLength = DefaultRandomLength;
        }

        var random = new Random();
        random.Shuffle(_alphabetArray);

        var alphabet = new string(_alphabetArray);
        var portalName = (model.PortalName ?? $"{prefix}-{shortUrl.GenerateRandomKey(randomLength, alphabet)}").Trim();

        model.PortalName = portalName;

        while (true)
        {
            (var success, error) = await CheckExistingNamePortalAsync(model.PortalName);

            if (success)
            {
                break;
            }

            if (error.GetType().GetProperty("error")?.GetValue(error).ToString() == "portalNameExist")
            {
                model.PortalName = $"{prefix}-{shortUrl.GenerateRandomKey(randomLength, alphabet)}";
            }
            else
            {
                sw.Stop();
                return BadRequest(error);
            }
        }

        option.LogDebug("PortalName = {0}; Elapsed ms. CheckExistingNamePortal: {1}", model.PortalName, sw.ElapsedMilliseconds);

        var clientIP = commonMethods.GetClientIp();

        if (commonMethods.CheckMuchRegistration(model, clientIP, sw))
        {
            return BadRequest(new
            {
                error = "tooMuchAttempts",
                message = "Too much attempts already"
            });
        }

        var language = model.Language ?? string.Empty;

        var tz = timeZonesProvider.GetCurrentTimeZoneInfo(language);

        option.LogDebug("PortalName = {0}; Elapsed ms. TimeZonesProvider.GetCurrentTimeZoneInfo: {1}", model.PortalName, sw.ElapsedMilliseconds);

        if (!string.IsNullOrEmpty(model.TimeZoneName))
        {
            tz = TimeZoneConverter.GetTimeZone(model.TimeZoneName.Trim(), false) ?? tz;

            option.LogDebug("PortalName = {0}; Elapsed ms. TimeZonesProvider.OlsonTimeZoneToTimeZoneInfo: {1}", model.PortalName, sw.ElapsedMilliseconds);
        }

        var lang = timeZonesProvider.GetCurrentCulture(language);

        option.LogDebug("PortalName = {0}; model.Language = {1}, resultLang.DisplayName = {2}", model.PortalName, language, lang.DisplayName);

        var info = new TenantRegistrationInfo
        {
            Name = configuration["web:portal-name"] ?? "",
            Address = model.PortalName,
            Culture = lang,
            FirstName = model.FirstName,
            LastName = model.LastName,
            PasswordHash = string.IsNullOrEmpty(model.PasswordHash) ? null : model.PasswordHash,
            Email = (model.Email ?? "").Trim(),
            TimeZoneInfo = tz,
            MobilePhone = string.IsNullOrEmpty(model.Phone) ? null : model.Phone.Trim(),
            Industry = (TenantIndustry)model.Industry,
            Spam = model.Spam,
            Calls = model.Calls,
            HostedRegion = model.Region,
            LimitedAccessSpace = model.LimitedAccessSpace,
            ActivationStatus = EmployeeActivationStatus.Activated // register as activated !!!
        };

        if (!string.IsNullOrEmpty(model.AffiliateId))
        {
            info.AffiliateId = model.AffiliateId;
        }

        if (!string.IsNullOrEmpty(model.PartnerId))
        {
            info.PartnerId = model.PartnerId;
        }

        if (!string.IsNullOrEmpty(model.Campaign))
        {
            info.Campaign = model.Campaign;
        }

        Tenant t;
        try
        {
            /****REGISTRATION!!!*****/

            t = await hostedSolution.RegisterTenantAsync(info);

            tenantManager.SetCurrentTenant(t);

            await cspSettingsHelper.SaveAsync(null);

            if (!coreBaseSettings.Standalone && apiSystemHelper.ApiCacheEnable)
            {
                t.PaymentId = await coreSettings.GetKeyAsync(t.Id);

                await apiSystemHelper.AddTenantToCacheAsync(t.GetTenantDomain(coreSettings), model.AWSRegion);

                option.LogDebug("PortalName = {0}; Elapsed ms. CacheController.AddTenantToCache: {1}", model.PortalName, sw.ElapsedMilliseconds);
            }

            if (loginProfile != null)
            {
                await accountLinker.AddLinkAsync(t.OwnerId, loginProfile);
            }

            /*********/

            option.LogDebug("PortalName = {0}; Elapsed ms. HostedSolution.RegisterTenant: {1}", model.PortalName, sw.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            sw.Stop();

            option.LogError(e, "");

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "registerNewTenantError",
                message = e.Message,
                stacktrace = e.StackTrace
            });
        }

        var trialQuota = configuration["quota:id"];
        if (!string.IsNullOrEmpty(trialQuota))
        {
            if (int.TryParse(trialQuota, out var trialQuotaId))
            {
                var dueDate = DateTime.MaxValue;
                if (int.TryParse(configuration["quota:due"], out var dueTrial))
                {
                    dueDate = DateTime.UtcNow.AddDays(dueTrial);
                }

                var tariff = new Tariff
                {
                    Quotas = [new Quota(trialQuotaId, 1)],
                    DueDate = dueDate
                };
                await hostedSolution.SetTariffAsync(t.Id, tariff);
            }
        }

        var isFirst = true;
        string sendCongratulationsAddress = null;

        var scheme = commonMethods.GetRequestScheme();

        if (!string.IsNullOrEmpty(model.PasswordHash))
        {
            sendCongratulationsAddress = await commonMethods.SendCongratulations(scheme, t, model.SkipWelcome);
            isFirst = sendCongratulationsAddress != null;
        }
        else if (coreBaseSettings.Standalone)
        {
            try
            {
                /* set wizard not completed*/
                tenantManager.SetCurrentTenant(t);

                var settings = await settingsManager.LoadAsync<WizardSettings>();

                settings.Completed = false;

                await settingsManager.SaveAsync(settings);
            }
            catch (Exception e)
            {
                option.LogError(e, "RegisterAsync");
            }
        }

        var reference = commonMethods.CreateReference(t.Id, scheme, t.GetTenantDomain(coreSettings), info.Email, isFirst);
        option.LogDebug("PortalName = {0}; Elapsed ms. CreateReferenceByCookie...: {1}", model.PortalName, sw.ElapsedMilliseconds);

        sw.Stop();

        return Ok(new
        {
            reference,
            tenant = commonMethods.ToTenantWrapper(t),
            referenceWelcome = sendCongratulationsAddress
        });
    }


    /// <summary>
    /// Deletes a portal with a name specified in the request.
    /// </summary>
    /// <short>
    /// Remove a portal
    /// </short>
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
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "error",
                message = "Method for server edition only."
            });
        }

        var (succ, tenant) = await commonMethods.TryGetTenantAsync(model);
        if (!succ)
        {
            option.LogError("Model without tenant");

            return BadRequest(new
            {
                error = "portalNameEmpty",
                message = "PortalName is required"
            });
        }

        if (tenant == null)
        {
            option.LogError("Tenant not found");

            return BadRequest(new
            {
                error = "portalNameNotFound",
                message = "Portal not found"
            });
        }

        var isLastFullAccessSpace = true;

        var activeTenants = await hostedSolution.GetTenantsAsync(default);

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
            return BadRequest(new
            {
                error = "error",
                message = "The last full access space cannot be deleted."
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
            tenant = commonMethods.ToTenantWrapper(tenant),
            removed = !wizardSettings.Completed
        });
    }

    /// <summary>
    /// Changes a portal activation status with a value specified in the request.
    /// </summary>
    /// <short>
    /// Change a portal status
    /// </short>
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
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "error",
                message = "Method for server edition only."
            });
        }

        var (succ, tenant) = await commonMethods.TryGetTenantAsync(model);
        if (!succ)
        {
            option.LogError("Model without tenant");

            return BadRequest(new
            {
                error = "portalNameEmpty",
                message = "PortalName is required"
            });
        }

        if (tenant == null)
        {
            option.LogError("Tenant not found");

            return BadRequest(new
            {
                error = "portalNameNotFound",
                message = "Portal not found"
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
            tenant = commonMethods.ToTenantWrapper(tenant)
        });
    }

    /// <summary>
    /// Checks if the specified name is available to create a portal.
    /// </summary>
    /// <short>
    /// Validate the portal name
    /// </short>
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
            return BadRequest(new
            {
                error = "portalNameEmpty",
                message = "PortalName is required"
            });
        }

        var (exists, error) = await CheckExistingNamePortalAsync((model.PortalName ?? "").Trim());

        if (!exists)
        {
            return BadRequest(error);
        }

        return Ok(new
        {
            message = "portalNameReadyToRegister"
        });
    }

    /// <summary>
    /// Returns a list of all the portals registered for the user with the email address specified in the request.
    /// </summary>
    /// <short>
    /// Get portals
    /// </short>
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
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "error",
                message = "Method for server edition only."
            });
        }

        try
        {
            var tenants = (await commonMethods.GetTenantsAsync(model))
                .Distinct()
                .Where(t => t.Status == TenantStatus.Active)
                .OrderBy(t => t.Id);

            var tenantsWrapper = new List<object>();

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

                    tenantsWrapper.Add(commonMethods.ToTenantWrapper(t, quotaUsage, owner, wizardSettings));
                }
                else
                {
                    tenantsWrapper.Add(commonMethods.ToTenantWrapper(t));
                }
            }

            return Ok(new
            {
                tenants = tenantsWrapper
            });
        }
        catch (Exception ex)
        {
            option.LogError(ex, "GetPortalsAsync");

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "error",
                message = ex.Message,
                stacktrace = ex.StackTrace
            });
        }
    }

    /// <summary>
    /// Signs in to the portal with the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Sign in to the portal
    /// </short>
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
            var sw = Stopwatch.StartNew();

            var clientIP = commonMethods.GetClientIp();

            if (commonMethods.CheckMuchRegistration(model, clientIP, sw))
            {
                if (string.IsNullOrEmpty(model.RecaptchaResponse))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        error = "tooMuchAttempts",
                        message = "Too much attempts already"
                    });
                }

                var error = await GetRecaptchaError(model, clientIP, sw);

                if (error != null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, error);
                }
            }

            var tenants = await commonMethods.GetTenantsAsync(model.Email, model.PasswordHash);

            var scheme = commonMethods.GetRequestScheme();

            var tenantsWrapper = from tenant in tenants
                                 let domain = tenant.GetTenantDomain(coreSettings)
                                 select new
                                 {
                                     portalName = $"{scheme}{Uri.SchemeDelimiter}{domain}",
                                     portalLink = commonMethods.CreateReference(tenant.Id, scheme, domain, model.Email)
                                 };
            return Ok(new
            {
                tenants = tenantsWrapper
            });
        }
        catch (Exception ex)
        {
            option.LogError(ex, "SignInToPortalAsync");

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "error",
                message = ex.Message,
                stacktrace = ex.StackTrace
            });
        }
    }


    /// <summary>
    /// Returns an Document Server license quota.
    /// </summary>
    /// <short>
    /// Get an Document Server license quota
    /// </short>
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
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "error",
                message = "Method for server edition only."
            });
        }

        var (userQuota, license) = await documentServiceLicense.GetLicenseQuotaAsync(useCache);

        userQuota ??= [];

        var totalUsers = userQuota.Count;
        var portalUsers = userQuota.Where(u => Guid.TryParse(u.Key, out _)).Count();
        var externalUsers = totalUsers - portalUsers;
        var licenseTypeByUsers = license != null && license.DSConnections == 0 && license.DSUsersCount > 0;

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

    /// <summary>
    /// Generates the Document Server license quota report.
    /// </summary>
    /// <short>
    /// Generate the Document Server license quota report
    /// </short>
    /// <path>apisystem/portal/quota/licensequota/report</path>
    [Tags("Portal")]
    [SwaggerResponse(200, "URL to the xlsx report file", typeof(IActionResult))]
    [HttpPost("licensequota/report")]
    [AllowCrossSiteJson]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default,auth:portal,auth:portalbasic")]
    public async Task<IActionResult> CreateDocumentServerLicenseQuotaReport([FromQuery] bool useCache = true)
    {
        if (!coreBaseSettings.Standalone)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "error",
                message = "Method for server edition only."
            });
        }

        var reportName = string.Format(Resource.DocumentServerLicenseQuotaReportName + ".csv", DateTime.UtcNow.ToShortDateString());

        var (userQuota, _) = await documentServiceLicense.GetLicenseQuotaAsync(useCache);

        if (userQuota == null)
        {
            Ok(null);
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

    #region Validate Method

    private async Task ValidateTenantAliasAsync(string alias)
    {
        // size
        tenantDomainValidator.ValidateDomainLength(alias);
        // characters
        tenantDomainValidator.ValidateDomainCharacters(alias);

        var forbidden = await hostedSolution.IsForbiddenDomainAsync(alias);

        var sameAliasTenants = forbidden ? [alias] : await apiSystemHelper.FindTenantsInCacheAsync(alias);

        if (sameAliasTenants != null)
        {
            throw new TenantAlreadyExistsException("Address busy.", sameAliasTenants);
        }
    }

    private async ValueTask<(bool, object)> CheckExistingNamePortalAsync(string portalName)
    {
        object error;
        if (string.IsNullOrEmpty(portalName))
        {
            error = new { error = "portalNameEmpty", message = "PortalName is required" };
            return (false, error);
        }

        portalName = portalName.Trim().ToLowerInvariant();

        try
        {
            if (!coreBaseSettings.Standalone && apiSystemHelper.ApiCacheEnable)
            {
                await ValidateTenantAliasAsync(portalName);
            }
            else
            {
                await hostedSolution.CheckTenantAddressAsync(portalName);
            }
        }
        catch (TenantAlreadyExistsException ex)
        {
            error = new { error = "portalNameExist", message = "Portal already exists", variants = ex.ExistsTenants.ToArray() };
            return (false, error);
        }
        catch (TenantTooShortException)
        {
            error = new { error = "tooShortError", message = "Portal name is too short" };
            return (false, error);

        }
        catch (TenantIncorrectCharsException)
        {
            error = new { error = "portalNameIncorrect", message = "Unallowable symbols in portalName" };
            return (false, error);
        }
        catch (Exception ex)
        {
            option.LogError(ex, "CheckExistingNamePortal");
            error = new { error = "error", message = ex.Message, stacktrace = ex.StackTrace };
            return (false, error);
        }

        return (true, null);
    }

    private bool CheckValidName(string name, out object error)
    {
        error = null;
        if (string.IsNullOrEmpty(name = (name ?? "").Trim()))
        {
            error = new { error = "error", message = "name is required" };
            return false;
        }

        if (!userFormatter.IsValidUserName(name, string.Empty))
        {
            error = new { error = "error", message = "name is incorrect" };
            return false;
        }

        return true;
    }

    private bool CheckPasswordPolicy(string pwd, out object error)
    {
        error = null;
        //Validate Password match
        if (string.IsNullOrEmpty(pwd))
        {
            return true;
        }

        var passwordSettings = settingsManager.GetDefault<PasswordSettings>();

        if (!passwordSettingsManager.CheckPasswordRegex(passwordSettings, pwd))
        {
            error = new { error = "passPolicyError", message = "Password is incorrect" };
            return false;
        }

        return true;
    }


    #region Recaptcha

    private async Task<object> GetRecaptchaError(TenantModel model, string clientIP, Stopwatch sw)
    {
        if (commonConstants.RecaptchaRequired && !commonMethods.IsTestEmail(model.Email))
        {
            if (!string.IsNullOrEmpty(model.AppKey) && commonConstants.AppSecretKeys.Contains(model.AppKey))
            {
                option.LogDebug("PortalName = {0}; Elapsed ms. ValidateRecaptcha via app key: {1}. {2}", model.PortalName, model.AppKey, sw.ElapsedMilliseconds);
                return null;
            }

            var data = $"{model.PortalName} {model.FirstName} {model.LastName} {model.Email} {model.Phone} {model.RecaptchaType}";

            /*** validate recaptcha ***/
            if (!await commonMethods.ValidateRecaptcha(model.RecaptchaType, model.RecaptchaResponse, clientIP))
            {
                option.LogDebug("PortalName = {0}; Elapsed ms. ValidateRecaptcha error: {1} {2}", model.PortalName, sw.ElapsedMilliseconds, data);
                sw.Stop();

                return new { error = "recaptchaInvalid", message = "Recaptcha is invalid", clientIP };

            }

            option.LogDebug("PortalName = {0}; Elapsed ms. ValidateRecaptcha: {1} {2}", model.PortalName, sw.ElapsedMilliseconds, data);
        }

        return null;
    }

    #endregion

    #endregion
}