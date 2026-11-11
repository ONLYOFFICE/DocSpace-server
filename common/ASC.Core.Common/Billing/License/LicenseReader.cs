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
public record DocsTrialLicense(DateTime DueDate);

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

    /// <summary>
    /// The name of the license quota of the trial an Enterprise installation runs while no license file is in force.
    /// </summary>
    public const string StartTrialQuotaName = "start_trial";

    /// <summary>
    /// Creates the license quota of the trial an Enterprise installation runs while no license file is in force: the
    /// default quota, marked as a trial.
    /// </summary>
    public static TenantQuota CreateStartTrialQuota(TenantQuota defaultQuota)
    {
        return new TenantQuota(defaultQuota) { Name = StartTrialQuotaName, Trial = true, TenantId = LicenseQuotaId };
    }

    /// <summary>
    /// Returns whether a license file is on disk, either the one in force or one uploaded and not yet accepted.
    /// </summary>
    public bool HasLicenseFile()
    {
        return !string.IsNullOrEmpty(LicensePath) && (File.Exists(LicensePath) || File.Exists(_licensePathTemp));
    }

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

        var defaultQuota = await tenantManager.GetTenantQuotaAsync(Tenant.DefaultTenant);

        var quota = new TenantQuota(LicenseQuotaId)
        {
            Name = LicenseQuotaName,
            Trial = license.Trial,
            Audit = true,
            Ldap = true,
            Sso = true,
            ThirdParty = true,
            Restore = true,
            Oauth = true,
            ContentSearch = true,
            MaxFileSize = defaultQuota.MaxFileSize,
            DocsEdition = true,
            Branding = license.Branding,
            Customization = license.Customization,
            Lifetime = !license.Trial && !license.TimeLimited,
            AutomationApi = license.AutomationApi,
            Statistic = true
        };

        await tenantManager.SaveTenantQuotaAsync(quota);

        var tariff = new Tariff
        {
            Quotas = [new Quota(quota.TenantId, 1)],
            DueDate = license.DueDate
        };

        await tariffService.SetTariffAsync(Tenant.DefaultTenant, tariff, [quota]);
    }

    /// <summary>
    /// Puts the installation's trial in force with the end date of the trial the editing service runs while neither
    /// it nor this installation has a license file, so the portal can be set up and used until that trial ends.
    /// </summary>
    /// <remarks>
    /// The license quota stays the installation's own trial quota (<see cref="StartTrialQuotaName"/>), created first
    /// when it is missing or another trial is in force; only the end date comes from the editing service. Never
    /// replaces a license put in force from a file: nothing is written while the license file is on disk or while the
    /// license quota was written from a file. A license file accepted later replaces the trial through the usual
    /// upload and accept. Repeating the call with the same trial changes nothing.
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

        // Only a quota written from a license file is kept. The other license quotas are trials DocSpace starts by
        // itself: the 30-day one of an Enterprise installation without a license, or the one activated on request
        var quota = await GetLicenseQuotaAsync();
        if (quota?.Name == LicenseQuotaName)
        {
            logger.InfoDocsTrialSkippedLicenseQuota(quota.Name);
            return false;
        }

        if (quota?.Name != StartTrialQuotaName)
        {
            quota = await tenantManager.SaveTenantQuotaAsync(CreateStartTrialQuota(await tenantManager.GetTenantQuotaAsync(Tenant.DefaultTenant)));
        }

        var tariff = new Tariff
        {
            Quotas = [new Quota(quota.TenantId, 1)],
            DueDate = trial.DueDate.Date
        };

        await tariffService.SetTariffAsync(Tenant.DefaultTenant, tariff, [quota]);

        logger.InfoDocsTrialSaved(trial.DueDate.Date);
        return true;
    }

    /// <summary>
    /// Returns whether the installation's own trial is in force rather than a license from a file.
    /// </summary>
    public async Task<bool> IsStartTrialInForceAsync()
    {
        return (await GetLicenseQuotaAsync())?.Name == StartTrialQuotaName;
    }

    private async Task<TenantQuota> GetLicenseQuotaAsync()
    {
        return (await tenantManager.GetTenantQuotasAsync(all: true)).FirstOrDefault(q => q.TenantId == LicenseQuotaId);
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
