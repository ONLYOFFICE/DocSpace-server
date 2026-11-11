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

namespace ASC.Core.Billing;

public enum LicenseType
{
    Enterprise,
    Developer
}

[Singleton]
public class LicenseReaderConfig
{
    public readonly LicenseType LicenseType;
    public readonly string LicensePath;
    public readonly string LicensePathTemp;
    public readonly string LicensePathBcp;

    public LicenseReaderConfig(IConfiguration configuration)
    {
        LicensePath = configuration["license:file:path"] ?? "";
        LicensePathTemp = LicensePath + ".tmp";
        LicensePathBcp = LicensePath + ".bcp";

        _ = Enum.TryParse(configuration["license:type"], true, out LicenseType);
    }
}

public record LicenseValidationResult(bool IsValid, string ErrorMessage);

/// <summary>
/// The trial the editing service runs while it has no license file, as it reports it in its answer to the license
/// command.
/// </summary>
/// <param name="DueDate">The UTC day the trial ends; only the date part is kept.</param>
/// <param name="Branding">Whether the trial allows branding.</param>
/// <param name="Customization">Whether the trial allows customization.</param>
/// <param name="AutomationApi">Whether the trial allows the Automation API.</param>
public record DocsTrialLicense(DateTime DueDate, bool Branding, bool Customization, bool AutomationApi);

[Scope]
public class LicenseReader(
    TenantManager tenantManager,
    ITariffService tariffService,
    CoreSettings coreSettings,
    LicenseReaderConfig licenseReaderConfig,
    ILogger<LicenseReader> logger)
{
    public readonly string LicensePath = licenseReaderConfig.LicensePath;
    private readonly string _licensePathTemp = licenseReaderConfig.LicensePathTemp;
    private readonly string _licensePathBcp = licenseReaderConfig.LicensePathBcp;
    private readonly LicenseType _licenseType = licenseReaderConfig.LicenseType;

    public const string CustomerIdKey = "CustomerId";

    /// <summary>The id of the quota that carries the license of a self-hosted Enterprise installation.</summary>
    public const int LicenseQuotaId = -1000;

    /// <summary>The name of the license quota written from a license file.</summary>
    public const string LicenseQuotaName = "license";

    /// <summary>The name of the license quota written from the trial the editing service runs without a license file.</summary>
    public const string DocsTrialQuotaName = "docstrial";

    public async Task SetCustomerIdAsync(string value)
    {
        await coreSettings.SaveSettingAsync(CustomerIdKey, value);
    }

    private FileStream GetLicenseStream(bool temp = false)
    {
        var path = temp ? _licensePathTemp : LicensePath;
        if (!File.Exists(path))
        {
            throw new BillingNotFoundException("License not found");
        }

        return File.OpenRead(path);
    }

    public async Task RejectLicenseAsync()
    {
        if (File.Exists(_licensePathTemp))
        {
            File.Delete(_licensePathTemp);
        }

        if (File.Exists(LicensePath))
        {
            File.Delete(LicensePath);
        }

        await tariffService.DeleteDefaultBillingInfoAsync();
    }

    /// <summary>
    /// Applies the license: a staged file (uploaded but not yet in force) replaces the active one first, otherwise
    /// the active file is re-read. For the calls made on behalf of the user who accepts the license.
    /// </summary>
    public Task RefreshLicenseAsync(Func<License, Task<LicenseValidationResult>> validateFunc)
    {
        return RefreshLicenseAsync(validateFunc, applyStaged: true);
    }

    /// <summary>
    /// Re-reads only the active license file and leaves a staged one where it is. For the background refresh: a
    /// staged file is put in force only by the user accepting it.
    /// </summary>
    public Task RefreshActiveLicenseAsync(Func<License, Task<LicenseValidationResult>> validateFunc)
    {
        return RefreshLicenseAsync(validateFunc, applyStaged: false);
    }

    private async Task RefreshLicenseAsync(Func<License, Task<LicenseValidationResult>> validateFunc, bool applyStaged)
    {
        if (string.IsNullOrEmpty(LicensePath))
        {
            throw new BillingNotFoundException("Empty license path");
        }

        var temp = applyStaged && File.Exists(_licensePathTemp);
        var bcp = temp && File.Exists(LicensePath);

        try
        {
            await using (var licenseStream = GetLicenseStream(temp))
            using (var reader = new StreamReader(licenseStream))
            {
                var licenseJsonString = await reader.ReadToEndAsync();
                var license = License.Parse(licenseJsonString);

                if (bcp)
                {
                    File.Move(LicensePath, _licensePathBcp, true);
                }

                if (temp)
                {
                    await SaveLicenseAsync(licenseStream, LicensePath);
                }

                var validationResult = await validateFunc(license);
                if (!validationResult.IsValid)
                {
                    throw new BillingNotConfiguredException($"License validation failed: {validationResult.ErrorMessage}");
                }

                await LicenseToDBAsync(license);
            }

            if (temp)
            {
                File.Delete(_licensePathTemp);
            }

            if (bcp)
            {
                File.Delete(_licensePathBcp);
            }
        }
        catch (BillingException ex) when (ex is BillingNotConfiguredException or BillingLicenseTypeException)
        {
            if (bcp)
            {
                File.Move(_licensePathBcp, LicensePath, true);
            }
            else if (temp)
            {
                File.Delete(LicensePath);
            }

            LogError(ex);
            throw;
        }
        catch (Exception ex)
        {
            LogError(ex);
            throw;
        }
    }

    public async Task<DateTime> SaveLicenseTemp(Stream licenseStream)
    {
        if (string.IsNullOrEmpty(LicensePath))
        {
            throw new BillingNotFoundException("Empty license path");
        }

        try
        {
            using var reader = new StreamReader(licenseStream);
            var licenseJsonString = await reader.ReadToEndAsync();
            var license = License.Parse(licenseJsonString);

            var dueDate = Validate(license);

            await SaveLicenseAsync(licenseStream, _licensePathTemp);

            return dueDate;
        }
        catch (Exception ex)
        {
            LogError(ex);

            throw;
        }
    }

    private static async Task SaveLicenseAsync(Stream licenseStream, string path)
    {
        ArgumentNullException.ThrowIfNull(licenseStream);

        if (licenseStream.CanSeek)
        {
            licenseStream.Seek(0, SeekOrigin.Begin);
        }

        await using var fs = File.Open(path, FileMode.Create);

        await licenseStream.CopyToAsync(fs);
    }

    private DateTime Validate(License license)
    {
        if (string.IsNullOrEmpty(license.CustomerId)
            || string.IsNullOrEmpty(license.Signature))
        {
            throw new BillingNotConfiguredException("License file is not correct");
        }

        if (license.StartDate.Date > DateTime.UtcNow.Date)
        {
            throw new ArgumentOutOfRangeException(nameof(license.StartDate));
        }

        // The edition check (docspace_dev flag against license:type) is disabled: Docs Developer Edition licenses
        // are issued without docspace_dev, so DocSpace could not be added to an existing Docs DE installation
        // even though Docs itself accepts the same license file (Bug 84212).
        //var invalidLicenseType = _licenseType == LicenseType.Enterprise ? license.Developer : !license.Developer;
        //if (invalidLicenseType)
        //{
        //    throw new BillingLicenseTypeException("License type is not correct");
        //}

        return license.DueDate.Date;
    }

    private async Task LicenseToDBAsync(License license)
    {
        Validate(license);

        await SetCustomerIdAsync(license.CustomerId);

        await SaveLicenseQuotaAsync(
            LicenseQuotaName,
            license.DueDate,
            trial: license.Trial,
            lifetime: !license.Trial && !license.TimeLimited,
            license.Branding,
            license.Customization,
            license.AutomationApi);
    }

    /// <summary>
    /// Writes the license quota and the tariff from the trial the editing service runs while neither it nor this
    /// installation has a license file, so the portal can be set up and used until the trial ends.
    /// </summary>
    /// <remarks>
    /// Never replaces a license put in force from a file: nothing is written while the license file is on disk or
    /// while the license quota was written by anything else than this method. A license file accepted later replaces
    /// the trial through the usual upload and accept. Repeating the call with the same trial changes nothing.
    /// </remarks>
    /// <returns><c>true</c> when the trial was written, <c>false</c> when it was skipped.</returns>
    public async Task<bool> DocsTrialToDBAsync(DocsTrialLicense trial)
    {
        ArgumentNullException.ThrowIfNull(trial);

        if (string.IsNullOrEmpty(LicensePath))
        {
            return false;
        }

        if (File.Exists(LicensePath))
        {
            logger.InfoDocsTrialSkippedLicenseFile();
            return false;
        }

        var licenseQuota = (await tenantManager.GetTenantQuotasAsync(all: true)).FirstOrDefault(q => q.TenantId == LicenseQuotaId);
        if (licenseQuota != null && licenseQuota.Name != DocsTrialQuotaName)
        {
            logger.InfoDocsTrialSkippedLicenseQuota(licenseQuota.Name);
            return false;
        }

        await SaveLicenseQuotaAsync(
            DocsTrialQuotaName,
            trial.DueDate.Date,
            trial: true,
            lifetime: false,
            trial.Branding,
            trial.Customization,
            trial.AutomationApi);

        logger.InfoDocsTrialSaved(trial.DueDate.Date);
        return true;
    }

    /// <summary>
    /// Returns whether the license quota in force was written from the editing service's trial rather than from a
    /// license file.
    /// </summary>
    public async Task<bool> IsDocsTrialInForceAsync()
    {
        var licenseQuota = (await tenantManager.GetTenantQuotasAsync(all: true)).FirstOrDefault(q => q.TenantId == LicenseQuotaId);
        return licenseQuota?.Name == DocsTrialQuotaName;
    }

    private async Task SaveLicenseQuotaAsync(string name, DateTime dueDate, bool trial, bool lifetime, bool branding, bool customization, bool automationApi)
    {
        var defaultQuota = await tenantManager.GetTenantQuotaAsync(Tenant.DefaultTenant);

        var quota = new TenantQuota(LicenseQuotaId)
        {
            Name = name,
            Trial = trial,
            Audit = true,
            Ldap = true,
            Sso = true,
            ThirdParty = true,
            Restore = true,
            Oauth = true,
            ContentSearch = true,
            MaxFileSize = defaultQuota.MaxFileSize,
            DocsEdition = true,
            Branding = branding,
            Customization = customization,
            Lifetime = lifetime,
            AutomationApi = automationApi,
            Statistic = true
        };

        await tenantManager.SaveTenantQuotaAsync(quota);

        var tariff = new Tariff
        {
            Quotas = [new Quota(quota.TenantId, 1)],
            DueDate = dueDate
        };

        await tariffService.SetTariffAsync(Tenant.DefaultTenant, tariff, [quota]);
    }

    private void LogError(Exception error)
    {
        if (error is BillingNotFoundException)
        {
            logger.DebugLicenseNotFound(error.Message);
        }
        else
        {
            logger.ErrorWithException(error);
        }
    }
}
