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

namespace ASC.AI.Core.Tools;

public class ToolHolder : IAsyncDisposable
{
    public readonly List<AITool> Tools = [];
    
    private readonly List<McpClient> _clients = [];
    private readonly Dictionary<string, ToolContext> _contexts = [];
    private readonly HashSet<SystemToolType> _systemTools = [];

    public void AddTool(SystemToolType systemToolType, ToolWrapper toolWrapper)
    {
        _systemTools.Add(systemToolType);
        Tools.Add(toolWrapper.Tool);
        _contexts.Add(toolWrapper.Tool.Name, toolWrapper.Context);
    }

    public void AddMcpTool(McpClient client, IEnumerable<ToolWrapper> toolWrappers)
    {
        _clients.Add(client);

        foreach (var toolWrapper in toolWrappers)
        {
            if (toolWrapper.Tool is not McpClientTool mcpClientTool || toolWrapper.Context.McpServerInfo is null)
            {
                continue;
            }
            
            if (!_contexts.ContainsKey(mcpClientTool.Name))
            {
                Tools.Add(toolWrapper.Tool);
                _contexts.Add(toolWrapper.Tool.Name, toolWrapper.Context);
                
                continue;
            }

            var serverName = toolWrapper.Context.McpServerInfo.ServerName;
            
            var uniqueName = GetUniqueName(serverName, toolWrapper.Tool.Name);
            if (string.IsNullOrEmpty(uniqueName))
            {
                continue;
            }

            toolWrapper.Tool = mcpClientTool.WithName(uniqueName);
            Tools.Add(toolWrapper.Tool);
            _contexts.Add(toolWrapper.Tool.Name, toolWrapper.Context);
        }
    }
    
    public ToolContext GetContext(string toolName)
    {
        return !_contexts.TryGetValue(toolName, out var properties) 
            ? throw new ArgumentException($"Tool {toolName} not found") 
            : properties;
    }
    
    public bool ContainsSystemTool(SystemToolType toolType)
    {
        return _systemTools.Contains(toolType);
    }
    
    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients)
        {
            await client.DisposeAsync();
        }
    }

    private static string GetUniqueName(string serverName, string toolName)
    {
        const int maxToolNameLength = 64;
        const int maxHashLength = 4;
        
        var name = $"{serverName}_{toolName}";
        if (name.Length <= maxToolNameLength)
        {
            return name;
        }
        
        var hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(serverName));
        var hash = Convert.ToHexStringLower(hashBytes).Replace("-", "").ToLower();

        var availableForHash = maxToolNameLength - (toolName.Length + 1);
        if (availableForHash < 1)
        {
            return string.Empty;
        }
        
        var hashLength = Math.Min(maxHashLength, availableForHash);
        var truncatedHash = hash[..hashLength];

        return $"{toolName}_{truncatedHash}";
    }
}

public class ToolWrapper
{
    public required AITool Tool { get; set; }
    public required ToolContext Context { get; init; }
}

public class ToolContext
{
    public McpServerInfo? McpServerInfo { get; init; }
    public required string Name { get; init; }
    public int RoomId { get; init; }
    public bool AutoInvoke { get; set; }
}