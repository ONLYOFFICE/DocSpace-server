// (c) Copyright Ascensio System SIA 2010-2023
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
/// Archives the data stream in the format selected in the settings
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

    public async Task<string> GetExt(string ext)
    {
        if (_exts.Contains(ext))
        {
            return ext;
        }

        return await GetArchiveExtension();
    }

    public async Task SetStream(Stream stream)
    {
        await (await GetCompress()).SetStream(stream);
    }

    /// <summary>
    /// The record name is created (the name of a separate file in the archive)
    /// </summary>
    /// <param name="title">File name with extension, this name will have the file in the archive</param>
    /// <param name="lastModification"></param>
    public async Task CreateEntry(string title, DateTime? lastModification = null)
    {
        await (await GetCompress()).CreateEntry(title, lastModification);
    }

    /// <summary>
    /// Transfer the file itself to the archive
    /// </summary>
    /// <param name="readStream">File data</param>
    public async Task PutStream(Stream readStream) => await (await GetCompress()).PutStream(readStream);

    /// <summary>
    /// Put an entry on the output stream.
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
    /// Resource title (does not affect the work of the class)
    /// </summary>
    /// <returns></returns>
    public async Task<string> GetTitle() => await (await GetCompress()).GetTitle();

    /// <summary>
    /// Extension the archive (does not affect the work of the class)
    /// </summary>
    /// <returns></returns>
    public async Task<string> GetArchiveExtension() => await (await GetCompress()).GetArchiveExtension();

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        _compress?.Dispose();
    }
}