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

using DriveFile = Google.Apis.Drive.v3.Data.File;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ASC.Files.Thirdparty.GoogleDrive;

[Transient(typeof(IThirdPartyStorage<DriveFile, DriveFile, DriveFile>))]
internal class GoogleDriveStorage(
        FileUtility fileUtility,
        ILoggerProvider monitor,
        TempStream tempStream,
        OAuth20TokenHelper oAuth20TokenHelper,
        IHttpClientFactory clientFactory)
    : IThirdPartyStorage<DriveFile, DriveFile, DriveFile>, IGoogleDriveItemStorage<DriveFile>, IDisposable
{
    public bool IsOpened { get; private set; }
    public AuthScheme AuthScheme => AuthScheme.OAuth;
    private string AccessToken
    {
        get
        {
            if (_token == null)
            {
                throw new Exception("Cannot create GoogleDrive session with given token");
            }

            if (_token.IsExpired)
            {
                _token = oAuth20TokenHelper.RefreshToken<GoogleLoginProvider>(_token);
            }

            return _token.AccessToken;
        }
    }

    private const long MaxChunkedUploadFileSize = 2L * 1024L * 1024L * 1024L;
    private readonly ILogger _logger = monitor.CreateLogger("ASC.Files");
    private DriveService _driveService;
    private OAuth20Token _token;
    
    private static readonly Lazy<CachedHttpClientFactory> _cachedHttpClientFactory = new(() => new CachedHttpClientFactory());

    public void Open(AuthData authData)
    {
        if (IsOpened)
        {
            return;
        }

        _token = authData.Token ?? throw new UnauthorizedAccessException("Cannot create GoogleDrive session with given token");

        var tokenResponse = new TokenResponse
        {
            AccessToken = _token.AccessToken,
            RefreshToken = _token.RefreshToken,
            IssuedUtc = _token.Timestamp,
            ExpiresInSeconds = _token.ExpiresIn,
            TokenType = "Bearer"
        };

        var apiCodeFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _token.ClientID,
                ClientSecret = _token.ClientSecret
            },
            Scopes = new[] { DriveService.Scope.Drive }
        });

        _driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = new UserCredential(apiCodeFlow, string.Empty, tokenResponse),
            HttpClientFactory = _cachedHttpClientFactory.Value
        });

        IsOpened = true;
    }

    public void Close()
    {
        IsOpened = false;
    }

    public async Task<long> GetFileSizeAsync(DriveFile file)
    {
        var ext = MimeMapping.GetExtention(file.MimeType);
        if (!GoogleLoginProvider.GoogleDriveExt.Contains(ext))
        {
            return file.Size ?? 0;
        }

        using var response = await SendDownloadRequestAsync(file, HttpCompletionOption.ResponseHeadersRead);
        return response.Content.Headers.ContentLength ?? 0;
    }
    
    public async Task<bool> CheckAccessAsync()
    {
        try
        {
            var rootFolder = await GetFolderAsync("root");
            return !string.IsNullOrEmpty(rootFolder.Id);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public Task<DriveFile> CreateFileAsync(Stream fileStream, string title, string parentId)
    {
        return InsertEntryAsync(fileStream, title, parentId);
    }

    public Task<DriveFile> CopyFileAsync(string fileId, string newFileName, string toFolderId)
    {
        return InternalCopyFileAsync(toFolderId, fileId, newFileName);
    }

    public async Task<DriveFile> SaveStreamAsync(string fileId, Stream fileStream)
    {
        var file = await GetFileAsync(fileId);
        return await SaveStreamAsync(fileId, fileStream, file.Name);
    }

    public Task<DriveFile> GetFileAsync(string fileId)
    {
        return GetItemAsync(fileId);
    }

    public async Task<Stream> DownloadStreamAsync(DriveFile file, int offset = 0)
    {
        ArgumentNullException.ThrowIfNull(file);

        var response = await SendDownloadRequestAsync(file);

        if (response.Content.Headers.ContentLength.HasValue)
        {
            file.Size = response.Content.Headers.ContentLength.Value;
        }

        if (offset == 0 && file.Size is > 0)
        {
            return new ResponseStream(await response.Content.ReadAsStreamAsync(), file.Size.Value);
        }

        var tempBuffer = tempStream.Create();
        await using var str = await response.Content.ReadAsStreamAsync();
        await str.CopyToAsync(tempBuffer);
        await tempBuffer.FlushAsync();
        tempBuffer.Seek(offset, SeekOrigin.Begin);

        return tempBuffer;
    }

    private async Task<HttpResponseMessage> SendDownloadRequestAsync(DriveFile file, 
        HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead)
    {
        var downloadArg = $"{file.Id}?alt=media";

        var ext = MimeMapping.GetExtention(file.MimeType);
        if (GoogleLoginProvider.GoogleDriveExt.Contains(ext))
        {
            var internalExt = fileUtility.GetGoogleDownloadableExtension(ext);
            var requiredMimeType = MimeMapping.GetMimeMapping(internalExt);

            downloadArg = $"{file.Id}/export?mimeType={HttpUtility.UrlEncode(requiredMimeType)}";
        }

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(GoogleLoginProvider.GoogleUrlFile + downloadArg),
            Method = HttpMethod.Get
        };
        
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

        var httpClient = clientFactory.CreateClient();
        var response = await httpClient.SendAsync(request, completionOption);
        return response;
    }

    public async Task<Stream> GetThumbnailAsync(string fileId, int width, int height)
    {
        try
        {
            var url = $"https://lh3.google.com/u/0/d/{fileId}=w{width}-h{height}-p-k-nu-iv1";
            var httpClient = _driveService.HttpClient;
            var response = await httpClient.GetAsync(url);
            return await response.Content.ReadAsStreamAsync();
        }
        catch (Exception)
        {
            return null;
        }
    }

    public Task<DriveFile> RenameFileAsync(string fileId, string newName)
    {
        return RenameEntryAsync(fileId, newName);
    }

    public Task<DriveFile> MoveFileAsync(string fileId, string newFileName, string toFolderId)
    {
        return MoveEntryAsync(fileId, newFileName, toFolderId);
    }

    public Task<DriveFile> CreateFolderAsync(string title, string parentId)
    {
        return InsertEntryAsync(null, title, parentId, true);
    }

    public Task<DriveFile> CopyFolderAsync(string folderId, string newFolderName, string toFolderId)
    {
        return InternalCopyFolderAsync(folderId, newFolderName, toFolderId);
    }

    public Task<DriveFile> GetFolderAsync(string folderId)
    {
        return GetItemAsync(folderId);
    }

    public Task<DriveFile> RenameFolderAsync(string folderId, string newName)
    {
        return RenameEntryAsync(folderId, newName);
    }

    public Task<DriveFile> MoveFolderAsync(string folderId, string newFolderName, string toFolderId)
    {
        return MoveEntryAsync(folderId, newFolderName, toFolderId);
    }

    public Task<List<DriveFile>> GetItemsAsync(string folderId)
    {
        return GetItemsInternalAsync(folderId);
    }

    public Task<List<DriveFile>> GetItemsAsync(string folderId, bool? folders)
    {
        return GetItemsInternalAsync(folderId, folders);
    }

    public Task DeleteItemAsync(DriveFile entry)
    {
        return _driveService.Files.Delete(entry.Id).ExecuteAsync();
    }

    public async Task<long> GetMaxUploadSizeAsync()
    {
        var request = _driveService.About.Get();
        request.Fields = "maxUploadSize";
        var about = await request.ExecuteAsync();

        return about.MaxUploadSize ?? MaxChunkedUploadFileSize;
    }

    public void Dispose() { }

    public static DriveFile FileConstructor(string title = null, string mimeType = null, string folderId = null)
    {
        var file = new DriveFile();

        if (!string.IsNullOrEmpty(title))
        {
            file.Name = title;
        }

        if (!string.IsNullOrEmpty(mimeType))
        {
            file.MimeType = mimeType;
        }

        if (!string.IsNullOrEmpty(folderId))
        {
            file.Parents = new List<string> { folderId };
        }

        return file;
    }

    public async ValueTask<RenewableUploadSession> CreateRenewableSessionAsync(DriveFile driveFile, long contentLength)
    {
        ArgumentNullException.ThrowIfNull(driveFile);

        var fileId = string.Empty;
        var method = "POST";
        var body = string.Empty;
        var folderId = driveFile.Parents.FirstOrDefault();

        if (driveFile.Id != null)
        {
            fileId = "/" + driveFile.Id;
            method = "PATCH";
        }
        else
        {
            var titleData = !string.IsNullOrEmpty(driveFile.Name) ? $"\"name\":\"{driveFile.Name}\"" : "";
            var parentData = !string.IsNullOrEmpty(folderId) ? $",\"parents\":[\"{folderId}\"]" : "";

            body = !string.IsNullOrEmpty(titleData + parentData) ? "{" + titleData + parentData + "}" : "";
        }

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(GoogleLoginProvider.GoogleUrlFileUpload + fileId + "?uploadType=resumable"),
            Method = new HttpMethod(method)
        };
        
        request.Headers.Add("X-Upload-Content-Type", MimeMapping.GetMimeMapping(driveFile.Name));
        request.Headers.Add("X-Upload-Content-Length", contentLength.ToString(CultureInfo.InvariantCulture));
        request.Headers.Add("Authorization", "Bearer " + AccessToken);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var httpClient = clientFactory.CreateClient();
        using var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(response.ReasonPhrase);
        }

        var uploadSession = new RenewableUploadSession(driveFile.Id, folderId, contentLength)
        {
            Location = response.Headers.Location?.ToString(),
            Status = RenewableUploadSessionStatus.Started
        };

        return uploadSession;
    }

    public async ValueTask TransferAsync(RenewableUploadSession googleDriveSession, Stream stream, long chunkLength, bool lastChunk)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (googleDriveSession.Status != RenewableUploadSessionStatus.Started)
        {
            throw new InvalidOperationException("Can't upload chunk for given upload session.");
        }

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(googleDriveSession.Location),
            Method = HttpMethod.Put
        };
        request.Headers.Add("Authorization", "Bearer " + AccessToken);
        request.Content = new StreamContent(stream);
        if (googleDriveSession.BytesToTransfer > 0)
        {
            request.Content.Headers.ContentRange = new ContentRangeHeaderValue(
                                                       googleDriveSession.BytesTransferred,
                                                       googleDriveSession.BytesTransferred + chunkLength - 1,
                                                       googleDriveSession.BytesToTransfer);
        }
        else
        {
            var bytesToTransfer = googleDriveSession.BytesTransferred + chunkLength;
            
            if (lastChunk)
            {

                request.Content.Headers.ContentRange = new ContentRangeHeaderValue(
                                               googleDriveSession.BytesTransferred,
                                               bytesToTransfer - 1,
                                               bytesToTransfer);
            }
            else
            {
                request.Content.Headers.ContentRange = new ContentRangeHeaderValue(
                                               googleDriveSession.BytesTransferred,
                                               bytesToTransfer - 1);
            }
        }
        var httpClient = clientFactory.CreateClient();
        HttpResponseMessage response;

        try
        {
            response = await httpClient.SendAsync(request);
        }
        catch (Exception exception) // todo create catch
        {
            _logger.ErrorWithException(exception);
            throw;
        }

        if (response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.OK)
        {
            googleDriveSession.BytesTransferred += chunkLength;

            {
                var locationHeader = response.Headers.Location;

                if (locationHeader != null)
                {
                    googleDriveSession.Location = locationHeader.ToString();
                }
            }
        }
        else
        {
            googleDriveSession.BytesTransferred += chunkLength;
            googleDriveSession.Status = RenewableUploadSessionStatus.Completed;

            var responseString =  await response.Content.ReadAsStringAsync();
            var responseJson = JObject.Parse(responseString);

            googleDriveSession.FileId = responseJson.Value<string>("id");
        }
    }
    
    public IDataWriteOperator CreateDataWriteOperator(CommonChunkedUploadSession chunkedUploadSession, CommonChunkedUploadSessionHolder sessionHolder)
    {
        return new ChunkZipWriteOperator(tempStream, chunkedUploadSession, sessionHolder);
    }

    private async Task<DriveFile> InternalCopyFileAsync(string toFolderId, string originEntryId, string newTitle)
    {
        var body = FileConstructor(folderId: toFolderId, title: newTitle);
        try
        {
            var request = _driveService.Files.Copy(body, originEntryId);
            request.Fields = GoogleLoginProvider.FilesFields;

            return await request.ExecuteAsync();
        }
        catch (GoogleApiException ex)
        {
            if (ex.HttpStatusCode == HttpStatusCode.Forbidden)
            {
                throw new SecurityException(ex.Error.Message);
            }
            throw;
        }
    }

    private async Task<DriveFile> GetItemAsync(string itemId)
    {
        try
        {
            var request = _driveService.Files.Get(itemId);
            request.Fields = GoogleLoginProvider.FilesFields;

            return await request.ExecuteAsync();
        }
        catch (GoogleApiException ex)
        {
            if (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            throw;
        }
    }

    private async Task<List<DriveFile>> GetItemsInternalAsync(string folderId, bool? folders = null)
    {
        var request = _driveService.Files.List();

        var query = "'" + folderId + "' in parents and trashed=false";

        if (folders.HasValue)
        {
            query += " and mimeType " + (folders.Value ? "" : "!") + "= '" + GoogleLoginProvider.GoogleDriveMimeTypeFolder + "'";
        }

        request.Q = query;

        request.Fields = "nextPageToken, files(" + GoogleLoginProvider.FilesFields + ")";

        var files = new List<DriveFile>();
        do
        {
            try
            {
                var fileList = await request.ExecuteAsync();

                files.AddRange(fileList.Files);

                request.PageToken = fileList.NextPageToken;
            }
            catch (Exception)
            {
                request.PageToken = null;
            }
        } while (!string.IsNullOrEmpty(request.PageToken));

        return files;
    }

    private async Task<DriveFile> InsertEntryAsync(Stream fileStream, string title, string parentId, bool folder = false)
    {
        var mimeType = folder ? GoogleLoginProvider.GoogleDriveMimeTypeFolder : MimeMapping.GetMimeMapping(title);

        var body = FileConstructor(title, mimeType, parentId);

        if (folder)
        {
            var requestFolder = _driveService.Files.Create(body);
            requestFolder.Fields = GoogleLoginProvider.FilesFields;

            return await requestFolder.ExecuteAsync();
        }

        var request = _driveService.Files.Create(body, fileStream, mimeType);
        request.Fields = GoogleLoginProvider.FilesFields;

        var result = await request.UploadAsync();
        if (result.Exception != null)
        {
            if (request.ResponseBody == null)
            {
                throw result.Exception;
            }

            _logger.ErrorWhileTryingToInsertEntity(result.Exception);
        }

        return request.ResponseBody;
    }

    private async Task<DriveFile> InternalCopyFolderAsync(string folderId, string newFolderName, string toFolderId)
    {
        var newFolder = await InsertEntryAsync(null, newFolderName, toFolderId, true);
        var items = await GetItemsAsync(folderId);

        foreach (var item in items)
        {
            if (item.MimeType == GoogleLoginProvider.GoogleDriveMimeTypeFolder)
            {
                await InternalCopyFolderAsync(item.Id, item.Name, newFolder.Id);
            }
            else
            {
                await InternalCopyFileAsync(newFolder.Id, item.Id, item.Name);
            }
        }

        return newFolder;
    }

    private Task<DriveFile> MoveEntryAsync(string entryId, string newEntryName, string toFolderId)
    {
        var request = _driveService.Files.Update(FileConstructor(title: newEntryName), entryId);
        request.AddParents = toFolderId;
        request.Fields = GoogleLoginProvider.FilesFields;

        return request.ExecuteAsync();
    }

    private Task<DriveFile> RenameEntryAsync(string fileId, string newTitle)
    {
        var request = _driveService.Files.Update(FileConstructor(newTitle), fileId);
        request.Fields = GoogleLoginProvider.FilesFields;

        return request.ExecuteAsync();
    }

    private async Task<DriveFile> SaveStreamAsync(string fileId, Stream fileStream, string fileTitle)
    {
        var mimeType = MimeMapping.GetMimeMapping(fileTitle);
        var file = FileConstructor(fileTitle, mimeType);

        var request = _driveService.Files.Update(file, fileId, fileStream, mimeType);
        request.Fields = GoogleLoginProvider.FilesFields;
        
        var result = await request.UploadAsync();
        if (result.Exception == null)
        {
            return request.ResponseBody;
        }

        if (request.ResponseBody == null)
        {
            throw result.Exception;
        }

        _logger.ErrorWhileTryingToInsertEntity(result.Exception);

        return request.ResponseBody;
    }

    private class CachedHttpClientFactory : Google.Apis.Http.HttpClientFactory
    {
        private static HttpMessageHandler _handler;
        
        protected override HttpMessageHandler CreateHandler(Google.Apis.Http.CreateHttpClientArgs args)
        {
            return _handler ??= base.CreateHandler(args);
        }
        
        ~CachedHttpClientFactory()
        {
            _handler?.Dispose();
        }
    }
}