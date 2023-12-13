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

namespace ASC.Files.Core.Services.DocumentBuilderService;

[Transient]
public class DocumentBuilderTask<T>(IServiceScopeFactory serviceProvider) : DistributedTaskProgress
{
    private int _tenantId;
    private Guid _userId;
    private string _script;
    private string _tempFileName;
    private string _outputFileName;

    public void Init(int tenantId, Guid userId, string script, string tempFileName, string outputFileName)
    {
        _tenantId = tenantId;
        _userId = userId;
        _script = script;
        _tempFileName = tempFileName;
        _outputFileName = outputFileName;

        Id = DocumentBuilderTaskManager.GetTaskId(tenantId, userId);
        Status = DistributedTaskStatus.Created;

        this["ResultFileId"] = default(T);
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

            var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(_tenantId);
            var filesLinkUtility = scope.ServiceProvider.GetService<FilesLinkUtility>();
            logger = scope.ServiceProvider.GetService<ILogger<DocumentBuilderTask<T>>>();
            
            var documentBuilderTask = scope.ServiceProvider.GetService<DocumentBuilderTask>();
            
            CancellationToken.ThrowIfCancellationRequested();

            Percentage = 30;

            PublishChanges();

            var fileUri = await documentBuilderTask.BuildFileAsync(CancellationToken, _script, _tempFileName);

            Percentage = 60;

            PublishChanges();

            CancellationToken.ThrowIfCancellationRequested();

            var file = scope.ServiceProvider.GetService<File<T>>();
            file = await documentBuilderTask.SaveFileFromUriAsync(file, new Uri(fileUri), _userId, _outputFileName);
            
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
            PublishChanges();
        }
    }
}

[Scope]
public class DocumentBuilderTask(
    DocumentServiceConnector documentServiceConnector,
    IHttpClientFactory clientFactory,
    IDaoFactory daoFactory,
    SocketManager socketManager)
{
    internal async Task<string> BuildFileAsync(CancellationToken cancellationToken, string script, string fileName)
    {
        var resultTuple = await documentServiceConnector.DocbuilderRequestAsync(null, script, true);

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

            if (resultTuple.Urls != null)
            {
                if (!resultTuple.Urls.Any())
                {
                    throw new Exception("DocbuilderRequest: empty Urls");
                }

                if (resultTuple.Urls.ContainsKey(fileName))
                {
                    break;
                }
            }
        }

        return resultTuple.Urls[fileName];
    }

    internal async Task<File<T>> SaveFileFromUriAsync<T>(File<T> file, Uri sourceUri, Guid userId, string title)
    {            
        file.ParentId = await daoFactory.GetFolderDao<T>().GetFolderIDUserAsync(false, userId);
        file.Title = title;
        
        using var request = new HttpRequestMessage();
        request.RequestUri = sourceUri;

        using var httpClient = clientFactory.CreateClient();
        using var response = await httpClient.SendAsync(request);
        await using var stream = await response.Content.ReadAsStreamAsync();
        
        var fileDao = daoFactory.GetFileDao<T>();

        file = await fileDao.SaveFileAsync(file, stream);
        await socketManager.CreateFileAsync(file);
        return file;
    }
}