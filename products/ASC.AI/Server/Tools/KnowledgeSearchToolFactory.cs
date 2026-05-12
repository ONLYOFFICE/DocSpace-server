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

using System.ComponentModel;

using ASC.AI.Tools.Core;
using ASC.Files.Core.Utils;

using Microsoft.Extensions.AI;

namespace ASC.AI.Tools;

[Scope(typeof(IAiToolFactory))]
public class KnowledgeSearchToolFactory(
    KnowledgeSearchEngine searchEngine,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    AiAccessibility aiAccessibility) : IAiToolFactory
{
    private const string Name = "docspace_knowledge_search";

    private const string Description =
        "Search the DocSpace knowledge base using semantic search to find relevant information from documents " +
        "and resources stored in the workspace. Finds content based on meaning and context, not just exact " +
        "keyword matches. Use for questions about company policies, procedures, reports, documentation, and " +
        "other organizational knowledge stored in DocSpace.";

    private const string Prompt =
        "When the user asks about company policies, procedures, internal reports, documentation or any " +
        "organizational knowledge stored in the workspace, call docspace_knowledge_search before answering " +
        "from your own memory. Prefer specific, focused queries over broad ones; you may call the tool " +
        "multiple times with different phrasings if the first result is weak.";

    public bool Owns(string toolName)
    {
        return toolName == Name;
    }

    public async IAsyncEnumerable<AiTool> BuildAsync(ToolContext context)
    {
        if (!await aiAccessibility.IsVectorizationEnabledAsync())
        {
            yield break;
        }

        var folderDao = daoFactory.GetFolderDao<int>();

        var agent = await folderDao.GetFolderAsync(context.AgentId);
        if (agent is null || !await fileSecurity.CanUseChatAsync(agent))
        {
            yield break;
        }

        var knowledge = await folderDao.GetFoldersAsync(agent.Id, FolderType.Knowledge).FirstAsync();
        if (knowledge is null || knowledge.FilesCount == 0)
        {
            yield break;
        }

        yield return new AiTool(Name, Prompt, MakeFunction(agent));
    }

    private AIFunction MakeFunction(Folder<int> agent)
    {
        return AIFunctionFactory.Create(Function, new AIFunctionFactoryOptions
        {
            Name = Name,
            Description = Description
        });

        Task<List<KnowledgeSearchResult>> Function([Description("Search query")] string query) =>
            searchEngine.SearchAsync(agent, query);
    }
}
