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
    ILogger<DocumentBuilderScriptRunner> logger,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    PathProvider pathProvider,
    DocumentServiceConnector documentServiceConnector,
    FileConverter fileConverter,
    EntryManager entryManager,
    FilesMessageService filesMessageService,
    WebhookManager webhookManager)
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
    /// The name a script saves a result under, the second argument of SaveFile. It is the key the caller uses to say
    /// where that result goes, so only a literal one can be matched; a name assembled at run time is invisible here
    /// and its result falls back to the default folder.
    /// </summary>
    private static readonly Regex _saveFileCall = new(
        """\.\s*SaveFile\s*\(\s*(?<q1>["'])[^"']*\k<q1>\s*,\s*(?<q2>["'])(?<name>[^"']*)\k<q2>""",
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
    /// The portal files and folders a run touches: the files the script opens or replaces and the folders it saves
    /// into. Read off the request only; <see cref="ValidateAsync"/> checks them.
    /// </summary>
    public static (List<int> Files, List<int> Folders) GetEntries(string script, int? folderId, Dictionary<string, FileBuilderOutputData> outputs)
    {
        List<int> files = [];
        List<int> folders = [];

        if (!string.IsNullOrEmpty(script))
        {
            foreach (Match match in _openFileCall.Matches(script))
            {
                if (int.TryParse(match.Groups["id"].Value, CultureInfo.InvariantCulture, out var id))
                {
                    files.Add(id);
                }
            }
        }

        foreach (var output in outputs?.Values ?? Enumerable.Empty<FileBuilderOutputData>())
        {
            if (output?.FileId is { } fileId)
            {
                files.Add(fileId);
            }

            if (output?.FolderId is { } outputFolderId)
            {
                folders.Add(outputFolderId);
            }
        }

        if (folderId.HasValue)
        {
            folders.Add(folderId.Value);
        }

        return ([.. files.Distinct()], [.. folders.Distinct()]);
    }

    /// <summary>
    /// Checks everything that can be refused without building. The web side calls it to fail fast; the worker
    /// checks again when it runs.
    /// </summary>
    public async Task ValidateAsync(string script, int? folderId, Dictionary<string, FileBuilderOutputData> outputs, string argument)
    {
        CheckScript(script);
        CheckArgument(argument);

        await ResolveOutputsAsync(script, outputs);

        var (_, source) = await ResolveFilesAsync(script, publishCopies: false);

        if (folderId is null && source is null)
        {
            var homeless = _saveFileCall.Matches(script)
                .Select(match => match.Groups["name"].Value)
                .FirstOrDefault(name => outputs is null || !outputs.ContainsKey(name));

            if (homeless != null)
            {
                throw new ArgumentException($"\"{homeless}\" has nowhere to go: name a folder for it, or one for the request", nameof(outputs));
            }

            if (outputs is not { Count: > 0 })
            {
                throw new ArgumentException("The script opens no file and the request names no folder, so there is nowhere to save the result", nameof(folderId));
            }
        }
    }

    /// <summary>
    /// Runs <paramref name="script"/> and puts each file it produced where <paramref name="outputs"/> says, keyed by
    /// the name the script saved it under: over an existing file as a new version of it, or into a folder as a new
    /// file. A produced file the map does not mention goes to <paramref name="folderId"/>, and failing that to the
    /// folder of the file the script opened. Saved files are added to <paramref name="saved"/> one by one, so a run
    /// that fails part way still reports them.
    /// </summary>
    public async Task RunAsync(
        string script,
        int? folderId,
        Dictionary<string, FileBuilderOutputData> outputs,
        string argument,
        IDictionary<string, StringValues> headers,
        List<File<int>> saved,
        CancellationToken cancellationToken)
    {
        CheckScript(script);
        CheckArgument(argument);

        var targets = await ResolveOutputsAsync(script, outputs);

        var (prepared, source) = await ResolveFilesAsync(script, publishCopies: true);

        var urls = await BuildAsync(prepared, argument, cancellationToken);

        await SaveAsync(urls, targets, source, folderId, headers, saved);
    }

    private static void CheckArgument(string argument)
    {
        if (argument == null)
        {
            return;
        }

        var root = JsonSerializer.Deserialize<JsonElement>(argument);

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("The argument must be a JSON object", nameof(argument));
        }

        if (HasAddress(root))
        {
            throw new ArgumentException("The argument must not carry an address", nameof(argument));
        }
    }

    // decoded values, so a JSON escape does not hide an address
    private static bool HasAddress(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => _absoluteUrl.IsMatch(element.GetString()),
            JsonValueKind.Object => element.EnumerateObject().Any(property => _absoluteUrl.IsMatch(property.Name) || HasAddress(property.Value)),
            JsonValueKind.Array => element.EnumerateArray().Any(HasAddress),
            _ => false
        };
    }

    private static void CheckScript(string script)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(script);

        // The portal resolves the files a script may touch, so an address written out by hand would step around that.
        // This is a guard rail rather than a boundary: the script is arbitrary code and can build an address it never
        // spells out. What keeps the caller honest is the access check, not this.
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
    /// Checks every file the script opens and, when <paramref name="publishCopies"/> is set, replaces its identifier
    /// with an address the document service can download it from. Answers with the script and the file it opened first.
    /// </summary>
    private async Task<(string Script, File<int> Source)> ResolveFilesAsync(string script, bool publishCopies)
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

            logger.DebugScriptOpensFile(fileId);

            if (publishCopies)
            {
                prepared.Remove(id.Index, id.Length).Insert(id.Index, await GetBuilderUrlAsync(fileDao, file));
            }

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

    private async Task<Dictionary<string, string>> BuildAsync(string script, string argument, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(script));

        // Started asynchronously and polled by the issued key, as DocumentBuilderTask does. No key of our own: the
        // service reads it as a poll for a build it never started and answers "cannot read run file".
        var (key, urls) = await documentServiceConnector.DocbuilderRequestFromFileAsync(stream, ScriptFileName, new BuilderFromFileBody { Async = true, Argument = argument == null ? null : JsonSerializer.Deserialize<JsonElement>(argument) });

        while (urls == null)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new InvalidOperationException("The document service did not hand back a key for the build");
            }

            await Task.Delay(1000, cancellationToken);

            (key, urls) = await documentServiceConnector.DocbuilderRequestAsync(key, null, true);
        }

        if (urls.Count == 0)
        {
            throw new ArgumentException("The script produced no file. It has to save one with SaveFile");
        }

        logger.InformationScriptProducedFiles(urls.Count);

        return urls;
    }

    /// <summary>
    /// Refuses a destination keyed by a name the script never saves under. The key is the second argument of
    /// SaveFile, so a key that matches none of them can never be used and is a typo in the request.
    /// </summary>
    /// <summary>
    /// Turns each requested destination into a loaded target, refusing anything that cannot work before the build
    /// runs: a key that names no saved file, a slot that is neither a file to replace nor a folder to fill, a title
    /// on a replacement, a format that does not match the file it would become a version of, or a place the caller
    /// may not write. The loaded files and folders are returned so the save does not read them again.
    /// </summary>
    private async Task<Dictionary<string, OutputTarget>> ResolveOutputsAsync(string script, Dictionary<string, FileBuilderOutputData> outputs)
    {
        var targets = new Dictionary<string, OutputTarget>(StringComparer.Ordinal);
        if (outputs is not { Count: > 0 })
        {
            return targets;
        }

        var saved = _saveFileCall.Matches(script)
            .Select(match => match.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var (name, output) in outputs)
        {
            if (!saved.Contains(name))
            {
                throw new ArgumentException($"The script saves no file named \"{name}\"", nameof(outputs));
            }

            if (output == null || output.FileId.HasValue == output.FolderId.HasValue)
            {
                throw new ArgumentException($"\"{name}\" has to name either a file to replace or a folder to save into", nameof(outputs));
            }

            if (output.FileId.HasValue)
            {
                if (!string.IsNullOrEmpty(output.Title))
                {
                    throw new ArgumentException($"\"{name}\" replaces a file, which keeps its own title", nameof(outputs));
                }

                var file = await daoFactory.GetFileDao<int>().GetFileAsync(output.FileId.Value).NotFoundIfNull("File not found");

                if (!await fileSecurity.CanEditAsync(file))
                {
                    throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
                }

                var produced = FileUtility.GetFileExtension(name);
                var current = FileUtility.GetFileExtension(file.Title);
                if (!produced.Equals(current, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"\"{name}\" is a {produced} file and cannot become a version of a {current} one", nameof(outputs));
                }

                targets[name] = new OutputTarget(file, null, null);
                continue;
            }

            var folder = await daoFactory.GetFolderDao<int>().GetFolderAsync(output.FolderId.Value).NotFoundIfNull("Folder not found");

            if (!await fileSecurity.CanCreateAsync(folder))
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_Create);
            }

            targets[name] = new OutputTarget(null, folder, output.Title);
        }

        return targets;
    }

    /// <summary>
    /// Puts each produced file where <see cref="ResolveOutputsAsync"/> settled it: a new version of an existing file,
    /// or a new file in a folder. A produced file the request did not mention falls back to the folder it named, and
    /// failing that to the folder of the file the script opened.
    /// </summary>
    private async Task SaveAsync(
        Dictionary<string, string> urls,
        Dictionary<string, OutputTarget> targets,
        File<int> source,
        int? folderId,
        IDictionary<string, StringValues> headers,
        List<File<int>> saved)
    {
        foreach (var (name, url) in urls)
        {
            var extension = FileUtility.GetFileExtension(name);

            if (targets.TryGetValue(name, out var target))
            {
                if (target.File != null)
                {
                    var file = await entryManager.SaveEditingAsync(target.File.Id, extension, url, null, keepLink: true);
                    await filesMessageService.SendAsync(MessageAction.FileUpdated, file, headers, file.Title);
                    await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, file);
                    saved.Add(file);
                    continue;
                }

                var chosen = string.IsNullOrEmpty(target.Title) ? name : target.Title;
                saved.Add(await fileConverter.SaveConvertedFileAsync(target.Folder, url, extension, chosen, updateIfExist: false));
                continue;
            }

            var parentId = folderId ?? source?.ParentId
                ?? throw new ArgumentException($"\"{name}\" has nowhere to go: name a folder for it, or one for the request");

            var folder = await daoFactory.GetFolderDao<int>().GetFolderAsync(parentId).NotFoundIfNull("Folder not found");

            saved.Add(await fileConverter.SaveConvertedFileAsync(folder, url, extension, name, updateIfExist: false));
        }
    }

    /// <summary>A produced file's settled destination: a file to replace, or a folder and the title to save under.</summary>
    private sealed record OutputTarget(File<int> File, Folder<int> Folder, string Title);
}
