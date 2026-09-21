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
/// One completed copy of a form, with the values that were entered into it.
/// </summary>
public class FormResultsDto
{
    /// <summary>
    /// When the portal recorded this copy, in UTC: the moment the filled copy was completed and its data indexed, not
    /// the moment the form itself was made.
    /// </summary>
    /// <example>2025-01-01T00:00:00</example>
    public DateTime CreateOn { get; set; }

    /// <summary>
    /// The values that were entered into this copy, one entry per field, preceded by an entry keyed `FormNumber` that
    /// carries the number of the copy and is what the submissions are ordered by. Fields holding a picture or a
    /// signature are left out of the record, so a field missing here was not necessarily left blank.
    /// </summary>
    /// <example>[{"key": "field1", "value": "Answer"}]</example>
    public IEnumerable<FormsItemData> FormsData { get; set; }
}

/// <summary>
/// All completed copies of a form, together with the description of the fields they were filled into.
/// </summary>
public class FormSubmissionsDto
{
    /// <summary>
    /// Describes the fields of the form version that is being filled - the key each value is stored under, the type
    /// and format of the field and, where the field offers a fixed set of answers, those answers - in the order the
    /// fields are laid out, which is the order to build a results table in. It comes back empty when the portal holds
    /// no indexed description of that version.
    /// </summary>
    /// <example>[]</example>
    public IEnumerable<FormMetadata> Metadata { get; set; }

    /// <summary>
    /// One entry per completed copy, ordered by the copy number that `formsData` carries. An empty list means nothing
    /// has been completed for the version that is currently being filled; the copies of earlier versions of the form
    /// are not reported here.
    /// </summary>
    /// <example>[]</example>
    public IEnumerable<FormResultsDto> Submissions { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class FormResultsDtoMapper
{
    public static partial FormResultsDto MapToFormResultsDto(this DbFormsItemDataSearch source);

}