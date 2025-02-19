﻿// (c) Copyright Ascensio System SIA 2009-2024
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
/// Request parameters for inserting a file
/// </summary>
public class InsertFileRequestDto : IModelWithFile, IDisposable
{
    /// <summary>
    /// File
    /// </summary>
    [OpenApiDescription("File")]
    public IFormFile File { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    [OpenApiDescription("File name")]
    public string Title { get; set; }

    /// <summary>
    /// Specifies whether to create a new file if it already exists or not
    /// </summary>
    [OpenApiDescription("Specifies whether to create a new file if it already exists or not")]
    public bool CreateNewIfExist { get; set; }

    /// <summary>
    /// Specifies whether to keep the file converting status or not
    /// </summary>
    [OpenApiDescription("Specifies whether to keep the file converting status or not")]
    public bool KeepConvertStatus { get; set; }


    private Stream _stream;
    private bool _disposedValue;

    /// <summary>
    /// Request input stream
    /// </summary>
    [OpenApiDescription("Request input stream")]
    public Stream Stream
    {
        get => File?.OpenReadStream() ?? _stream;
        set => _stream = value;
    }

    public void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing && _stream != null)
            {
                _stream.Close();
                _stream.Dispose();
                _stream = null;
            }

            _disposedValue = true;
        }
    }

    ~InsertFileRequestDto()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// 
/// </summary>
public class InsertWithFileRequestDto<T>
{
    /// <summary>
    /// Folder ID
    /// </summary>
    [FromRoute(Name = "folderId")]
    [OpenApiDescription("Folder ID")]
    public T FolderId { get; set; }

    /// <summary>
    /// Insert file
    /// </summary>
    [FromForm]
    [ModelBinder(BinderType = typeof(InsertFileModelBinder))]
    [OpenApiDescription("Insert file")]
    public InsertFileRequestDto InsertFile { get; set; }
}