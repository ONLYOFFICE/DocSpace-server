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
/// The FormService class provides methods for managing and manipulating form-related workflows,
/// such as managing form roles, handling form filling actions, saving forms as PDF documents,
/// and checking draft statuses of forms.
/// </summary>
[Scope]
public class FormService(
    AuthContext authContext,
    UserManager userManager,
    FileUtility fileUtility,
    FilesLinkUtility filesLinkUtility,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    PathProvider pathProvider,
    FileSecurity fileSecurity,
    SocketManager socketManager,
    IDaoFactory daoFactory,
    FileMarker fileMarker,
    EntryManager entryManager,
    FilesMessageService filesMessageService,
    DocumentServiceHelper documentServiceHelper,
    DocumentServiceConnector documentServiceConnector,
    FileSharing fileSharing,
    NotifyClient notifyClient,
    IServiceProvider serviceProvider,
    ExternalShare externalShare,
    IHttpClientFactory clientFactory,
    TempStream tempStream,
    SecurityContext securityContext,
    FormRoleDtoHelper formRoleDtoHelper,
    WebhookManager webhookManager,
    SharingService sharingService,
    ILogger<FormService> logger)
{
    /// <summary>
    /// Initiates the process of filling a PDF form. This method ensures the file and its parent folder settings
    /// are valid for form filling and performs necessary operations like updating properties and triggering sockets for synchronization.
    /// </summary>
    /// <typeparam name="T">The type of the file identifier.</typeparam>
    /// <param name="fileId">The identifier of the file to start the filling process for.</param>
    /// <returns>The file object with updated properties and synchronization.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file with the specified ID is not found.</exception>
    public async Task<File<T>> StartFillingAsync<T>(T fileId)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();
        var file = await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        var folder = await folderDao.GetFolderAsync(file.ParentId);

        if (folder.FolderType == FolderType.FillingFormsRoom && FileUtility.GetFileTypeByFileName(file.Title) == FileType.Pdf)
        {
            var ace = await fileSharing.GetPureSharesAsync(folder, new List<Guid> { authContext.CurrentAccount.ID }).FirstOrDefaultAsync();
            if (ace is { Access: FileShare.FillForms })
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
            }

            var properties = await fileDao.GetProperties(fileId) ?? new EntryProperties<T> { FormFilling = new FormFillingProperties<T>() };
            properties.FormFilling.StartFilling = true;
            properties.FormFilling.OriginalFormId = fileId;

            await fileDao.SaveProperties(fileId, properties);

            var count = await sharingService.GetPureSharesCountAsync(folder.Id, FileEntryType.Folder, ShareFilterType.UserOrGroup, "");
            await socketManager.CreateFormAsync(file, securityContext.CurrentAccount.ID, count <= 1);
            await socketManager.CreateFileAsync(file);
        }

        return file;
    }

    /// <summary>
    /// Checks and prepares a draft form for filling, ensuring the necessary file properties, permissions, and configurations
    /// are in place before providing a link to open the form in the web editor.
    /// </summary>
    /// <typeparam name="T">The type of the file identifier.</typeparam>
    /// <param name="fileId">The identifier of the file to check for a fillable form draft.</param>
    /// <param name="version">The version of the file to consider for the draft.</param>
    /// <param name="editPossible">Indicates whether editing is allowed for this file.</param>
    /// <param name="view">Indicates whether the request is for viewing mode only.</param>
    /// <returns>A link to the web editor for filling the form draft.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user does not have sufficient permissions to fill the form.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified file cannot be located.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the file cannot be prepared as a fillable form due to validation or configuration issues.</exception>
    public async Task<string> CheckFillFormDraftAsync<T>(T fileId, int version, bool editPossible, bool view)
    {
        var (file, configuration, _) = await documentServiceHelper.GetParamsAsync(fileId, version, editPossible, !view, true, editPossible);
        var properties = await daoFactory.GetFileDao<T>().GetProperties(file.Id);

        var linkId = await externalShare.GetLinkIdAsync();
        if (linkId != Guid.Empty)
        {
            configuration.Document.SharedLinkKey += externalShare.GetKey();
        }

        if (configuration.EditorConfig.ModeWrite
            && fileUtility.CanWebRestrictedEditing(file.Title)
            && await fileSecurity.CanFillFormsAsync(file)
            && !await fileSecurity.CanEditAsync(file)
            && (properties != null && properties.FormFilling.StartFilling))
        {
            if (!await entryManager.LinkedForMeAsync(file))
            {
                await fileMarker.RemoveMarkAsNewAsync(file);

                Folder<T> folderIfNew;
                File<T> form;
                try
                {
                    (form, folderIfNew) = await entryManager.GetFillFormDraftAsync(file, file.ParentId);
                }
                catch (Exception ex)
                {
                    logger.ErrorDocEditor(ex);
                    throw;
                }

                var comment = folderIfNew == null
                    ? string.Empty
                    : "#message/" + HttpUtility.UrlEncode(string.Format(FilesCommonResource.MessageFillFormDraftCreated, folderIfNew.Title));

                await socketManager.StopEditAsync(fileId);
                return filesLinkUtility.GetFileWebEditorUrl(form.Id) + comment;
            }

            if (!await entryManager.CheckFillFormDraftAsync(file))
            {
                var comment = "#message/" + HttpUtility.UrlEncode(FilesCommonResource.MessageFillFormDraftDiscard);

                return filesLinkUtility.GetFileWebEditorUrl(file.Id) + comment;
            }
        }

        return filesLinkUtility.GetFileWebEditorUrl(file.Id);
    }

    /// <summary>
    /// Saves a specified file as a PDF document. This operation converts the file to PDF format
    /// and stores it in the specified folder with the provided title.
    /// </summary>
    /// <typeparam name="T">The type of the file identifier.</typeparam>
    /// <param name="fileId">The identifier of the file to be converted to PDF.</param>
    /// <param name="folderId">The identifier of the folder where the new PDF file will be saved.</param>
    /// <param name="title">The title of the resulting PDF file.</param>
    /// <returns>The newly created PDF file object.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file or folder with the specified ID is not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user does not have permission to save the file in the specified folder.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the operation fails due to invalid file type or other processing issues.</exception>
    public async Task<File<T>> SaveAsPdf<T>(T fileId, T folderId, string title)
    {
        try
        {
            var fileDao = daoFactory.GetFileDao<T>();
            var folderDao = daoFactory.GetFolderDao<T>();

            var file = await fileDao.GetFileAsync(fileId);
            file.NotFoundIfNull();
            if (!await fileSecurity.CanReadAsync(file))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            var folder = await folderDao.GetFolderAsync(folderId);
            folder.NotFoundIfNull();
            if (!await fileSecurity.CanCreateAsync(folder))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            var fileUri = pathProvider.GetFileStreamUrl(file);
            var fileExtension = file.ConvertedExtension;
            var docKey = await documentServiceHelper.GetDocKeyAsync(file);

            fileUri = documentServiceConnector.ReplaceCommunityAddress(fileUri);

            var (_, convertedDocumentUri, _) = await documentServiceConnector.GetConvertedUriAsync(fileUri, fileExtension, "pdf", docKey, null, CultureInfo.CurrentUICulture.Name, null, null, null, false, false);

            var pdfFile = serviceProvider.GetService<File<T>>();
            pdfFile.Title = !string.IsNullOrEmpty(title) ? $"{title}.pdf" : FileUtility.ReplaceFileExtension(file.Title, "pdf");
            pdfFile.ParentId = folder.Id;
            pdfFile.Comment = FilesCommonResource.CommentCreate;

            var request = new HttpRequestMessage { RequestUri = new Uri(convertedDocumentUri) };

            var httpClient = clientFactory.CreateClient();
            using var response = await httpClient.SendAsync(request);
            await using var fileStream = await response.Content.ReadAsStreamAsync();
            File<T> result;

            if (fileStream.CanSeek)
            {
                pdfFile.ContentLength = fileStream.Length;
                result = await fileDao.SaveFileAsync(pdfFile, fileStream);
            }
            else
            {
                var (buffered, isNew) = await tempStream.TryGetBufferedAsync(fileStream);
                try
                {
                    pdfFile.ContentLength = buffered.Length;
                    result = await fileDao.SaveFileAsync(pdfFile, buffered);
                }
                finally
                {
                    if (isNew)
                    {
                        await buffered.DisposeAsync();
                    }
                }
            }

            if (result != null)
            {
                await filesMessageService.SendAsync(MessageAction.FileCreated, result, result.Title);
                await fileMarker.MarkAsNewAsync(result);
                await socketManager.CreateFileAsync(result);
                await webhookManager.PublishAsync(WebhookTrigger.FileCreated, result);
            }

            return result;
        }
        catch (Exception e)
        {
            throw FileStorageService.GenerateException(e, logger, authContext);
        }
    }

    /// <summary>
    /// Saves the form-role mapping for a given file. Ensures proper permissions are validated before applying the new roles.
    /// Updates file properties as necessary and triggers synchronization updates.
    /// </summary>
    /// <typeparam name="T">The type of the file identifier.</typeparam>
    /// <param name="formId">The identifier of the form to apply the role mappings to.</param>
    /// <param name="roles">A collection of <see cref="FormRole"/> objects representing the roles to be mapped to the form.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current user lacks sufficient permissions to modify the form roles.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified form cannot be located in the system.</exception>
    public async Task SaveFormRoleMapping<T>(T formId, IEnumerable<FormRole> roles)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();

        var form = await fileDao.GetFileAsync(formId);
        var currentRoom = await DocSpaceHelper.GetParentRoom(form, folderDao);

        await ValidateChangeRolesPermission(form);

        if ((roles?.Any() == false && !await fileSecurity.CanResetFillingAsync(form, authContext.CurrentAccount.ID)) ||
            (roles?.Any() == true && !await fileSecurity.CanStartFillingAsync(form, authContext.CurrentAccount.ID)))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
        }

        await fileDao.SaveFormRoleMapping(formId, roles);

        var properties = await fileDao.GetProperties(formId) ?? new EntryProperties<T> { FormFilling = new FormFillingProperties<T>() };
        if (roles?.Any() == false)
        {
            await fileDao.SaveProperties(formId, null);
        }
        else
        {
            properties.FormFilling.StartFilling = true;
            properties.FormFilling.StartedByUserId = authContext.CurrentAccount.ID;
            await fileDao.SaveProperties(formId, properties);
            var user = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);
            await filesMessageService.SendAsync(MessageAction.FormStartedToFill, form, MessageInitiator.DocsService, user?.DisplayUserName(false, displayUserSettingsHelper), form.Title);

            var currentUserId = authContext.CurrentAccount.ID;
            var recipients = roles
                .Where(role => role.UserId != currentUserId)
                .Select(role => role.UserId)
                .Distinct()
                .ToList();

            if (recipients.Count > 0)
            {
                await notifyClient.SendFormFillingEvent(
                    currentRoom, form, recipients, NotifyConstants.EventFormStartedFilling, currentUserId);
            }

            var roleUserIds = roles.Where(r => r.UserId != currentUserId).Select(r => r.UserId);

            var aces = fileSecurity.GetPureSharesAsync(currentRoom, roleUserIds);

            var formFillers = await aces.Where(ace => ace is { Share: FileShare.FillForms }).Select(s => s.Subject).ToListAsync();

            if (formFillers.Count != 0)
            {
                if (!form.ParentId.Equals(currentRoom.Id))
                {
                    var parentFolders = await folderDao.GetParentFoldersAsync(form.ParentId).Where(f => !DocSpaceHelper.IsRoom(f.FolderType)).ToListAsync();
                    foreach (var folder in parentFolders)
                    {
                        await socketManager.CreateFolderAsync(folder, formFillers);
                    }
                }
                await socketManager.CreateFileAsync(form, formFillers);
            }

        }

        await socketManager.UpdateFileAsync(form);
    }

    /// <summary>
    /// Retrieves all form roles associated with a specific form. This method ensures
    /// that the specified form exists, checks its type validity, and retrieves the roles
    /// associated with it. Additional properties and conditions are applied to provide
    /// role-specific details.
    /// </summary>
    /// <typeparam name="T">The type of the form identifier.</typeparam>
    /// <param name="formId">The identifier of the form for which roles are to be retrieved.</param>
    /// <returns>An asynchronous enumerable containing <see cref="FormRoleDto"/> objects representing the roles of the specified form.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the form with the specified ID does not exist,
    /// or when the file is not a form or a completed form.
    /// </exception>
    public async IAsyncEnumerable<FormRoleDto> GetAllFormRoles<T>(T formId)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var form = await fileDao.GetFileAsync(formId);

        if (form == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }
        if (!await DocSpaceHelper.IsFormOrCompletedForm(form, daoFactory))
        {
            throw new InvalidOperationException();
        }
        var roles = await fileDao.GetFormRoles(formId).ToListAsync();
        var properties = await daoFactory.GetFileDao<T>().GetProperties(formId);
        var currentStep = roles.Where(r => !r.Submitted).Min(r => (int?)r.Sequence) ?? 0;


        foreach (var r in roles)
        {
            var role = await formRoleDtoHelper.Get(properties, r);
            if (!DateTime.MinValue.Equals(properties.FormFilling.FillingStopedDate) &&
                properties.FormFilling.FormFillingInterruption?.RoleName == role.RoleName)
            {
                role.RoleStatus = FormFillingStatus.Stoped;
            }
            else
            {
                role.RoleStatus = currentStep switch
                {
                    0 => FormFillingStatus.Complete,
                    _ when currentStep > role.Sequence => FormFillingStatus.Complete,
                    _ when currentStep < role.Sequence => FormFillingStatus.Draft,
                    _ when currentStep == role.Sequence && !role.Submitted && r.OpenedAt.Equals(DateTime.MinValue) => FormFillingStatus.YouTurn,
                    _ when currentStep == role.Sequence && !role.Submitted && !r.OpenedAt.Equals(DateTime.MinValue) => FormFillingStatus.InProgress,
                    _ => FormFillingStatus.Complete
                };
            }

            yield return role;
        }
    }

    /// <summary>
    /// Manages the form filling process by allowing it to be either resumed or stopped based on the specified action.
    /// Updates form properties, manages associated roles, and notifies users about the changes.
    /// </summary>
    /// <typeparam name="T">The type of the form identifier.</typeparam>
    /// <param name="formId">The identifier of the form for which the action is to be performed.</param>
    /// <param name="action">The action to be performed, such as stopping or resuming the form filling process.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an unsupported action is provided.</exception>
    public async Task ManageFormFilling<T>(T formId, FormFillingManageAction action)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();
        var form = await fileDao.GetFileAsync(formId);
        await ValidateChangeRolesPermission(form);

        var properties = await daoFactory.GetFileDao<T>().GetProperties(formId);
        switch (action)
        {
            case FormFillingManageAction.Stop:
                if (!await fileSecurity.CanStopFillingAsync(form, authContext.CurrentAccount.ID))
                {
                    throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
                }
                var role = await fileDao.GetFormRoles(formId).Where(r => r.Submitted == false).FirstOrDefaultAsync();
                properties.FormFilling.FillingStopedDate = DateTime.UtcNow;
                properties.FormFilling.FormFillingInterruption =
                    new FormFillingInterruption
                    {
                        UserId = authContext.CurrentAccount.ID,
                        RoleName = role?.RoleName
                    };
                var room = await DocSpaceHelper.GetParentRoom(form, folderDao);
                var allRoleUserIds = await fileDao.GetFormRoles(form.Id).Where(role => role.UserId != authContext.CurrentAccount.ID).Select(r => r.UserId).ToListAsync();

                var user = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);
                await filesMessageService.SendAsync(MessageAction.FormStopped, form, MessageInitiator.DocsService, user?.DisplayUserName(false, displayUserSettingsHelper), form.Title);
                await notifyClient.SendFormFillingEvent(room, form, allRoleUserIds, NotifyConstants.EventStoppedFormFilling, authContext.CurrentAccount.ID);
                break;

            case FormFillingManageAction.Resume:
                properties.FormFilling.FillingStopedDate = DateTime.MinValue;
                properties.FormFilling.FormFillingInterruption = null;
                break;

            default:
                throw new InvalidOperationException();
        }

        await fileDao.SaveProperties(formId, properties);
        await socketManager.UpdateFileAsync(form);
    }
    
    private async Task ValidateChangeRolesPermission<T>(File<T> form)
    {
        if (form == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }
        if (!form.IsForm)
        {
            throw new InvalidOperationException();
        }


        var folderDao = daoFactory.GetFolderDao<T>();
        var currentRoom = await DocSpaceHelper.GetParentRoom(form, folderDao);

        if (currentRoom == null)
        {
            throw new InvalidOperationException();
        }
    }
}