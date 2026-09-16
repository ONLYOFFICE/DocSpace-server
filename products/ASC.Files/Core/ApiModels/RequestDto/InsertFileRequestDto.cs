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
    /// The content to store, sent as a `multipart/form-data` part. The same content may instead be sent as the raw
    /// request body, which is what a client that cannot build a form does; when both are present the form part wins.
    /// </summary>
    /// <example>binary file data</example>
    public IFormFile File { get; set; }

    /// <summary>
    /// The name to store the file under, extension included. It wins over the name of the uploaded part, which is the
    /// reason to choose this operation over the plain upload, and it is the only name available when the content
    /// arrives as a raw body. Characters a title cannot hold are replaced with underscores and the name is cut to 170
    /// characters before the file is stored.
    /// </summary>
    /// <example>Quarterly report.docx</example>
    public string Title { get; set; }

    /// <summary>
    /// Settles the clash with a file already carrying that title: left out, the content is written as the next
    /// version of that file; set to true, both survive and the new one gets a numeric suffix in its title.
    /// </summary>
    /// <example>true</example>
    public bool CreateNewIfExist { get; set; }

    /// <summary>
    /// Decides whether the outcome of the background conversion outlives the conversion itself. True keeps the queue
    /// record, so `GET api/2.0/files/file/{fileId}/checkconversion` can still report the result or the error; left
    /// out, the record is cleared the moment the conversion ends and that call finds nothing.
    /// </summary>
    /// <example>true</example>
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
    /// The folder that receives the file; take the id from a listing such as `GET api/2.0/files/@root`. A room or an
    /// ordinary folder inside one is accepted, a section root is not.
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