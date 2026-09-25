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


namespace ASC.Files.Core.Services.DocumentBuilderService;

/// <summary>
/// Runs a document builder script written by a caller against the files of this portal. The script addresses a file by
/// its portal identifier, which is resolved here into an address the document service can download, once the caller has
/// been shown to have access to it.
/// </summary>
[Scope]
public class DocumentBuilderScriptRunner(
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    PathProvider pathProvider,
    DocumentServiceConnector documentServiceConnector,
    FileConverter fileConverter,
    FileStorageService fileStorageService)
{
    /// <summary>
    /// A call that opens a portal file, with the identifier the caller wrote in place of the address. Both objects the
    /// document builder exposes are accepted, and so is the temporary-file flavour used when a script compares two
    /// documents.
    /// </summary>
    private static readonly Regex _openFileCall = new(
        """\b(?:builder|builderJS)\s*\.\s*(?<method>OpenFile|OpenTmpFile)\s*\(\s*(?<quote>["'])(?<id>[^"']*)\k<quote>""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// An address written out in the script. Only the two schemes are looked for: the bare "//" of a scheme-relative
    /// address is also how the document builder starts a comment, and rejecting it would reject ordinary scripts.
    /// </summary>
    private static readonly Regex _absoluteUrl = new(
        "https?://",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    /// <summary>
    /// The same call on any receiver and with anything at all between the brackets. Counting these against the calls
    /// that name a file literally on the builder itself is what catches both an address assembled at run time and the
    /// builder reached through a name of its own, neither of which the literal pattern above would ever see.
    /// </summary>
    private static readonly Regex _anyOpenFileCall = new(
        """\.\s*(?:OpenFile|OpenTmpFile)\s*\(""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// A call that creates a new file, on any receiver and whatever the format looks like. A script that neither
    /// creates a file nor opens one has no document to work on, and the document builder reports that only later, when
    /// the save finds nothing open.
    /// </summary>
    private static readonly Regex _anyCreateFileCall = new(
        """\.\s*CreateFile\s*\(""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Reaching the builder object by index, which is how the name of a method is hidden from the patterns above.
    /// </summary>
    private static readonly Regex _builderIndexer = new(
        """\b(?:builder|builderJS)\s*\[""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// The format a script asks a new file to be created in. Only a quoted format is looked at, because the builder
    /// also takes the numeric format codes, and one written without quotes is left for it to judge.
    /// </summary>
    private static readonly Regex _createFileCall = new(
        """\b(?:builder|builderJS)\s*\.\s*CreateFile\s*\(\s*(?<quote>["'])(?<type>[^"']*)\k<quote>""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private const string ScriptFileName = "script.docbuilder";

    /// <summary>
    /// Runs <paramref name="script"/> and saves what it produced. With <paramref name="overwrite"/> the result is
    /// written back over the file the script opened, as a new version of it; otherwise every produced file is saved as
    /// a new file, beside the opened one or in <paramref name="folderId"/> when one is named.
    /// </summary>
    public async Task<List<File<int>>> RunAsync(string script, int? folderId, bool overwrite)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(script);

        // The portal resolves the files a script may touch, so an address written out by hand would step around that.
        // This is a guard rail rather than a boundary: the script is arbitrary code and can build an address it never
        // spells out. What keeps the caller honest is the access check below, not this.
        if (_absoluteUrl.IsMatch(script))
        {
            throw new ArgumentException("The script must address a portal file by its identifier, not by an address", nameof(script));
        }

        if (_builderIndexer.IsMatch(script))
        {
            throw new ArgumentException("The script must call the document builder by name, not through an indexer", nameof(script));
        }

        CheckCreatedFormats(script);

        // Every file the script opens has to be named by a literal identifier on the builder itself, or the portal
        // cannot tell which file it is about to hand over. A call whose argument is put together at run time, or one
        // made through a name the script gave the builder, is refused rather than passed on.
        if (_anyOpenFileCall.Matches(script).Count != _openFileCall.Matches(script).Count)
        {
            throw new ArgumentException("Every OpenFile call must be made on the document builder and name a portal file by a literal identifier", nameof(script));
        }

        if (!_anyOpenFileCall.IsMatch(script) && !_anyCreateFileCall.IsMatch(script))
        {
            throw new ArgumentException("The script has to create a file with CreateFile or open one with OpenFile", nameof(script));
        }

        var (prepared, source) = await ResolveFilesAsync(script);

        var parentId = await GetTargetFolderIdAsync(source, folderId, overwrite);

        var urls = await BuildAsync(prepared);

        return overwrite
            ? [await OverwriteAsync(source, urls)]
            : await SaveBesideAsync(parentId, urls);
    }

    /// <summary>
    /// Refuses a script that asks for a new file in a format that is plainly the identifier of an existing one.
    /// CreateFile takes the format of the file to create, and a caller who reads it as the file to open gets a bare
    /// document service error instead of an answer. Only an all-digit format is refused, so that a format this
    /// document service knows and this portal has never heard of still reaches it.
    /// </summary>
    private static void CheckCreatedFormats(string script)
    {
        var identifier = _createFileCall.Matches(script)
            .Select(match => match.Groups["type"].Value)
            .FirstOrDefault(type => type.Length > 0 && type.All(char.IsAsciiDigit));

        if (identifier != null)
        {
            throw new ArgumentException($"CreateFile expects the format of the file to create, such as docx, and not a file identifier, got \"{identifier}\"", nameof(script));
        }
    }

    /// <summary>
    /// Replaces every identifier the script opens with an address the document service can download it from, and
    /// answers with the rewritten script and the file the script opened first.
    /// </summary>
    private async Task<(string Script, File<int> Source)> ResolveFilesAsync(string script)
    {
        var fileDao = daoFactory.GetFileDao<int>();
        var prepared = new StringBuilder(script);
        File<int> source = null;

        // Walked backwards so that replacing one call does not move the offsets of the calls before it. The first call
        // of the script is therefore the last one seen, which is what leaves it in source.
        foreach (var match in _openFileCall.Matches(script).Reverse())
        {
            var id = match.Groups["id"];

            if (!int.TryParse(id.Value, CultureInfo.InvariantCulture, out var fileId))
            {
                throw new ArgumentException($"{match.Groups["method"].Value} expects the identifier of a portal file, got \"{id.Value}\"", nameof(script));
            }

            var file = await fileDao.GetFileAsync(fileId).NotFoundIfNull("File not found");

            if (!await fileSecurity.CanReadAsync(file))
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
            }

            prepared.Remove(id.Index, id.Length).Insert(id.Index, await GetBuilderUrlAsync(fileDao, file));

            if (match.Groups["method"].Value == "OpenFile")
            {
                source = file;
            }
        }

        return (prepared.ToString(), source);
    }

    /// <summary>
    /// Publishes the content of a file to the portal temporary storage and answers with the address the document
    /// builder downloads it from. The stream address of a file cannot be used here: it asks the caller for the
    /// signature header the document service adds to its own requests, and the builder, which fetches the file itself,
    /// sends no such header. The temporary address is authorised by its key alone, and the portal drops the copy as
    /// soon as it has been read.
    /// </summary>
    private async Task<string> GetBuilderUrlAsync(IFileDao<int> fileDao, File<int> file)
    {
        await using var stream = await fileDao.GetFileStreamAsync(file);

        var url = await pathProvider.GetTempUrlAsync(stream, FileUtility.GetFileExtension(file.Title));

        return documentServiceConnector.ReplaceCommunityAddress(url);
    }

    private async Task<int> GetTargetFolderIdAsync(File<int> source, int? folderId, bool overwrite)
    {
        if (overwrite)
        {
            if (source == null)
            {
                throw new ArgumentException("A script that updates its source has to open one with OpenFile");
            }

            if (!await fileSecurity.CanEditAsync(source))
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
            }

            return source.ParentId;
        }

        if (folderId.HasValue)
        {
            return folderId.Value;
        }

        if (source == null)
        {
            throw new ArgumentException("A script that opens no file has to be told the folder to save its result in");
        }

        return source.ParentId;
    }

    private async Task<Dictionary<string, string>> BuildAsync(string script)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(script));

        // No key: the document service issues one itself and reads a key in the request as a poll for a build it is
        // already running. Sending one of our own makes it look for a script it never stored, which it reports as
        // "cannot read run file".
        var (_, urls) = await documentServiceConnector.DocbuilderRequestFromFileAsync(stream, ScriptFileName, new BuilderFromFileBody());

        if (urls is not { Count: > 0 })
        {
            throw new ArgumentException("The script produced no file. It has to save one with SaveFile");
        }

        return urls;
    }

    private async Task<File<int>> OverwriteAsync(File<int> source, Dictionary<string, string> urls)
    {
        if (urls.Count > 1)
        {
            throw new ArgumentException("A script that updates its source has to produce exactly one file");
        }

        var (name, url) = urls.First();

        return await fileStorageService.SaveEditingAsync(source.Id, FileUtility.GetFileExtension(name), url, null);
    }

    private async Task<List<File<int>>> SaveBesideAsync(int parentId, Dictionary<string, string> urls)
    {
        var folder = await daoFactory.GetFolderDao<int>().GetFolderAsync(parentId).NotFoundIfNull("Folder not found");

        var saved = new List<File<int>>(urls.Count);

        foreach (var (name, url) in urls)
        {
            saved.Add(await fileConverter.SaveConvertedFileAsync(folder, url, FileUtility.GetFileExtension(name), name, updateIfExist: false));
        }

        return saved;
    }
}
