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
        FileMarker fileMarker,
        SocketManager socketManager,
        SecurityContext securityContext)
        : EditorController<int>(fileStorageService, documentServiceHelper, encryptionKeyPairDtoHelper, settingsManager, entryManager, httpContextAccessor, folderDtoHelper, fileDtoHelper, externalShare, authContext, configurationConverter, daoFactory, fileMarker, socketManager, securityContext);

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
        FileMarker fileMarker,
        SocketManager socketManager,
        SecurityContext securityContext)
        : EditorController<string>(fileStorageService, documentServiceHelper, encryptionKeyPairDtoHelper, settingsManager, entryManager, httpContextAccessor, folderDtoHelper, fileDtoHelper, externalShare, authContext, configurationConverter, daoFactory, fileMarker, socketManager, securityContext);

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
        FileMarker fileMarker,
        SocketManager socketManager,
        SecurityContext securityContext)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{

    /// <summary>
    /// Saves edits to a file with the ID specified in the request.
    /// </summary>
    /// <short>Save file edits</short>
    /// <param type="System.Int32, System" method="url" name="fileId">File ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.SaveEditingRequestDto, ASC.Files.Core" name="inDto">Request parameters for saving file edits</param>
    /// <category>Files</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileDto, ASC.Files.Core">Saved file parameters</returns>
    /// <path>api/2.0/files/file/{fileId}/saveediting</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{fileId}/saveediting")]
    public async Task<FileDto<T>> SaveEditingFromFormAsync(T fileId, [FromForm] SaveEditingRequestDto inDto)
    {
        await using var stream = httpContextAccessor.HttpContext.Request.Body;

        return await _fileDtoHelper.GetAsync(await fileStorageService.SaveEditingAsync(fileId, inDto.FileExtension, inDto.DownloadUri, stream, inDto.Forcesave));
    }

    /// <summary>
    /// Informs about opening a file with the ID specified in the request for editing, locking it from being deleted or moved (this method is called by the mobile editors).
    /// </summary>
    /// <short>Start file editing</short>
    /// <param type="System.Int32, System" method="url" name="fileId">File ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.StartEditRequestDto, ASC.Files.Core" name="inDto">Request parameters for starting file editing</param>
    /// <category>Files</category>
    /// <returns type="System.Object, System">File key for Document Service</returns>
    /// <path>api/2.0/files/file/{fileId}/startedit</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("{fileId}/startedit")]
    public async Task<object> StartEditAsync(T fileId, StartEditRequestDto inDto)
    {
        return await fileStorageService.StartEditAsync(fileId, inDto.EditingAlone);
    }

    /// <summary>
    /// Starts filling a file with the ID specified in the request.
    /// </summary>
    /// <short>Starts filling</short>
    /// <param type="System.Int32, System" method="url" name="fileId">File ID</param>
    /// <category>Files</category>
    /// <path>api/2.0/files/file/{fileId}/startfilling</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{fileId}/startfilling")]
    public async Task StartFillingAsync(T fileId)
    {
        await fileStorageService.StartFillingAsync(fileId);
    }

    /// <summary>
    /// Tracks file changes when editing.
    /// </summary>
    /// <short>Track file editing</short>
    /// <param type="System.Int32, System" method="url" name="fileId">File ID</param>
    /// <param type="System.Guid, System" name="tabId">Tab ID</param>
    /// <param type="System.String, System" name="docKeyForTrack">Document key for tracking</param>
    /// <param type="System.Boolean, System" name="isFinish">Specifies whether to finish file tracking or not</param>
    /// <category>Files</category>
    /// <returns type="System.Collections.Generic.KeyValuePair{System.Boolean, System.String}, System.Collections.Generic">File changes</returns>
    /// <path>api/2.0/files/file/{fileId}/trackeditfile</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("{fileId}/trackeditfile")]
    public async Task<KeyValuePair<bool, string>> TrackEditFileAsync(T fileId, Guid tabId, string docKeyForTrack, bool isFinish)
    {
        return await fileStorageService.TrackEditFileAsync(fileId, tabId, docKeyForTrack, isFinish);
    }

    /// <summary>
    /// Returns the initialization configuration of a file to open it in the editor.
    /// </summary>
    /// <short>Open a file</short>
    /// <param type="System.Int32, System" method="url" name="fileId">File ID</param>
    /// <param type="System.Int32, System" name="version">File version</param>
    /// <param type="System.Boolean, System" name="view">Specifies if a document will be opened for viewing only or not</param>
    /// <param type="ASC.Web.Files.Services.DocumentService.EditorType, ASC.Files.Core" name="editorType">Editor type (Desktop, Mobile, Embedded)</param>
    /// <param type="System.Boolean, System" name="edit"></param>
    /// <category>Files</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.ConfigurationDto, ASC.Files.Core">Configuration parameters</returns>
    /// <path>api/2.0/files/file/{fileId}/openedit</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <httpMethod>GET</httpMethod>
    [AllowAnonymous]
    [AllowNotPayment]
    [HttpGet("{fileId}/openedit")]
    public async Task<ConfigurationDto<T>> OpenEditAsync(T fileId, int version, bool view, EditorType editorType, bool edit)
    {
        var (file, lastVersion) = await documentServiceHelper.GetCurFileInfoAsync(fileId, version);
        var extension = FileUtility.GetFileExtension(file.Title);
        var fileType = FileUtility.GetFileTypeByExtention(extension);

        bool canEdit;
        bool canFill;
        var canStartFilling = true;
        var isSubmitOnly = false;
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
                        editorType = EditorType.Embedded;
                        break;
                    }

                   
                    if (edit)
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
                                editorType = EditorType.Embedded;

                                await fileMarker.MarkAsNewAsync(formDraft);
                                await socketManager.CreateFileAsync(formDraft);
                                await socketManager.UpdateFileAsync(file);

                                file = formDraft;
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
                    editorType = EditorType.Embedded;
                    break;

                case FolderType.FormFillingFolderDone:
                    canEdit = false;
                    canFill = false;
                    break;

                default:
                    canEdit = edit;
                    canFill = !edit;
                    break;
            }
        }
        else
        {
            canEdit = true;
            canFill = true;
        }

        var docParams = await documentServiceHelper.GetParamsAsync(file, lastVersion, canEdit, !view, true, canFill, editorType, isSubmitOnly);
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

        var result = await configurationConverter.Convert(configuration, file);
        
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

        return result;
    }

    /// <summary>
    /// Returns a link to download a file with the ID specified in the request asynchronously.
    /// </summary>
    /// <short>Get file download link asynchronously</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" method="url" name="fileId">File ID</param>
    /// <returns type="ASC.Files.Core.Helpers.DocumentService.FileLink, ASC.Files.Core">File download link</returns>
    /// <path>api/2.0/files/file/{fileId}/presigned</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("{fileId}/presigned")]
    public async Task<DocumentService.FileLink> GetPresignedUriAsync(T fileId)
    {
        return await fileStorageService.GetPresignedUriAsync(fileId);
    }

    /// <summary>
    /// Returns a list of users with their access rights to the file with the ID specified in the request.
    /// </summary>
    /// <short>Get shared users</short>
    /// <category>Sharing</category>
    /// <param type="System.Int32, System" method="url" name="fileId">File ID</param>
    /// <returns type="ASC.Web.Files.Services.WCFService.MentionWrapper, ASC.Files.Core">List of users with their access rights to the file</returns>
    /// <path>api/2.0/files/file/{fileId}/sharedusers</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    /// <visible>false</visible>
    [HttpGet("{fileId}/sharedusers")]
    public async Task<List<MentionWrapper>> SharedUsers(T fileId)
    {
        return await fileStorageService.SharedUsersAsync(fileId);
    }

    /// <summary>
    /// Return list of users with their access rights to the file
    /// </summary>
    /// <short>Return list of users with their access rights to the file</short>
    /// <category>Files</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.GetInfoUsersRequestDto, ASC.Files.Core" name="inDto">Base batch request parameters</param>
    /// <returns type="ASC.Web.Files.Services.WCFService.MentionWrapper, ASC.Files.Core">List of users with their access rights to the file</returns>
    /// <path>api/2.0/files/infousers</path>
    /// <httpMethod>POST</httpMethod>
    /// <visible>false</visible>
    [HttpPost("infousers")]
    public async Task<List<MentionWrapper>> GetInfoUsers(GetInfoUsersRequestDto inDto)
    {
        return await fileStorageService.GetInfoUsersAsync(inDto.UserIds);
    }

    /// <summary>
    /// Returns the reference data to uniquely identify a file in its system and check the availability of insering data into the destination spreadsheet by the external link.
    /// </summary>
    /// <short>Get reference data</short>
    /// <category>Files</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.GetReferenceDataDto, ASC.Files.Core" name="inDto">Request parameters for getting reference data</param>
    /// <returns type="ASC.Web.Files.Services.DocumentService.FileReference, ASC.Files.Core">File reference data</returns>
    /// <path>api/2.0/files/file/referencedata</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("referencedata")]
    public async Task<FileReference> GetReferenceDataAsync(GetReferenceDataDto<T> inDto)
    {
        return await fileStorageService.GetReferenceDataAsync(inDto.FileKey, inDto.InstanceId, inDto.SourceFileId, inDto.Path, inDto.Link);
    }

    /// <summary>
    /// Returns a list of users with their access rights to the protected file with the ID specified in the request.
    /// </summary>
    /// <short>Get users with the access to the protected file</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" name="fileId">File ID</param>
    /// <returns type="ASC.Web.Files.Services.WCFService.MentionWrapper, ASC.Files.Core">List of users with their access rights to the protected file</returns>
    /// <path>api/2.0/files/file/{fileId}/protectusers</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("{fileId}/protectusers")]
    public async Task<List<MentionWrapper>> ProtectUsers(T fileId)
    {
        return await fileStorageService.ProtectUsersAsync(fileId);
    }
}

public class EditorController(FilesLinkUtility filesLinkUtility,
        MessageService messageService,
        DocumentServiceConnector documentServiceConnector,
        CommonLinkUtility commonLinkUtility,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        PermissionContext permissionContext)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Checks the document service location.
    /// </summary>
    /// <short>Check the document service URL</short>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.CheckDocServiceUrlRequestDto, ASC.Files.Core" name="inDto">Request parameters for checking the document service location</param>
    /// <category>Settings</category>
    /// <returns type="System.String, System">Document service information: the Document Server address, the Document Server address in the local private network, the Community Server address</returns>
    /// <path>api/2.0/files/docservice</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
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
        }
        catch (Exception)
        {        
            await filesLinkUtility.SetDocServiceUrlAsync(currentDocServiceUrl);
            await filesLinkUtility.SetDocServiceUrlInternalAsync(currentDocServiceUrlInternal);
            await filesLinkUtility.SetDocServicePortalUrlAsync(currentDocServicePortalUrl);
          
            throw new Exception("Unable to establish a connection with the Document Server.");
        }

        return await GetDocServiceUrlAsync(false);

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
    /// <category>Settings</category>
    /// <param type="System.Boolean, System" name="version">Specifies the editor version or not</param>
    /// <returns type="System.Object, System">The document service URL with the editor version specified</returns>
    /// <path>api/2.0/files/docservice</path>
    /// <httpMethod>GET</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <visible>false</visible>
    [AllowAnonymous]
    [HttpGet("docservice")]
    public async Task<DocServiceUrlDto> GetDocServiceUrlAsync(bool version)
    {
        var url = commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.DocServiceApiUrl);

        var dsVersion = "";

        if (version)
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