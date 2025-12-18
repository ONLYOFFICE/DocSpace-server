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

using ASC.Files.Tests.ApiFactories;

using DocSpace.API.SDK.Api.Group;
using DocSpace.API.SDK.Api.People;
using DocSpace.API.SDK.Api.Settings;

using QuotaApi = DocSpace.API.SDK.Api.Files.QuotaApi;
using RoomsApi = DocSpace.API.SDK.Api.Rooms.RoomsApi;

namespace ASC.Files.Tests.Tests;

[Collection("Test Collection")]
public class BaseTest(
    FilesApiFactory filesFactory, 
    WepApiFactory apiFactory, 
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram
    ) : IAsyncLifetime
{
    protected readonly HttpClient _filesClient = filesFactory.HttpClient;
    protected readonly HttpClient _peopleClient = peopleFactory.HttpClient;
    protected readonly FoldersApi _foldersApi = filesFactory.FoldersApi;
    protected readonly FilesApi _filesApi = filesFactory.FilesApi;
    protected readonly OperationsApi _filesOperationsApi = filesFactory.OperationsApi;
    protected readonly RoomsApi _roomsApi = filesFactory.RoomsApi;
    protected readonly SettingsApi _filesSettingsApi = filesFactory.SettingsApi;
    protected readonly QuotaApi _quotaApi = filesFactory.QuotaApi;
    protected readonly SharingApi _sharingApi = filesFactory.SharingApi;
    
    protected readonly GroupApi _groupApi = peopleFactory.GroupApi;
    protected readonly UserStatusApi _userStatusApi = peopleFactory.UserStatusApi;
    
    protected readonly CommonSettingsApi _commonSettingsApi = apiFactory.CommonSettingsApi;
    protected readonly DocSpace.API.SDK.Api.Settings.QuotaApi _settingsQuotaApi = apiFactory.SettingsQuotaApi;
    
    private readonly Func<Task> _resetDatabase = filesFactory.ResetDatabaseAsync;

    //   FileShare.None
    public static TheoryData<FileShare> ValidFileShare =>
    [
        FileShare.Editing, FileShare.Review, FileShare.Comment, FileShare.Read
    ];
    
    public static TheoryData<FileShare> InvalidFileShare =>
    [
       FileShare.ReadWrite, FileShare.Varies, FileShare.RoomManager, FileShare.ContentCreator
    ];
    
    public static TheoryData<FileShare> InvalidFileShareFillingForms =>
    [
        FileShare.ReadWrite, FileShare.Varies, FileShare.RoomManager, FileShare.ContentCreator,  FileShare.Editing, FileShare.Review, FileShare.Comment //, FileShare.Read
    ];
    
    public static TheoryData<RoomType> ValidRoomTypesForShare =>
    [
        RoomType.CustomRoom, RoomType.PublicRoom
    ];
    
    public static TheoryData<RoomType> InValidRoomTypesForShare =>
    [
        RoomType.EditingRoom, RoomType.VirtualDataRoom
    ];
    
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
    
    protected async Task<int> GetShareFolderIdAsync(User user)
    {
        return await GetFolderIdAsync(FolderType.SHARE, user);
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
    
    protected async Task<FileDtoInteger> CreateFileInMy(string fileName, User user)
    {
        var folderId = await GetUserFolderIdAsync( user);
        
        return await CreateFile(fileName, folderId);
    }
    
    protected async Task<FileDtoInteger> CreateFile(string fileName, int folderId)
    {
        return (await _filesApi.CreateFileAsync(folderId, new CreateFileJsonElement(fileName))).Response;
    }
    
    protected async Task<FolderDtoInteger> CreateFolder(string folderName, FolderType folderType, User user)
    {
        var folderId = await GetFolderIdAsync(folderType, user);
        
        return await CreateFolder(folderName, folderId);
    }
    
    protected async Task<FolderDtoInteger> CreateFolderInMy(string folderName, User user)
    {
        var folderId = await GetUserFolderIdAsync( user);
        
        return await CreateFolder(folderName, folderId);
    }
    
    protected async Task<FolderDtoInteger> CreateFolder(string folderName, int folderId)
    {
        return (await _foldersApi.CreateFolderAsync(folderId, new CreateFolder(folderName), TestContext.Current.CancellationToken)).Response;
    }
    
    protected async Task<FolderDtoInteger> CreateVirtualRoom(string roomTitle, bool indexing = true)
    {
        return (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto(roomTitle, indexing: indexing, roomType: RoomType.VirtualDataRoom), TestContext.Current.CancellationToken)).Response;
    }
    
    protected async Task<FolderDtoInteger> CreateCustomRoom(string roomTitle)
    {
        return (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto(roomTitle, roomType: RoomType.CustomRoom), TestContext.Current.CancellationToken)).Response;
    }
    
    protected async Task<FolderDtoInteger> CreateCollaborationRoom(string roomTitle)
    {
        return (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto(roomTitle, roomType: RoomType.EditingRoom), TestContext.Current.CancellationToken)).Response;
    }
    
    protected async Task<FolderDtoInteger> CreateFillingFormsRoom(string roomTitle)
    {
        return (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto(roomTitle, roomType: RoomType.FillingFormsRoom), TestContext.Current.CancellationToken)).Response;
    }
    
    protected async Task<FolderDtoInteger> CreatePublicRoom(string roomTitle)
    {
        return (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto(roomTitle, roomType: RoomType.PublicRoom), TestContext.Current.CancellationToken)).Response;
    }
    
    protected async Task<FolderDtoInteger> CreateVDRRoom(string roomTitle)
    {
        return (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto(roomTitle, roomType: RoomType.VirtualDataRoom), TestContext.Current.CancellationToken)).Response;
    }
    
    protected async Task<List<FileOperationDto>?> WaitLongOperation(string? operationId = null)
    {
        List<FileOperationDto>? statuses;

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            timeoutCts.Token, 
            TestContext.Current.CancellationToken);
        
        while (true)
        {
            statuses = (await _filesOperationsApi.GetOperationStatusesAsync(id: operationId, cancellationToken: linkedCts.Token)).Response;

            if (statuses.Count > 0 && statuses.TrueForAll(r => r.Finished) || linkedCts.Token.IsCancellationRequested)
            {
                break;
            }
            
            await Task.Delay(100, linkedCts.Token);
        }

        return statuses;
    }
    
    
    protected async Task<(string, int)> CreateFileAndShare(FileShare fileShare, bool primary = true, bool varInternal = false, DateTime? expirationDate = null)
    {
        await _filesClient.Authenticate(Initializer.Owner);
        
        var file = await CreateFileInMy("file_update_link.docx", Initializer.Owner);
        
        // Create initial external link
        var initialLinkParams = new FileLinkRequest(
            access: fileShare,
            primary: primary,
            @internal: varInternal
        );

        if (expirationDate != null)
        {
            initialLinkParams.ExpirationDate = new ApiDateTime { UtcTime = expirationDate.Value };
        }
        
        var initialLink = (await _filesApi.CreateFilePrimaryExternalLinkAsync(file.Id, initialLinkParams, TestContext.Current.CancellationToken)).Response;
        
        return (initialLink.SharedLink.RequestToken, file.Id);
    }
    
    protected async Task<FileDtoInteger> TryOpenEditAsync(string share, int fileId, User? user = null, bool throwException = false)
    {
        if (user != null)
        {
            await _filesClient.Authenticate(user);
        }
        else
        {
            _filesClient.DefaultRequestHeaders.Authorization = null;
        }

        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, share);
        
        if (throwException)
        {
            await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.OpenEditFileAsync(fileId, cancellationToken: TestContext.Current.CancellationToken));
            _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
            return null!;
        }

        var openEditResult = (await _filesApi.OpenEditFileAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        return openEditResult.File;
    }
    
    private async Task<int> GetFolderIdAsync(FolderType folderType, User user)
    {
        await _filesClient.Authenticate(user);
        
        var rootFolder = (await _foldersApi.GetRootFoldersAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        var folderId = rootFolder.FirstOrDefault(r => r.Current.RootFolderType.HasValue && r.Current.RootFolderType.Value == folderType)!.Current.Id;
        
        return folderId;
    }
}