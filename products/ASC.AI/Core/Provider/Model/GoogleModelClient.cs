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

using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Types;

namespace ASC.AI.Core.Provider.Model;

public class GoogleModelClient(IHttpClientFactory httpClientFactory, string apiKey) : IModelClient
{
    private readonly GenerativeModel _generativeModel =
        new GoogleAI(apiKey: apiKey, httpClientFactory: httpClientFactory).GenerativeModel();

    private static readonly HashSet<string> _restrictedKeywords = ["robotics", "image", "computer", "lyria"];

    public Task PingAsync()
    {
        return _generativeModel.ListModels(pageSize: 1);
    }

    public async Task<IEnumerable<ModelInfo>> ListModelsAsync()
    {
        var models = await _generativeModel.ListModels(pageSize: 1000);

        return models
            .Where(x => !string.IsNullOrEmpty(x.Name))
            .Where(x =>
                x.SupportedGenerationMethods != null && x.SupportedGenerationMethods.Contains(Method.GenerateContent))
            .Where(x => IsChatModel(x.Name))
            .Reverse()
            .Select(x => new ModelInfo
            {
                Id = x.Name!,
                Created = 0,
                Alias = x.DisplayName,
                Capabilities = new AiModelCapabilities
                {
                    ToolCalling = true,
                    Vision = true,
                    Thinking = x.Thinking ?? false
                }
            });
    }

    private static bool IsChatModel(string? modelId)
    {
        if (modelId == null)
        {
            return true;
        }

        return !_restrictedKeywords.Any(modelId.Contains);
    }
}
