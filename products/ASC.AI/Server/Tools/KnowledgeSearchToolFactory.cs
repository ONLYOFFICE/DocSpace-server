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
        """
        <knowledge_search_tool_usage_rules>
        **CRITICAL: ALWAYS use knowledge base search for ANY user question or information request.**
        This is a mandatory requirement: before providing any substantive answer, you MUST perform a knowledge base search. No exceptions except for the minimal cases listed below.
        **MANDATORY knowledge base search for:**
        - ANY user question or information request
        - ANY topic, keyword, or noun phrase
        - Generic questions that might have internal context
        - Technical questions of any kind
        - Questions phrased as "tell me," "explain," "what is," "how to," "show me," etc.
        - Unclear or ambiguous queries
        - Before using general knowledge to answer anything
        - Even if you think you know the answer
        **The ONLY exceptions (when you may skip knowledge base search):**
        - Simple greetings: "hi," "hello," "hey"
        - Simple thanks: "thanks," "thank you"
        - Meta-questions about your capabilities: "what can you do?"
        - Pure creative requests with no information need: "write me a poem about cats"
        **If unsure whether to search: ALWAYS SEARCH.**
        Search strategy:
        - **First action for any substantive message: search knowledge base**
        - Use searches liberally and proactively
        - If initial search results are insufficient, refine queries with different keywords
        - Avoid more than two back-to-back searches for the same information
        - Each search query should be distinct and not redundant
        - Can be used in combination with web search for comprehensive answers
        **Citation requirements for knowledge base results:**
        - You MUST cite all information from knowledge base search results
        - Citation format: Some fact [Document title](/doceditor?fileId=XXX)
        - Use the document name without extension as the link text
        - One piece of information can have multiple citations: Some fact [Doc1](/doceditor?fileId=61) [Doc2](/doceditor?fileId=62)
        - If multiple lines use the same source, group them together with one citation
        - **CRITICAL: Use URLs EXACTLY as provided in search results**
          - URLs come in format `/doceditor?fileId=XXX`
          - DO NOT modify, expand, or change these URLs
          - DO NOT add domain, protocol, or base path
          - Simply copy the URL directly: [Policy](/doceditor?fileId=61)
        Example citation:
        - Search returns: title="Company Policy.pdf", url="/doceditor?fileId=61"
        - Your response: "According to our policy [Company Policy](/doceditor?fileId=61), employees can..."
        **IMPORTANT: Never ask permission to search.**
        Just search immediately and seamlessly.
        **Examples - ALL require knowledge base search:**
        - User asks "What is OAuth?" → Search knowledge base FIRST
        - User asks "Tell me about Python" → Search knowledge base FIRST
        - User asks "How does Redis work?" → Search knowledge base FIRST
        - User writes "machine learning" → Search knowledge base FIRST
        - User asks "What's the deadline?" → Search knowledge base FIRST
        - User asks "Explain async/await" → Search knowledge base FIRST
        - User asks "quarterly report" → Search knowledge base FIRST
        **After knowledge base search:**
        - If results are found: use them in your answer with proper citations
        - If no results found: you may use general knowledge or web search
        - You can combine knowledge base results with web search for comprehensive answers
        Remember: **DEFAULT ACTION = SEARCH KNOWLEDGE BASE. Always search first, answer second.**
        </knowledge_search_tool_usage_rules>
        """;

    public bool Owns(string toolName)
    {
        return toolName == Name;
    }

    public async Task<ToolBundle> BuildAsync(ResolvedToolContext context)
    {
        if (context.Folder is not Folder<int> { IsAgent: true } agent ||
            !await aiAccessibility.IsVectorizationEnabledAsync() ||
            !await fileSecurity.CanUseChatAsync(agent))
        {
            return ToolBundle.Empty;
        }

        var folderDao = daoFactory.GetFolderDao<int>();

        var knowledge = await folderDao.GetFoldersAsync(agent.Id, FolderType.Knowledge).FirstAsync();
        if (knowledge is null || knowledge.FilesCount == 0)
        {
            return ToolBundle.Empty;
        }

        return new ToolBundle(Prompt, [new AiTool(Name, MakeFunction(agent))]);
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
