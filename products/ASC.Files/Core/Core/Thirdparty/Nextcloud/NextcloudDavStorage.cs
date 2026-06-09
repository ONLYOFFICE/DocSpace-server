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

namespace ASC.Files.Core.Core.Thirdparty.Nextcloud;

[Transient(typeof(IThirdPartyStorage<NextcloudDavEntry, NextcloudDavEntry, NextcloudDavEntry>))]
public class NextcloudDavStorage(TempStream tempStream, IHttpClientFactory httpClientFactory, SetupInfo setupInfo, OAuth20TokenHelper oAuth20TokenHelper, ConsumerFactory consumerFactory)
    : IThirdPartyStorage<NextcloudDavEntry, NextcloudDavEntry, NextcloudDavEntry>, IDisposable
{
    public bool IsOpened => _client != null;
    public AuthScheme AuthScheme => AuthScheme.OAuth;

    private OAuth20Token _token;
    private string AccessToken
    {
        get
        {
            if (_token == null)
            {
                throw new Exception("Cannot create Nextcloud WebDav session with given token");
            }

            if (_token.IsExpired)
            {
                _token = oAuth20TokenHelper.RefreshToken<NextcloudLoginProvider>(_token);
            }

            return _token.AccessToken;
        }
    }

    private const string DepthHeader = "Depth";
    private HttpRequestHeaders _defaultHeaders;
    private WebDavClient _client;
    private Uri _baseUri;
    private string _absolutePath;

    public void Open(AuthData authData)
    {
        if (IsOpened)
        {
            return;
        }

        _token = authData.Token ?? throw new UnauthorizedAccessException("Cannot create Nextcloud WebDav session with given token");

        var uri = new Uri(authData.Url);
        _baseUri = new UriBuilder(uri.Scheme, uri.Host, uri.Port).Uri;
        _absolutePath = HttpUtility.UrlDecode(uri.AbsolutePath).Trim('/');

        var httpClient = httpClientFactory.CreateClient("customHttpClientNoCookie");
        _client = new WebDavClient(httpClient);
        _defaultHeaders = httpClient.DefaultRequestHeaders;
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

    public Task<Stream> GetThumbnailAsync(string fileId, uint width, uint height)
    {
        return Task.FromResult<Stream>(null);
    }

    public Task<NextcloudDavEntry> GetFileAsync(string fileId)
    {
        var resourceUrl = BuildResourceUrl(fileId);
        return GetEntryAsync(resourceUrl);
    }

    public async Task<NextcloudDavEntry> CreateFileAsync(Stream fileStream, string title, string parentId)
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

    public async Task<Stream> DownloadStreamAsync(NextcloudDavEntry file, int offset = 0)
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

    public async Task<NextcloudDavEntry> MoveFileAsync(string fileId, string newFileName, string toFolderId)
    {
        var newPath = CombinePath(toFolderId, newFileName);
        var toResourceUrl = BuildResourceUrl(newPath);

        if (!await MoveEntryAsync(BuildResourceUrl(fileId), toResourceUrl))
        {
            return null;
        }

        return await GetEntryAsync(toResourceUrl);
    }

    public async Task<NextcloudDavEntry> CopyFileAsync(string fileId, string newFileName, string toFolderId)
    {
        var path = CombinePath(toFolderId, newFileName);
        var toResourceUrl = BuildResourceUrl(path);

        if (!await CopyEntryAsync(BuildResourceUrl(fileId), toResourceUrl))
        {
            return null;
        }

        return await GetEntryAsync(toResourceUrl);
    }

    public async Task<NextcloudDavEntry> RenameFileAsync(string fileId, string newName)
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

    public async Task<NextcloudDavEntry> SaveStreamAsync(string fileId, Stream fileStream)
    {
        var resourceUrl = BuildResourceUrl(fileId);

        var response = await PutStreamAsync(resourceUrl, fileStream);
        return !response.IsSuccessful ? null : await GetEntryAsync(resourceUrl);
    }

    public Task<long> GetFileSizeAsync(NextcloudDavEntry file)
    {
        return Task.FromResult(file.ContentLength ?? 0);
    }

    public Task<NextcloudDavEntry> GetFolderAsync(string folderId)
    {
        var resourceUrl = BuildResourceUrl(folderId, true);
        return GetEntryAsync(resourceUrl);
    }

    public async Task<NextcloudDavEntry> CreateFolderAsync(string title, string parentId)
    {
        var path = CombinePath(parentId, title);
        var resourceUrl = BuildResourceUrl(path, true);

        var response = await SendAsync(() => _client.Mkcol(resourceUrl));
        if (!response.IsSuccessful)
        {
            return null;
        }

        return await GetEntryAsync(resourceUrl);
    }

    public async Task<NextcloudDavEntry> MoveFolderAsync(string folderId, string newFolderName, string toFolderId)
    {
        var newPath = CombinePath(toFolderId, newFolderName);
        var toResourceUrl = BuildResourceUrl(newPath, true);

        if (!await MoveEntryAsync(BuildResourceUrl(folderId), toResourceUrl))
        {
            return null;
        }

        return await GetEntryAsync(toResourceUrl);
    }

    public async Task<NextcloudDavEntry> CopyFolderAsync(string folderId, string newFolderName, string toFolderId)
    {
        var newPath = CombinePath(toFolderId, newFolderName);
        var toResourceUrl = BuildResourceUrl(newPath, true);

        if (!await CopyEntryAsync(BuildResourceUrl(folderId), toResourceUrl))
        {
            return null;
        }

        return await GetEntryAsync(toResourceUrl);
    }

    public async Task<NextcloudDavEntry> RenameFolderAsync(string folderId, string newName)
    {
        var parentPath = GetParentPath(folderId);
        var newPath = CombinePath(parentPath, newName);
        var toResourceUrl = BuildResourceUrl(newPath, true);

        if (!await MoveEntryAsync(BuildResourceUrl(folderId, true), toResourceUrl))
        {
            return null;
        }

        return await GetEntryAsync(toResourceUrl);
    }

    public async Task<List<NextcloudDavEntry>> GetItemsAsync(string folderId)
    {
        var response = await SendAsync(() => _client.Propfind(BuildResourceUrl(folderId, true), new PropfindParameters
        {
            Headers = [new KeyValuePair<string, string>(DepthHeader, "1")]
        }));

        return !response.IsSuccessful
            ? []
            : response.Resources.Skip(1).Select(ToEntry).ToList(); // Skip the folder itself
    }

    public Task DeleteItemAsync(NextcloudDavEntry item)
    {
        return SendAsync(() => _client.Delete(BuildResourceUrl(item.Id, item.IsCollection)));
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

    private async Task<NextcloudDavEntry> GetEntryAsync(string url)
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

    private string BuildResourceUrl(string path, bool isDirectory = false)
    {
        var url = _baseUri + HttpUtility.UrlPathEncode(CombinePath(_absolutePath, path)).TrimStart('/');
        if (isDirectory && !url.EndsWith('/'))
        {
            url += '/';
        }

        return url;
    }

    private NextcloudDavEntry ToEntry(WebDavResource resource)
    {
        var entry = new NextcloudDavEntry();

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

    private async Task<T> SendAsync<T>(Func<Task<T>> action) where T : WebDavResponse
    {
        _defaultHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        return await action();
    }

    public IDataWriteOperator CreateDataWriteOperator(CommonChunkedUploadSession chunkedUploadSession, CommonChunkedUploadSessionHolder sessionHolder)
    {
        return new ChunkZipWriteOperator(tempStream, chunkedUploadSession, sessionHolder);
    }
}
