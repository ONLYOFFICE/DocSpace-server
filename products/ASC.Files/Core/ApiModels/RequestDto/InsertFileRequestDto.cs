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
/// The request parameters for inserting a file.
/// </summary>
public class InsertFileRequestDto : IModelWithFile, IDisposable
{
    /// <summary>
    /// The file to be inserted.
    /// </summary>
    /// <example>binary file data</example>
    public IFormFile File { get; set; }

    /// <summary>
    /// The file title to be inserted.
    /// </summary>
    /// <example>My Document</example>
    public string Title { get; set; }

    /// <summary>
    /// Specifies whether to create a new file if it already exists or not.
    /// </summary>
    /// <example>true</example>
    public bool CreateNewIfExist { get; set; }

    /// <summary>
    /// Specifies whether to keep the file converting status or not.
    /// </summary>
    /// <example>false</example>
    public bool KeepConvertStatus { get; set; }


    private Stream _stream;
    private bool _disposedValue;

    /// <summary>
    /// The request input stream.
    /// </summary>
    /// <example>binary stream data</example>
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
/// The generic request parameters for inserting a file.
/// </summary>
public class InsertWithFileRequestDto<T>
{
    /// <summary>
    /// The folder ID for inserting a file.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public required T FolderId { get; set; }

    /// <summary>
    /// The request parameters for inserting a file.
    /// </summary>
    /// <example>{"title": "My Document", "createNewIfExist": true}</example>
    [FromForm]
    [ModelBinder(BinderType = typeof(InsertFileModelBinder))]
    public InsertFileRequestDto InsertFile { get; set; }
}