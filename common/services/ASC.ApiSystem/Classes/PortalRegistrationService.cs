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

namespace ASC.ApiSystem.Classes;

[Scope]
public class PortalRegistrationService(
    ILogger<PortalRegistrationService> logger,
    IConfiguration configuration,
    TenantManager tenantManager,
    SettingsManager settingsManager,
    ApiSystemHelper apiSystemHelper,
    HostedSolution hostedSolution,
    CoreSettings coreSettings,
    TenantDomainValidator tenantDomainValidator,
    TimeZonesProvider timeZonesProvider,
    CspSettingsHelper cspSettingsHelper,
    CoreBaseSettings coreBaseSettings,
    ShortUrl shortUrl,
    UserFormatter userFormatter,
    PasswordSettingsManager passwordSettingsManager,
    CommonConstants commonConstants,
    CommonMethods commonMethods,
    PasswordHasher passwordHasher,
    LoginProfileTransport loginProfileTransport,
    ProviderManager providerManager,
    AccountLinker accountLinker,
    ConsumerFactory consumerFactory)
{
    private readonly char[] _alphabetArray = Enumerable.Range('a', 26).Union(Enumerable.Range('0', 10)).Select(x => (char)x).ToArray();
    internal const string DefaultPrefix = "docspace";
    private const int DefaultRandomLength = 6;
    private const int MaxRetries = 100;

    public bool CheckPasswordAndHash(TenantModel model, out PortalRegistrationErrorDto error)
    {
        error = null;
        if (string.IsNullOrEmpty(model.PasswordHash))
        {
            if (!CheckPasswordPolicy(model.Password, out error))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                model.PasswordHash = passwordHasher.GetClientPassword(model.Password);
            }
        }
        return true;
    }

    public async Task<(PortalRegistrationResponseDto Response, PortalRegistrationErrorDto Error, int StatusCode)> HandleRegisterAsync(TenantModel model)
    {
        var sw = Stopwatch.StartNew();

        if (!CheckPasswordAndHash(model, out var error))
        {
            sw.Stop();
            return (null, error, StatusCodes.Status400BadRequest);
        }

        model.FirstName = (model.FirstName ?? "").Trim();
        model.LastName = (model.LastName ?? "").Trim();

        if (!CheckValidName(model.FirstName + model.LastName, out error))
        {
            sw.Stop();
            return (null, error, StatusCodes.Status400BadRequest);
        }

        (var portalName, error) = await EnsurePortalNameAsync(model.PortalName);
        if (string.IsNullOrEmpty(portalName))
        {
            return (null, error ?? new PortalRegistrationErrorDto { Error = "portalNameEmpty", Message = "PortalName is required" }, StatusCodes.Status400BadRequest);
        }

        model.PortalName = portalName;
        logger.DebugCheckExistingNamePortal(model.PortalName, sw.ElapsedMilliseconds);

        error = await ValidateRegistrationAsync(model, sw);
        if (error != null)
        {
            return (null, error, StatusCodes.Status400BadRequest);
        }

        var (tz, lang) = GetTimeZoneAndCulture(model.Language, model.TimeZoneName);
        logger.DebugLanguageCulture(model.PortalName, model.Language, lang.DisplayName);

        var info = BuildTenantRegistrationInfo(
            model.PortalName,
            model.FirstName,
            model.LastName,
            model.Email ?? "",
            model.PasswordHash,
            model.Phone,
            model.Industry,
            model.Spam,
            model.Calls,
            model.Region,
            model.LimitedAccessSpace,
            tz,
            lang,
            model.AffiliateId,
            model.PartnerId,
            model.Campaign);

        var (t, reference, sendCongratulationsAddress, regError) = await RegisterPortalFlowAsync(info, model, sw, model.AWSRegion);
        if (regError != null)
        {
            sw.Stop();
            return (null, regError, StatusCodes.Status500InternalServerError);
        }

        sw.Stop();

        return (new PortalRegistrationResponseDto
        {
            Reference = reference,
            Tenant = await commonMethods.ToTenantResponseDto(t),
            ReferenceWelcome = sendCongratulationsAddress
        }, null, StatusCodes.Status200OK);
    }

    public async Task<(PortalRegistrationResponseDto Response, PortalRegistrationErrorDto Error, int StatusCode)> HandleRegisterByEmailAsync(TenantModel model)
    {
        var (loginProfile, autoGeneratedEmail) = await ProcessThirdPartyProfileAsync(model);

        if (string.IsNullOrWhiteSpace(model.Email))
        {
            return (null, new PortalRegistrationErrorDto { Error = "emailEmpty", Message = "Email is required" }, StatusCodes.Status400BadRequest);
        }

        var sw = Stopwatch.StartNew();

        if (string.IsNullOrEmpty(model.PasswordHash))
        {
            if (string.IsNullOrEmpty(model.Password))
            {
                model.Password = Guid.NewGuid().ToString();
            }
            if (!CheckPasswordAndHash(model, out var error1))
            {
                sw.Stop();
                return (null, error1, StatusCodes.Status400BadRequest);
            }
        }

        model.FirstName = (model.FirstName ?? "").Trim();
        model.LastName = (model.LastName ?? "").Trim();

        var fullName = model.FirstName + model.LastName;
        PortalRegistrationErrorDto error = null;

        if (string.IsNullOrEmpty(fullName) || !CheckValidName(fullName, out error))
        {
            model.FirstName = "Administrator";
            model.LastName = "";

            if (error != null)
            {
                logger.DebugCheckValidNameFailed(fullName, sw.ElapsedMilliseconds);
            }
        }

        (var portalName, error) = await GetRandomPortalNameAsync();
        if (string.IsNullOrEmpty(portalName))
        {
            return (null, error ?? new PortalRegistrationErrorDto { Error = "portalNameEmpty", Message = "PortalName is required" }, StatusCodes.Status400BadRequest);
        }

        model.PortalName = portalName;
        logger.DebugCheckExistingNamePortal(model.PortalName, sw.ElapsedMilliseconds);

        error = await ValidateRegistrationAsync(model, sw);
        if (error != null)
        {
            return (null, error, StatusCodes.Status400BadRequest);
        }

        var (tz, lang) = GetTimeZoneAndCulture(model.Language, model.TimeZoneName);
        logger.DebugLanguageCulture(model.PortalName, model.Language, lang.DisplayName);

        var activationStatus = autoGeneratedEmail ? EmployeeActivationStatus.AutoGenerated : EmployeeActivationStatus.Activated;

        var info = BuildTenantRegistrationInfo(
            model.PortalName,
            model.FirstName,
            model.LastName,
            model.Email ?? "",
            model.PasswordHash,
            model.Phone,
            model.Industry,
            model.Spam,
            model.Calls,
            model.Region,
            model.LimitedAccessSpace,
            tz,
            lang,
            model.AffiliateId,
            model.PartnerId,
            model.Campaign,
            activationStatus);

        var (t, reference, sendCongratulationsAddress, regError) = await RegisterPortalFlowAsync(info, model, sw, model.AWSRegion, loginProfile: loginProfile);
        if (regError != null)
        {
            sw.Stop();
            return (null, regError, StatusCodes.Status500InternalServerError);
        }

        sw.Stop();

        return (new PortalRegistrationResponseDto
        {
            Reference = reference,
            Tenant = await commonMethods.ToTenantResponseDto(t),
            ReferenceWelcome = sendCongratulationsAddress,
            AutoGeneratedEmail = autoGeneratedEmail
        }, null, StatusCodes.Status200OK);
    }

    public async Task<(PortalRegistrationResponseDto Response, PortalRegistrationErrorDto Error, int StatusCode)> HandleProvisionAsync(ProvisionPortalRequestDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Email))
        {
            return (null, new PortalRegistrationErrorDto { Error = "emailEmpty", Message = "Email is required" }, StatusCodes.Status400BadRequest);
        }

        model.Email = model.Email.Trim().ToLowerInvariant();

        var providerName = (model.Provider.Name ?? "").Trim().ToLowerInvariant();
        var consumer = consumerFactory.GetByKey(providerName);
        if (consumer is not ILoginProvider)
        {
            return (null, new PortalRegistrationErrorDto { Error = "unknownProvider", Message = $"Provider '{providerName}' is not supported" }, StatusCodes.Status400BadRequest);
        }

        var (portalName, nameError) = await GetRandomPortalNameAsync($"{DefaultPrefix}-{providerName}");
        if (string.IsNullOrEmpty(portalName))
        {
            return (null, nameError ?? new PortalRegistrationErrorDto { Error = "portalNameEmpty", Message = "PortalName is required" }, StatusCodes.Status400BadRequest);
        }

        var sw = Stopwatch.StartNew();
        var rateModel = new TenantModel { Email = model.Email, PortalName = portalName, RecaptchaResponse = model.RecaptchaResponse, RecaptchaType = model.RecaptchaType, AppKey = model.AppKey };

        var error = await ValidateRegistrationAsync(rateModel, sw);
        if (error != null)
        {
            return (null, error, StatusCodes.Status400BadRequest);
        }

        model.FirstName = string.IsNullOrWhiteSpace(model.FirstName) ? "Administrator" : model.FirstName.Trim();
        model.LastName = (model.LastName ?? "").Trim();

        var (tz, lang) = GetTimeZoneAndCulture(model.Language, model.TimeZoneName);
        logger.DebugLanguageCulture(portalName, model.Language, lang.DisplayName);

        var info = BuildTenantRegistrationInfo(
            portalName,
            model.FirstName,
            model.LastName,
            model.Email,
            passwordHasher.GetClientPassword(Guid.NewGuid().ToString()),
            model.Phone,
            model.Industry,
            model.Spam,
            model.Calls,
            model.Region,
            model.LimitedAccessSpace,
            tz,
            lang);

        var cspDomains = string.IsNullOrEmpty(model.Provider.CspDomain)
            ? (IEnumerable<string>)null
            : [model.Provider.CspDomain];

        var (t, reference, _, regError) = await RegisterPortalFlowAsync(info, model, sw, model.AWSRegion, cspDomains);
        if (regError != null)
        {
            return (null, regError, StatusCodes.Status500InternalServerError);
        }

        var (providerConfigured, providerConfigurationError) = await ConfigureOAuthAsync(consumer, model.Provider, portalName, sw);

        logger.DebugProvisionFinish(portalName, providerName, sw.ElapsedMilliseconds);

        sw.Stop();

        return (new PortalRegistrationResponseDto
        {
            Reference = reference,
            Tenant = await commonMethods.ToTenantResponseDto(t),
            ProviderConfigured = providerConfigured,
            ProviderConfigurationError = providerConfigurationError
        }, null, StatusCodes.Status200OK);
    }

    public bool CheckValidName(string name, out PortalRegistrationErrorDto error)
    {
        error = null;
        if (string.IsNullOrEmpty(name = (name ?? "").Trim()))
        {
            error = new PortalRegistrationErrorDto { Error = "error", Message = "name is required" };
            return false;
        }

        if (!userFormatter.IsValidUserName(name, string.Empty))
        {
            error = new PortalRegistrationErrorDto { Error = "error", Message = "name is incorrect" };
            return false;
        }

        return true;
    }

    public bool CheckPasswordPolicy(string pwd, out PortalRegistrationErrorDto error)
    {
        error = null;
        if (string.IsNullOrEmpty(pwd))
        {
            return true;
        }

        var passwordSettings = settingsManager.GetDefault<PasswordSettings>();

        if (!passwordSettingsManager.CheckPasswordRegex(passwordSettings, pwd))
        {
            error = new PortalRegistrationErrorDto { Error = "passPolicyError", Message = "Password is incorrect" };
            return false;
        }

        return true;
    }

    public async Task<PortalRegistrationErrorDto> GetRecaptchaErrorAsync(TenantModel model, string clientIp, Stopwatch sw)
    {
        if (commonConstants.RecaptchaRequired && !commonMethods.IsTestEmail(model.Email))
        {
            if (!string.IsNullOrEmpty(model.AppKey) && commonConstants.AppSecretKeys.Contains(model.AppKey))
            {
                logger.DebugRecaptchaByAppKey(model.PortalName, model.AppKey, sw.ElapsedMilliseconds);
                return null;
            }

            var data = $"{model.PortalName} {model.FirstName} {model.LastName} {model.Email} {model.Phone} {model.RecaptchaType}";

            if (!await commonMethods.ValidateRecaptcha(model.RecaptchaType, model.RecaptchaResponse, clientIp))
            {
                logger.DebugRecaptchaError(model.PortalName, sw.ElapsedMilliseconds, data);
                sw.Stop();

                return new PortalRegistrationErrorDto { Error = "recaptchaInvalid", Message = "Recaptcha is invalid", ClientIP = clientIp };
            }

            logger.DebugRecaptchaSuccess(model.PortalName, sw.ElapsedMilliseconds, data);
        }

        return null;
    }

    public async Task<PortalRegistrationErrorDto> ValidateRegistrationAsync(TenantModel model, Stopwatch sw)
    {
        var clientIp = commonMethods.GetClientIp();

        if (commonMethods.CheckMuchRegistration(model, clientIp, sw))
        {
            return new PortalRegistrationErrorDto
            {
                Error = "tooMuchAttempts",
                Message = "Too much attempts already"
            };
        }

        return await GetRecaptchaErrorAsync(model, clientIp, sw);
    }

    public async Task<(LoginProfile Profile, bool AutoGeneratedEmail)> ProcessThirdPartyProfileAsync(TenantModel model)
    {
        if (string.IsNullOrEmpty(model.ThirdPartyProfile))
        {
            return (null, false);
        }

        try
        {
            var profile = await loginProfileTransport.FromPureTransport(model.ThirdPartyProfile);
            if (profile != null && string.IsNullOrWhiteSpace(profile.AuthorizationError))
            {
                var autoGeneratedEmail = false;
                model.Email = profile.EMail;

                if (string.IsNullOrWhiteSpace(model.Email) &&
                    ProviderManager.DummyEmailProviders.Contains(profile.Provider) &&
                    providerManager.GetLoginProvider(profile.Provider) is IDummyEmailProvider provider)
                {
                    model.Email = provider.GenerateEmail(profile);
                    autoGeneratedEmail = true;
                }

                if (!string.IsNullOrEmpty(profile.FirstName))
                {
                    model.FirstName = profile.FirstName;
                }
                if (!string.IsNullOrEmpty(profile.LastName))
                {
                    model.LastName = profile.LastName;
                }

                return (profile, autoGeneratedEmail);
            }
        }
        catch (Exception e)
        {
            logger.ErrorThirdPartyProfile(e);
        }

        return (null, false);
    }

    public async Task<(string PortalName, PortalRegistrationErrorDto Error)> EnsurePortalNameAsync(string requestedPortalName, string aliasPrefix = null)
    {
        var portalName = (coreBaseSettings.Standalone ? (requestedPortalName ?? "") : string.Empty).Trim();

        if (string.IsNullOrEmpty(portalName))
        {
            return await GetRandomPortalNameAsync(aliasPrefix);
        }

        return (portalName, null);
    }

    public async Task<(string PortalName, PortalRegistrationErrorDto Error)> GetRandomPortalNameAsync(string aliasPrefix = null)
    {
        var prefix = aliasPrefix ?? configuration["web:alias:prefix"] ?? DefaultPrefix;
        var randomLength = int.Parse(configuration["web:alias:random-length"] ?? DefaultRandomLength.ToString());

        if (prefix.Length + randomLength > tenantDomainValidator.MaxLength || prefix.Length + randomLength < tenantDomainValidator.MinLength)
        {
            prefix = DefaultPrefix;
            randomLength = DefaultRandomLength;
        }

        var random = new Random();
        random.Shuffle(_alphabetArray);

        var alphabet = new string(_alphabetArray);
        var portalName = $"{prefix}-{shortUrl.GenerateRandomKey(randomLength, alphabet)}";

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var (success, error) = await CheckExistingNamePortalAsync(portalName);
            if (success)
            {
                return (portalName, null);
            }

            if (error is { Error: "portalNameExist" })
            {
                portalName = $"{prefix}-{shortUrl.GenerateRandomKey(randomLength, alphabet)}";
            }
            else
            {
                return (null, error);
            }
        }

        return (null, new PortalRegistrationErrorDto { Error = "portalNameNotAvailable", Message = "Could not generate a unique portal name" });
    }

    public async ValueTask<(bool Success, PortalRegistrationErrorDto Error)> CheckExistingNamePortalAsync(string portalName)
    {
        if (string.IsNullOrEmpty(portalName))
        {
            return (false, new PortalRegistrationErrorDto { Error = "portalNameEmpty", Message = "PortalName is required" });
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
            return (false, new PortalRegistrationErrorDto { Error = "portalNameExist", Message = "Portal already exists", Variants = ex.ExistsTenants.ToArray() });
        }
        catch (TenantTooShortException)
        {
            return (false, new PortalRegistrationErrorDto { Error = "tooShortError", Message = "Portal name is too short" });
        }
        catch (TenantIncorrectCharsException)
        {
            return (false, new PortalRegistrationErrorDto { Error = "portalNameIncorrect", Message = "Unallowable symbols in portalName" });
        }
        catch (Exception ex)
        {
            logger.ErrorCheckExistingNamePortal(ex);
            return (false, new PortalRegistrationErrorDto { Error = "error", Message = ex.Message });
        }

        return (true, null);
    }

    public (TimeZoneInfo TimeZone, CultureInfo Culture) GetTimeZoneAndCulture(string language, string timeZoneName)
    {
        var tz = timeZonesProvider.GetCurrentTimeZoneInfo(language ?? string.Empty);

        if (!string.IsNullOrEmpty(timeZoneName))
        {
            tz = TimeZoneConverter.GetTimeZone(timeZoneName.Trim(), false) ?? tz;
        }

        var lang = timeZonesProvider.GetCurrentCulture(language ?? string.Empty);

        return (tz, lang);
    }

    public TenantRegistrationInfo BuildTenantRegistrationInfo(
        string portalName,
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        string phone,
        int industry,
        bool spam,
        bool calls,
        string region,
        bool limitedAccessSpace,
        TimeZoneInfo timeZone,
        CultureInfo culture,
        string affiliateId = null,
        string partnerId = null,
        string campaign = null,
        EmployeeActivationStatus activationStatus = EmployeeActivationStatus.NotActivated)
    {
        var info = new TenantRegistrationInfo
        {
            Name = configuration["web:portal-name"] ?? "",
            Address = portalName,
            Culture = culture,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = string.IsNullOrEmpty(passwordHash) ? null : passwordHash,
            Email = email.Trim(),
            TimeZoneInfo = timeZone,
            MobilePhone = string.IsNullOrEmpty(phone) ? null : phone.Trim(),
            Industry = (TenantIndustry)industry,
            Spam = spam,
            Calls = calls,
            HostedRegion = region,
            LimitedAccessSpace = limitedAccessSpace
        };

        if (activationStatus != EmployeeActivationStatus.NotActivated)
        {
            info.ActivationStatus = activationStatus;
        }

        if (!string.IsNullOrEmpty(affiliateId))
        {
            info.AffiliateId = affiliateId;
        }

        if (!string.IsNullOrEmpty(partnerId))
        {
            info.PartnerId = partnerId;
        }

        if (!string.IsNullOrEmpty(campaign))
        {
            info.Campaign = campaign;
        }

        return info;
    }

    public async Task<(bool Success, string Error)> ConfigureOAuthAsync(Consumer consumer, ProvisionProviderDto providerModel, string portalName, Stopwatch sw)
    {
        (string Suffix, string Value)[] fieldMap =
        [
            ("clientid",     providerModel.ClientId),
            ("clientsecret", providerModel.ClientSecret),
            ("baseurl",      providerModel.BaseUrl),
            ("redirecturl",  providerModel.RedirectUri),
            ("redirecturi",  providerModel.RedirectUri),
        ];

        try
        {
            foreach (var managedKey in consumer.ManagedKeys)
            {
                var lowerKey = managedKey.ToLowerInvariant();
                foreach (var (suffix, value) in fieldMap)
                {
                    if (string.IsNullOrEmpty(value) || !lowerKey.EndsWith(suffix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    await consumer.SetAsync(managedKey, value);
                    break;
                }
            }

            logger.DebugProvisionOAuthConfigured(portalName, consumer.Name, sw.ElapsedMilliseconds);
            return (true, null);
        }
        catch (Exception e)
        {
            logger.ErrorProvisionOAuthFailed(consumer.Name, e);
            return (false, e.Message);
        }
    }

    public async Task<(Tenant Tenant, string Reference, string SendCongratulationsAddress, PortalRegistrationErrorDto Error)> RegisterPortalFlowAsync(
        TenantRegistrationInfo info,
        TenantModel model,
        Stopwatch sw,
        string awsRegion,
        IEnumerable<string> cspDomains = null,
        LoginProfile loginProfile = null)
    {
        var (t, regError) = await RegisterTenantCoreAsync(info, awsRegion, cspDomains);
        if (regError != null)
        {
            return (null, null, null, regError);
        }

        if (loginProfile != null)
        {
            await accountLinker.AddLinkAsync(t.OwnerId, loginProfile);
        }

        logger.DebugRegisterTenant(t.Alias, sw.ElapsedMilliseconds);

        await ApplyTrialQuotaAsync(t.Id);

        var isFirst = true;
        string sendCongratulationsAddress = null;
        var scheme = commonMethods.GetRequestScheme();

        if (!string.IsNullOrEmpty(model.PasswordHash))
        {
            var autoGeneratedEmail = info.ActivationStatus == EmployeeActivationStatus.AutoGenerated;
            sendCongratulationsAddress = autoGeneratedEmail ? null : await commonMethods.SendCongratulations(scheme, t, model.SkipWelcome);
            isFirst = sendCongratulationsAddress != null;
        }
        else
        {
            await ConfigureWizardForStandaloneAsync(t);
        }

        var reference = commonMethods.CreateReference(t.Id, scheme, t.GetTenantDomain(coreSettings), info.Email, isFirst);
        logger.DebugCreateReference(t.Alias, sw.ElapsedMilliseconds);

        return (t, reference, sendCongratulationsAddress, null);
    }

    public async Task<(Tenant Tenant, PortalRegistrationErrorDto Error)> RegisterTenantCoreAsync(
        TenantRegistrationInfo info,
        string awsRegion,
        IEnumerable<string> cspDomains = null)
    {
        try
        {
            var tenant = await hostedSolution.RegisterTenantAsync(info);
            tenantManager.SetCurrentTenant(tenant);

            await cspSettingsHelper.SaveAsync(cspDomains);

            if (!coreBaseSettings.Standalone && apiSystemHelper.ApiCacheEnable)
            {
                tenant.PaymentId = await coreSettings.GetKeyAsync(tenant.Id);
                await apiSystemHelper.AddTenantToCacheAsync(tenant.GetTenantDomain(coreSettings), awsRegion);
            }

            logger.InfoTenantRegistered(info.Address);

            return (tenant, null);
        }
        catch (Exception e)
        {
            logger.ErrorTenantRegistrationFailed(e);
            return (null, new PortalRegistrationErrorDto
            {
                Error = "registerNewTenantError",
                Message = e.Message
            });
        }
    }

    public async Task ApplyTrialQuotaAsync(int tenantId)
    {
        var trialQuota = configuration["quota:id"];
        if (!string.IsNullOrEmpty(trialQuota) && int.TryParse(trialQuota, out var trialQuotaId))
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
            await hostedSolution.SetTariffAsync(tenantId, tariff);
        }
    }

    public async Task ConfigureWizardForStandaloneAsync(Tenant tenant)
    {
        if (coreBaseSettings.Standalone)
        {
            try
            {
                tenantManager.SetCurrentTenant(tenant);

                var settings = await settingsManager.LoadAsync<WizardSettings>();
                settings.Completed = false;

                await settingsManager.SaveAsync(settings);
            }
            catch (Exception e)
            {
                logger.ErrorConfigureWizard(e);
            }
        }
    }

    private async Task ValidateTenantAliasAsync(string alias)
    {
        tenantDomainValidator.ValidateDomainLength(alias);
        tenantDomainValidator.ValidateDomainCharacters(alias);

        var forbidden = await hostedSolution.IsForbiddenDomainAsync(alias);
        var sameAliasTenants = forbidden ? [alias] : await apiSystemHelper.FindTenantsInCacheAsync(alias);

        if (sameAliasTenants != null)
        {
            throw new TenantAlreadyExistsException("Address busy.", sameAliasTenants);
        }
    }
}
