// (c) Copyright Ascensio System SIA 2010-2023
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

using ASC.Web.Api.Core;

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
        TimeZoneConverter timeZoneConverter,
        PasswordHasher passwordHasher,
        CspSettingsHelper cspSettingsHelper,
        CoreBaseSettings coreBaseSettings,
        QuotaUsageManager quotaUsageManager,
        PasswordSettingsManager passwordSettingsManager)
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

    [HttpPost("register")]
    //[AllowCrossSiteJson]
    [Authorize(AuthenticationSchemes = "auth:allowskip:registerportal,auth:portal")]
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
            var message = new JArray();

            foreach (var k in ModelState.Keys)
            {
                message.Add(ModelState[k].Errors.FirstOrDefault().ErrorMessage);
            }

            return BadRequest(new
            {
                error = "params",
                message
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

        if (!CheckRecaptcha(model, clientIP, sw, out error))
        {
            return BadRequest(error);
        }

        var language = model.Language ?? string.Empty;

        var tz = timeZonesProvider.GetCurrentTimeZoneInfo(language);

        option.LogDebug("PortalName = {0}; Elapsed ms. TimeZonesProvider.GetCurrentTimeZoneInfo: {1}", model.PortalName, sw.ElapsedMilliseconds);

        if (!string.IsNullOrEmpty(model.TimeZoneName))
        {
            tz = timeZoneConverter.GetTimeZone(model.TimeZoneName.Trim(), false) ?? tz;

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

            await cspSettingsHelper.SaveAsync(null, true);

            if (!coreBaseSettings.Standalone && apiSystemHelper.ApiCacheEnable)
            { 
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
                    Quotas = [new(trialQuotaId, 1)],
                    DueDate = dueDate
                };
                await hostedSolution.SetTariffAsync(t.Id, tariff);
            }
        }

        var isFirst = true;
        string sendCongratulationsAddress = null;

        if (!string.IsNullOrEmpty(model.PasswordHash))
        {
            isFirst = !commonMethods.SendCongratulations(Request.Scheme, t, model.SkipWelcome, out sendCongratulationsAddress);
        }
        else if (configuration["core:base-domain"] == "localhost")
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

        var reference = commonMethods.CreateReference(t.Id, Request.Scheme, t.GetTenantDomain(coreSettings), info.Email, isFirst);
        option.LogDebug("PortalName = {0}; Elapsed ms. CreateReferenceByCookie...: {1}", model.PortalName, sw.ElapsedMilliseconds);

        sw.Stop();

        return Ok(new
        {
            reference,
            tenant = commonMethods.ToTenantWrapper(t),
            referenceWelcome = sendCongratulationsAddress
        });
    }

    [HttpDelete("remove")]
    [AllowCrossSiteJson]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default,auth:portal")]
    public async Task<IActionResult> RemoveAsync([FromQuery] TenantModel model)
    {
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

        await hostedSolution.RemoveTenantAsync(tenant);

        return Ok(new
        {
            tenant = commonMethods.ToTenantWrapper(tenant)
        });
    }

    [HttpPut("status")]
    [AllowCrossSiteJson]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default,auth:portal")]
    public async Task<IActionResult> ChangeStatusAsync(TenantModel model)
    {
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

    [HttpPost("validateportalname")]
    [AllowCrossSiteJson]
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

    [HttpGet("get")]
    [AllowCrossSiteJson]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default,auth:portal")]
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
                    tenantsWrapper.Add(commonMethods.ToTenantWrapper(t, quotaUsage, owner));
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

    #endregion

    #region Validate Method

    private async Task ValidateTenantAliasAsync(string alias)
    {
        // size
        tenantDomainValidator.ValidateDomainLength(alias);
        // characters
        tenantDomainValidator.ValidateDomainCharacters(alias);

        var sameAliasTenants = await apiSystemHelper.FindTenantsInCacheAsync(alias);

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

    private bool CheckRecaptcha(TenantModel model, string clientIP, Stopwatch sw, out object error)
    {
        error = null;
        if (commonConstants.RecaptchaRequired
            && !commonMethods.IsTestEmail(model.Email))
        {
            if (!string.IsNullOrEmpty(model.AppKey) && commonConstants.AppSecretKeys.Contains(model.AppKey))
            {
                option.LogDebug("PortalName = {0}; Elapsed ms. ValidateRecaptcha via app key: {1}. {2}", model.PortalName, model.AppKey, sw.ElapsedMilliseconds);
                return true;
            }

            var data = $"{model.PortalName} {model.FirstName} {model.LastName} {model.Email} {model.Phone} {model.RecaptchaType}";

            /*** validate recaptcha ***/
            if (!commonMethods.ValidateRecaptcha(model.RecaptchaResponse, model.RecaptchaType, clientIP))
            {
                option.LogDebug("PortalName = {0}; Elapsed ms. ValidateRecaptcha error: {1} {2}", model.PortalName, sw.ElapsedMilliseconds, data);
                sw.Stop();

                error = new { error = "recaptchaInvalid", message = "Recaptcha is invalid" };
                return false;

            }

            option.LogDebug("PortalName = {0}; Elapsed ms. ValidateRecaptcha: {1} {2}", model.PortalName, sw.ElapsedMilliseconds, data);
        }

        return true;
    }

    #endregion

    #endregion
}
