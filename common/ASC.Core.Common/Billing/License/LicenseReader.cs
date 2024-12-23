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

    public async Task RefreshLicenseAsync(Func<Task<bool>> validateFunc)
    {
        if (string.IsNullOrEmpty(LicensePath))
        {
            throw new BillingNotFoundException("Empty license path");
        }

        var temp = File.Exists(_licensePathTemp);
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
                    await Task.Delay(100);
                }

                if (!await validateFunc())
                {
                    throw new BillingNotConfiguredException("License not correct");
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
        catch (BillingNotConfiguredException ex)
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
        var invalidLicenseType = _licenseType == LicenseType.Enterprise ? license.Developer : !license.Developer;

        if (string.IsNullOrEmpty(license.CustomerId)
            || string.IsNullOrEmpty(license.Signature)
            || invalidLicenseType)
        {
            throw new BillingNotConfiguredException("License not correct", license.OriginalLicense);
        }

        return license.DueDate.Date;
    }

    private async Task LicenseToDBAsync(License license)
    {
        Validate(license);

        await SetCustomerIdAsync(license.CustomerId);

        var defaultQuota = await tenantManager.GetTenantQuotaAsync(Tenant.DefaultTenant);

        var quota = new TenantQuota(-1000)
        {
            Name = "license",
            Trial = license.Trial,
            Audit = true,
            Ldap = true,
            Sso = true,
            ThirdParty = true,
            AutoBackupRestore = true,
            Oauth = true,
            ContentSearch = true,
            MaxFileSize = defaultQuota.MaxFileSize,
            DocsEdition = true,
            Branding = license.Branding,
            Customization = license.Customization,
            Lifetime = !license.TimeLimited,
            Statistic = true
        };

        await tenantManager.SaveTenantQuotaAsync(quota);

        var tariff = new Tariff
        {
            Quotas = [new(quota.TenantId, 1)],
            DueDate = license.DueDate
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
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.ErrorWithException(error);
            }
            else
            {
                logger.ErrorWithException(error);
            }
        }
    }
}
