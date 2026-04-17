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

namespace ASC.AI.Api;

[Scope]
[DefaultRoute]
[ApiController]
[AiFeature]
[ControllerName("ai")]
public class McpController(McpService mcpService, ApiContext apiContext) : ControllerBase
{
    /// <summary>
    /// Register a custom MCP server
    /// </summary>
    /// <remarks>
    /// Registers a new custom MCP (Model Context Protocol) server for the current tenant.
    /// The system validates the server name (only letters, numbers, underscores, and hyphens are allowed),
    /// checks that it is not reserved or already taken, and then attempts to connect to the provided endpoint
    /// to verify reachability and credentials before persisting the configuration.
    /// Requires DocSpace administrator privileges.
    /// </remarks>
    /// <path>api/2.0/ai/servers</path>
    [Tags("AI / MCP")]
    [SwaggerResponse(200, "Newly registered MCP server configuration", typeof(McpServerDto))]
    [SwaggerResponse(400, "Invalid server name, reserved name, duplicate name, incorrect credentials, or invalid endpoint URL")]
    [SwaggerResponse(403, "You don't have permission to manage MCP servers")]
    [HttpPost("servers")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<McpServerDto> AddServerAsync(AddServerRequestDto inDto)
    {
        var server = await mcpService.AddCustomServerAsync(
            inDto.Body.Endpoint,
            inDto.Body.Name,
            inDto.Body.Description,
            inDto.Body.Headers,
            inDto.Body.Icon);

        return server.MapToDto();
    }

    /// <summary>
    /// Update a custom MCP server
    /// </summary>
    /// <remarks>
    /// Updates the configuration of an existing custom MCP server identified by its unique ID.
    /// Any combination of fields (name, description, endpoint, headers, icon) can be updated in a single request.
    /// If the endpoint or headers are changed, the system re-validates connectivity by attempting to reach
    /// the new endpoint before saving. Name uniqueness and format rules are enforced on every update.
    /// Requires DocSpace administrator privileges.
    /// </remarks>
    /// <path>api/2.0/ai/servers/{id}</path>
    [Tags("AI / MCP")]
    [SwaggerResponse(200, "Updated MCP server configuration", typeof(McpServerDto))]
    [SwaggerResponse(400, "Invalid server name, reserved name, duplicate name, incorrect credentials, or invalid endpoint URL")]
    [SwaggerResponse(403, "You don't have permission to manage MCP servers")]
    [SwaggerResponse(404, "The MCP server with the specified ID was not found")]
    [HttpPut("servers/{id}")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<McpServerDto> UpdateServerAsync(UpdateServerRequestDto inDto)
    {
        var server = await mcpService.UpdateCustomServerAsync(
            inDto.Id,
            inDto.Body.Endpoint,
            inDto.Body.Name,
            inDto.Body.Headers,
            inDto.Body.Description,
            inDto.Body.UpdateIcon,
            inDto.Body.Icon);

        return server.MapToDto();
    }

    /// <summary>
    /// Enable or disable an MCP server
    /// </summary>
    /// <remarks>
    /// Toggles the enabled/disabled state of an MCP server. When a server is disabled, it becomes
    /// unavailable for assignment to rooms and will not be used during AI chat sessions.
    /// Enabling a previously disabled server restores its availability across the tenant.
    /// Requires DocSpace administrator privileges.
    /// </remarks>
    /// <path>api/2.0/ai/servers/{id}/status</path>
    [Tags("AI / MCP")]
    [SwaggerResponse(200, "MCP server with the updated status", typeof(McpServerDto))]
    [SwaggerResponse(403, "You don't have permission to manage MCP servers")]
    [SwaggerResponse(404, "The MCP server with the specified ID was not found")]
    [HttpPut("servers/{id}/status")]
    public async Task<McpServerDto> SetServerStatusAsync(SetServerStatusRequestDto inDto)
    {
        var server = await mcpService.SetServerStateAsync(inDto.Id, inDto.Body.Enabled);

        return server.MapToDto();
    }

    /// <summary>
    /// Delete MCP servers
    /// </summary>
    /// <remarks>
    /// Permanently removes one or more MCP servers from the current tenant by their IDs.
    /// All room associations and connection data for the deleted servers are also cleaned up.
    /// This action is irreversible. Requires DocSpace administrator privileges.
    /// </remarks>
    /// <path>api/2.0/ai/servers</path>
    [Tags("AI / MCP")]
    [SwaggerResponse(204, "MCP servers were successfully deleted")]
    [SwaggerResponse(403, "You don't have permission to manage MCP servers")]
    [HttpDelete("servers")]
    public async Task<NoContentResult> DeleteServerAsync(DeleteServersRequestDto inDto)
    {
        await mcpService.DeleteServersAsync(inDto.Body.Servers);

        return NoContent();
    }

    /// <summary>
    /// Get an MCP server by ID
    /// </summary>
    /// <remarks>
    /// Retrieves a summary view of a single MCP server by its unique identifier, including its name,
    /// type, enabled state, and icon. This endpoint returns a compact representation without
    /// sensitive details such as endpoint URL or authentication headers.
    /// Requires DocSpace administrator privileges.
    /// </remarks>
    /// <path>api/2.0/ai/servers/{id}</path>
    [Tags("AI / MCP")]
    [SwaggerResponse(200, "MCP server summary information", typeof(McpServerShortDto))]
    [SwaggerResponse(403, "You don't have permission to manage MCP servers")]
    [SwaggerResponse(404, "The MCP server with the specified ID was not found")]
    [HttpGet("servers/{id}")]
    public async Task<McpServerShortDto> GetServerAsync(GetServersRequestDto inDto)
    {
        var server = await mcpService.GetServerAsync(inDto.Id);

        return server.MapToShortDto();
    }

    /// <summary>
    /// Get all MCP servers
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of all MCP servers registered for the current tenant, including both
    /// enabled and disabled servers. Each entry contains the full configuration (endpoint, headers,
    /// icon, type, and status). Supports pagination via the startIndex and count query parameters.
    /// The total number of servers is included in the response metadata.
    /// Requires DocSpace administrator privileges.
    /// </remarks>
    /// <path>api/2.0/ai/servers</path>
    /// <collection>list</collection>
    [Tags("AI / MCP")]
    [SwaggerResponse(200, "Paginated list of all registered MCP servers", typeof(List<McpServerDto>))]
    [SwaggerResponse(403, "You don't have permission to manage MCP servers")]
    [HttpGet("servers")]
    public async Task<List<McpServerDto>> GetServersAsync(PaginatedRequestDto inDto)
    {
        var (servers, count) = await mcpService.GetAllServersAsync(inDto.StartIndex, inDto.Count);

        apiContext.SetCount(servers.Count).SetTotalCount(count);

        return servers.Select(x => x.MapToDto()).ToList();
    }

    /// <summary>
    /// Get available MCP servers
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of MCP servers that are currently active (enabled) and available for
    /// assignment to rooms. Only servers in the enabled state are included. Each entry contains a compact
    /// summary with the server name, type, icon, and status. Supports pagination via startIndex and count.
    /// The total count of available servers is included in the response metadata.
    /// </remarks>
    /// <path>api/2.0/ai/servers/available</path>
    /// <collection>list</collection>
    [Tags("AI / MCP")]
    [SwaggerResponse(200, "Paginated list of active MCP servers available for room assignment", typeof(List<McpServerShortDto>))]
    [HttpGet("servers/available")]
    public async Task<List<McpServerShortDto>> GetAvailableServersAsync(PaginatedRequestDto inDto)
    {
        var (servers, count) = await mcpService.GetActiveServersAsync(inDto.StartIndex, inDto.Count);

        apiContext.SetCount(servers.Count).SetTotalCount(count);

        return servers.Select(x => x.MapToShortDto()).ToList();
    }

    /// <summary>
    /// Assign MCP servers to a room
    /// </summary>
    /// <remarks>
    /// Associates one or more MCP servers with a specific room, making them available for AI chat sessions
    /// within that room. A maximum of 5 MCP servers can be assigned to a single room. If OAuth-based servers
    /// are included, each room member will need to individually authorize their connection.
    /// Requires room edit permissions.
    /// </remarks>
    /// <path>api/2.0/ai/rooms/{roomId}/servers</path>
    /// <collection>list</collection>
    [Tags("AI / MCP")]
    [SwaggerResponse(200, "List of MCP server statuses after assignment", typeof(List<McpServerStatusDto>))]
    [SwaggerResponse(400, "The maximum number of servers per room has been exceeded")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The room with the specified ID was not found")]
    [HttpPost("rooms/{roomId}/servers")]
    public async Task<List<McpServerStatusDto>> AddRoomServersAsync(AddRoomServersRequestDto inDto)
    {
        var statuses = await mcpService.AddServersToRoomAsync(inDto.RoomId, inDto.Body.Servers);

        return statuses.Select(x => x.MapToStatusDto()).ToList();
    }

    /// <summary>
    /// Get MCP servers assigned to a room
    /// </summary>
    /// <remarks>
    /// Returns the list of MCP servers currently assigned to the specified room along with their connection
    /// statuses for the current user. For OAuth-based servers, the connection status reflects whether the
    /// current user has completed authorization. Requires access to the room's AI chat.
    /// </remarks>
    /// <path>api/2.0/ai/rooms/{roomId}/servers</path>
    /// <collection>list</collection>
    [Tags("AI / MCP")]
    [SwaggerResponse(200, "List of MCP server statuses in the room", typeof(List<McpServerStatusDto>))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The room with the specified ID was not found")]
    [HttpGet("rooms/{roomId}/servers")]
    public async Task<List<McpServerStatusDto>> GetRoomServersAsync(GetRoomServersRequestDto inDto)
    {
        var statuses = await mcpService.GetServersStatusesAsync(inDto.RoomId);

        return statuses.Select(x => x.MapToStatusDto()).ToList();
    }

    /// <summary>
    /// Remove MCP servers from a room
    /// </summary>
    /// <remarks>
    /// Detaches one or more MCP servers from the specified room. After removal, the servers will no longer
    /// be available in AI chat sessions within this room. Existing connections and tool configurations for
    /// the removed servers are also cleaned up. Requires room edit permissions.
    /// </remarks>
    /// <path>api/2.0/ai/rooms/{roomId}/servers</path>
    [Tags("AI / MCP")]
    [SwaggerResponse(204, "MCP servers were successfully removed from the room")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The room with the specified ID was not found")]
    [HttpDelete("rooms/{roomId}/servers")]
    public async Task<NoContentResult> DeleteRoomServersAsync(DeleteRoomServersRequestDto inDto)
    {
        await mcpService.DeleteServersFromRoomAsync(inDto.RoomId, inDto.Body.Servers);

        return NoContent();
    }

    /// <summary>
    /// Configure MCP server tools in a room
    /// </summary>
    /// <remarks>
    /// Updates the set of disabled tools for an MCP server within a specific room. Pass a list of tool names
    /// that should be disabled — all other tools exposed by the server will remain enabled. This allows
    /// room administrators to restrict which MCP capabilities are available during AI chat sessions.
    /// Requires room edit permissions.
    /// </remarks>
    /// <path>api/2.0/ai/rooms/{roomId}/servers/{serverId}/tools</path>
    /// <collection>list</collection>
    [Tags("AI / MCP")]
    [SwaggerResponse(200, "Complete list of tools with their enabled/disabled states", typeof(List<McpToolDto>))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The room or MCP server was not found")]
    [HttpPut("rooms/{roomId}/servers/{serverId}/tools")]
    public async Task<List<McpToolDto>> SetToolsAsync(SetMcpToolsRequestDto inDto)
    {
        var tools = await mcpService.SetToolsAsync(inDto.RoomId, inDto.ServerId,
            inDto.Body.DisabledTools);

        return tools.Select(x => new McpToolDto
        {
            Name = x.Key,
            Enabled = x.Value
        }).ToList();
    }

    /// <summary>
    /// Get MCP server tools in a room
    /// </summary>
    /// <remarks>
    /// Retrieves the full list of tools exposed by an MCP server within the context of a specific room,
    /// along with each tool's enabled or disabled state. Disabled tools will not be invoked during
    /// AI chat sessions in this room. Requires access to the room's AI chat.
    /// </remarks>
    /// <path>api/2.0/ai/rooms/{roomId}/servers/{serverId}/tools</path>
    /// <collection>list</collection>
    [Tags("AI / MCP")]
    [SwaggerResponse(200, "List of tools with their enabled/disabled states", typeof(List<McpToolDto>))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The room or MCP server was not found")]
    [HttpGet("rooms/{roomId}/servers/{serverId}/tools")]
    public async Task<List<McpToolDto>> GetToolsAsync(GetMcpToolsRequestDto inDto)
    {
        var tools = await mcpService.GetToolsAsync(inDto.RoomId, inDto.ServerId);

        return tools.Select(x => new McpToolDto
        {
            Name = x.Key,
            Enabled = x.Value
        }).ToList();
    }

    /// <summary>
    /// Connect an OAuth-based MCP server in a room
    /// </summary>
    /// <remarks>
    /// Completes the OAuth authorization flow for an MCP server within a specific room on behalf of the
    /// current user. The authorization code obtained from the OAuth provider must be passed in the request body.
    /// Upon successful token exchange, the system verifies connectivity to the server and stores
    /// the credentials for the current user. Requires room edit permissions.
    /// </remarks>
    /// <path>api/2.0/ai/rooms/{roomId}/servers/{serverId}/connect</path>
    [Tags("AI / MCP")]
    [SwaggerResponse(200, "MCP server connection status after authorization", typeof(McpServerStatusDto))]
    [SwaggerResponse(400, "The provided authorization code is invalid")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The room or MCP server connection was not found")]
    [HttpPost("rooms/{roomId}/servers/{serverId}/connect")]
    public async Task<McpServerStatusDto> ConnectServerAsync(ConnectServerRequestDto inDto)
    {
        var status = await mcpService.ConnectServerAsync(inDto.RoomId, inDto.ServerId, inDto.Body.Code);

        return status.MapToStatusDto();
    }

    /// <summary>
    /// Disconnect an MCP server in a room
    /// </summary>
    /// <remarks>
    /// Revokes the current user's OAuth connection to an MCP server within the specified room. After
    /// disconnection, the server's tools will no longer be available to this user in AI chat sessions
    /// until they re-authorize. Other room members' connections are not affected.
    /// Requires room edit permissions.
    /// </remarks>
    /// <path>api/2.0/ai/rooms/{roomId}/servers/{serverId}/disconnect</path>
    [Tags("AI / MCP")]
    [SwaggerResponse(200, "MCP server connection status after disconnection", typeof(McpServerStatusDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The room or MCP server connection was not found")]
    [HttpPost("rooms/{roomId}/servers/{serverId}/disconnect")]
    public async Task<McpServerStatusDto> DisconnectServerAsync(DisconnectServerRequestDto inDto)
    {
        var status = await mcpService.DisconnectServerAsync(inDto.RoomId, inDto.ServerId);

        return status.MapToStatusDto();
    }
}
