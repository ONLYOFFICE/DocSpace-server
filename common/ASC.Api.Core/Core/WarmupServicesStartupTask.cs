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

namespace ASC.Api.Core.Core;

/// <summary>
///     https://andrewlock.net/reducing-latency-by-pre-building-singletons-in-asp-net-core/
/// </summary>
public class WarmupServicesStartupTask(IServiceCollection services, IServiceProvider provider) : IStartupTask
{
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var processedFailed = 0;
        var processedSucceeded = 0;
        var startTime = TimeProvider.System.GetTimestamp();

        using var scope = provider.CreateScope();
        var logger = scope.ServiceProvider.GetService<ILogger<WarmupServicesStartupTask>>();
        logger.TraceWarmupStarted();

        foreach (var service in GetServices(services))
        {
            try
            {
                var timestamp = TimeProvider.System.GetTimestamp();
                scope.ServiceProvider.GetService(service);
                var totalMilliseconds = TimeProvider.System.GetElapsedTime(timestamp).TotalMilliseconds;
                if (totalMilliseconds > 10)
                {
                    logger.TraceWarmupTime(service.FullName, totalMilliseconds);
                }
                processedSucceeded++;
            }
            catch (Exception ex)
            {
                processedFailed++;

                if (ex.Message != TenantManager.CouldNotResolveCurrentTenant)
                {
                    logger.DebugWarmupFailed(processedFailed, service.FullName, ex);
                }
            }
        }

        var processed = processedSucceeded + processedFailed;

        logger.TraceWarmupFinished(processed,
            processedSucceeded,
            processedFailed,
            TimeProvider.System.GetElapsedTime(startTime).TotalMilliseconds);

        return Task.CompletedTask;
    }

    private static IEnumerable<Type> GetServices(IServiceCollection services)
    {
        return services
            .Where(descriptor => descriptor.Lifetime == ServiceLifetime.Singleton)
            .Where(descriptor => descriptor.ImplementationType != typeof(WarmupServicesStartupTask))
            .Where(descriptor => !descriptor.ServiceType.ContainsGenericParameters)
            .Select(descriptor => descriptor.ServiceType)
            .Distinct();
    }
}