﻿// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Web.Files.Core.Compress;

/// <summary>
/// Archives the data stream into the .zip format.
/// </summary>
[Scope]
public class CompressToZip : ICompress
{
    private ZipOutputStream _zipStream;
    private ZipEntry _zipEntry;


    /// <summary>
    /// Initializes and sets a new output stream for archiving.
    /// </summary>
    /// <param name="stream">Accepts a new stream, it will contain an archive upon completion of work.</param>
    public Task SetStream(Stream stream)
    {
        _zipStream = new ZipOutputStream(stream) { UseZip64 = UseZip64.Dynamic };
        _zipStream.IsStreamOwner = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates an archive entity (a separate file in the archive).
    /// </summary>
    /// <param name="title">The file name with an extension.</param>
    /// <param name="lastModification">The date and time when the file was last modified.</param>
    public Task CreateEntry(string title, DateTime? lastModification = null)
    {
        _zipEntry = new ZipEntry(title) { IsUnicodeText = true };

        if (lastModification.HasValue)
        {
            _zipEntry.DateTime = lastModification.Value;
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
        await readStream.CopyToAsync(_zipStream);
    }

    /// <summary>
    /// Puts an entry to the output stream.
    /// </summary>
    public Task PutNextEntry()
    {
        _zipStream.PutNextEntry(_zipEntry);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Closes the current entry.
    /// </summary>
    public Task CloseEntry()
    {
        _zipStream.CloseEntry();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the resource title (does not affect the work of the class).
    /// </summary>
    public Task<string> GetTitle() => Task.FromResult(FilesUCResource.FilesWillBeCompressedZip);

    /// <summary>
    /// Returns the archive extension (does not affect the work of the class).
    /// </summary>
    public Task<string> GetArchiveExtension() => Task.FromResult(CompressToArchive.ZipExt);

    /// <summary>
    /// Performs the application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        _zipStream?.Dispose();
    }
}
