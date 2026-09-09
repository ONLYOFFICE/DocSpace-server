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

using AttachmentResult = ASC.AI.Service.AttachmentResult;

namespace ASC.AI.Models.ResponseDto;

public class AttachmentDto
{
    public required Guid Id { get; init; }
    public required string Kind { get; init; }
    public required FileType Type { get; init; }
    public required string Title { get; init; }
    public string? Content { get; init; }
    public string? DataUrl { get; init; }
    public string? EntryId { get; init; }
    public long CreatedAt { get; init; }
    public bool CanAnalyze { get; init; }

    /// <summary>
    /// Starter questions about the attached form's submissions, in the current user's language. Empty
    /// unless <see cref="CanAnalyze"/> is set. On attach they are usually still being generated, so the
    /// array arrives empty and the client re-reads the attachment to pick them up; batch reads never
    /// carry them.
    /// </summary>
    public IReadOnlyList<FormQuestionDto> SuggestedQuestions { get; init; } = [];
}

/// <summary>A starter question about a form's submissions, and the request the chat gets when it is picked.</summary>
public class FormQuestionDto
{
    /// <summary>Short, button-sized question.</summary>
    public required string Question { get; init; }

    /// <summary>The expanded request sent to the chat.</summary>
    public required string Prompt { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None,
    PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class AttachmentMapper
{
    [MapperIgnoreSource(nameof(AttachmentResult.ThirdpartyEntryId))]
    [MapPropertyFromSource(nameof(AttachmentDto.EntryId), Use = nameof(MapEntryId))]
    [MapPropertyFromSource(nameof(AttachmentDto.Type), Use = nameof(MapType))]
    public static partial AttachmentDto MapToDto(AttachmentResult result);

    private static string? MapEntryId(AttachmentResult result) =>
        result.EntryId?.ToString() ?? result.ThirdpartyEntryId;

    private static FileType MapType(AttachmentResult result) =>
        FileUtility.GetFileTypeByFileName(result.Title);

    private static string MapKindToString(AttachmentKind kind) => kind.ToStringLowerFast();

    private static long MapDateTimeToMs(DateTime dateTime) =>
        new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
}
