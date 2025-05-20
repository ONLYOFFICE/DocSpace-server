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

namespace ASC.Files.Core.Core;

/// <summary>
/// The ThirdPartyIntegrationService class provides methods for managing third-party integrations,
/// including operations such as fetching, saving, deleting, and interacting with third-party and DocuSign providers.
/// </summary>
[Scope]
public class ThirdPartyIntegrationService(
    GlobalFolderHelper globalFolderHelper,
    FilesSettingsHelper filesSettingsHelper,
    AuthContext authContext,
    UserManager userManager,
    FileSecurity fileSecurity,
    IDaoFactory daoFactory,
    FileMarker fileMarker,
    EntryManager entryManager,
    FilesMessageService filesMessageService,
    DocuSignToken docuSignToken,
    DocuSignHelper docuSignHelper,
    ThirdpartyConfiguration thirdpartyConfiguration,
    ConsumerFactory consumerFactory,
    ILogger<ThirdPartyIntegrationService> logger)
{
    /// <summary>
    /// Retrieves an asynchronous enumerable list of third-party integration parameters.
    /// </summary>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of <see cref="ThirdPartyParams"/> containing the details
    /// of the connected third-party integrations. If no providers are configured, an empty enumerable is returned.
    /// </returns>
    public IAsyncEnumerable<ThirdPartyParams> GetThirdPartyAsync()
    {
        var providerDao = daoFactory.ProviderDao;
        if (providerDao == null)
        {
            return AsyncEnumerable.Empty<ThirdPartyParams>();
        }

        return InternalGetThirdPartyAsync(providerDao);
    }

    /// <summary>
    /// Retrieves an asynchronous task that returns a folder for third-party account backup details.
    /// </summary>
    /// <returns>A <see cref="ValueTask{TResult}"/> of <see cref="Folder{T}"/> representing the folder for the third-party account backup.
    /// If no backup folder exists or access is restricted, <c>null</c> is returned.
    /// </returns>
    public async ValueTask<Folder<string>> GetBackupThirdPartyAsync()
    {
        var providerDao = daoFactory.ProviderDao;
        if (providerDao == null)
        {
            return null;
        }

        var providerInfo = await providerDao.GetProvidersInfoAsync(FolderType.ThirdpartyBackup).SingleOrDefaultAsync();

        if (providerInfo != null)
        {
            var folderDao = daoFactory.GetFolderDao<string>();
            var folder = await folderDao.GetFolderAsync(providerInfo.RootFolderId);
            if (!await fileSecurity.CanReadAsync(folder))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ViewFolder);
            }

            return folder;
        }

        return null;
    }

    /// <summary>
    /// Saves a third-party integration based on the provided parameters.
    /// </summary>
    /// <param name="thirdPartyParams">The parameters containing details about the third-party integration to be saved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is a <see cref="Folder{T}"/>
    /// representing the folder associated with the saved third-party integration.</returns>
    public async ValueTask<Folder<string>> SaveThirdPartyAsync(ThirdPartyParams thirdPartyParams)
    {
        var providerDao = daoFactory.ProviderDao;

        if (providerDao == null)
        {
            return null;
        }

        var folderDaoInt = daoFactory.GetFolderDao<int>();
        var folderDao = daoFactory.GetFolderDao<string>();

        if (thirdPartyParams == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        int folderId;
        FolderType folderType;

        if (thirdPartyParams.Corporate)
        {
            folderId = await globalFolderHelper.FolderCommonAsync;
            folderType = FolderType.COMMON;
        }
        else if (thirdPartyParams.RoomsStorage)
        {
            folderId = await globalFolderHelper.FolderVirtualRoomsAsync;
            folderType = FolderType.VirtualRooms;
        }
        else
        {
            folderId = await globalFolderHelper.FolderMyAsync;
            folderType = FolderType.USER;
        }

        var parentFolder = await folderDaoInt.GetFolderAsync(folderId);

        if (!await fileSecurity.CanCreateAsync(parentFolder))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }

        if (!await filesSettingsHelper.GetEnableThirdParty())
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }

        var currentFolderType = FolderType.USER;
        int currentProviderId;

        MessageAction messageAction;
        if (thirdPartyParams.ProviderId == null)
        {
            if (!thirdpartyConfiguration.SupportInclusion(daoFactory) || !await filesSettingsHelper.GetEnableThirdParty())
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
            }

            thirdPartyParams.CustomerTitle = Global.ReplaceInvalidCharsAndTruncate(thirdPartyParams.CustomerTitle);

            if (string.IsNullOrEmpty(thirdPartyParams.CustomerTitle))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_InvalidTitle);
            }

            try
            {
                currentProviderId = await providerDao.SaveProviderInfoAsync(thirdPartyParams.ProviderKey, thirdPartyParams.CustomerTitle, thirdPartyParams.AuthData, folderType);
                messageAction = MessageAction.ThirdPartyCreated;
            }
            catch (UnauthorizedAccessException e)
            {                
                throw OperationService.GenerateException(e, logger, authContext, true);
            }
            catch (Exception e)
            {                
                throw OperationService.GenerateException(e.InnerException ?? e, logger, authContext, true);
            }
        }
        else
        {
            currentProviderId = thirdPartyParams.ProviderId.Value;

            var currentProvider = await providerDao.GetProviderInfoAsync(currentProviderId);
            if (currentProvider.Owner != authContext.CurrentAccount.ID)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            currentFolderType = currentProvider.RootFolderType;

            switch (currentProvider.RootFolderType)
            {
                case FolderType.COMMON when !thirdPartyParams.Corporate:
                    {
                        var lostFolder = await folderDao.GetFolderAsync(currentProvider.RootFolderId);
                        await fileMarker.RemoveMarkAsNewForAllAsync(lostFolder);
                        break;
                    }
                case FolderType.VirtualRooms or FolderType.RoomTemplates or FolderType.Archive:
                    {
                        var updatedProvider = await providerDao.UpdateRoomProviderInfoAsync(new ProviderData { Id = currentProviderId, AuthData = thirdPartyParams.AuthData });
                        currentProviderId = updatedProvider.ProviderId;
                        break;
                    }
                default:
                    currentProviderId = await providerDao.UpdateProviderInfoAsync(currentProviderId, thirdPartyParams.CustomerTitle, thirdPartyParams.AuthData, folderType);
                    break;
            }

            messageAction = MessageAction.ThirdPartyUpdated;
        }

        var provider = await providerDao.GetProviderInfoAsync(currentProviderId);
        await provider.InvalidateStorageAsync();

        var folderDao1 = daoFactory.GetFolderDao<string>();
        var folder = await folderDao1.GetFolderAsync(provider.RootFolderId);
        if (!await fileSecurity.CanReadAsync(folder))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ViewFolder);
        }

        await filesMessageService.SendAsync(messageAction, parentFolder, folder.Id, provider.ProviderKey);

        if (thirdPartyParams.Corporate && currentFolderType != FolderType.COMMON)
        {
            await fileMarker.MarkAsNewAsync(folder);
        }

        return folder;
    }

    /// <summary>
    /// Saves a backup of third-party integration data asynchronously and returns the folder containing the backup.
    /// </summary>
    /// <param name="thirdPartyParams">The parameters associated with the third-party integration, including authentication and provider details.</param>
    /// <returns>A <see cref="Folder{T}"/> containing the backup data for the specified third-party integration. If the operation fails or is invalid, null may be returned.</returns>
    public async ValueTask<Folder<string>> SaveThirdPartyBackupAsync(ThirdPartyParams thirdPartyParams)
    {
        var providerDao = daoFactory.ProviderDao;

        if (providerDao == null)
        {
            return null;
        }

        if (thirdPartyParams == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        if (!await filesSettingsHelper.GetEnableThirdParty())
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }

        var folderType = FolderType.ThirdpartyBackup;

        int curProviderId;

        MessageAction messageAction;

        var thirdparty = await GetBackupThirdPartyAsync();
        if (thirdparty == null)
        {
            if (!thirdpartyConfiguration.SupportInclusion(daoFactory) || !await filesSettingsHelper.GetEnableThirdParty())
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
            }

            thirdPartyParams.CustomerTitle = Global.ReplaceInvalidCharsAndTruncate(thirdPartyParams.CustomerTitle);
            if (string.IsNullOrEmpty(thirdPartyParams.CustomerTitle))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_InvalidTitle);
            }

            try
            {
                curProviderId = await providerDao.SaveProviderInfoAsync(thirdPartyParams.ProviderKey, thirdPartyParams.CustomerTitle, thirdPartyParams.AuthData, folderType);
                messageAction = MessageAction.ThirdPartyCreated;
            }
            catch (UnauthorizedAccessException e)
            {                
                throw OperationService.GenerateException(e, logger, authContext, true);
            }
            catch (Exception e)
            {                
                throw OperationService.GenerateException(e.InnerException ?? e, logger, authContext);
            }
        }
        else
        {
            curProviderId = await providerDao.UpdateBackupProviderInfoAsync(thirdPartyParams.ProviderKey, thirdPartyParams.CustomerTitle, thirdPartyParams.AuthData);
            messageAction = MessageAction.ThirdPartyUpdated;
        }

        var provider = await providerDao.GetProviderInfoAsync(curProviderId);
        await provider.InvalidateStorageAsync();

        var folderDao1 = daoFactory.GetFolderDao<string>();
        var folder = await folderDao1.GetFolderAsync(provider.RootFolderId);

        filesMessageService.Send(messageAction, folder.Id, provider.ProviderKey);

        return folder;
    }

    /// <summary>
    /// Deletes a third-party integration by its provider ID.
    /// </summary>
    /// <param name="providerId">The ID of the third-party provider to be deleted.</param>
    /// <returns>A <see cref="string"/> representing the ID of the deleted third-party folder. Returns null if the target provider is not found or an error occurs.</returns>
    public async ValueTask<string> DeleteThirdPartyAsync(string providerId)
    {
        var providerDao = daoFactory.ProviderDao;
        if (providerDao == null)
        {
            return null;
        }

        var curProviderId = Convert.ToInt32(providerId);
        var providerInfo = await providerDao.GetProviderInfoAsync(curProviderId);

        var folder = entryManager.GetFakeThirdpartyFolder(providerInfo);
        if (!await fileSecurity.CanDeleteAsync(folder))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_DeleteFolder);
        }

        if (providerInfo.RootFolderType == FolderType.COMMON)
        {
            await fileMarker.RemoveMarkAsNewForAllAsync(folder);
        }

        await providerDao.RemoveProviderInfoAsync(folder.ProviderId);
        await filesMessageService.SendAsync(MessageAction.ThirdPartyDeleted, folder, folder.Id, providerInfo.ProviderKey);

        return folder.Id;
    }

    /// <summary>
    /// Saves the DocuSign token retrieved using the provided authentication code.
    /// </summary>
    /// <param name="code">The authentication code used to generate the DocuSign token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the token was successfully saved.</returns>
    public async Task<bool> SaveDocuSignAsync(string code)
    {
        if (!authContext.IsAuthenticated || await userManager.IsGuestAsync(authContext.CurrentAccount.ID) || !await filesSettingsHelper.GetEnableThirdParty() || !thirdpartyConfiguration.SupportDocuSignInclusion)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }

        var token = consumerFactory.Get<DocuSignLoginProvider>().GetAccessToken(code);
        await docuSignHelper.ValidateTokenAsync(token);
        await docuSignToken.SaveTokenAsync(token);

        return true;
    }

    /// <summary>
    /// Deletes the stored DocuSign token asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation to delete the token for DocuSign integration.</returns>
    public async Task DeleteDocuSignAsync()
    {
        await docuSignToken.DeleteTokenAsync();
    }

    /// <summary>
    /// Sends a document for signing using DocuSign with the specified file and signing metadata.
    /// </summary>
    /// <param name="fileId">The identifier of the file to be sent for signing.</param>
    /// <param name="docuSignData">The data containing metadata and settings required for DocuSign.</param>
    /// <typeparam name="T">The type of the file identifier.</typeparam>
    /// <returns>A <see cref="Task{T}"/> that represents the asynchronous operation. The task result contains the unique identifier for the sent document in DocuSign.</returns>
    public async Task<string> SendDocuSignAsync<T>(T fileId, DocuSignData docuSignData)
    {
        try
        {
            if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID) || !await filesSettingsHelper.GetEnableThirdParty() || !thirdpartyConfiguration.SupportDocuSignInclusion)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
            }

            return await docuSignHelper.SendDocuSignAsync(fileId, docuSignData);
        }
        catch (Exception e)
        {
            throw OperationService.GenerateException(e, logger, authContext);
        }
    }
    
    private static IAsyncEnumerable<ThirdPartyParams> InternalGetThirdPartyAsync(IProviderDao providerDao)
    {
        return providerDao.GetProvidersInfoAsync().Select(r => new ThirdPartyParams
        {
            CustomerTitle = r.CustomerTitle,
            Corporate = r.RootFolderType == FolderType.COMMON,
            RoomsStorage = r.RootFolderType is FolderType.VirtualRooms or FolderType.RoomTemplates or FolderType.Archive,
            ProviderId = r.ProviderId,
            ProviderKey = r.ProviderKey
        });
    }
}