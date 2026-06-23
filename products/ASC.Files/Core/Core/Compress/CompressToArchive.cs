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
/// Archives the data stream in the format selected in the settings.
/// </summary>
[Scope]
public class CompressToArchive(
    FilesSettingsHelper filesSettings,
    CompressToTarGz compressToTarGz,
    CompressToZip compressToZip)
    : ICompress
{
    internal static readonly string TarExt = ".tar.gz";
    internal static readonly string ZipExt = ".zip";
    private static readonly List<string> _exts = [TarExt, ZipExt];

    private ICompress _compress;

    private async Task<ICompress> GetCompress()
    {
        _compress ??= await filesSettings.GetDownloadTarGz()
            ? compressToTarGz
            : compressToZip;

        return _compress;
    }

    /// <summary>
    /// Returns the archive extension.
    /// </summary>
    /// <param name="ext">The file extension.</param>
    public async Task<string> GetExt(string ext)
    {
        if (_exts.Contains(ext))
        {
            return ext;
        }

        return await GetArchiveExtension();
    }

    /// <summary>
    /// Initializes and sets a new output stream for archiving.
    /// </summary>
    /// <param name="stream">Accepts a new stream, it will contain an archive upon completion of work.</param>
    public async Task SetStream(Stream stream)
    {
        await (await GetCompress()).SetStream(stream);
    }

    /// <summary>
    /// Creates an archive entity (a separate file in the archive).
    /// </summary>
    /// <param name="title">The file name with an extension.</param>
    /// <param name="lastModification">The date and time when the file was last modified.</param>
    public async Task CreateEntry(string title, DateTime? lastModification = null)
    {
        await (await GetCompress()).CreateEntry(title, lastModification);
    }

    /// <summary>
    /// Transfers the file itself to the archive.
    /// </summary>
    /// <param name="readStream">The file data.</param>
    public async Task PutStream(Stream readStream) => await (await GetCompress()).PutStream(readStream);

    /// <summary>
    /// Puts an entry to the output stream.
    /// </summary>
    public async Task PutNextEntry()
    {
        await (await GetCompress()).PutNextEntry();
    }

    /// <summary>
    /// Closes the current entry.
    /// </summary>
    public async Task CloseEntry()
    {
        await (await GetCompress()).CloseEntry();
    }

    /// <summary>
    /// Returns the resource title (does not affect the work of the class).
    /// </summary>
    public async Task<string> GetTitle() => await (await GetCompress()).GetTitle();

    /// <summary>
    /// Returns the archive extension (does not affect the work of the class).
    /// </summary>
    public async Task<string> GetArchiveExtension() => await (await GetCompress()).GetArchiveExtension();

    /// <summary>
    /// Performs the application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        _compress?.Dispose();
    }
}