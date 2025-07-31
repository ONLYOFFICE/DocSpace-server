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

namespace ASC.AI.Api;

[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("ai")]
public class McpController(McpService mcpService, IMapper mapper, ApiContext apiContext) : ControllerBase
{
    [HttpGet("mcp")]
    public async Task<List<McpServerDto>> GetServersAsync(PaginatedRequestDto inDto)
    {
        var (servers, count) = await mcpService.GetServersAsync(inDto.StartIndex, inDto.Count);
        
        apiContext.SetCount(servers.Count).SetTotalCount(count);
        
        return mapper.Map<List<McpServer>, List<McpServerDto>>(servers);
    }
    
    [HttpPost("rooms/{roomId}/mcp")]
    public async Task<McpRoomDto> AddRoomServersAsync(AddRoomServersRequestDto inDto)
    {
        var servers = await mcpService.AddServersToRoomAsync(inDto.RoomId, inDto.Body.Servers);
        
        return mapper.Map<McpServerOptions, McpRoomDto>(servers.First());
    }

    [HttpGet("rooms/{roomId}/mcp")]
    public async Task<List<McpRoomDto>> GetRoomServersAsync(GetRoomServersRequestDto inDto)
    {
        var servers = await mcpService.GetServersAsync(inDto.RoomId);
        
        return mapper.Map<List<McpServerOptions>, List<McpRoomDto>>(servers);
    }

    [HttpDelete("rooms/{roomId}/mcp")]
    public async Task<NoContentResult> DeleteRoomServersAsync(DeleteRoomServersRequestDto inDto)
    {
        await mcpService.DeleteServersFromRoomAsync(inDto.RoomId, inDto.Body.Servers);
        return NoContent();
    }

    [HttpPut("rooms/{roomId}/mcp/{serverId}/tools")]
    public async Task<List<McpToolDto>> SetToolsAsync(SetMcpToolsRequestDto inDto)
    {
        var tools = await mcpService.SetToolsSettingsAsync(inDto.RoomId, inDto.ServerId, 
            inDto.Body.DisabledTools);
        return tools.Select(x => new McpToolDto
        {
            Name = x.Key,
            Enabled = x.Value
        }).ToList();
    }

    [HttpGet("rooms/{roomId}/mcp/{serverId}/tools")]
    public async Task<List<McpToolDto>> GetToolsAsync(GetMcpToolsRequestDto inDto)
    {
        var tools = await mcpService.GetToolsAsync(inDto.RoomId, inDto.ServerId);
        return tools.Select(x => new McpToolDto
        {
            Name = x.Key,
            Enabled = x.Value
        }).ToList();
    }
}