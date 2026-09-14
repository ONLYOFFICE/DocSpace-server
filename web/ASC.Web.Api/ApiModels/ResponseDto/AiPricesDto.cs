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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// What the AI features cost out of the portal wallet, grouped by the kind of model, in one currency.
/// </summary>
public class AiPricesDto
{
    /// <summary>
    /// The chat models on offer, each priced per million prompt and completion tokens. A model listed here is one
    /// the installation can bill for, not necessarily one this portal may use -
    /// `GET api/2.0/portal/payment/ai-model/restrictions` says which are allowed.
    /// </summary>
    /// <example>[{"id":"gpt-4o","alias":"GPT-4o","provider":"openai","image":"https://cdn.example.com/providers/openai.png","price":{"prompt":5.0,"completion":15.0}}]</example>
    public required List<AiEntryPricingDto<AiChatPriceDto>> Chat { get; init; }

    /// <summary>
    /// The embedding models on offer, priced per million tokens of input; an embedding model has no completion
    /// side, so its price object carries `prompt` alone.
    /// </summary>
    /// <example>[{"id":"text-embedding-3-large","alias":"Text Embedding 3 Large","provider":"openai","image":"https://cdn.example.com/providers/openai.png","price":{"prompt":0.13}}]</example>
    public required List<AiEntryPricingDto<AiEmbeddingPriceDto>> Embedding { get; init; }

    /// <summary>
    /// The image models on offer, priced per million prompt and completion tokens plus a price for each image
    /// produced.
    /// </summary>
    /// <example>[{"id":"gpt-5.4-image-2","alias":"GPT 5.4 Image 2","provider":"OpenRouter","image":"https://cdn.example.com/providers/openai.png","price":{"prompt":8.0,"completion":15.0,"image":30.0}}]</example>
    public required List<AiEntryPricingDto<AiImagePriceDto>> Image { get; init; }

    /// <summary>
    /// The web search providers on offer. Their `price` is a bare number - the cost of one search - rather than
    /// an object, because there are no tokens to distinguish.
    /// </summary>
    /// <example>[{"id":"web-search","alias":"Web Search","provider":"tavily","image":"https://cdn.example.com/providers/tavily.png","price":0.01}]</example>
    public required List<AiEntryPricingDto<decimal>> WebSearch { get; init; }

    /// <summary>
    /// The currency every price above is expressed in, with its ISO code and symbol. One answer never mixes
    /// currencies, so this is the only place to read it.
    /// </summary>
    /// <example>{"code":"USD","symbol":"$"}</example>
    public required CurrencyInfo Currency { get; init; }
}

/// <summary>
/// One AI model or service on the price list: how to name it, who provides it, and what it costs.
/// </summary>
/// <typeparam name="T">The shape of the price: a token-priced object for a model, a bare amount for a search
/// provider.</typeparam>
public class AiEntryPricingDto<T>
{
    /// <summary>
    /// The model identifier to send to the AI operations. It is the value to branch on, while `alias` is for display
    /// only.
    /// </summary>
    /// <example>gpt-4o</example>
    public required string Id { get; init; }

    /// <summary>
    /// The model name as the vendor writes it, meant to be shown to a person rather than matched on.
    /// </summary>
    /// <example>GPT-4o</example>
    public required string Alias { get; init; }

    /// <summary>
    /// Who runs the model. Two entries can share a provider, and one provider's models can be priced quite
    /// differently, so the price always belongs to the entry and never to the provider.
    /// </summary>
    /// <example>openai</example>
    public required string Provider { get; init; }

    /// <summary>
    /// The absolute URL of the provider's icon, for rendering next to the entry.
    /// </summary>
    /// <example>https://cdn.example.com/providers/openai.png</example>
    public required string Image { get; init; }

    /// <summary>
    /// What the entry costs, in the currency the answer names. Amounts per token are normalised per million
    /// tokens, so they are not the price of a single call.
    /// </summary>
    /// <example>{"prompt":5.0,"completion":15.0}</example>
    public required T Price { get; init; }

    /// <summary>
    /// The provider's own page for the model, for a person to read the model's terms. It is empty when the
    /// provider publishes none.
    /// </summary>
    /// <example>https://openai.com/pricing</example>
    public required string Link { get; init; }
}

/// <summary>
/// What a chat model charges, split by the direction the tokens flow in.
/// </summary>
public class AiChatPriceDto
{
    /// <summary>
    /// The cost of one million tokens sent to the model, which includes the conversation history resent with
    /// every turn and not just the newest message.
    /// </summary>
    /// <example>5.0</example>
    public decimal Prompt { get; init; }

    /// <summary>
    /// The cost of one million tokens the model writes back. It is normally the dearer of the two directions.
    /// </summary>
    /// <example>15.0</example>
    public decimal Completion { get; init; }
}

/// <summary>
/// What an embedding model charges, which has one direction only.
/// </summary>
public class AiEmbeddingPriceDto
{
    /// <summary>
    /// The cost of one million tokens turned into vectors. Embedding produces no completion, so this single
    /// figure is the whole price.
    /// </summary>
    /// <example>0.13</example>
    public decimal Prompt { get; init; }
}

/// <summary>
/// What an image model charges: the tokens of the request and the images that come out of it.
/// </summary>
public class AiImagePriceDto
{
    /// <summary>
    /// The cost of one million tokens sent to the image model, which is the prompt describing the picture.
    /// </summary>
    /// <example>8.0</example>
    public decimal Prompt { get; init; }

    /// <summary>
    /// The cost of one million tokens the image model writes back alongside the picture.
    /// </summary>
    /// <example>15.0</example>
    public decimal Completion { get; init; }

    /// <summary>
    /// The cost of one produced image, charged on top of the token amounts above.
    /// </summary>
    /// <example>30.0</example>
    public decimal Image { get; init; }
}
