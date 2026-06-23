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

namespace ASC.Web.Files.Core.Compress;

/// <summary>
/// Archives the data stream into the .tar.gz format.
/// </summary>
[Scope]
public class CompressToTarGz : ICompress
{
    private GZipOutputStream _gzoStream;
    private TarOutputStream _gzip;
    private TarEntry _tarEntry;

    /// <summary>
    /// Initializes and sets a new output stream for archiving.
    /// </summary>
    /// <param name="stream">Accepts a new stream, it will contain an archive upon completion of work.</param>
    public Task SetStream(Stream stream)
    {
        _gzoStream = new GZipOutputStream(stream) { IsStreamOwner = false };
        _gzip = new TarOutputStream(_gzoStream, Encoding.UTF8);
        _gzoStream.IsStreamOwner = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates an archive entity (a separate file in the archive).
    /// </summary>
    /// <param name="title">The file name with an extension.</param>
    /// <param name="lastModification">The date and time when the file was last modified.</param>
    public Task CreateEntry(string title, DateTime? lastModification = null)
    {
        _tarEntry = TarEntry.CreateTarEntry(title);
        if (lastModification.HasValue)
        {
            _tarEntry.ModTime = lastModification.Value;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Transfers the file itself to the archive.
    /// </summary>
    /// <param name="readStream">The file data.</param>
    public async Task PutStream(Stream readStream)
    {
        await PutNextEntry();
        await readStream.CopyToAsync(_gzip);
    }

    /// <summary>
    /// Puts an entry to the output stream.
    /// </summary>
    public Task PutNextEntry()
    {
        _gzip.PutNextEntry(_tarEntry);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Closes the current entry.
    /// </summary>
    public Task CloseEntry()
    {
        _gzip.CloseEntry();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the resource title (does not affect the work of the class).
    /// </summary>
    public Task<string> GetTitle() => Task.FromResult(FilesUCResource.FilesWillBeCompressedTarGz);

    /// <summary>
    /// Returns the archive extension (does not affect the work of the class).
    /// </summary>
    public Task<string> GetArchiveExtension() => Task.FromResult(CompressToArchive.TarExt);

    /// <summary>
    /// Performs the application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        _gzip?.Dispose();
        _gzoStream?.Dispose();
    }
}