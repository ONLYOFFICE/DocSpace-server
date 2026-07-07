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

namespace ASC.AI.Core.Chat;

[Scope]
public class ChatTools(
    McpService mcpService,
    WebSearchTool webSearchTool,
    WebCrawlingTool webCrawlingTool,
    KnowledgeSearchTool knowledgeSearchTool,
    GeneratePresentationTool generatePresentationTool,
    GenerateDocxTool generateDocxTool,
    GenerateFormTool generateFormTool,
    AiSettingsStore aiSettingsStore,
    AiGateway aiGateway,
    AiAccessibility aiAccessibility,
    TenantManager tenantManager,
    AuthContext authContext)
{
    public async Task<(ToolHolder, string? error)> GetAsync(Folder<int> agent,
        UserChatSettings chatSettings,
        bool knowledgeHasFiles,
        int resultStorageId,
        Dictionary<string, string>? metadata)
    {
        var (holder, error) = await mcpService.GetToolsAsync(agent.Id);

        var tenantQuota = await tenantManager.GetCurrentTenantQuotaAsync();
        if (tenantQuota.AutomationApi)
        {
            var userId = authContext.CurrentAccount.ID;

            holder.AddTool(
                SystemToolType.GeneratePresentation,
                ToWrapper(agent.Id, generatePresentationTool.Init(resultStorageId, userId))
            );
            holder.AddTool(
                SystemToolType.GenerateDocx,
                ToWrapper(agent.Id, generateDocxTool.Init(resultStorageId, userId))
            );
            holder.AddTool(
                SystemToolType.GenerateForm,
                ToWrapper(agent.Id, generateFormTool.Init(resultStorageId, userId))
            );
        }

        if (knowledgeHasFiles && await aiAccessibility.IsVectorizationEnabledAsync())
        {
            var knowledgeFunc = knowledgeSearchTool.Init(agent);
            var knowledgeWrapper = ToWrapper(agent.Id, knowledgeFunc);
            holder.AddTool(SystemToolType.KnowledgeSearch, knowledgeWrapper);
        }

        if (!chatSettings.WebSearchEnabled)
        {
            return (holder, error);
        }

        var config = await GetWebConfigAsync();
        if (config == null)
        {
            return (holder, error);
        }

        var webTool = webSearchTool.Init(config, metadata);
        var webWrapper = ToWrapper(agent.Id, webTool);
        holder.AddTool(SystemToolType.WebSearch, webWrapper);

        if (!config.CrawlingSupported())
        {
            return (holder, error);
        }

        var crawlTool = webCrawlingTool.Init(config, metadata);
        var crawlWrapper = ToWrapper(agent.Id, crawlTool);
        holder.AddTool(SystemToolType.WebCrawling, crawlWrapper);

        return (holder, error);
    }

    private async Task<EngineConfig?> GetWebConfigAsync()
    {
        if (aiGateway.Configured)
        {
            if (!await aiGateway.IsAiEnabledAsync())
            {
                return null;
            }

            return new InternalWebSearchConfig
            {
                BaseUrl = aiGateway.Url,
                ApiKey = await aiGateway.GetKeyAsync()
            };
        }

        var settings = await aiSettingsStore.GetWebSearchSettingsAsync();

        if (!settings.Enabled || settings.Type == EngineType.None)
        {
            return null;
        }

        return settings.Type != EngineType.PortalAi ? settings.Config : null;
    }

    private static ToolWrapper ToWrapper(int roomId, AIFunction tool)
    {
        return new ToolWrapper
        {
            Tool = tool,
            Context = new ToolContext
            {
                Name = tool.Name,
                RoomId = roomId,
                AutoInvoke = true
            }
        };
    }

    private static ToolWrapper ToWrapper(int roomId, EditorToolRegistration registration)
    {
        return new ToolWrapper
        {
            Tool = registration.Tool,
            Context = new ToolContext
            {
                Name = registration.Tool.Name,
                RoomId = roomId,
                AutoInvoke = true,
                OnToolCallReceived = registration.OnToolCall
            }
        };
    }
}
