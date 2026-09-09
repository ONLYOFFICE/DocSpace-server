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
/// The progress of one file conversion, together with the converted file once it exists.
/// </summary>
public class ConversationResultDto
{
    /// <summary>
    /// The identifier of the conversion entry. The portal leaves it empty for file conversions, so a caller follows
    /// its own conversion by the file it queued rather than by this value.
    /// </summary>
    /// <example>12345</example>
    public required string Id { get; set; }

    /// <summary>
    /// Tells which kind of file operation the entry describes, so that a conversion can be told apart from the copy,
    /// move and download entries that share this envelope. A conversion entry reports the conversion type.
    /// </summary>
    /// <example>6</example>
    [JsonPropertyName("Operation")]
    public required FileOperationType OperationType { get; set; }

    /// <summary>
    /// How far the conversion has got, counted in percent from 0 while it is only queued to 100 once it is over -
    /// whether it ended with a converted file or with an error. 100 is the value a polling caller waits for.
    /// </summary>
    /// <example>50</example>
    public required int Progress { get; set; }

    /// <summary>
    /// Describes what is being converted: the identifier of the source file, the version that was taken and whether
    /// an existing result may be overwritten, packed as a JSON object inside a string. It is what identifies the
    /// entry when several conversions of the same caller are in flight.
    /// </summary>
    /// <example>{"id":9846,"version":1,"updateIfExist":false}</example>
    public string Source { get; set; }

    /// <summary>
    /// Carries the converted file once the conversion has finished, in the shape a file has elsewhere in this API
    /// plus the title of the folder it was saved to, and stays empty while the conversion is still running. A
    /// conversion that stopped because the source is password-protected reports the plain word `password` here
    /// instead of a file.
    /// </summary>
    /// <example>{"id": 10, "title": "converted_file.pdf"}</example>
    [JsonPropertyName("result")]
    public object File { get; set; }

    /// <summary>
    /// The reason the conversion stopped, in the language of the caller, and empty while it is running and after it
    /// has succeeded. `progress` reaches 100 for a failure as well, so this field is what separates a converted file
    /// from a broken conversion; a conversion still unfinished after ten minutes ends with a timeout reported here.
    /// </summary>
    /// <example>Conversion failed</example>
    public string Error { get; set; }

    /// <summary>
    /// Reports whether the portal has taken the entry as far as it goes: `1` once the conversion has finished or
    /// failed, and empty while it is still queued or still being converted. It is the bookkeeping of the conversion
    /// queue rather than a result - what happened is in `progress`, `error` and `result`.
    /// </summary>
    /// <example>1</example>
    public string Processed { get; set; }
}