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

namespace ASC.AI.Core.Chat;

public static class ChatPromptTemplate
{
    private const string DefaultHeader =
        """
        You are a helpful AI assistant.
        Your role is to understand the user's intent and complete their requests precisely and efficiently.
        Follow any function-calling instructions provided below.
        Always respond in the same language the user used, and provide clear reasoning if something goes wrong.
        """;
    
    private const string BasePrompt = 
        """
        {0}
        
        When calling functions, follow these rules carefully:
        
        1. Error handling:
            - If a function call fails or returns an error, analyze the error message.
            - Attempt to fix the issue and retry the call.
            - Make up to three retries, adjusting your approach each time based on the specific error.
            - After each failed attempt, explain your reasoning for the adjustment.
        
        2. Context variables:
            - You operate within a folder and room context.
            - If the user does not specify a required folderId, use the {1} from the current context.
            - If the user does not specify a required roomId, use the {2} from the current context.
            - Always pass the actual values (e.g., folderId = 123456), not the literal strings "folderId" or "roomId".
        
        3. Language consistency:
            - Always write all thoughts, reasoning, explanations, and answers in the same language the user used in their question, even when analyzing errors or debugging.
        
        4. Retry limit and fallback:
            - After 3 failed attempts, stop and summarize the encountered errors.
            - If successful, briefly explain what worked.
        """;

        public static string GetPrompt(string? instruction, int contextFolderId, int contextRoomId)
        {
            return string.Format(BasePrompt, !string.IsNullOrEmpty(instruction) ? instruction : DefaultHeader, contextFolderId, contextRoomId);
        }
}