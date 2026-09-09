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

namespace ASC.Files.Core.ApiModels.RequestDto;

/// <summary>
/// The parameters of one file conversion.
/// </summary>
public class CheckConversionRequestDto<T>
{
    /// <summary>
    /// The file to convert. It is taken from the route of the operation, so a value sent in the body is overwritten.
    /// </summary>
    /// <example>1</example>
    public T FileId { get; set; }

    /// <summary>
    /// How to wait for the result: `true` converts inside the request and answers with the finished result, which is
    /// only sensible for small documents, while `false` queues the conversion and answers with an entry to poll.
    /// </summary>
    /// <example>false</example>
    public bool Sync { get; set; }

    /// <summary>
    /// Whether the conversion is to be started. It is set by the operation itself, so a value sent in the body is
    /// overwritten.
    /// </summary>
    /// <example>true</example>
    public bool StartConvert { get; set; }

    /// <summary>
    /// The version to convert; 0 or less means the current version.
    /// </summary>
    /// <example>1</example>
    public int Version { get; set; }

    /// <summary>
    /// The password that opens the source document, for a file that is protected by one; anything else may be left
    /// out.
    /// </summary>
    /// <example>password123</example>
    public string Password { get; set; }

    /// <summary>
    /// The extension of the format to convert into, without the dot, and one the portal can produce from that
    /// source format; left out, the default of the portal for that kind of document is used.
    /// </summary>
    /// <example>pdf</example>
    public string OutputType { get; set; }

    /// <summary>
    /// Where the result goes when the file has been converted before: `true` creates another file beside the source,
    /// `false` replaces the converted file that already exists.
    /// </summary>
    /// <example>false</example>
    public bool CreateNewIfExist { get; set; }
}

/// <summary>
/// The request that queues the conversion of a file.
/// </summary>
public class StartConversionRequestDto<T>
{
    /// <summary>
    /// The file to convert.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The parameters of the conversion. The whole body may be omitted, in which case the defaults of the portal
    /// apply.
    /// </summary>
    /// <example>
    /// {"sync": false, "version": 1, "password": "p@ssw0rd", "outputType": "docx", "createNewIfExist": false}
    /// </example>
    [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]
    public CheckConversionRequestDto<T> CheckConversion { get; set; }
}

/// <summary>
/// The query that reads the conversion status of a file.
/// </summary>
public class CheckConversionStatusRequestDto<T>
{
    /// <summary>
    /// The file whose conversion is asked about.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// Whether to start the conversion as well: `true` queues it with the default output format and no password,
    /// `false` only reports what the portal already knows.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "start")]
    public bool Start { get; set; }
}