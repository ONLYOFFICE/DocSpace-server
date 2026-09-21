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

namespace ASC.AI.Migration;

[Scope]
public class LegacyAiProvidersMigrator(
    IDbContextFactory<LegacyAiDbContext> legacyDbContextFactory,
    IDbContextFactory<FilesDbContext> filesDbContextFactory,
    ProfileStorage profileStorage,
    AssignmentsStorage assignmentsStorage,
    SettingsManager settingsManager,
    InstanceCrypto crypto,
    ILogger<LegacyAiProvidersMigrator> logger)
{
    private readonly record struct ModelTarget(int ProviderId, string ModelId);

    public async Task<bool> MigrateAsync(int tenantId)
    {
        var settings = await settingsManager.LoadAsync<LegacyAiProvidersMigrationSettings>(tenantId);
        if (settings.Completed)
        {
            return false;
        }

        List<DbAiProvider> providers;
        List<DbAiModelSettings> modelSettings;
        DbDefaultAiProvider? defaultProvider;

        await using (var legacyContext = await legacyDbContextFactory.CreateDbContextAsync())
        {
            providers = await legacyContext.ProvidersAsync(tenantId).ToListAsync();
            modelSettings = await legacyContext.ModelSettingsAsync(tenantId).ToListAsync();
            defaultProvider = await legacyContext.DefaultProviderAsync(tenantId);
        }

        var providersById = providers.ToDictionary(p => p.Id);

        List<AiAgentChatBinding> agents;
        await using (var filesContext = await filesDbContextFactory.CreateDbContextAsync())
        {
            agents = await filesContext.AiAgentChatBindingsAsync(tenantId).ToListAsync();
        }

        ModelTarget? defaultTarget = null;
        if (defaultProvider != null)
        {
            if (providersById.ContainsKey(defaultProvider.ProviderId) && !string.IsNullOrEmpty(defaultProvider.DefaultModel))
            {
                defaultTarget = new ModelTarget(defaultProvider.ProviderId, defaultProvider.DefaultModel);
            }
            else
            {
                logger.WarningDefaultProviderMissing(tenantId, defaultProvider.ProviderId);
            }
        }

        var agentTargets = new Dictionary<int, ModelTarget>();
        foreach (var agent in agents)
        {
            if (agent.ChatProviderId == AiGateway.ProviderId)
            {
                continue;
            }

            if (agent.ChatProviderId > 0 && !string.IsNullOrEmpty(agent.ModelId))
            {
                if (!providersById.ContainsKey(agent.ChatProviderId))
                {
                    logger.WarningAgentProviderMissing(tenantId, agent.RoomId, agent.ChatProviderId);
                    continue;
                }

                agentTargets[agent.RoomId] = new ModelTarget(agent.ChatProviderId, agent.ModelId);
            }
            else if (defaultTarget.HasValue)
            {
                agentTargets[agent.RoomId] = defaultTarget.Value;
            }
            else
            {
                logger.WarningAgentUnbound(tenantId, agent.RoomId);
            }
        }

        var targets = agentTargets.Values.ToHashSet();
        if (defaultTarget.HasValue)
        {
            targets.Add(defaultTarget.Value);
        }

        var profileIds = await EnsureProfilesAsync(tenantId, targets, providersById, modelSettings);

        var boundAgents = 0;
        foreach (var (roomId, target) in agentTargets)
        {
            if (profileIds.TryGetValue(target, out var profileId) && await assignmentsStorage.CreateAsync(tenantId, ActionType.Chat, profileId, roomId))
            {
                boundAgents++;
            }
        }

        if (defaultTarget.HasValue && profileIds.TryGetValue(defaultTarget.Value, out var defaultProfileId))
        {
            await assignmentsStorage.CreateAsync(tenantId, ActionType.Default, defaultProfileId);
        }

        if (!await settingsManager.SaveAsync(new LegacyAiProvidersMigrationSettings { Completed = true }, tenantId))
        {
            logger.WarningFlagNotSaved(tenantId);
        }

        logger.InfoTenantMigrated(tenantId, profileIds.Count, boundAgents);

        return true;
    }

    private async Task<Dictionary<ModelTarget, Guid>> EnsureProfilesAsync(
        int tenantId,
        HashSet<ModelTarget> targets,
        Dictionary<int, DbAiProvider> providersById,
        List<DbAiModelSettings> modelSettings)
    {
        var profileIds = new Dictionary<ModelTarget, Guid>();
        if (targets.Count == 0)
        {
            return profileIds;
        }

        var existing = await profileStorage.ReadAllAsync(tenantId);
        var decryptedKeys = new Dictionary<int, string?>();
        var toCreate = new List<ProfileData>();
        var toCreateTargets = new List<ModelTarget>();

        foreach (var target in targets)
        {
            var provider = providersById[target.ProviderId];

            var providerType = LegacyProviderMapper.ToProviderType(provider.Type);
            if (providerType == null)
            {
                logger.WarningUnsupportedProviderType(tenantId, provider.Id, provider.Type);
                continue;
            }

            var match = existing.Find(p =>
                p.ProviderType == providerType
                && p.BaseUrl == provider.Url
                && string.Equals(p.ModelId, target.ModelId, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                profileIds[target] = match.Id;
                continue;
            }

            if (!decryptedKeys.TryGetValue(provider.Id, out var key))
            {
                key = await TryDecryptKeyAsync(tenantId, provider);
                decryptedKeys[provider.Id] = key;
            }

            if (key == null)
            {
                continue;
            }

            var settings = modelSettings.Find(m =>
                m.ProviderId == target.ProviderId
                && string.Equals(m.ModelId, target.ModelId, StringComparison.OrdinalIgnoreCase));

            toCreate.Add(LegacyProviderMapper.ToProfileData(provider, providerType, target.ModelId, settings, key));
            toCreateTargets.Add(target);
        }

        var created = await profileStorage.CreateManyAsync(tenantId, toCreate);
        for (var i = 0; i < created.Count; i++)
        {
            profileIds[toCreateTargets[i]] = created[i].Id;
        }

        return profileIds;
    }

    private async Task<string?> TryDecryptKeyAsync(int tenantId, DbAiProvider provider)
    {
        try
        {
            return await crypto.DecryptAsync(provider.Key);
        }
        catch (Exception e) when (e is CryptographicException or FormatException)
        {
            logger.WarningKeyDecryptFailed(tenantId, provider.Id, provider.Title, e);
            return null;
        }
    }
}
