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

namespace ASC.AI.Core.Tools.Retrieval;

[Scope]
public class KnowledgeSearchTool(KnowledgeSearchEngine searchEngine)
{
    public const string Name = "docspace_knowledge_search";
    private const string Description = "Search the DocSpace knowledge base using hybrid lexical and semantic search to find relevant information from documents and resources stored in the workspace. Finds content by combining exact keywords, document titles, and contextual meaning. Use for questions about company policies, procedures, reports, documentation, and other organizational knowledge stored in DocSpace.";
    
    private static AIFunctionFactoryOptions FactoryOptions => new()
    {
        Name = Name, 
        Description = Description
    };

    public AIFunction Init(Folder<int> agent)
    {
        return AIFunctionFactory.Create(Function, FactoryOptions);
        
        async Task<ToolResponse<List<KnowledgeSearchResult>>> Function([Description("Search query. Include important keywords, names, and natural-language intent for hybrid lexical and semantic retrieval.")] string query)
        {
            try
            {
                var results = await searchEngine.SearchAsync(agent, query);
                
                return new ToolResponse<List<KnowledgeSearchResult>>
                {
                    Data = results
                };
            }
            catch (Exception e)
            {
                return new ToolResponse<List<KnowledgeSearchResult>>
                {
                    Error = e.Message
                };
            }
        }
    }
}
