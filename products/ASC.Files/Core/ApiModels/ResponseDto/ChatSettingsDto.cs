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
    public ChatMultimodalSettingsDto Multimodal { get; init; }

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

    /// <summary>
    /// The maximum image size in bytes. 0 means no limit.
    /// </summary>
    /// <example>20971520</example>
    public long MaxSize { get; init; }
}