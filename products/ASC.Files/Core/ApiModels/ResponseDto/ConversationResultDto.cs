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
/// The result of file convertion operation.
/// </summary>
public class ConversationResultDto
{
    /// <summary>
    /// The conversion operation ID.
    /// </summary>
    /// <example>12345</example>
    public required string Id { get; set; }

    /// <summary>
    /// The conversion operation type.
    /// </summary>
    /// <example>0</example>
    [JsonPropertyName("Operation")]
    public required FileOperationType OperationType { get; set; }

    /// <summary>
    /// The conversion operation progress.
    /// </summary>
    /// <example>50</example>
    public required int Progress { get; set; }

    /// <summary>
    /// The source file for the conversion.
    /// </summary>
    /// <example>document.docx</example>
    public string Source { get; set; }

    /// <summary>
    /// The resulting file after the conversion.
    /// </summary>
    /// <example>{"id": 10, "title": "converted_file.pdf"}</example>
    [JsonPropertyName("result")]
    public object File { get; set; }

    /// <summary>
    /// The conversion operation error message.
    /// </summary>
    /// <example>Conversion failed</example>
    public string Error { get; set; }

    /// <summary>
    /// Specifies if the conversion operation is processed or not.
    /// </summary>
    /// <example>true</example>
    public string Processed { get; set; }
}