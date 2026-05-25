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

using System.Collections.Immutable;

namespace ASC.AI.Core.MCP.Data;

[Singleton]
public class SystemMcpConfig
{
    public readonly IReadOnlyDictionary<Guid, SystemMcpServer> Servers = new Dictionary<Guid, SystemMcpServer>().AsReadOnly();

    public readonly ImmutableHashSet<string> ReservedServerNames;
    
    private readonly FrozenDictionary<string, StaticServerInfo> _staticInfos =
        new Dictionary<string, StaticServerInfo>
        {
            {"docspace", new StaticServerInfo
            {
                Id = new Guid("883da87d-5ae0-49fd-8cb9-2cb82181667e"),
                ServerType = ServerType.DocSpace,
                ConnectionType = ConnectionType.Direct
            }},
            {"github", new StaticServerInfo
            {
                Id = new Guid("b55705b3-035f-442e-9983-0ea5bb4daa57"),
                ServerType = ServerType.Github,
                ConnectionType = ConnectionType.OAuth,
                LoginProviderSelector = x => x.Get<GithubLoginProvider>()
            }},
            {"box", new StaticServerInfo
            {
                Id = new Guid("791b1cd0-e8c3-4ba2-b966-9037ab3a825b"),
                ServerType = ServerType.Box,
                ConnectionType = ConnectionType.OAuth,
                LoginProviderSelector = x => x.Get<BoxLoginProvider>()
            }}
        }.ToFrozenDictionary();
    
    public SystemMcpConfig(IConfiguration configuration)
    {
        var reservedServerNames = new HashSet<string>();

        foreach (var item in _staticInfos)
        {
            reservedServerNames.Add(item.Key);
        }

        reservedServerNames.Add("docspace");
        
        ReservedServerNames = reservedServerNames.ToImmutableHashSet();
        
        var settings = configuration.GetSection("ai:mcp").Get<List<McpConfig>>();
        if (settings == null)
        {
            return;
        }

        var servers = new Dictionary<Guid, SystemMcpServer>();

        foreach (var item in settings)
        {
            if (string.IsNullOrEmpty(item.Endpoint) || item.Id == Guid.Empty)
            {
                continue;
            }

            if (!_staticInfos.TryGetValue(item.Name, out var staticInfo))
            {
                continue;
            }
            
            var server = new SystemMcpServer
            {
                Id = item.Id,
                Name = item.Name,
                Type = staticInfo.ServerType,
                Headers = item.Headers,
                Endpoint = item.Endpoint,
                ConnectionType = staticInfo.ConnectionType,
                LoginProviderSelector = staticInfo.LoginProviderSelector
            };
            
            servers.Add(server.Id, server);
        }

        Servers = servers;
    }

    private class StaticServerInfo
    {
        public Guid Id { get; init; }
        public ServerType ServerType { get; init; }
        public ConnectionType ConnectionType { get; init; }
        public Func<ConsumerFactory, OauthProvider>? LoginProviderSelector { get; init; }
    }
}