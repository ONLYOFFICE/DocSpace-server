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

namespace ASC.AI.Core.Chat.MCP;

[Scope]
public class McpService(
    TenantManager tenantManager,
    AuthContext authContext,
    McpDao mcpDao, 
    IFolderDao<int> folderDao,
    FileSecurity fileSecurity,
    IHttpClientFactory httpClientFactory,
    PredefinedMcpSource predefinedMcpSource,
    ILogger<McpService> logger,
    IServiceProvider serviceProvider)
{
    public async Task<IReadOnlyDictionary<string, bool>> SetToolsSettingsAsync(int roomId, Guid serverId, List<string> disabledTools)
    {
        var room = await folderDao.GetFolderAsync(roomId);
        if (room == null || !await fileSecurity.CanUseChatsAsync(room))
        {
            throw new ItemNotFoundException();
        }
        
        var dataBuilder = predefinedMcpSource.GetServerDataBuilder(serverId);
        if (dataBuilder == null)
        {
            throw new ItemNotFoundException();
        }
        
        var tools = disabledTools.Where(x => !string.IsNullOrEmpty(x)).ToHashSet();

        var settings = await mcpDao.SetToolsSettingsAsync(
            tenantManager.GetCurrentTenantId(), roomId, authContext.CurrentAccount.ID, serverId, tools);
        
        return await GetToolsAsync(dataBuilder, settings);
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetToolsAsync(int roomId, Guid serverId)
    {
        var dataBuilder = predefinedMcpSource.GetServerDataBuilder(serverId);
        if (dataBuilder == null)
        {
            throw new ItemNotFoundException();
        }
        
        var settings = await mcpDao.GetToolsSettings(
            tenantManager.GetCurrentTenantId(), roomId, authContext.CurrentAccount.ID, serverId);
        
        return await GetToolsAsync(dataBuilder, settings);
    }
    
    public async Task<ToolHolder> GetToolsAsync(int roomId)
    {
        var httpClient = httpClientFactory.CreateClient();

        var dataList = predefinedMcpSource.Servers
            .Select(dataBuilder => dataBuilder.Build(serviceProvider))
            .ToList();
        
        var settingsMap = await mcpDao.GetToolsSettings(
            tenantManager.GetCurrentTenantId(), roomId, authContext.CurrentAccount.ID, dataList.Select(data => data.Id));
        
        var tasks = dataList.Select(data => 
            ConnectAsync(data, settingsMap.GetValueOrDefault(data.Id), httpClient));

        var clients = new List<IMcpClient>();
        var tools = new List<AITool>();
        
        var result = await Task.WhenAll(tasks);
        foreach (var pair in result)
        {
            if (pair == null)
            {
                continue;
            }

            clients.Add(pair.Item1);
            tools.AddRange(pair.Item2);
        }
        
        return new ToolHolder(clients, tools);
    }
    
    private async Task<IReadOnlyDictionary<string, bool>> GetToolsAsync(IMcpServerOptionsBuilder optionsBuilder, McpToolsSettings? settings)
    {
        var data = optionsBuilder.Build(serviceProvider);
        
        await using var mcpClient = await McpClientFactory.CreateAsync(
            new SseClientTransport(
                new SseClientTransportOptions 
                { 
                    Name = data.Name, 
                    Endpoint = data.Endpoint, 
                    AdditionalHeaders = data.Headers,
                    TransportMode = HttpTransportMode.AutoDetect, 
                    ConnectionTimeout = TimeSpan.FromSeconds(15)
                }, 
                httpClientFactory.CreateClient()));
        
        var tools = await mcpClient.ListToolsAsync();
        
        return settings?.Tools == null 
            ? tools.ToDictionary(t => t.Name, _ => true) 
            : tools.ToDictionary(t => t.Name, t => !settings.Tools.Excluded.Contains(t.Name));
    }

    private async Task<Tuple<IMcpClient, IEnumerable<AITool>>?> ConnectAsync(
        McpServerOptions options, McpToolsSettings? settings, HttpClient httpClient)
    {
        try
        {
            var mcpClient = await McpClientFactory.CreateAsync(
                new SseClientTransport(
                    new SseClientTransportOptions 
                    { 
                        Name = options.Name, 
                        Endpoint = options.Endpoint, 
                        AdditionalHeaders = options.Headers,
                        TransportMode = HttpTransportMode.AutoDetect, 
                        ConnectionTimeout = TimeSpan.FromSeconds(5) 
                    }, 
                    httpClient));

            var tools = await mcpClient.ListToolsAsync();
            if (settings?.Tools == null)
            {
                return new Tuple<IMcpClient, IEnumerable<AITool>>(mcpClient, tools);
            }

            return new Tuple<IMcpClient, IEnumerable<AITool>>(mcpClient, 
                tools.Where(t => !settings.Tools.Excluded.Contains(t.Name)).ToList());

        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            return null;
        }
    }
}