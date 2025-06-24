// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Files.Tests.FilesController;

[Collection("Test Collection")]
public class BaseTest(
    FilesApiFactory filesFactory, 
    WepApiFactory apiFactory, 
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram
    ) : IAsyncLifetime
{
    protected readonly HttpClient _filesClient = filesFactory.HttpClient;
    protected readonly FilesFoldersApi _foldersApi = filesFactory.FilesFoldersApi;
    protected readonly FilesFilesApi _filesApi = filesFactory.FilesFilesApi;
    protected readonly FilesOperationsApi _filesOperationsApi = filesFactory.FilesOperationsApi;
    protected readonly RoomsApi _roomsApi = filesFactory.RoomsApi;
    protected readonly FilesSettingsApi _filesSettingsApi = filesFactory.FilesSettingsApi;
    protected readonly FilesQuotaApi _quotaApi = filesFactory.FilesQuotaApi;
    protected readonly SettingsQuotaApi _settingsQuotaApi = apiFactory.SettingsQuotaApi;
    private readonly Func<Task> _resetDatabase = filesFactory.ResetDatabaseAsync;

    public async ValueTask InitializeAsync()
    {
        await Initializer.InitializeAsync(filesFactory, apiFactory, peopleFactory, filesServiceProgram);
    }

    public async ValueTask DisposeAsync()
    {
        await _resetDatabase();
    }

    protected async Task<FileDtoInteger> GetFile(int fileId)
    {
        return (await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
    }
    
    protected async Task<int> GetFolderIdAsync(FolderType folderType, User user)
    {
        await _filesClient.Authenticate(user);
        
        var rootFolder = (await _foldersApi.GetRootFoldersAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        var folderId = rootFolder.FirstOrDefault(r => r.Current.RootFolderType.HasValue && r.Current.RootFolderType.Value == folderType)!.Current.Id;
        
        return folderId;
    }
    
    protected async Task<int> GetUserFolderIdAsync(User user)
    {
        return await GetFolderIdAsync(FolderType.USER, user);
    }
    
    protected async Task<FileDtoInteger> CreateFile(string fileName, FolderType folderType, User user)
    {
        await _filesClient.Authenticate(user);
        
        var folderId = await GetFolderIdAsync(folderType, user);
        
        return await CreateFile(fileName, folderId);
    }
    
    protected async Task<FileDtoInteger> CreateFile(string fileName, int folderId)
    {
        return (await _filesApi.CreateFileAsync(folderId, new CreateFileJsonElement(fileName))).Response;
    }
    
    protected async Task<FolderDtoInteger> CreateFolder(string folderName, FolderType folderType, User user)
    {
        await _filesClient.Authenticate(user);
        
        var folderId = await GetFolderIdAsync(folderType, user);
        
        return await CreateFolder(folderName, folderId);
    }
    
    protected async Task<FolderDtoInteger> CreateFolder(string folderName, int folderId)
    {
        return (await _foldersApi.CreateFolderAsync(folderId, new CreateFolder(folderName), TestContext.Current.CancellationToken)).Response;
    }
    
    protected async Task<FolderDtoInteger> CreateVirtualRoom(string roomTitle, bool indexing = true)
    {
        return (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto(roomTitle, indexing: true, roomType: RoomType.VirtualDataRoom), TestContext.Current.CancellationToken)).Response;
    }
    
    protected async Task<FolderDtoInteger> CreateCustomRoom(string roomTitle)
    {
        return (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto(roomTitle, roomType: RoomType.CustomRoom), TestContext.Current.CancellationToken)).Response;
    }
    
    protected async Task<List<FileOperationDto>?> WaitLongOperation()
    {
        List<FileOperationDto>? statuses;

        while (true)
        {
            statuses = (await _filesOperationsApi.GetOperationStatusesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

            if (statuses.TrueForAll(r => r.Finished) || TestContext.Current.CancellationToken.IsCancellationRequested)
            {
                break;
            }
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        return statuses;
    }
}