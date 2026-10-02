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
/// Fails the service once its RabbitMQ consumer has been gone for longer than a recreation or a connection
/// recovery takes: the pod keeps running and answering, but no event of its queue is handled any more.
/// </summary>
public class EventBusConsumerHealthCheck(IServiceProvider serviceProvider) : IHealthCheck
{
    // a broker-closed channel comes back as soon as the handler in flight returns, a lost connection
    // within the client's 5s recovery interval; a restart beats waiting for anything longer
    private static readonly TimeSpan _gracePeriod = TimeSpan.FromMinutes(2);

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (serviceProvider.GetService<IEventBus>() is not EventBusRabbitMQ eventBus)
        {
            return Task.FromResult(HealthCheckResult.Healthy("No RabbitMQ event bus"));
        }

        var downTime = eventBus.GetConsumerDownTime();

        if (downTime is null)
        {
            return Task.FromResult(HealthCheckResult.Healthy("RabbitMQ consumer is in place or not needed"));
        }

        var description = $"RabbitMQ consumer is missing for {downTime.Value.TotalSeconds:F0}s";

        return Task.FromResult(downTime < _gracePeriod
            ? HealthCheckResult.Degraded(description)
            : HealthCheckResult.Unhealthy(description));
    }
}
