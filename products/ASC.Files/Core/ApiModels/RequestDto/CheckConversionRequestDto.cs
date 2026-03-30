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

namespace ASC.Files.Core.ApiModels.RequestDto;

/// <summary>
/// The parameters for checking file conversion.
/// </summary>
public class CheckConversionRequestDto<T>
{
    /// <summary>
    /// The file ID to check conversion proccess.
    /// </summary>
    /// <example>1</example>
    public T FileId { get; set; }

    /// <summary>
    /// Specifies if the conversion process is synchronous or not.
    /// </summary>
    /// <example>false</example>
    public bool Sync { get; set; }

    /// <summary>
    /// Specifies whether to start a conversion process or not.
    /// </summary>
    /// <example>true</example>
    public bool StartConvert { get; set; }

    /// <summary>
    /// The file version that is converted.
    /// </summary>
    /// <example>1</example>
    public int Version { get; set; }

    /// <summary>
    /// The password of the converted file.
    /// </summary>
    /// <example>password123</example>
    public string Password { get; set; }

    /// <summary>
    /// The conversion output type.
    /// </summary>
    /// <example>pdf</example>
    public string OutputType { get; set; }

    /// <summary>
    /// Specifies whether to create a new file if it exists or not.
    /// </summary>
    /// <example>false</example>
    public bool CreateNewIfExist { get; set; }
}

/// <summary>
/// The parameters for starting file conversion.
/// </summary>
public class StartConversionRequestDto<T>
{
    /// <summary>
    /// The file ID to start conversion proccess.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The parameters for checking file conversion.
    /// </summary>
    /// <example>{"fileId": "1", "sync": false, "startConvert": true, "version": 1, "password": "password123", "outputType": "pdf", "createNewIfExist": false}</example>
    [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]
    public CheckConversionRequestDto<T> CheckConversion { get; set; }
}

/// <summary>
/// The parameters for checking file conversion status.
/// </summary>
public class CheckConversionStatusRequestDto<T>
{
    /// <summary>
    /// The file ID to check conversion status.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// Specifies whether a conversion operation is started or not.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "start")]
    public bool Start { get; set; }
}