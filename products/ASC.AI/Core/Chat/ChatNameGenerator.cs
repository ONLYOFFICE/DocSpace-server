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

namespace ASC.AI.Core.Chat;

[Scope]
public class ChatNameGenerator(
    ChatClientFactory chatClientFactory,
    AuthContext authContext,
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

    public async Task<string?> GenerateAsync(string userMessage, ChatClientOptions options)
    {
        ArgumentException.ThrowIfNullOrEmpty(userMessage);

        try
        {
            var client = chatClientFactory.Create(options, authContext.CurrentAccount.ID);

            var messages = new List<ChatMessage> { _systemMessage, new(ChatRole.User, userMessage) };

            var response = await client.GetResponseAsync(messages);
            var message = response.Messages.First();

            var content = message.Contents.First();
            var textContent = content as TextContent;

            if (string.IsNullOrEmpty(textContent?.Text))
            {
                throw new InvalidOperationException();
            }

            var processedText = TextContentUtils.CutThink(textContent.Text);
            return ProcessTitle(processedText);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to generate chat title");
            return null;
        }
    }

    public static string Generate(string text)
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
