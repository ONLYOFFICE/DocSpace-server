// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.AI.Core.Chat;

[Scope]
public class ChatTools(
    McpService mcpService,
    WebSearchTool webSearchTool,
    WebCrawlingTool webCrawlingTool,
    KnowledgeSearchTool knowledgeSearchTool,
    AiSettingsStore aiSettingsStore,
    AiGateway aiGateway,
    AiAccessibility aiAccessibility)
{
    public async Task<(ToolHolder, string? error)> GetAsync(int roomId, UserChatSettings chatSettings, bool knowledgeHasFiles)
    {
        var (holder, error) = await mcpService.GetToolsAsync(roomId);

        if (knowledgeHasFiles && await aiAccessibility.IsVectorizationEnabledAsync())
        {
            var knowledgeFunc = knowledgeSearchTool.Init(roomId);
            var knowledgeWrapper = ToWrapper(roomId, knowledgeFunc);
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

        var webTool = webSearchTool.Init(config);
        var webWrapper = ToWrapper(roomId, webTool);
        holder.AddTool(SystemToolType.WebSearch, webWrapper);

        if (!config.CrawlingSupported())
        {
            return (holder, error);
        }

        var crawlTool = webCrawlingTool.Init(config);
        var crawlWrapper = ToWrapper(roomId, crawlTool);
        holder.AddTool(SystemToolType.WebCrawling, crawlWrapper);

        return (holder, error);
    }

    private async Task<EngineConfig?> GetWebConfigAsync()
    {
        if (!await aiSettingsStore.IsWebSearchEnabledAsync())
        {
            return null;
        }
        
        if (aiGateway.Configured)
        {
            return new DocSpaceWebSearchConfig 
            { 
                BaseUrl = aiGateway.Url, 
                ApiKey = await aiGateway.GetKeyAsync() 
            };
        }
        
        var settings = await aiSettingsStore.GetWebSearchSettingsAsync();
        return settings is not { Enabled: true, Type: not EngineType.None, Config: not null } ? null : settings.Config;
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
}