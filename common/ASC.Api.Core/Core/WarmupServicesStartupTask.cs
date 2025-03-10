﻿// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Api.Core.Core;

/// <summary>
///     https://andrewlock.net/reducing-latency-by-pre-building-singletons-in-asp-net-core/
/// </summary>
public class WarmupServicesStartupTask(IServiceCollection services, IServiceProvider provider) : IStartupTask
{
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {      
        var processedFailed = 0;
        var processedSuccessed = 0;
        var startTime = DateTime.UtcNow;

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
                processedSuccessed++;
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

        var processed = processedSuccessed + processedFailed;

        logger.TraceWarmupFinished(processed,
            processedSuccessed,
            processedFailed,
            (DateTime.UtcNow - startTime).TotalMilliseconds);
        
        return Task.CompletedTask;
    }

    static IEnumerable<Type> GetServices(IServiceCollection services)
    {
        return services
            .Where(descriptor => descriptor.ImplementationType != typeof(WarmupServicesStartupTask))
            .Where(descriptor => !descriptor.ServiceType.ContainsGenericParameters)
            .Select(descriptor => descriptor.ServiceType)
            .Distinct();
    }
}