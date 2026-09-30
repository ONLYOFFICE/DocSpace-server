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

namespace ASC.Web.Files.Services.WCFService.FileOperations;

[ProtoContract]
public record FileBuilderOutputData
{
    [ProtoMember(1)]
    public int? FileId { get; set; }

    [ProtoMember(2)]
    public int? FolderId { get; set; }

    [ProtoMember(3)]
    public string Title { get; set; }
}

[ProtoContract]
public record FileBuilderOperationData<T> : FileOperationData<T>
{
    [ProtoMember(8)]
    public string Script { get; set; }

    [ProtoMember(9)]
    public int? FolderId { get; set; }

    [ProtoMember(10)]
    public Dictionary<string, FileBuilderOutputData> Outputs { get; set; }

    [ProtoMember(11)]
    public string Argument { get; set; }

    public FileBuilderOperationData()
    {

    }

    public FileBuilderOperationData(
        IEnumerable<T> folders,
        IEnumerable<T> files,
        string script,
        int? folderId,
        Dictionary<string, FileBuilderOutputData> outputs,
        string argument,
        int tenantId,
        Guid userId,
        IDictionary<string, string> headers,
        ExternalSessionSnapshot sessionSnapshot,
        bool holdResult = true) : base(folders, files, tenantId, userId, headers, sessionSnapshot, holdResult)
    {
        Script = script;
        FolderId = folderId;
        Outputs = outputs;
        Argument = argument;
    }

    // the script may carry document content, so it stays out of the logged event
    protected override bool PrintMembers(StringBuilder builder)
    {
        if (base.PrintMembers(builder))
        {
            builder.Append(", ");
        }

        builder.Append("FolderId = ").Append(FolderId);

        return true;
    }
}

[Transient]
public class FileBuilderOperation : ComposeFileOperation<FileBuilderOperationData<string>, FileBuilderOperationData<int>>
{
    public override FileOperationType FileOperationType { get; set; } = FileOperationType.Build;
    public FileBuilderOperation() { }
    public FileBuilderOperation(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override Task RunJob(CancellationToken cancellationToken)
    {
        DaoOperation = new FileBuilderOperation<int>(_serviceProvider, Data);
        ThirdPartyOperation = new FileBuilderOperation<string>(_serviceProvider, ThirdPartyData);

        return base.RunJob(cancellationToken);
    }
}

internal class FileBuilderOperation<T>(IServiceProvider serviceProvider, FileBuilderOperationData<T> data) : FileOperation<FileBuilderOperationData<T>, T>(serviceProvider, data)
{
    private readonly FileBuilderOperationData<T> _data = data;
    public override FileOperationType FileOperationType { get; set; } = FileOperationType.Build;

    protected override int InitTotalProgressSteps()
    {
        return Files.Count + Folders.Count > 0 ? 1 : 0;
    }

    protected override async Task DoJob(AsyncServiceScope serviceScope)
    {
        var runner = serviceScope.ServiceProvider.GetRequiredService<DocumentBuilderScriptRunner>();
        List<File<int>> saved = [];

        try
        {
            await runner.RunAsync(_data.Script, _data.FolderId, _data.Outputs, _data.Argument, Headers, saved, CancellationToken);
        }
        catch (Exception e) when (e is not (OperationCanceledException or AuthorizingException or FileNotFoundException or DirectoryNotFoundException))
        {
            // the base job only logs other errors, but the caller has to see why the build failed
            Err = e.Message;
            Logger.ErrorWithException(e);
        }
        finally
        {
            // files saved before a failure are reported too
            Result = string.Join(SplitChar, saved.Select(f => "file_" + f.Id));
            IncrementProgress();
        }
    }
}
