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

[Scope]
public class ChatNameGenerator(
    ChatClientFactory chatClientFactory,
    ILogger<ChatNameGenerator> logger)
{
    private const int MaxTitleLength = 120;
    
    private static readonly string _instruction = 
        $"""
        You are a chat title generator. Based on the user's message, create a concise, descriptive title for the conversation.
        
        Requirements:
        - Maximum {MaxTitleLength} characters
        - Capture the main topic or intent of the user's query
        - Be specific and informative
        - Use natural, clear language
        - No quotes, punctuation at the end, or special formatting
        - Generate the title in that same language
        
        Examples:
        User: "How do I make sourdough bread from scratch?"
        Title: How to Make Sourdough Bread from Scratch
        
        User: "Explain quantum entanglement simply"
        Title: Simple Explanation of Quantum Entanglement
        
        User: "Write a Python script to parse CSV files"
        Title: Python Script for CSV File Parsing
        
        Generate only the title, nothing else.
        """;
    
    private static readonly ChatMessage _systemMessage = new(ChatRole.System, _instruction);
    
    public async Task<string?> GenerateAsync(ChatExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context.UserMessage);
        ArgumentException.ThrowIfNullOrEmpty(context.Message);
        
        try
        {
            var client = chatClientFactory.Create(context.ClientOptions);

            var messages = new List<ChatMessage> { _systemMessage, context.UserMessage };

            var response = await client.GetResponseAsync(messages);
            var message = response.Messages.First();

            var content = message.Contents.First();
            var textContent = content as TextContent;

            return string.IsNullOrEmpty(textContent?.Text) 
                ? throw new InvalidOperationException() 
                : ProcessTitle(textContent.Text);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to generate chat title");
            return null;
        }
    }

    public string Generate(string text)
    {
        return ProcessTitle(text);
    }

    private static string ProcessTitle(string text)
    {
        const string suffix = "...";

        var title = text.Replace("\n", " ").Replace("\r", " ").Trim();
        if (title.Length > MaxTitleLength)
        {
            title = title[..(MaxTitleLength - suffix.Length)].TrimEnd() + suffix;
        }
        
        return title;
    }
}