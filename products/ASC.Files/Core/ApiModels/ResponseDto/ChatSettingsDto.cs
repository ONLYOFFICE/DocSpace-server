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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The chat settings parameters.
/// </summary>
public class ChatSettingsDto
{
    /// <summary>
    /// The AI provider ID.
    /// </summary>
    /// <example>1</example>
    public int ProviderId { get; set; }

    /// <summary>
    /// The AI model ID used for chat completions.
    /// </summary>
    /// <example>gpt-5.2</example>
    public string ModelId { get; init; }

    /// <summary>
    /// The AI model display alias.
    /// </summary>
    /// <example>GPT-5.2</example>
    public string ModelAlias { get; init; }

    /// <summary>
    /// The system prompt for the chat.
    /// </summary>
    /// <example>You are a helpful assistant.</example>
    public string Prompt { get; init; }

    /// <summary>
    /// The multimodal settings for the chat model.
    /// </summary>
    [Obsolete("Use Capabilities instead")]
    public ChatMultimodalSettingsDto Multimodal { get; init; }

    /// <summary>
    /// Indicates whether the model supports extended thinking mode.
    /// </summary>
    /// <example>false</example>
    [Obsolete("Use Capabilities instead")]
    public bool Thinking { get; init; }

    /// <summary>
    /// The model capabilities.
    /// </summary>
    public AiModelCapabilities Capabilities { get; init; }

    /// <summary>
    /// Indicates whether this is an internal AI gateway provider.
    /// </summary>
    /// <example>false</example>
    public bool Internal => ProviderId == AiGateway.ProviderId;
}

/// <summary>
/// The multimodal settings for the chat model.
/// </summary>
public class ChatMultimodalSettingsDto
{
    /// <summary>
    /// The image multimodal settings.
    /// </summary>
    public ChatImageMultimodalSettingsDto Image { get; init; }
}

/// <summary>
/// The image multimodal settings for the chat model.
/// </summary>
public class ChatImageMultimodalSettingsDto
{
    /// <summary>
    /// The supported image formats.
    /// </summary>
    /// <example>[".jpeg", ".gif"]</example>
    public IEnumerable<string> Formats { get; init; }
}