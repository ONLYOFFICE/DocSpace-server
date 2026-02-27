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

using ASC.Core.Common.AI;

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// Data transfer object that encapsulates comprehensive pricing information for various AI services.
/// Provides organized collections of pricing details for chat models, embedding services, and web search functionality,
/// along with the currency in which prices are denominated.
/// </summary>
public class AiPricesDto
{
    /// <summary>
    /// Gets the list of pricing entries for AI chat models.
    /// </summary>
    /// <example>[{"id":"gpt-4o","alias":"GPT-4o","provider":"openai","image":"https://cdn.example.com/providers/openai.png","price":{"prompt":5.0,"completion":15.0}}]</example>
    public required List<AiEntryPricingDto<AiChatPriceDto>> Chat { get; init; }

    /// <summary>
    /// Gets the list of pricing entries for AI embedding models.
    /// </summary>
    /// <example>[{"id":"text-embedding-3-large","alias":"Text Embedding 3 Large","provider":"openai","image":"https://cdn.example.com/providers/openai.png","price":{"prompt":0.13}}]</example>
    public required List<AiEntryPricingDto<AiEmbeddingPriceDto>> Embedding { get; init; }

    /// <summary>
    /// Gets the list of pricing entries for AI web search operations.
    /// </summary>
    /// <example>[{"id":"web-search","alias":"Web Search","provider":"tavily","image":"https://cdn.example.com/providers/tavily.png","price":0.01}]</example>
    public required List<AiEntryPricingDto<decimal>> WebSearch { get; init; }

    /// <summary>
    /// Gets the currency information for the AI pricing data.
    /// </summary>
    /// <example>{"code":"USD","symbol":"$"}</example>
    public required CurrencyInfo Currency { get; init; }
}

/// <summary>
/// Data transfer object that represents pricing information for a specific AI service entry.
/// Contains identification details, provider information, and associated pricing data of generic type T.
/// </summary>
/// <typeparam name="T">The type of pricing information, which can be a chat price, embedding price, or a simple decimal value.</typeparam>
public class AiEntryPricingDto<T>
{
    /// <summary>
    /// Gets the unique identifier for the AI pricing entry.
    /// </summary>
    /// <example>gpt-4o</example>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display name (alias) for the AI model or service entry.
    /// </summary>
    /// <example>GPT-4o</example>
    public required string Alias { get; init; }

    /// <summary>
    /// Gets the provider name for the AI service or model.
    /// </summary>
    /// <example>openai</example>
    public required string Provider { get; init; }

    /// <summary>
    /// Gets the image URL or identifier associated with the AI model entry.
    /// </summary>
    /// <example>https://cdn.example.com/providers/openai.png</example>
    public required string Image { get; init; }

    /// <summary>
    /// Gets the pricing information for the AI entry.
    /// </summary>
    /// <example>{"prompt":5.0,"completion":15.0}</example>
    public required T Price { get; init; }
}

/// <summary>
/// Data transfer object that represents the pricing information for an AI chat interaction.
/// </summary>
public class AiChatPriceDto
{
    /// <summary>
    /// Gets the price per one million prompt tokens.
    /// </summary>
    /// <example>5.0</example>
    public decimal Prompt { get; init; }

    /// <summary>
    /// Gets the price per one million completion tokens.
    /// </summary>
    /// <example>15.0</example>
    public decimal Completion { get; init; }
}

/// <summary>
/// Represents a data transfer object that encapsulates pricing information for AI embedding operations.
/// This DTO is used to transport cost-related details associated with generating embeddings through AI models.
/// </summary>
public class AiEmbeddingPriceDto
{
    /// <summary>
    /// Gets the price per one million tokens for embedding generation.
    /// </summary>
    /// <example>0.13</example>
    public decimal Prompt { get; init; }
}

/// <summary>
/// Data transfer object representing pricing information for AI-powered web search operations.
/// </summary>
public class AiWebSearchPricingDto
{
    /// <summary>
    /// Gets the alias identifier for the AI model or service.
    /// </summary>
    /// <example>Web Search</example>
    public required string Alias { get; init; }

    /// <summary>
    /// Gets the provider identifier for the AI service.
    /// </summary>
    /// <example>Exa</example>
    public required string Provider { get; init; }

    /// <summary>
    /// Gets the image identifier or URL representing the search functionality.
    /// </summary>
    /// <example>https://cdn.example.com/features/search.png</example>
    public required string SearchImage { get; init; }

    /// <summary>
    /// Gets the image identifier or URL representing the crawling functionality.
    /// </summary>
    /// <example>https://cdn.example.com/features/crawling.png</example>
    public required string CrawlingImage { get; init; }

    /// <summary>
    /// Gets the price per web search operation.
    /// </summary>
    /// <example>0.01</example>
    public decimal Search { get; init; }

    /// <summary>
    /// Gets the price per crawled content item.
    /// </summary>
    /// <example>0.001</example>
    public decimal Contents { get; init; }
}