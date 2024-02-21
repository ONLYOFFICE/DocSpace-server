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

using Box.V2.Exceptions;

namespace ASC.Files.Thirdparty.Box;

[Transient]
internal class BoxStorage(TempStream tempStream) : IThirdPartyStorage<BoxFile, BoxFolder, BoxItem>
{
    private BoxClient _boxClient;

    private readonly List<string> _boxFields = ["created_at", "modified_at", "name", "parent", "size"];

    public bool IsOpened { get; private set; }

    private const long MaxChunkedUploadFileSize = 250L * 1024L * 1024L;

    public void Open(OAuth20Token token)
    {
        if (IsOpened)
        {
            return;
        }

        var config = new BoxConfig(token.ClientID, token.ClientSecret, new Uri(token.RedirectUri));
        var session = new OAuthSession(token.AccessToken, token.RefreshToken, (int)token.ExpiresIn, "bearer");
        _boxClient = new BoxClient(config, session);

        IsOpened = true;
    }

    public void Close()
    {
        IsOpened = false;
    }

    public async Task<BoxFolder> GetFolderAsync(string folderId)
    {
        try
        {
            return await _boxClient.FoldersManager.GetInformationAsync(folderId, _boxFields);
        }
        catch (Exception ex)
        {
            if (ex.InnerException is BoxAPIException boxException && boxException.Error.Status == ((int)HttpStatusCode.NotFound).ToString())
            {
                return null;
            }

            throw;
        }
    }

    public async Task<bool> CheckAccessAsync()
    {
        try
        {
            var rootFolder = await GetFolderAsync("0");
            return !string.IsNullOrEmpty(rootFolder.Id);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public Task<BoxFile> GetFileAsync(string fileId)
    {
        try
        {
            return _boxClient.FilesManager.GetInformationAsync(fileId, _boxFields);
        }
        catch (Exception ex)
        {
            if (ex.InnerException is BoxAPIException boxException && boxException.Error.Status == ((int)HttpStatusCode.NotFound).ToString())
            {
                return Task.FromResult<BoxFile>(null);
            }
            throw;
        }
    }

    public async Task<List<BoxItem>> GetItemsAsync(string folderId)
    {
        var folderItems = await _boxClient.FoldersManager.GetFolderItemsAsync(folderId, 500, 0, _boxFields);

        return folderItems.Entries;
    }

    public async Task<Stream> DownloadStreamAsync(BoxFile file, int offset = 0)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (offset > 0 && file.Size.HasValue)
        {
            var streamWithOffset = await _boxClient.FilesManager.DownloadAsync(file.Id, startOffsetInBytes: offset, endOffsetInBytes: (int)file.Size - 1);
            
            return new ResponseStream(streamWithOffset, Math.Max(file.Size.Value - offset, 0));
        }

        var stream = await _boxClient.FilesManager.DownloadAsync(file.Id);
        
        if (offset == 0)
        {
            return file.Size.HasValue ? new ResponseStream(stream, file.Size.Value) : stream;
        }

        var tempBuffer = tempStream.Create();
        
        if (stream == null)
        {
            return tempBuffer;
        }

        await stream.CopyToAsync(tempBuffer);
        await tempBuffer.FlushAsync();
        tempBuffer.Seek(offset, SeekOrigin.Begin);

        await stream.DisposeAsync();

        return tempBuffer;
    }

    public async Task<BoxFolder> CreateFolderAsync(string title, string parentId)
    {
        var boxFolderRequest = new BoxFolderRequest
        {
            Name = title,
            Parent = new BoxRequestEntity
            {
                Id = parentId
            }
        };

        return await _boxClient.FoldersManager.CreateAsync(boxFolderRequest, _boxFields);
    }

    public async Task<BoxFile> CreateFileAsync(Stream fileStream, string title, string parentId)
    {
        var boxFileRequest = new BoxFileRequest
        {
            Name = title,
            Parent = new BoxRequestEntity
            {
                Id = parentId
            }
        };

        return await _boxClient.FilesManager.UploadAsync(boxFileRequest, fileStream, _boxFields, setStreamPositionToZero: false);
    }

    public async Task DeleteItemAsync(BoxItem boxItem)
    {
        if (boxItem is BoxFolder)
        {
            await _boxClient.FoldersManager.DeleteAsync(boxItem.Id, true);
        }
        else
        {
            await _boxClient.FilesManager.DeleteAsync(boxItem.Id);
        }
    }

    public async Task<BoxFolder> MoveFolderAsync(string folderId, string newFolderName, string toFolderId)
    {
        var boxFolderRequest = new BoxFolderRequest
        {
            Id = folderId,
            Name = newFolderName,
            Parent = new BoxRequestEntity
            {
                Id = toFolderId
            }
        };

        return await _boxClient.FoldersManager.UpdateInformationAsync(boxFolderRequest, _boxFields);
    }

    public async Task<BoxFile> MoveFileAsync(string fileId, string newFileName, string toFolderId)
    {
        var boxFileRequest = new BoxFileRequest
        {
            Id = fileId,
            Name = newFileName,
            Parent = new BoxRequestEntity
            {
                Id = toFolderId
            }
        };

        return await _boxClient.FilesManager.UpdateInformationAsync(boxFileRequest, null, _boxFields);
    }

    public async Task<BoxFolder> CopyFolderAsync(string folderId, string newFolderName, string toFolderId)
    {
        var boxFolderRequest = new BoxFolderRequest
        {
            Id = folderId,
            Name = newFolderName,
            Parent = new BoxRequestEntity
            {
                Id = toFolderId
            }
        };

        return await _boxClient.FoldersManager.CopyAsync(boxFolderRequest, _boxFields);
    }

    public async Task<BoxFile> CopyFileAsync(string fileId, string newFileName, string toFolderId)
    {
        var boxFileRequest = new BoxFileRequest
        {
            Id = fileId,
            Name = newFileName,
            Parent = new BoxRequestEntity
            {
                Id = toFolderId
            }
        };

        return await _boxClient.FilesManager.CopyAsync(boxFileRequest, _boxFields);
    }

    public async Task<BoxFolder> RenameFolderAsync(string folderId, string newName)
    {
        var boxFolderRequest = new BoxFolderRequest { Id = folderId, Name = newName };

        return await _boxClient.FoldersManager.UpdateInformationAsync(boxFolderRequest, _boxFields);
    }

    public async Task<BoxFile> RenameFileAsync(string fileId, string newName)
    {
        var boxFileRequest = new BoxFileRequest { Id = fileId, Name = newName };

        return await _boxClient.FilesManager.UpdateInformationAsync(boxFileRequest, null, _boxFields);
    }

    public async Task<BoxFile> SaveStreamAsync(string fileId, Stream fileStream)
    {
        return await _boxClient.FilesManager.UploadNewVersionAsync(null, fileId, fileStream, fields: _boxFields, setStreamPositionToZero: false);
    }
    
    public long GetFileSize(BoxFile file)
    {
        return file.Size ?? 0;
    }
    
    public async Task<long> GetMaxUploadSizeAsync()
    {
        var boxUser = await _boxClient.UsersManager.GetCurrentUserInformationAsync(new List<string> { "max_upload_size" });
        var max = boxUser.MaxUploadSize ?? MaxChunkedUploadFileSize;

        //todo: without chunked uploader:
        return Math.Min(max, MaxChunkedUploadFileSize);
    }

    public async Task<Stream> GetThumbnailAsync(string fileId, int width, int height)
    {
        var boxRepresentation = new BoxRepresentationRequest { FileId = fileId, XRepHints = "[jpg?dimensions=320x320]" };
        return await _boxClient.FilesManager.GetRepresentationContentAsync(boxRepresentation);
    }
}
