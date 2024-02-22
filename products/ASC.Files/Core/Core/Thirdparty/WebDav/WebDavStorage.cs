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

namespace ASC.Files.Core.Core.Thirdparty.WebDav;

[Transient]
public class WebDavStorage(TempStream tempStream, IHttpClientFactory httpClientFactory, SetupInfo setupInfo) 
    : IThirdPartyStorage<WebDavResource, WebDavResource, WebDavResource>, IDisposable
{
    public bool IsOpened => _client != null;
    public AuthScheme AuthScheme => AuthScheme.Basic;
    
    private const string DepthHeader = "Depth";
    private WebDavClient _client;

    public void Open(AuthData authData)
    {
        if (IsOpened)
        {
            return;
        }

        if (authData.Provider == ProviderTypes.kDrive.ToStringFast())
        {
            authData.Url = "https://connect.drive.infomaniak.com";
        }

        var httpClient = httpClientFactory.CreateClient();
        
        httpClient.BaseAddress = new Uri(authData.Url);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", 
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{authData.Login}:{authData.Password}")));
        
        _client = new WebDavClient(httpClient);
    }

    public void Close()
    {
        _client?.Dispose();
        _client = null;
    }

    public Task<long> GetMaxUploadSizeAsync()
    {
        return Task.FromResult(setupInfo.AvailableFileSize);
    }

    public async Task<bool> CheckAccessAsync()
    {
        var root = await GetFolderAsync("/");
        return root != null;
    }

    public Task<Stream> GetThumbnailAsync(string fileId, int width, int height)
    {
        throw new NotSupportedException();
    }

    public async Task<WebDavResource> GetFileAsync(string fileId)
    {
        var response = await _client.Propfind(fileId);

        return !response.IsSuccessful ? null : response.Resources.FirstOrDefault();
    }

    public async Task<WebDavResource> CreateFileAsync(Stream fileStream, string title, string parentId)
    {
        var path = MakePath(parentId, title);
        
        var response = await _client.PutFile(path, fileStream);
        if (!response.IsSuccessful)
        {
            return null;
        }

        return await GetFileAsync(path);
    }

    public async Task<Stream> DownloadStreamAsync(WebDavResource file, int offset = 0)
    {
        var response = await _client.GetProcessedFile(file.Uri);

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

    public async Task<WebDavResource> MoveFileAsync(string fileId, string newFileName, string toFolderId)
    {
        var newPath = MakePath(toFolderId, newFileName);
        
        if (!await MoveEntryAsync(fileId, newPath))
        {
            return null;
        }
        
        return await GetFileAsync(newPath);
    }

    public async Task<WebDavResource> CopyFileAsync(string fileId, string newFileName, string toFolderId)
    {
        var path = MakePath(toFolderId, newFileName);
        
        if (!await CopyEntryAsync(fileId, path))
        {
            return null;
        }
        
        return await GetFileAsync(path);
    }

    public async Task<WebDavResource> RenameFileAsync(string fileId, string newName)
    {
        var parentPath = GetParentPath(fileId);
        var newPath = MakePath(parentPath, newName);
        
        if (!await MoveEntryAsync(fileId, newPath))
        {
            return null;
        }
        
        return await GetFileAsync(newPath);
    }

    public async Task<WebDavResource> SaveStreamAsync(string fileId, Stream fileStream)
    {
        var response = await _client.PutFile(fileId, fileStream);
        
        return !response.IsSuccessful ? null : await GetFileAsync(fileId);
    }

    public long GetFileSize(WebDavResource file)
    {
        return file.ContentLength ?? 0;
    }

    public async Task<WebDavResource> GetFolderAsync(string folderId)
    {
        var response = await _client.Propfind(folderId, new PropfindParameters
        {
            Headers = new [] { new KeyValuePair<string, string>(DepthHeader, "0") }
        });

        return !response.IsSuccessful ? null : response.Resources.FirstOrDefault();
    }

    public async Task<WebDavResource> CreateFolderAsync(string title, string parentId)
    {
        var path = MakePath(parentId, title);
        
        var response = await _client.Mkcol(path);
        if (!response.IsSuccessful)
        {
            return null;
        }

        return await GetFolderAsync(path);
    }

    public async Task<WebDavResource> MoveFolderAsync(string folderId, string newFolderName, string toFolderId)
    {
        var newPath = MakePath(toFolderId, newFolderName);
        
        if (!await MoveEntryAsync(folderId, newPath))
        {
            return null;
        }
        
        return await GetFolderAsync(newPath);
    }

    public async Task<WebDavResource> CopyFolderAsync(string folderId, string newFolderName, string toFolderId)
    {
        var newPath = MakePath(toFolderId, newFolderName);
        
        if (!await CopyEntryAsync(folderId, newPath))
        {
            return null;
        }
        
        return await GetFolderAsync(newPath);
    }

    public async Task<WebDavResource> RenameFolderAsync(string folderId, string newName)
    {
        var parentPath = GetParentPath(folderId);
        var newPath = MakePath(parentPath, newName);

        if (!await MoveEntryAsync(folderId, newPath))
        {
            return null;
        }

        return await GetFolderAsync(newPath);
    }

    public async Task<List<WebDavResource>> GetItemsAsync(string folderId)
    {
        var response = await _client.Propfind(folderId, new PropfindParameters
        {
            Headers = new[] { new KeyValuePair<string, string>(DepthHeader, "1") }
        });

        return !response.IsSuccessful 
            ? null 
            : response.Resources.Skip(1).ToList(); // Skip the folder itself
    }

    public async Task DeleteItemAsync(WebDavResource item)
    {
        await _client.Delete(item.Uri);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    private async Task<bool> MoveEntryAsync(string fromPath, string toPath)
    {
        var response = await _client.Move(fromPath, toPath);

        return response.IsSuccessful;
    }

    private async Task<bool> CopyEntryAsync(string fromPath, string toPath)
    {
        var response = await _client.Copy(fromPath, toPath);
        
        return response.IsSuccessful;
    }

    private static string MakePath(string parentPath, string name)
    {
        return (parentPath ?? string.Empty) + "/" + (name ?? string.Empty);
    }
    
    private static string GetParentPath(string path)
    {
        var index = path.LastIndexOf('/');
        return index == -1 ? path : path[..index];
    }
}