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

using ASC.AI.Core.WebSearch;

namespace ASC.AI.Core.Chat;

[Scope]
public class ChatTools(
    McpService mcpService,
    KnowledgeSearchEngine searchEngine,
    AiConfigurationService configurationService, 
    IHttpClientFactory httpClientFactory)
{
    public async Task<ToolHolder> GetAsync(int roomId)
    {
        var holder = await mcpService.GetToolsAsync(roomId);
        
        var searchTool = MakeKnowledgeSearchTool(roomId);
        holder.AddTool(searchTool);
        
        // var webSearchTool = MakeWebSearchTool(roomId);
        // holder.AddTool(webSearchTool);
        
        return holder;
    }

    private ToolWrapper MakeKnowledgeSearchTool(int roomId)
    {
        var searchTool = AIFunctionFactory.Create(
            ([Description("Query to search")]string query) => searchEngine.SearchAsync(roomId, query), 
            new AIFunctionFactoryOptions
            {
                Name = "docspace_knowledge_search",
                Description = "Search in knowledge base"
            });

        return new ToolWrapper
        {
            Tool = searchTool, 
            Properties = new ToolProperties
            {
                Name = searchTool.Name,
                RoomId = roomId,
                AutoInvoke = true
            }
        };
    }

    private ToolWrapper MakeWebSearchTool(int roomId)
    {
        var httpClient = httpClientFactory.CreateClient();
        var settings = configurationService.GetWebSearchConfigAsync().Result;
        var config = settings.Config as ExaConfig;
        
        var exaEngine = new ExaWebSearchEngine(httpClient, config!);
        
        var webSearchTool = AIFunctionFactory.Create([Description("Query to search")]async (string query) =>
        {
            var results = await exaEngine.SearchAsync(new SearchQuery { Query = query, MaxResults = 5 });
            
            var content = JsonSerializer.Serialize(results, AiUtils.ContentSerializerOptions);
            
            return new KnowledgeSearchResult { Content = [new TextContent(content)] };
        },
        new AIFunctionFactoryOptions
        {
            Name = "docspace_web_search",
            Description = "Search in web"
        });

        return new ToolWrapper
        {
            Tool = webSearchTool,
            Properties = new ToolProperties
            {
                Name = webSearchTool.Name, 
                RoomId = roomId, 
                AutoInvoke = true
            }
        };
    }
}