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
/// The DocumentProcessingService class provides methods for managing document processing, including editing, tracking, history retrieval, and file conversion.
/// </summary>
/// <remarks>
/// This service integrates with various dependencies to handle document-related operations effectively. It serves as the core processing unit for document lifecycle management in the application.
/// </remarks>
[Scope]
public class DocumentProcessingService(
    AuthContext authContext,
    FileSecurity fileSecurity,
    SocketManager socketManager,
    IDaoFactory daoFactory,
    EntryManager entryManager,
    DocumentServiceTrackerHelper documentServiceTrackerHelper,
    FileConverter fileConverter,
    DocumentServiceHelper documentServiceHelper,
    IServiceProvider serviceProvider,
    TenantManager tenantManager,
    FileTrackerHelper fileTracker,
    ExternalShare externalShare,
    ILogger<FileOperationsService> logger)
{
    /// Tracks the editing process of a specified file, validating the document key and updating the editing state.
    /// This helps in managing the document operations and ensures proper tracking of file modifications within the service.
    /// <param name="fileId">The unique identifier of the file being tracked.</param>
    /// <param name="tabId">The identifier for the editing tab or session associated with the file.</param>
    /// <param name="docKeyForTrack">The document key used for tracking and verifying the file edit session.</param>
    /// <param name="isFinish">A boolean value indicating whether the editing session is being completed. Defaults to false.</param>
    /// <returns>A key-value pair where the boolean indicates success (true) or failure (false), and the string provides additional information or an error message.</returns>
    /// <exception cref="SecurityException">Thrown if the user is not authenticated or lacks proper permissions to track the specified file.</exception>
    /// <exception cref="Exception">Thrown if an error occurs during the file tracking process, such as invalid document keys or system-related issues.</exception>
    public async Task<KeyValuePair<bool, string>> TrackEditFileAsync<T>(T fileId, Guid tabId, string docKeyForTrack, bool isFinish = false)
    {
        try
        {
            if (!authContext.IsAuthenticated && await externalShare.GetLinkIdAsync() == Guid.Empty)
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            var (file, _) = await documentServiceHelper.GetCurFileInfoAsync(fileId, -1);

            if (docKeyForTrack != await documentServiceHelper.GetDocKeyAsync(fileId, -1, DateTime.MinValue) && docKeyForTrack != await documentServiceHelper.GetDocKeyAsync(file.Id, file.Version, file.ProviderEntry ? file.ModifiedOn : file.CreateOn))
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            if (isFinish)
            {
                await fileTracker.RemoveAsync(fileId, tabId);
                await socketManager.StopEditAsync(fileId);
            }
            else
            {
                await entryManager.TrackEditingAsync(fileId, tabId, authContext.CurrentAccount.ID, tenantManager.GetCurrentTenant());
            }

            return new KeyValuePair<bool, string>(true, string.Empty);
        }
        catch (Exception ex)
        {
            return new KeyValuePair<bool, string>(false, ex.Message);
        }
    }

    /// Initiates the editing session for a specified file by generating the appropriate key required for tracking the document
    /// within the document service.
    /// <param name="fileId">The unique identifier of the file to be edited.</param>
    /// <param name="editingAlone">A boolean value indicating whether the document editing should be carried out in a single-user mode. Defaults to false.</param>
    /// <returns>A string representing the unique key required for the document editing session, determined by the document service.</returns>
    /// <exception cref="Exception">Throws an exception if initiating the editing session fails due to internal errors or invalid file parameters.</exception>
    public async Task<string> StartEditAsync<T>(T fileId, bool editingAlone = false)
    {
        try
        {
            if (editingAlone)
            {
                if (await fileTracker.IsEditingAsync(fileId))
                {
                    throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_EditFileTwice);
                }

                await entryManager.TrackEditingAsync(fileId, Guid.Empty, authContext.CurrentAccount.ID, tenantManager.GetCurrentTenant(), true);

                //without StartTrack, track via old scheme
                return await documentServiceHelper.GetDocKeyAsync(fileId, -1, DateTime.MinValue);
            }

            var fileOptions = await documentServiceHelper.GetParamsAsync(fileId, -1, true, true, false, true);

            var configuration = fileOptions.Configuration;
            if (!configuration.EditorConfig.ModeWrite || !(configuration.Document.Permissions.Edit || configuration.Document.Permissions.ModifyFilter || configuration.Document.Permissions.Review
                                                           || configuration.Document.Permissions.FillForms || configuration.Document.Permissions.Comment))
            {
                throw new InvalidOperationException(!string.IsNullOrEmpty(configuration.Error) ? configuration.Error : FilesCommonResource.ErrorMessage_SecurityException_EditFile);
            }

            var key = configuration.Document.Key;

            if (!await documentServiceTrackerHelper.StartTrackAsync(fileId.ToString(), key))
            {
                throw new Exception(FilesCommonResource.ErrorMessage_StartEditing);
            }

            return key;
        }
        catch (Exception e)
        {
            await fileTracker.RemoveAsync(fileId);

            throw GenerateException(e);
        }
    }

    /// Retrieves the editing history of a specified file asynchronously, ensuring valid permissions and file existence.
    /// This method yields the history in an enumerable format for efficient streaming of results.
    /// <param name="fileId">The unique identifier of the file for which the editing history is being retrieved.</param>
    /// <typeparam name="T">The type of the identifier used for the file.</typeparam>
    /// <returns>An asynchronous enumerable of the file's edit history, containing information about past modifications.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the file is not found, access is denied due to insufficient security, or if the file is improperly requested from an external provider.</exception>
    public async IAsyncEnumerable<EditHistory> GetEditHistoryAsync<T>(T fileId)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (!await fileSecurity.CanReadHistoryAsync(file))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
        }

        if (file.ProviderEntry)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        await foreach (var f in fileDao.GetEditHistoryAsync(documentServiceHelper, file.Id))
        {
            yield return f;
        }
    }

    /// Performs a conversion check on the specified files asynchronously and returns the results as an enumerable sequence.
    /// This process evaluates the conversion status of specified files and provides results for further operations or monitoring.
    /// <param name="filesInfoJson">A list of file information, including the details required to check the conversion status.</param>
    /// <param name="sync">A boolean value indicating whether the operation should be performed synchronously. Defaults to false.</param>
    /// <returns>An asynchronous enumerable sequence of <see cref="FileOperationResult"/> instances, each containing the result of the conversion check for a specific file.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the provided file information list is null or empty.</exception>
    /// <exception cref="Exception">Thrown if an error occurs during the conversion check process.</exception>
    public async IAsyncEnumerable<FileOperationResult> CheckConversionAsync<T>(List<CheckConversionRequestDto<T>> filesInfoJson, bool sync = false)
    {
        if (filesInfoJson == null || filesInfoJson.Count == 0)
        {
            yield break;
        }

        var results = AsyncEnumerable.Empty<FileOperationResult>();
        var fileDao = daoFactory.GetFileDao<T>();
        var files = new List<KeyValuePair<File<T>, bool>>();
        foreach (var fileInfo in filesInfoJson)
        {
            var file = fileInfo.Version > 0
                ? await fileDao.GetFileAsync(fileInfo.FileId, fileInfo.Version)
                : await fileDao.GetFileAsync(fileInfo.FileId);

            if (file == null)
            {
                var newFile = serviceProvider.GetService<File<T>>();
                newFile.Id = fileInfo.FileId;
                newFile.Version = fileInfo.Version;

                files.Add(new KeyValuePair<File<T>, bool>(newFile, true));

                continue;
            }

            if (!await fileSecurity.CanConvertAsync(file))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
            }

            if (fileInfo.StartConvert && fileConverter.MustConvert(file))
            {
                try
                {
                    if (sync)
                    {
                        results = results.Append(await fileConverter.ExecSynchronouslyAsync(file, !fileInfo.CreateNewIfExist, fileInfo.OutputType));
                    }
                    else
                    {
                        await fileConverter.ExecAsynchronouslyAsync(file, false, !fileInfo.CreateNewIfExist, fileInfo.Password, fileInfo.OutputType);
                    }
                }
                catch (Exception e)
                {
                    throw GenerateException(e);
                }
            }

            files.Add(new KeyValuePair<File<T>, bool>(file, false));
        }

        if (!sync)
        {
            results = fileConverter.GetStatusAsync(files);
        }

        await foreach (var res in results)
        {
            yield return res;
        }
    }
    
    private Exception GenerateException(Exception error, bool warning = false)
    {
        if (warning || error is ItemNotFoundException or SecurityException or ArgumentException or TenantQuotaException or InvalidOperationException)
        {
            logger.Information(error.ToString());
        }
        else
        {
            logger.ErrorFileStorageService(error);
        }

        if (error is ItemNotFoundException)
        {
            return !authContext.CurrentAccount.IsAuthenticated
                ? new SecurityException(FilesCommonResource.ErrorMessage_SecurityException)
                : error;
        }

        return new InvalidOperationException(error.Message, error);
    }
}