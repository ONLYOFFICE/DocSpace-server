// (c) Copyright Ascensio System SIA 2009-2026
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

#nullable enable
using ASC.Core.Common.EF.Model.Ai;

namespace ASC.Core.Common.AI;

[Singleton]
public class ProviderSettings
{
    private readonly FrozenDictionary<ProviderType, ProviderSettingsData> _settings;
    private readonly FrozenDictionary<(ProviderType, string), ModelSettingsData> _modelsByProvider;
    private readonly FrozenDictionary<string, MultimodalSettings> _multimodalByModelId;

    public ProviderSettings(IConfiguration configuration, CoreBaseSettings coreBaseSettings)
    {
        var section = configuration.GetSection("ai:providers");
        var providers = section.Get<List<ProviderSettingsData>>() ?? [];

        _settings = coreBaseSettings.Standalone
            ? providers.ToFrozenDictionary(p => p.Type)
            : providers.Where(p => p.Type != ProviderType.OpenAiCompatible)
                .ToFrozenDictionary(p => p.Type);

        var modelsByProvider = new Dictionary<(ProviderType, string), ModelSettingsData>();
        var multimodalByModelId = new Dictionary<string, MultimodalSettings>();

        foreach (var provider in _settings.Values)
        {
            if (provider.Models == null)
            {
                continue;
            }

            foreach (var model in provider.Models)
            {
                modelsByProvider[(provider.Type, model.Id)] = model;

                if (model.Multimodal != null)
                {
                    multimodalByModelId.TryAdd(model.Id, model.Multimodal);
                }
            }
        }

        _modelsByProvider = modelsByProvider.ToFrozenDictionary();
        _multimodalByModelId = multimodalByModelId.ToFrozenDictionary();
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

    public HashSet<string>? GetSupportedModels(ProviderType type)
    {
        var models = _settings.GetValueOrDefault(type)?.Models;
        return models?.Select(m => m.Id).ToHashSet();
    }

    public ModelSettingsData? GetModel(ProviderType type, string modelId)
    {
        return _modelsByProvider.GetValueOrDefault((type, modelId));
    }

    public MultimodalSettings? GetMultimodalSettings(string modelId)
    {
        return _multimodalByModelId.GetValueOrDefault(modelId);
    }
}
