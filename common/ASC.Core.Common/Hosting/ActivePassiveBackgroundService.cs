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

namespace ASC.Core.Common.Hosting;
public abstract class ActivePassiveBackgroundService<T>(ILogger logger,
                                                        IServiceScopeFactory scopeFactory) : BackgroundService where T : ActivePassiveBackgroundService<T>
{
    protected abstract Task ExecuteTaskAsync(CancellationToken stoppingToken);
    protected abstract TimeSpan ExecuteTaskPeriod { get; set; }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var serviceName = GetType().Name;

        logger.DebugActivePassiveBackgroundServiceStarting(serviceName);

        stoppingToken.Register(() => logger.DebugActivePassiveBackgroundServiceStopping(serviceName));

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var serviceScope = scopeFactory.CreateAsyncScope();
           
            var registerInstanceService = serviceScope.ServiceProvider.GetService<IRegisterInstanceManager<T>>();
            var workerOptions = serviceScope.ServiceProvider.GetService<IOptions<InstanceWorkerOptions<T>>>().Value;

            if (!await registerInstanceService.IsActive())
            {
                logger.DebugActivePassiveBackgroundServiceIsNotActive(serviceName, workerOptions.InstanceId);

                await Task.Delay(1000, stoppingToken);

                continue;
            }

            await ExecuteTaskAsync(stoppingToken);

            await Task.Delay(ExecuteTaskPeriod, stoppingToken);
        }

    }
}
