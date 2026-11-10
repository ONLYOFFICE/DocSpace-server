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

namespace ASC.Files.Worker.Services;

/// <summary>
/// Re-reads the license in force from the license path, so a file replaced on disk or a renewal takes effect without
/// a restart.
/// </summary>
/// <remarks>
/// Nothing is applied before the license has been accepted (the setup wizard or the accept call), and a staged file
/// is never touched: putting a license in force is the user's decision, not this service's.
/// </remarks>
[Singleton]
public class RefreshLicenseService(
        IServiceScopeFactory scopeFactory,
        ILogger<RefreshLicenseService> logger,
        IConfiguration configuration)
    : ActivePassiveBackgroundService<RefreshLicenseService>(logger, scopeFactory)
{
    private readonly IServiceScopeFactory _serviceScopeFactory = scopeFactory;

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.Parse(configuration["files:refreshLicense:period"] ?? "1", CultureInfo.InvariantCulture);

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();

            var licenseReader = scope.ServiceProvider.GetRequiredService<LicenseReader>();
            if (string.IsNullOrEmpty(licenseReader.LicensePath))
            {
                return;
            }

            var settingsManager = scope.ServiceProvider.GetRequiredService<SettingsManager>();
            if (!await TariffSettings.GetLicenseAcceptAsync(settingsManager))
            {
                return;
            }

            var documentServiceLicense = scope.ServiceProvider.GetRequiredService<DocumentServiceLicense>();
            await licenseReader.RefreshActiveLicenseAsync(documentServiceLicense.ValidateLicense);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }
    }
}