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
public abstract class DocumentBuilderTask<TId, TData> : DistributedTaskProgress
{
    private string _baseUri;
    private int _tenantId;
    protected Guid _userId;
    protected TData _data;
    private readonly IServiceScopeFactory _serviceProvider;

    public TId ResultFileId { get; set; }
    public string ResultFileName { get; set; }
    public string ResultFileUrl { get; set; }

    public DocumentBuilderTask()
    {

    }

    protected DocumentBuilderTask(IServiceScopeFactory serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Init(string baseUri, int tenantId, Guid userId, TData data)
    {
        Init(baseUri, tenantId, userId, data, DocumentBuilderTaskManager.GetTaskId(tenantId, userId));
    }

    public void Init(string baseUri, int tenantId, Guid userId, TData data, string taskId)
    {
        _baseUri = baseUri;
        _tenantId = tenantId;
        _userId = userId;
        _data = data;

        Id = taskId;
        Status = DistributedTaskStatus.Created;

        ResultFileId = default;
        ResultFileName = string.Empty;
        ResultFileUrl = string.Empty;
    }

    protected override async Task DoJob()
    {
        ILogger logger = null;

        try
        {
            CancellationToken.ThrowIfCancellationRequested();

            await using var scope = _serviceProvider.CreateAsyncScope();

            if (!string.IsNullOrEmpty(_baseUri))
            {
                var commonLinkUtility = scope.ServiceProvider.GetService<CommonLinkUtility>();
                commonLinkUtility.ServerUri = _baseUri;
            }

            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(_tenantId);

            var securityContext = scope.ServiceProvider.GetService<SecurityContext>();
            if (_userId != ASC.Core.Configuration.Constants.Guest.ID)
            {
                await securityContext.AuthenticateMeWithoutCookieAsync(_userId);
            }
            else
            {
                securityContext.Logout();
            }
            var filesLinkUtility = scope.ServiceProvider.GetService<FilesLinkUtility>();
            logger = scope.ServiceProvider.GetService<ILogger<DocumentBuilderTask<TId, TData>>>();

            var documentBuilderTask = scope.ServiceProvider.GetService<DocumentBuilderTask>();

            CancellationToken.ThrowIfCancellationRequested();

            var inputData = await GetDocumentBuilderInputDataAsync(scope.ServiceProvider);

            Percentage = 30;

            await PublishChanges();

            CancellationToken.ThrowIfCancellationRequested();

            var fileUri = await documentBuilderTask.BuildFileAsync(inputData, CancellationToken);

            Percentage = 60;

            await PublishChanges();

            CancellationToken.ThrowIfCancellationRequested();

            var file = await ProcessSourceFileAsync(scope.ServiceProvider, new Uri(fileUri), inputData);

            ResultFileId = file.Id;
            ResultFileName = file.Title;
            ResultFileUrl = filesLinkUtility.GetFileWebEditorUrl(file.Id);

            Percentage = 100;

            Status = DistributedTaskStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            Status = DistributedTaskStatus.Canceled;
            throw;
        }
        catch (Exception ex)
        {
            logger?.ErrorWithException(ex);
            Status = DistributedTaskStatus.Failted;
            Exception = ex;
        }
        finally
        {
            IsCompleted = true;
            await PublishChanges();
        }
    }

    protected abstract Task<DocumentBuilderInputData> GetDocumentBuilderInputDataAsync(IServiceProvider serviceProvider);
    protected abstract Task<File<TId>> ProcessSourceFileAsync(IServiceProvider serviceProvider, Uri fileUri, DocumentBuilderInputData inputData);
}

public record DocumentBuilderInputData(string Script, string TempFileName, string OutputFileName);

[Scope]
public class DocumentBuilderTask(DocumentServiceConnector documentServiceConnector)
{
    internal async Task<string> BuildFileAsync(DocumentBuilderInputData inputData, CancellationToken cancellationToken)
    {
        var resultTuple = await documentServiceConnector.DocbuilderRequestAsync(null, inputData.Script, true);

        if (string.IsNullOrEmpty(resultTuple.BuilderKey))
        {
            throw new Exception("DocbuilderRequest: empty Key");
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(1000, cancellationToken);

            resultTuple = await documentServiceConnector.DocbuilderRequestAsync(resultTuple.BuilderKey, null, true);

            if (string.IsNullOrEmpty(resultTuple.BuilderKey))
            {
                throw new Exception("DocbuilderRequest: empty Key");
            }

            if (resultTuple.Urls == null)
            {
                continue;
            }

            if (resultTuple.Urls.Count == 0)
            {
                throw new Exception("DocbuilderRequest: empty Urls");
            }

            if (resultTuple.Urls.ContainsKey(inputData.TempFileName))
            {
                break;
            }
        }

        return resultTuple.Urls[inputData.TempFileName];
    }
}