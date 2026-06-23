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

namespace ASC.Core.Common.Hosting;

#nullable enable

public class RegisterInstanceManager<T>(
    IRegisterInstanceDao<T> registerInstanceRepository,
    IOptions<InstanceWorkerOptions<T>> optionsSettings) : IRegisterInstanceManager<T> where T : IHostedService
{
    private readonly InstanceWorkerOptions<T> _options = optionsSettings.Value;

    public async Task Register()
    {
        if (_options.SingletonMode)
        {
            return;
        }

        var workerTypeName = _options.WorkerTypeName;

        var instances = await registerInstanceRepository.GetAllAsync(workerTypeName);
        var registeredInstance = instances.FirstOrDefault(x => x.InstanceRegistrationId == _options.InstanceId);
        var instance = registeredInstance ?? new InstanceRegistration
        {
            InstanceRegistrationId = _options.InstanceId,
            WorkerTypeName = workerTypeName
        };

        instance.LastUpdated = DateTime.UtcNow;

        if (instances.Count != 0 && !instances.Any(x => x.IsActive))
        {
            var firstAliceInstance = FirstAliveInstance(instances);

            if (firstAliceInstance == null || firstAliceInstance.InstanceRegistrationId == instance.InstanceRegistrationId)
            {
                instance.IsActive = true;
            }
        }

        await registerInstanceRepository.AddOrUpdateAsync(instance);

        var oldRegistrations = instances.Where(IsOrphanInstance).ToList();

        foreach (var instanceRegistration in oldRegistrations)
        {
            await registerInstanceRepository.DeleteAsync(instanceRegistration.InstanceRegistrationId);
        }
    }

    public async Task UnRegister()
    {
        await registerInstanceRepository.DeleteAsync(_options.InstanceId);
    }

    public async Task<bool> IsActive()
    {
        if (_options.SingletonMode)
        {
            return true;
        }

        var workerTypeName = _options.WorkerTypeName;
        var instances = await registerInstanceRepository.GetAllAsync(workerTypeName);
        var instance = instances.FirstOrDefault(x => x.InstanceRegistrationId == _options.InstanceId);

        return instance is not null && instance.IsActive;
    }

    private InstanceRegistration? FirstAliveInstance(IEnumerable<InstanceRegistration> instances)
    {
        Func<InstanceRegistration, long> getTicksCreationService = x => Convert.ToInt64(x.InstanceRegistrationId.Split('_').Last());

        return instances.Where(x => !IsOrphanInstance(x)).MinBy(getTicksCreationService);
    }

    private bool IsOrphanInstance(InstanceRegistration obj)
    {
        return obj.LastUpdated.HasValue && obj.LastUpdated.Value.AddSeconds(_options.TimeUntilUnregisterInSeconds) < DateTime.UtcNow;
    }
}