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

#nullable enable
using ASC.Core.Common.EF.Model.Ai;

namespace ASC.Core.Common.AI;

[Singleton]
public class AiConfiguration
{
    public int MaxImageSize { get; private set; }
    public string? RecommendedModelForForms { get; private set; }
    public static readonly FrozenSet<string> SupportedImageFormats =
        ((HashSet<string>)[".jpeg", ".jpg", ".gif", ".webp", ".png"])
        .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private readonly FrozenDictionary<ProviderType, ProviderSettingsData> _settings;
    private readonly FrozenDictionary<(ProviderType, string), ModelSettingsData> _modelsByProvider;
    private readonly FrozenDictionary<(ProviderType, string), string> _modelIdMigrations;
    private readonly FrozenDictionary<string, string> _aliasByModelId;
    private readonly FrozenDictionary<string, EffortSettingsData> _effortSettings;

    public AiConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("ai");
        var providers = section.GetSection("providers").Get<List<ProviderSettingsData>>() ?? [];
        var maxImgSize = section.GetSection("maxImageSize").Get<int>();
        var effort = section.GetSection("effort").Get<Dictionary<string, EffortSettingsData>>() ?? [];

        MaxImageSize = maxImgSize > 0 ? maxImgSize : 0;
        RecommendedModelForForms = section["recommendedModelForForms"];
        _effortSettings = effort.ToFrozenDictionary(e =>
            e.Key, e => e.Value, StringComparer.OrdinalIgnoreCase);

        _settings = providers.ToFrozenDictionary(p => p.Type);

        var modelsByProvider = new Dictionary<(ProviderType, string), ModelSettingsData>();
        var modelIdMigrations = new Dictionary<(ProviderType, string), string>();
        var aliasByModelId = new Dictionary<string, string>();

        foreach (var provider in _settings.Values)
        {
            if (provider.Models == null)
            {
                continue;
            }

            foreach (var model in provider.Models)
            {
                modelsByProvider[(provider.Type, model.Id)] = model;
                aliasByModelId.TryAdd(model.Id, model.Alias);

                if (model.Replaces == null)
                {
                    continue;
                }

                foreach (var previousId in model.Replaces)
                {
                    modelIdMigrations[(provider.Type, previousId)] = model.Id;
                }
            }
        }

        _modelsByProvider = modelsByProvider.ToFrozenDictionary();
        _modelIdMigrations = modelIdMigrations.ToFrozenDictionary();
        _aliasByModelId = aliasByModelId.ToFrozenDictionary();
    }

    public ProviderSettingsData? Get(ProviderType type)
    {
        var provider = _settings.GetValueOrDefault(type);
        return provider is { Enabled: true } ? provider : null;
    }

    public IEnumerable<ProviderSettingsData> GetAvailableProviders()
    {
        return _settings.Values.Where(x => x.Enabled);
    }

    public HashSet<string>? GetRecommendedModels(ProviderType type)
    {
        var models = _settings.GetValueOrDefault(type)?.Models;
        return models?.Select(m => m.Id).ToHashSet();
    }

    public ModelSettingsData? GetModel(ProviderType type, string modelId)
    {
        return _modelsByProvider.GetValueOrDefault((type, modelId));
    }

    public string ResolveModelId(ProviderType type, string modelId)
    {
        return _modelIdMigrations.GetValueOrDefault((type, modelId), modelId);
    }

    public IReadOnlyDictionary<string, string> GetModelAliases()
    {
        return _aliasByModelId;
    }

    public EffortSettingsData? GetEffortSettings(string effortName)
    {
        return _effortSettings.GetValueOrDefault(effortName);
    }
}
