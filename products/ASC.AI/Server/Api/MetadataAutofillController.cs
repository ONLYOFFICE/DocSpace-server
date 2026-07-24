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

namespace ASC.AI.Api;

[Scope]
[DefaultRoute]
[ApiController]
[AiFeature]
[ControllerName("ai")]
public class MetadataAutofillController(
    MetadataAutofillService metadataAutofillService,
    MetadataAutofillPublisher metadataAutofillPublisher,
    MetadataDtoHelper metadataDtoHelper) : ControllerBase
{
    /// <summary>
    /// Autofill document metadata
    /// </summary>
    /// <remarks>
    /// Extracts the text of the specified document and fills the metadata field values of the assigned templates using the default AI provider.
    /// By default only the empty fields are filled; pass "overwrite" to replace the existing values, or "dryRun" to get the proposed values without saving.
    /// </remarks>
    /// <path>api/2.0/ai/metadata/autofill</path>
    [Tags("AI / Metadata")]
    [SwaggerResponse(200, "The metadata values filled from the document", typeof(List<MetadataValueDto>))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "File not found")]
    [HttpPost("metadata/autofill")]
    public async Task<List<MetadataValueDto>> Autofill(MetadataAutofillRequestDto inDto)
    {
        var values = await metadataAutofillService.AutofillAsync(inDto.FileId, inDto.TemplateId, inDto.Overwrite, inDto.DryRun);

        return values.Select(metadataDtoHelper.Get).ToList();
    }

    /// <summary>
    /// Autofill metadata of a batch of documents
    /// </summary>
    /// <remarks>
    /// Submits the specified files for the background metadata autofill. Each file is processed asynchronously; files the current user cannot edit are skipped.
    /// </remarks>
    /// <path>api/2.0/ai/metadata/autofill/batch</path>
    [Tags("AI / Metadata")]
    [SwaggerResponse(200, "The batch was successfully submitted")]
    [HttpPost("metadata/autofill/batch")]
    public async Task AutofillBatch(MetadataAutofillBatchRequestDto inDto)
    {
        await metadataAutofillPublisher.PublishAsync(inDto.FileIds, inDto.TemplateId, inDto.Overwrite);
    }

    /// <summary>
    /// Suggest new metadata fields
    /// </summary>
    /// <remarks>
    /// Proposes new custom metadata fields for the globally visible system template based on the document content. Nothing is created:
    /// the client confirms a proposal by creating the field via the metadata API and setting its value.
    /// </remarks>
    /// <path>api/2.0/ai/metadata/suggest-fields</path>
    [Tags("AI / Metadata")]
    [SwaggerResponse(200, "The proposed metadata fields", typeof(List<MetadataFieldSuggestion>))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "File not found")]
    [HttpPost("metadata/suggest-fields")]
    public async Task<List<MetadataFieldSuggestion>> SuggestFields(MetadataSuggestFieldsRequestDto inDto)
    {
        return await metadataAutofillService.SuggestFieldsAsync(inDto.FileId);
    }
}
