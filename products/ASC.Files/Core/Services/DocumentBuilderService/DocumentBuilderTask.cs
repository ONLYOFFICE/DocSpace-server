// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Files.Core.Services.DocumentBuilderService;
public abstract class DocumentBuilderTask<TId, TData>(IServiceScopeFactory serviceProvider) : DistributedTaskProgress
{
    private string _baseUri;
    private int _tenantId;
    protected Guid _userId;
    protected TData _data;
    
    public void Init(string baseUri, int tenantId, Guid userId, TData data)
    {
        _baseUri = baseUri;
        _tenantId = tenantId;
        _userId = userId;
        _data = data;
        
        Id = DocumentBuilderTaskManager.GetTaskId(tenantId, userId);
        Status = DistributedTaskStatus.Created;

        this["ResultFileId"] = default(TId);
        this["ResultFileName"] = string.Empty;
        this["ResultFileUrl"] = string.Empty;
    }

    protected override async Task DoJob()
    {
        ILogger logger = null;

        try
        {
            CancellationToken.ThrowIfCancellationRequested();

            await using var scope = serviceProvider.CreateAsyncScope();

            if (!string.IsNullOrEmpty(_baseUri))
            {
                var commonLinkUtility = scope.ServiceProvider.GetService<CommonLinkUtility>();
                commonLinkUtility.ServerUri = _baseUri;
            }

            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(_tenantId);

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

            this["ResultFileId"] = file.Id;
            this["ResultFileName"] = file.Title;
            this["ResultFileUrl"] = filesLinkUtility.GetFileWebEditorUrl(file.Id);

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