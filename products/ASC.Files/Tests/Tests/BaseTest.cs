// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

using DocSpace.API.SDK.Api.Group;
using DocSpace.API.SDK.Api.Privacyroom;

using QuotaApi = DocSpace.API.SDK.Api.Files.QuotaApi;
using RoomsApi = DocSpace.API.SDK.Api.Rooms.RoomsApi;
using SettingsApi = DocSpace.API.SDK.Api.Files.SettingsApi;

namespace ASC.Files.Tests.Tests;

[Collection("Test Collection")]
public class BaseTest(
    AspireAppFixture fixture
    ) : IAsyncLifetime
{
    protected readonly HttpClient _filesClient = fixture.FilesHttpClient;
    protected readonly HttpClient _peopleClient = fixture.PeopleHttpClient;
    protected readonly FoldersApi _foldersApi = fixture.FoldersApi;
    protected readonly FilesApi _filesApi = fixture.FilesApi;
    protected readonly OperationsApi _filesOperationsApi = fixture.OperationsApi;
    protected readonly RoomsApi _roomsApi = fixture.RoomsApi;
    protected readonly SettingsApi _filesSettingsApi = fixture.SettingsApi;
    protected readonly QuotaApi _quotaApi = fixture.QuotaApi;
    protected readonly SharingApi _sharingApi = fixture.SharingApi;
    protected readonly PrivacyroomApi _privacyRoomApi = fixture.PrivacyroomApi;

    protected readonly GroupApi _groupApi = fixture.GroupApi;
    protected readonly UserStatusApi _userStatusApi = fixture.UserStatusApi;
    protected readonly PhotosApi _photosApi = fixture.PhotosApi;

    protected readonly CommonSettingsApi _commonSettingsApi = fixture.CommonSettingsApi;
    protected readonly DocSpace.API.SDK.Api.Settings.QuotaApi _settingsQuotaApi = fixture.WebApiSettingsQuotaApi;
    protected readonly HttpClient _webApiClient = fixture.WebApiHttpClient;
    protected readonly AuthenticationApi _authenticationApi = fixture.AuthenticationApi;

    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

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
        await Initializer.InitializeAsync(fixture);
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

    public async Task<int> GetFolderIdAsync(FolderType folderType, User user)
    {
        await _filesClient.Authenticate(user);

        var rootFolder = (await _foldersApi.GetRootFoldersAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        var folderId = rootFolder.FirstOrDefault(r => r.Current.RootFolderType.HasValue && r.Current.RootFolderType.Value == folderType)!.Current.Id;

        return folderId;
    }
}
