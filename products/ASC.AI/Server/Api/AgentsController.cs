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

namespace ASC.AI.Api
{
    [Scope]
    [DefaultRoute]
    [ApiController]
    [ControllerName("ai")]
    public class AgentsController(
        FileStorageService fileStorageService,
        FolderDtoHelper folderDtoHelper,
        FileDeleteOperationsManager fileDeleteOperationsManager,
        FileOperationDtoHelper fileOperationDtoHelper,
        GlobalFolderHelper globalFolderHelper,
        FolderContentDtoHelper folderContentDtoHelper,
        FilesMessageService filesMessageService,
        SettingsManager settingsManager,
        SystemMcpConfig systemMcpConfig,
        McpService mcpService,
        ILogger<AgentsController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Get ai agents
        /// </summary>
        /// <short>Get ai agents</short>
        /// <path>api/2.0/ai/agents</path>
        [SwaggerResponse(200, "Agent information", typeof(FolderContentDto<int>))]
        [HttpGet("agents")]
        public async Task<FolderContentDto<int>> GetAgents(GetAgentListRequestDto inDto)
        {
            var parentId = await globalFolderHelper.GetFolderAiAgentsAsync();

            HashSet<FilterType> filter = [FilterType.AiRooms];

            var tagNames = !string.IsNullOrEmpty(inDto.Tags)
                ? JsonSerializer.Deserialize<IEnumerable<string>>(inDto.Tags)
                : null;

            OrderBy? orderBy = null;
            if (SortedByTypeExtensions.TryParse(inDto.SortBy, true, out var sortBy))
            {
                orderBy = new OrderBy(sortBy, inDto.SortOrder == SortOrder.Ascending);
            }

            var startIndex = inDto.StartIndex;
            var count = inDto.Count;
            var filterValue = inDto.Text;

            var content = await fileStorageService.GetFolderItemsAsync(
                parentId,
                startIndex,
                count,
                filter,
                false,
                inDto.SubjectId,
                filterValue,
                [],
                true,
                false,
                orderBy,
                SearchArea.AiAgents,
                0,
                inDto.WithoutTags ?? false,
                tagNames,
                inDto.ExcludeSubject ?? false,
                ProviderFilter.None,
                inDto.SubjectFilter ?? SubjectFilter.Owner,
                quotaFilter: inDto.QuotaFilter ?? QuotaFilter.All,
                storageFilter: StorageFilter.None);

            var dto = await folderContentDtoHelper.GetAsync(parentId, content, startIndex);

            return dto.NotFoundIfNull();
        }

        /// <summary>
        /// Creates an ai agent.
        /// </summary>
        /// <short>Create an ai agent</short>
        /// <path>api/2.0/ai/agents</path>
        [SwaggerResponse(200, "Agent information", typeof(FolderDto<int>))]
        [HttpPost("agents")]
        public async Task<FolderDto<int>> CreateAgent(CreateAgentRequestDto inDto)
        {
            var lifetime = inDto.Lifetime.Map();
            if (lifetime != null)
            {
                lifetime.StartDate = DateTime.UtcNow;
            }

            var room = await fileStorageService.CreateAiAgentAsync(inDto.Title, inDto.Private,
                inDto.Indexing, inDto.Share, inDto.Quota, lifetime, inDto.DenyDownload, inDto.Watermark, inDto.Color, inDto.Cover,
                inDto.Tags, inDto.Logo, inDto.ChatSettings);

            try
            {
                var server = systemMcpConfig.Servers.Values.FirstOrDefault(
                    x => x.Type == ServerType.DocSpace);

                if (server != null)
                {
                    await mcpService.AddServersToRoomAsync(room, [server.Id]);
                }
            }
            catch (Exception e)
            {
                logger.ErrorWithException(e);
            }

            return await folderDtoHelper.GetAsync(room);
        }

        /// <summary>
        /// Returns an ai agent.
        /// </summary>
        /// <short>Return an ai agent</short>
        /// <path>api/2.0/ai/agents/{id}</path>
        [SwaggerResponse(200, "Agent information", typeof(FolderDto<int>))]
        [HttpGet("agents/{id}")]
        public async Task<FolderDto<int>> GetAgentInfo(RoomIdRequestDto<int> inDto)
        {
            var folder = await fileStorageService.GetFolderAsync(inDto.Id).NotFoundIfNull("Folder not found");

            return await folderDtoHelper.GetAsync(folder);
        }

        /// <summary>
        /// Updates an ai agent.
        /// </summary>
        /// <short>Update an ai agent</short>
        /// <path>api/2.0/ai/agents/{id}</path>
        [SwaggerResponse(200, "Updated agent information", typeof(FolderDto<int>))]
        [HttpPut("agents/{id}")]
        public async Task<FolderDto<int>> UpdateAgent(UpdateRoomRequestDto<int> inDto)
        {
            var room = await fileStorageService.UpdateRoomAsync(inDto.Id, inDto.UpdateRoom);

            return await folderDtoHelper.GetAsync(room);
        }

        /// <summary>
        /// Removes an ai agent.
        /// </summary>
        /// <short>Remove an ai agent</short>
        /// <path>api/2.0/ai/agents/{id}</path>
        [SwaggerResponse(200, "File operation", typeof(FileOperationDto))]
        [HttpDelete("agents/{id}")]
        public async Task<FileOperationDto> DeleteAgent(DeleteRoomRequestDto<int> inDto)
        {
            await fileDeleteOperationsManager.Publish([inDto.Id], [], false, !inDto.DeleteRoom.DeleteAfter, true);

            return await fileOperationDtoHelper.GetAsync((await fileDeleteOperationsManager.GetOperationResults()).FirstOrDefault());
        }


        /// <summary>
        /// Changes the quota limit for the AI agents with the IDs specified in the request.
        /// </summary>
        /// <short>
        /// Change the AI agent quota limit
        /// </short>
        /// <path>api/2.0/ai/agents/agentquota</path>
        /// <collection>list</collection>
        [SwaggerResponse(200, "List of AI agents with the detailed information", typeof(IAsyncEnumerable<FolderDto<int>>))]
        [HttpPut("agents/agentquota")]
        public async IAsyncEnumerable<FolderDto<int>> UpdateAgentsQuota(UpdateRoomsQuotaRequestDto<int> inDto)
        {
            var (agentIntIds, _) = FileOperationsManager.GetIds(inDto.RoomIds);

            var agentNames = new List<string>();

            foreach (var agentId in agentIntIds)
            {
                var agent = await fileStorageService.FolderQuotaChangeAsync(agentId, inDto.Quota);
                agentNames.Add(agent.Title);
                yield return await folderDtoHelper.GetAsync(agent);
            }

            if (inDto.Quota >= 0)
            {
                filesMessageService.Send(MessageAction.CustomQuotaPerAiAgentChanged, inDto.Quota.ToString(), agentNames.ToArray());
            }
            else
            {
                filesMessageService.Send(MessageAction.CustomQuotaPerAiAgentDisabled, string.Join(", ", agentNames.ToArray()));
            }
        }

        /// <summary>
        /// Resets the quota limit for the AI agents with the IDs specified in the request.
        /// </summary>
        /// <short>
        /// Reset the AI agents quota limit
        /// </short>
        /// <path>api/2.0/ai/agents/resetquota</path>
        /// <collection>list</collection>
        [SwaggerResponse(200, "List of AI agents with the detailed information", typeof(IAsyncEnumerable<FolderDto<int>>))]
        [HttpPut("agents/resetquota")]
        public async IAsyncEnumerable<FolderDto<int>> ResetAgentsQuota(UpdateRoomsRoomIdsRequestDto<int> inDto)
        {
            var (agentIntIds, _) = FileOperationsManager.GetIds(inDto.RoomIds);
            var agentTitles = new List<string>();
            var quotaAiAgentSettings = await settingsManager.LoadAsync<TenantAiAgentQuotaSettings>();

            foreach (var agentId in agentIntIds)
            {
                var agent = await fileStorageService.FolderQuotaChangeAsync(agentId, -2);
                agentTitles.Add(agent.Title);

                yield return await folderDtoHelper.GetAsync(agent);
            }

            filesMessageService.Send(MessageAction.CustomQuotaPerAiAgentDefault, quotaAiAgentSettings.DefaultQuota.ToString(), agentTitles.ToArray());
        }
    }
}
