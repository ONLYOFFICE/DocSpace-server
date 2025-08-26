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

using ASC.Core.Common.Configuration;
using ASC.FederatedLogin.LoginProviders;

namespace ASC.AI.Core.MCP.Data;

[Singleton]
public class SystemMcpConfig
{
    public readonly IReadOnlyDictionary<Guid, SystemMcpServer> Servers = new Dictionary<Guid, SystemMcpServer>().AsReadOnly();
    
    private readonly FrozenDictionary<string, StaticServerInfo> _staticInfos =
        new Dictionary<string, StaticServerInfo>
        {
            {"onlyoffice-docspace", new StaticServerInfo
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
            }}
        }.ToFrozenDictionary();
    
    public SystemMcpConfig(IConfiguration configuration)
    {
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