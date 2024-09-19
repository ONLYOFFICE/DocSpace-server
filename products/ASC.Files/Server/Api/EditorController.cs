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

using ASC.Files.Core.ApiModels.RequestDto;
using ASC.Web.Api.Core;

namespace ASC.Files.Api;

[ConstraintRoute("int")]
[DefaultRoute("file")]
public class EditorControllerInternal(FileStorageService fileStorageService,
        DocumentServiceHelper documentServiceHelper,
        EncryptionKeyPairDtoHelper encryptionKeyPairDtoHelper,
        SettingsManager settingsManager,
        EntryManager entryManager,
        IHttpContextAccessor httpContextAccessor,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        ExternalShare externalShare,
        AuthContext authContext,
        ConfigurationConverter<int> configurationConverter,
        IDaoFactory daoFactory,
        SecurityContext securityContext)
        : EditorController<int>(fileStorageService, documentServiceHelper, encryptionKeyPairDtoHelper, settingsManager, entryManager, httpContextAccessor, folderDtoHelper, fileDtoHelper, externalShare, authContext, configurationConverter, daoFactory, securityContext);

[DefaultRoute("file")]
public class EditorControllerThirdparty(FileStorageService fileStorageService,
        DocumentServiceHelper documentServiceHelper,
        EncryptionKeyPairDtoHelper encryptionKeyPairDtoHelper,
        SettingsManager settingsManager,
        EntryManager entryManager,
        IHttpContextAccessor httpContextAccessor,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        ExternalShare externalShare,
        AuthContext authContext,
        ConfigurationConverter<string> configurationConverter,
        IDaoFactory daoFactory,
        SecurityContext securityContext)
        : EditorController<string>(fileStorageService, documentServiceHelper, encryptionKeyPairDtoHelper, settingsManager, entryManager, httpContextAccessor, folderDtoHelper, fileDtoHelper, externalShare, authContext, configurationConverter, daoFactory, securityContext);

public abstract class EditorController<T>(FileStorageService fileStorageService,
        DocumentServiceHelper documentServiceHelper,
        EncryptionKeyPairDtoHelper encryptionKeyPairDtoHelper,
        SettingsManager settingsManager,
        EntryManager entryManager,
        IHttpContextAccessor httpContextAccessor,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        ExternalShare externalShare,
        AuthContext authContext,
        ConfigurationConverter<T> configurationConverter,
        IDaoFactory daoFactory,
        SecurityContext securityContext)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{

    /// <summary>
    /// Saves edits to a file with the ID specified in the request.
    /// </summary>
    /// <short>Save file edits</short>
    /// <path>api/2.0/files/file/{fileId}/saveediting</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Saved file parameters", typeof(FileDto<int>))]
    [HttpPut("{fileId}/saveediting")]
    public async Task<FileDto<T>> SaveEditingFromFormAsync(SaveEditingRequestDto<T> inDto)
    {
        await using var stream = httpContextAccessor.HttpContext.Request.Body;

        return await _fileDtoHelper.GetAsync(await fileStorageService.SaveEditingAsync(inDto.FileId, inDto.File.FileExtension, inDto.File.DownloadUri, stream, inDto.File.Forcesave));
    }

    /// <summary>
    /// Informs about opening a file with the ID specified in the request for editing, locking it from being deleted or moved (this method is called by the mobile editors).
    /// </summary>
    /// <short>Start file editing</short>
    /// <path>api/2.0/files/file/{fileId}/startedit</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File key for Document Service", typeof(object))]
    [HttpPost("{fileId}/startedit")]
    public async Task<object> StartEditAsync(StartEditRequestDto<T> inDto)
    {
        return await fileStorageService.StartEditAsync(inDto.FileId, inDto.File.EditingAlone);
    }

    /// <summary>
    /// Starts filling a file with the ID specified in the request.
    /// </summary>
    /// <short>Starts filling</short>
    /// <path>api/2.0/files/file/{fileId}/startfilling</path>
    [Tags("Files / Files")]
    [HttpPut("{fileId}/startfilling")]
    public async Task StartFillingAsync(StartFillingRequestDto<T> inDto)
    {
        await fileStorageService.StartFillingAsync(inDto.FileId);
    }

    /// <summary>
    /// Tracks file changes when editing.
    /// </summary>
    /// <short>Track file editing</short>
    /// <path>api/2.0/files/file/{fileId}/trackeditfile</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File changes", typeof(KeyValuePair<bool, string>))]
    [HttpGet("{fileId}/trackeditfile")]
    public async Task<KeyValuePair<bool, string>> TrackEditFileAsync(TrackEditFileRequestDto<T> inDto)
    {
        return await fileStorageService.TrackEditFileAsync(inDto.FileId, inDto.File.TabId, inDto.File.DocKeyForTrack, inDto.File.IsFinish);
    }

    /// <summary>
    /// Returns the initialization configuration of a file to open it in the editor.
    /// </summary>
    /// <short>Open a file</short>
    /// <path>api/2.0/files/file/{fileId}/openedit</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Configuration parameters", typeof(ConfigurationDto<int>))]
    [AllowAnonymous]
    [AllowNotPayment]
    [HttpGet("{fileId}/openedit")]
    public async Task<ConfigurationDto<T>> OpenEditAsync(OpenEditRequestDto<T> inDto)
    {
        var (file, lastVersion) = await documentServiceHelper.GetCurFileInfoAsync(inDto.FileId, inDto.File.Version);
        var extension = FileUtility.GetFileExtension(file.Title);
        var fileType = FileUtility.GetFileTypeByExtention(extension);

        bool canEdit;
        bool canFill;
        var canStartFilling = true;
        var isSubmitOnly = false;

        var fillingSessionId = "";
        if (fileType == FileType.Pdf)
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            var rootFolder = await folderDao.GetFolderAsync(file.ParentId);
            if (!DocSpaceHelper.IsRoom(rootFolder.FolderType) && rootFolder.FolderType != FolderType.FormFillingFolderInProgress && rootFolder.FolderType != FolderType.FormFillingFolderDone)
            {
                var (rId, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(rootFolder);
                if (int.TryParse(rId.ToString(), out var roomId) && roomId != -1)
                {
                    var room = await folderDao.GetFolderAsync((T)Convert.ChangeType(roomId, typeof(T)));
                    if (room.FolderType == FolderType.FillingFormsRoom)
                    {
                        rootFolder = room;
                    }
                }
            }

            switch (rootFolder.FolderType)
            {
                case FolderType.FillingFormsRoom:
                    var properties = await daoFactory.GetFileDao<T>().GetProperties(file.Id);
                    var linkDao = daoFactory.GetLinkDao<T>();
                    var fileDao = daoFactory.GetFileDao<T>();
                    canStartFilling = false;

                    if (securityContext.CurrentAccount.ID.Equals(ASC.Core.Configuration.Constants.Guest.ID))
                    {
                        canEdit = false;
                        canFill = true;
                        isSubmitOnly = true;
                        inDto.File.EditorType = EditorType.Embedded;
                        fillingSessionId = Guid.NewGuid().ToString("N");
                        break;
                    }

                   
                    if (inDto.File.Edit)
                    {
                        await linkDao.DeleteAllLinkAsync(file.Id);
                        await fileDao.SaveProperties(file.Id, null);
                        canEdit = true;
                        canFill = false;
                    }
                    else
                    {
                        if (file.RootFolderType == FolderType.Archive)
                        {
                            canEdit = false;
                            canFill = false;
                        }
                        else
                        {
                            if (properties != null && properties.FormFilling.StartFilling)
                            {
                                var linkedId = await linkDao.GetLinkedAsync(file.Id);
                                var formDraft = !Equals(linkedId, default(T)) ? await fileDao.GetFileAsync(linkedId) : (await entryManager.GetFillFormDraftAsync(file, rootFolder.Id)).file;


                                canEdit = false;
                                canFill = true;
                                inDto.File.EditorType = EditorType.Embedded;

                                file = formDraft;
                                fillingSessionId = Guid.NewGuid().ToString("N");
                            }
                            else
                            {
                                canEdit = true;
                                canFill = false;
                            }
                        }
                    }

                    break;

                case FolderType.FormFillingFolderInProgress:
                    canEdit = false;
                    canFill = true;
                    inDto.File.EditorType = EditorType.Embedded;
                    fillingSessionId = Guid.NewGuid().ToString("N");
                    break;

                case FolderType.FormFillingFolderDone:
                    inDto.File.EditorType = EditorType.Embedded;
                    canEdit = false;
                    canFill = false;
                    break;

                default:
                    canEdit = inDto.File.Edit;
                    canFill = !inDto.File.Edit;
                    break;
            }
        }
        else
        {
            canEdit = true;
            canFill = true;
        }

        var docParams = await documentServiceHelper.GetParamsAsync(file, lastVersion, canEdit, !inDto.File.View, true, canFill, inDto.File.EditorType, isSubmitOnly);
        var configuration = docParams.Configuration;
        file = docParams.File;

        if (file.RootFolderType == FolderType.Privacy && await PrivacyRoomSettings.GetEnabledAsync(settingsManager) || docParams.LocatedInPrivateRoom)
        {
            var keyPair = await encryptionKeyPairDtoHelper.GetKeyPairAsync();
            if (keyPair != null)
            {
                configuration.EditorConfig.EncryptionKeys = new EncryptionKeysConfig
                {
                    PrivateKeyEnc = keyPair.PrivateKeyEnc,
                    PublicKey = keyPair.PublicKey
                };
            }
        }

        var result = await configurationConverter.Convert(configuration, file, fillingSessionId);
        
        if (authContext.IsAuthenticated && !file.Encrypted && !file.ProviderEntry 
            && result.File.Security.TryGetValue(FileSecurity.FilesSecurityActions.Read, out var canRead) && canRead)
        {
            var linkId = await externalShare.GetLinkIdAsync();

            if (linkId != default && file.RootFolderType == FolderType.USER && file.CreateBy != authContext.CurrentAccount.ID)
            {
                await entryManager.MarkAsRecentByLink(file, linkId);
            }
            else
            {
                await entryManager.MarkAsRecent(file);
            }
        }
        
        if (fileType == FileType.Pdf && file.IsForm)
        {
            result.StartFilling = canStartFilling;
        }

        if (!string.IsNullOrEmpty(fillingSessionId))
        {
            result.FillingSessionId = fillingSessionId;
        }
       
        return result;
    }

    /// <summary>
    /// Returns a link to download a file with the ID specified in the request asynchronously.
    /// </summary>
    /// <short>Get file download link asynchronously</short>
    /// <path>api/2.0/files/file/{fileId}/presigned</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File download link", typeof(DocumentService.FileLink))]
    [HttpGet("{fileId}/presigned")]
    public async Task<DocumentService.FileLink> GetPresignedFileUriAsync(FileIdRequestDto<T> inDto)
    {
        return await fileStorageService.GetPresignedUriAsync(inDto.FileId);
    }

    /// <summary>
    /// Returns a list of users with their access rights to the file with the ID specified in the request.
    /// </summary>
    /// <short>Get shared users</short>
    /// <path>api/2.0/files/file/{fileId}/sharedusers</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "List of users with their access rights to the file", typeof(List<MentionWrapper>))]
    [HttpGet("{fileId}/sharedusers")]
    public async Task<List<MentionWrapper>> SharedUsers(FileIdRequestDto<T> inDto)
    {
        return await fileStorageService.SharedUsersAsync(inDto.FileId);
    }

    /// <summary>
    /// Return list of users with their access rights to the file
    /// </summary>
    /// <short>Return list of users with their access rights to the file</short>
    /// <path>api/2.0/files/infousers</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "List of users with their access rights to the file", typeof(List<MentionWrapper>))]
    [HttpPost("infousers")]
    public async Task<List<MentionWrapper>> GetInfoUsers(GetInfoUsersRequestDto inDto)
    {
        return await fileStorageService.GetInfoUsersAsync(inDto.UserIds);
    }

    /// <summary>
    /// Returns the reference data to uniquely identify a file in its system and check the availability of insering data into the destination spreadsheet by the external link.
    /// </summary>
    /// <short>Get reference data</short>
    /// <path>api/2.0/files/file/referencedata</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File reference data", typeof(FileReference))]
    [HttpPost("referencedata")]
    public async Task<FileReference> GetReferenceDataAsync(GetReferenceDataDto<T> inDto)
    {
        return await fileStorageService.GetReferenceDataAsync(inDto.FileKey, inDto.InstanceId, inDto.SourceFileId, inDto.Path, inDto.Link);
    }

    /// <summary>
    /// Returns a list of users with their access rights to the protected file with the ID specified in the request.
    /// </summary>
    /// <short>Get users with the access to the protected file</short>
    /// <path>api/2.0/files/file/{fileId}/protectusers</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "List of users with their access rights to the protected file", typeof(List<MentionWrapper>))]
    [HttpGet("{fileId}/protectusers")]
    public async Task<List<MentionWrapper>> ProtectUsers(FileIdRequestDto<T> inDto)
    {
        return await fileStorageService.ProtectUsersAsync(inDto.FileId);
    }
}

public class EditorController(FilesLinkUtility filesLinkUtility,
        MessageService messageService,
        DocumentServiceConnector documentServiceConnector,
        CommonLinkUtility commonLinkUtility,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        CspSettingsHelper cspSettingsHelper,
        PermissionContext permissionContext)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Checks the document service location.
    /// </summary>
    /// <short>Check the document service URL</short>
    /// <path>api/2.0/files/docservice</path>
    /// <collection>list</collection>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Document service information: the Document Server address, the Document Server address in the local private network, the Community Server address", typeof(DocServiceUrlDto))]
    [HttpPut("docservice")]
    public async Task<DocServiceUrlDto> CheckDocServiceUrl(CheckDocServiceUrlRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        
        var currentDocServiceUrl = filesLinkUtility.GetDocServiceUrl();
        var currentDocServiceUrlInternal = filesLinkUtility.GetDocServiceUrlInternal();
        var currentDocServicePortalUrl = filesLinkUtility.GetDocServicePortalUrl();

        if (!ValidateUrl(inDto.DocServiceUrl) ||
            !ValidateUrl(inDto.DocServiceUrlInternal) ||
            !ValidateUrl(inDto.DocServiceUrlPortal))
        {
            throw new Exception("Invalid input urls");
        }

        await filesLinkUtility.SetDocServiceUrlAsync(inDto.DocServiceUrl);
        await filesLinkUtility.SetDocServiceUrlInternalAsync(inDto.DocServiceUrlInternal);
        await filesLinkUtility.SetDocServicePortalUrlAsync(inDto.DocServiceUrlPortal);

        var https = new Regex(@"^https://", RegexOptions.IgnoreCase);
        var http = new Regex(@"^http://", RegexOptions.IgnoreCase);
        if (https.IsMatch(commonLinkUtility.GetFullAbsolutePath("")) && http.IsMatch(filesLinkUtility.GetDocServiceUrl()))
        {
            throw new Exception("Mixed Active Content is not allowed. HTTPS address for Document Server is required.");
        }

        try
        {
            await documentServiceConnector.CheckDocServiceUrlAsync();

            await messageService.SendAsync(MessageAction.DocumentServiceLocationSetting);

            var settings = await cspSettingsHelper.LoadAsync();

            _ = await cspSettingsHelper.SaveAsync(settings.Domains ?? []);
        }
        catch (Exception)
        {
            await filesLinkUtility.SetDocServiceUrlAsync(currentDocServiceUrl);
            await filesLinkUtility.SetDocServiceUrlInternalAsync(currentDocServiceUrlInternal);
            await filesLinkUtility.SetDocServicePortalUrlAsync(currentDocServicePortalUrl);

            throw new Exception("Unable to establish a connection with the Document Server.");
        }
        var version = new DocServiceUrlRequestDto { Version = false };
        return await GetDocServiceUrlAsync(version);

        bool ValidateUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return true;
            }

            var success = Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri);

            if (uri == null || uri.IsAbsoluteUri && !String.IsNullOrEmpty(uri.Query))
            {
                return false;
            }

            return success;
        }
    }

    /// <summary>
    /// Returns the address of the connected editors.
    /// </summary>
    /// <short>Get the document service URL</short>
    /// <path>api/2.0/files/docservice</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The document service URL with the editor version specified", typeof(DocServiceUrlDto))]
    [AllowAnonymous]
    [HttpGet("docservice")]
    public async Task<DocServiceUrlDto> GetDocServiceUrlAsync(DocServiceUrlRequestDto inDto)
    {
        var url = commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.DocServiceApiUrl);

        var dsVersion = "";

        if (inDto.Version)
        {
            dsVersion = await documentServiceConnector.GetVersionAsync();
        }

        return new DocServiceUrlDto
        {
            Version = dsVersion,
            DocServiceUrlApi = url,
            DocServiceUrl = filesLinkUtility.GetDocServiceUrl(),
            DocServiceUrlInternal =filesLinkUtility.GetDocServiceUrlInternal(),
            DocServicePortalUrl = filesLinkUtility.GetDocServicePortalUrl(),
            IsDefault = filesLinkUtility.IsDefault
        };
    }
}