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

namespace ASC.Files.Core.Core.Thirdparty.WebDav;

[Transient(typeof(IThirdPartyStorage<WebDavEntry, WebDavEntry, WebDavEntry>))]
public class WebDavStorage(TempStream tempStream, IHttpClientFactory httpClientFactory, SetupInfo setupInfo) 
    : IThirdPartyStorage<WebDavEntry, WebDavEntry, WebDavEntry>, IDisposable
{
    public bool IsOpened => _client != null;
    public AuthScheme AuthScheme => AuthScheme.Basic;
    
    private const string DepthHeader = "Depth";
    private WebDavClient _client;
    private Uri _baseUri;
    private string _absolutePath;
    private AuthData _authData;

    public void Open(AuthData authData)
    {
        if (IsOpened)
        {
            return;
        }
        
        ArgumentException.ThrowIfNullOrEmpty(authData.Url);
        
        var uri = new Uri(authData.Url);
        _baseUri = new UriBuilder(uri.Scheme, uri.Host, uri.Port).Uri;
        _absolutePath = HttpUtility.UrlDecode(uri.AbsolutePath).Trim('/');

        var httpClient = httpClientFactory.CreateClient();
        
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{authData.Login}:{authData.Password}")));
        
        _client = new WebDavClient(httpClient);
        _authData = authData;
    }

    public void Close()
    {
        Dispose();
    }

    public Task<long> GetMaxUploadSizeAsync()
    {
        return Task.FromResult(setupInfo.AvailableFileSize);
    }

    public async Task<bool> CheckAccessAsync()
    {
        var root = await GetFolderAsync("/");
        return root is { IsCollection: true };
    }

    public Task<Stream> GetThumbnailAsync(string fileId, int width, int height)
    {
        return Task.FromResult<Stream>(null);
    }

    public Task<WebDavEntry> GetFileAsync(string fileId)
    {
        var resourceUrl = BuildResourceUrl(fileId);
        return GetEntryAsync(resourceUrl);
    }

    public async Task<WebDavEntry> CreateFileAsync(Stream fileStream, string title, string parentId)
    {
        var path = CombinePath(parentId, title);
        var resourceUrl = BuildResourceUrl(path);
        
        var response = await PutStreamAsync(resourceUrl, fileStream);
        if (!response.IsSuccessful)
        {
            return null;
        }

        return await GetEntryAsync(resourceUrl);
    }

    public async Task<Stream> DownloadStreamAsync(WebDavEntry file, int offset = 0)
    {
        var resourceUrl = BuildResourceUrl(file.Id);
        
        var response = await SendAsync(() => _client.GetProcessedFile(resourceUrl));
        if (!response.IsSuccessful)
        {
            return null;
        }

        switch (offset)
        {
            case > 0 when file.ContentLength.HasValue:
                return new ResponseStream(response.Stream, Math.Max(file.ContentLength.Value - offset, 0));
            case 0 when file.ContentLength.HasValue:
                return new ResponseStream(response.Stream, file.ContentLength.Value);
        }

        if (!response.Stream.CanSeek)
        {
            var tempBuffer = tempStream.Create();

            await response.Stream.CopyToAsync(tempBuffer);
            await tempBuffer.FlushAsync();
            tempBuffer.Seek(offset, SeekOrigin.Begin);

            await response.Stream.DisposeAsync();

            return tempBuffer;
        }

        response.Stream.Seek(offset, SeekOrigin.Begin);

        return response.Stream;
    }

    public async Task<WebDavEntry> MoveFileAsync(string fileId, string newFileName, string toFolderId)
    {
        var newPath = CombinePath(toFolderId, newFileName);
        var toResourceUrl = BuildResourceUrl(newPath);
        
        if (!await MoveEntryAsync(BuildResourceUrl(fileId), toResourceUrl))
        {
            return null;
        }
        
        return await GetEntryAsync(toResourceUrl);
    }

    public async Task<WebDavEntry> CopyFileAsync(string fileId, string newFileName, string toFolderId)
    {
        var path = CombinePath(toFolderId, newFileName);
        var toResourceUrl = BuildResourceUrl(path);
        
        if (!await CopyEntryAsync(BuildResourceUrl(fileId), toResourceUrl))
        {
            return null;
        }
        
        return await GetEntryAsync(toResourceUrl);
    }

    public async Task<WebDavEntry> RenameFileAsync(string fileId, string newName)
    {
        var parentPath = GetParentPath(fileId);
        var newPath = CombinePath(parentPath, newName);
        var toResourceUrl = BuildResourceUrl(newPath);
        
        if (!await MoveEntryAsync(BuildResourceUrl(fileId), toResourceUrl))
        {
            return null;
        }
        
        return await GetEntryAsync(toResourceUrl);
    }

    public async Task<WebDavEntry> SaveStreamAsync(string fileId, Stream fileStream)
    {
        var resourceUrl = BuildResourceUrl(fileId);
        
        var response = await PutStreamAsync(resourceUrl, fileStream);
        return !response.IsSuccessful ? null : await GetEntryAsync(resourceUrl);
    }

    public Task<long> GetFileSizeAsync(WebDavEntry file)
    {
        return Task.FromResult(file.ContentLength ?? 0);
    }

    public Task<WebDavEntry> GetFolderAsync(string folderId)
    {
        var resourceUrl = BuildResourceUrl(folderId);
        return GetEntryAsync(resourceUrl);
    }

    public async Task<WebDavEntry> CreateFolderAsync(string title, string parentId)
    {
        var path = CombinePath(parentId, title);
        var resourceUrl = BuildResourceUrl(path);

        var response = await SendAsync(() => _client.Mkcol(resourceUrl));
        if (!response.IsSuccessful)
        {
            return null;
        }

        return await GetEntryAsync(resourceUrl);
    }

    public async Task<WebDavEntry> MoveFolderAsync(string folderId, string newFolderName, string toFolderId)
    {
        var newPath = CombinePath(toFolderId, newFolderName);
        var toResourceUrl = BuildResourceUrl(newPath);
        
        if (!await MoveEntryAsync(BuildResourceUrl(folderId), toResourceUrl))
        {
            return null;
        }

        return await GetEntryAsync(toResourceUrl);
    }

    public async Task<WebDavEntry> CopyFolderAsync(string folderId, string newFolderName, string toFolderId)
    {
        var newPath = CombinePath(toFolderId, newFolderName);
        var toResourceUrl = BuildResourceUrl(newPath);
        
        if (!await CopyEntryAsync(BuildResourceUrl(folderId), toResourceUrl))
        {
            return null;
        }
        
        return await GetEntryAsync(toResourceUrl);
    }

    public async Task<WebDavEntry> RenameFolderAsync(string folderId, string newName)
    {
        var parentPath = GetParentPath(folderId);
        var newPath = CombinePath(parentPath, newName);
        var toResourceUrl = BuildResourceUrl(newPath);

        if (!await MoveEntryAsync(BuildResourceUrl(folderId), toResourceUrl))
        {
            return null;
        }

        return await GetEntryAsync(toResourceUrl);
    }

    public async Task<List<WebDavEntry>> GetItemsAsync(string folderId)
    {
        var response = await SendAsync(() => _client.Propfind(BuildResourceUrl(folderId), new PropfindParameters
        {
            Headers = [new KeyValuePair<string, string>(DepthHeader, "1")]
        }));

        return !response.IsSuccessful
            ? []
            : response.Resources.Skip(1).Select(ToEntry).ToList(); // Skip the folder itself
    }
    
    public Task DeleteItemAsync(WebDavEntry item)
    {
        return SendAsync(() => _client.Delete(BuildResourceUrl(item.Id)));
    }

    public void Dispose()
    {
        _client?.Dispose();
        _client = null;
    }
    
    private async Task<WebDavResponse> PutStreamAsync(string url, Stream fileStream)
    {
        var parentPath = GetParentPath(url);
        var parentResource = await GetEntryAsync(parentPath);
        
        if (parentResource is not { IsCollection: true })
        {
            return null;
        }
        
        if (fileStream.CanSeek)
        {
            return await SendAsync(() => _client.PutFile(url, fileStream));
        }

        await using var tempBuffer = tempStream.Create();
        await fileStream.CopyToAsync(tempBuffer);
        await tempBuffer.FlushAsync();
        tempBuffer.Seek(0, SeekOrigin.Begin);
        
        return await SendAsync(() => _client.PutFile(url, tempBuffer));
    }
    
    private async Task<WebDavEntry> GetEntryAsync(string url)
    {
        var response = await SendAsync(() => _client.Propfind(url, new PropfindParameters
        {
            Headers = [new KeyValuePair<string, string>(DepthHeader, "0")]
        }));

        return !response.IsSuccessful ? null : ToEntry(response.Resources.FirstOrDefault());
    }

    private async Task<bool> MoveEntryAsync(string fromPath, string toPath)
    {
        var response = await SendAsync(() => _client.Move(fromPath, toPath));

        return response.IsSuccessful;
    }

    private async Task<bool> CopyEntryAsync(string fromPath, string toPath)
    {
        var response = await SendAsync(() => _client.Copy(fromPath, toPath));
        
        return response.IsSuccessful;
    }
    
    private string BuildResourceUrl(string path)
    {
        return _baseUri + HttpUtility.UrlPathEncode(CombinePath(_absolutePath, path)).TrimStart('/');
    }
    
    private WebDavEntry ToEntry(WebDavResource resource)
    {
        var entry = new WebDavEntry();

        var uri = HttpUtility.UrlDecode(resource.Uri.Trim('/'));
        var baseUrl = _baseUri.ToString().Trim('/');
        
        if (uri.StartsWith(baseUrl))
        {
            uri = uri.Replace(baseUrl, string.Empty);
        }
        
        if (!string.IsNullOrEmpty(_absolutePath))
        { 
            var index = uri.IndexOf(_absolutePath, StringComparison.Ordinal);
            var id = index == -1 ? uri : uri.Remove(index, _absolutePath.Length);

            entry.Id = string.IsNullOrEmpty(id) ? "/" : id;
        }
        else
        {
            entry.Id = "/" + uri;
        }

        entry.DisplayName = !string.IsNullOrEmpty(resource.DisplayName) ? resource.DisplayName : uri.Split('/').LastOrDefault();
        entry.CreationDate = resource.CreationDate?.ToUniversalTime() ?? DateTime.MinValue;
        entry.LastModifiedDate = resource.LastModifiedDate?.ToUniversalTime() ?? DateTime.MinValue;
        entry.ContentLength = resource.ContentLength ?? 0;
        entry.IsCollection = resource.IsCollection;

        return entry;
    }

    private static string CombinePath(string left, string right)
    {
        left = left.Trim('/');
        right = right.Trim('/');

        return right.Length == 0 ? left : left + '/' + right;
    }
    
    private static string GetParentPath(string path)
    {
        var index = path.LastIndexOf('/');
        return index <= 0 ? string.Empty : path[..index];
    }
    
    private async Task<T> SendAsync<T>(Func<Task<T>> action) where T: WebDavResponse
    {
        var response = await action();
        if (response.StatusCode != (int)HttpStatusCode.Unauthorized)
        {
            return response;
        }
        
        var client = new HttpClient(
            new HttpClientHandler 
            {
                Credentials = new NetworkCredential(_authData.Login, _authData.Password) 
            });
        
        _client = new WebDavClient(client);
        
        return await action();
    }
    
    public IDataWriteOperator CreateDataWriteOperator(CommonChunkedUploadSession chunkedUploadSession, CommonChunkedUploadSessionHolder sessionHolder)
    {
        return null;
    }
}