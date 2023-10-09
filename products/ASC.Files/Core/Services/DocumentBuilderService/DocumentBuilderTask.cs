// (c) Copyright Ascensio System SIA 2010-2022
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

[Scope]
public class DocumentBuilderTask<T> : DistributedTaskProgress
{
    private readonly IServiceProvider _serviceProvider;

    private int _tenantId;
    private Guid _userId;
    private string _script;
    private string _tempFileName;
    private string _outputFileName;

    public DocumentBuilderTask(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

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

            await using var scope = _serviceProvider.CreateAsyncScope();

            var scopeClass = scope.ServiceProvider.GetService<DocumentBuilderTaskScope>();

            var (tenantManager, documentServiceConnector, clientFactory, daoFactory, filesLinkUtility, log) = scopeClass;

            logger = log;

            await tenantManager.SetCurrentTenantAsync(_tenantId);

            CancellationToken.ThrowIfCancellationRequested();

            Percentage = 30;

            PublishChanges();

            var fileUri = await BuildFileAsync(documentServiceConnector, CancellationToken, _script, _tempFileName);

            Percentage = 60;

            PublishChanges();

            CancellationToken.ThrowIfCancellationRequested();

            var file = scope.ServiceProvider.GetService<File<T>>();

            file.ParentId = await daoFactory.GetFolderDao<T>().GetFolderIDUserAsync(false, _userId);
            file.Title = _outputFileName;

            file = await SaveFileFromUriAsync(clientFactory, daoFactory, new Uri(fileUri), file);

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

    private static async Task<string> BuildFileAsync(DocumentServiceConnector documentServiceConnector, CancellationToken cancellationToken, string script, string fileName)
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

    private static async Task<File<T>> SaveFileFromUriAsync(IHttpClientFactory clientFactory, IDaoFactory daoFactory, Uri sourceUri, File<T> destinationFile)
    {
        using var request = new HttpRequestMessage();

        request.RequestUri = sourceUri;

        var httpClient = clientFactory.CreateClient();

        using var response = await httpClient.SendAsync(request);

        using var stream = await response.Content.ReadAsStreamAsync();

        var _fileDao = daoFactory.GetFileDao<T>();

        var file = await _fileDao.SaveFileAsync(destinationFile, stream);

        return file;
    }
}

[Scope]
public class DocumentBuilderTaskScope
{
    private readonly TenantManager _tenantManager;
    private readonly DocumentServiceConnector _documentServiceConnector;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IDaoFactory _daoFactory;
    private readonly FilesLinkUtility _filesLinkUtility;
    private readonly ILogger _logger;

    public DocumentBuilderTaskScope(TenantManager tenantManager, DocumentServiceConnector documentServiceConnector, IHttpClientFactory clientFactory, IDaoFactory daoFactory, FilesLinkUtility filesLinkUtility, ILogger<DocumentBuilderTaskScope> logger)
    {
        _tenantManager = tenantManager;
        _documentServiceConnector = documentServiceConnector;
        _clientFactory = clientFactory;
        _daoFactory = daoFactory;
        _filesLinkUtility = filesLinkUtility;
        _logger = logger;
    }

    public void Deconstruct(out TenantManager tenantManager, out DocumentServiceConnector documentServiceConnector, out IHttpClientFactory clientFactory, out IDaoFactory daoFactory, out FilesLinkUtility filesLinkUtility, out ILogger logger)
    {
        tenantManager = _tenantManager;
        documentServiceConnector = _documentServiceConnector;
        clientFactory = _clientFactory;
        daoFactory = _daoFactory;
        filesLinkUtility = _filesLinkUtility;
        logger = _logger;
    }
}