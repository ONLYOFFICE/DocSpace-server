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

    /// <summary>
    /// Gets the URL link to the AI model or service entry.
    /// </summary>
    /// <example>https://openai.com/pricing</example>
    public required string Link { get; init; }
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
