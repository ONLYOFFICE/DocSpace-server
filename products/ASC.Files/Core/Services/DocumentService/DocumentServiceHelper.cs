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

namespace ASC.Web.Files.Services.DocumentService;

[Scope]
public class DocumentServiceHelper(IDaoFactory daoFactory,
        UserManager userManager,
        DisplayUserSettingsHelper displayUserSettingsHelper,
        FileSecurity fileSecurity,
        FileUtility fileUtility,
        FilesLinkUtility filesLinkUtility,
        MachinePseudoKeys machinePseudoKeys,
        Global global,
        TenantUtil tenantUtil,
        DocumentServiceConnector documentServiceConnector,
        LockerManager lockerManager,
        CustomFilterManager customFilterManager,
        FileTrackerHelper fileTracker,
        EntryStatusManager entryStatusManager,
        IServiceProvider serviceProvider,
        ExternalShare externalShare,
        IHttpContextAccessor httpContextAccessor,
        AuthContext authContext,
        SecurityContext securityContext)
    {

    public async Task<(File<T> File, bool LastVersion)> GetCurFileInfoAsync<T>(T fileId, int version)
    {
        var lastVersion = true;

        var fileDao = daoFactory.GetFileDao<T>();

        var file = await fileDao.GetFileAsync(fileId);
        if (file != null && 0 < version && version < file.Version)
        {
                file = await fileDao.GetFileAsync(fileId, version);
                lastVersion = false;
        }

        if (file == null)
        {
            throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        return (file, lastVersion);
            }

    public async Task<(File<T> File, Configuration<T> Configuration, bool LocatedInPrivateRoom)> GetParamsAsync<T>(File<T> file, bool lastVersion, bool editPossible, bool tryEdit,
        bool tryCoauth, bool fillFormsPossible, EditorType editorType, bool isSubmitOnly = false)
    {
        var docParams = await GetParamsAsync(file, lastVersion, true, editPossible, editPossible, tryEdit, tryCoauth, fillFormsPossible);
        docParams.Configuration.EditorType = editorType;

        if (isSubmitOnly)
        {
            docParams.Configuration.Document.Key = GetDocSubmitKeyAsync(docParams.Configuration.Document.Key);
        }

        return docParams;
    }

    public async Task<(File<T> File, Configuration<T> Configuration, bool LocatedInPrivateRoom)> GetParamsAsync<T>(T fileId, int version, bool editPossible, bool tryEdit,
        bool tryCoAuthoring, bool fillFormsPossible)
    {
        var (file, lastVersion) = await GetCurFileInfoAsync(fileId, version);

        return await GetParamsAsync(file, lastVersion, true, true, editPossible, tryEdit, tryCoAuthoring, fillFormsPossible);
    }

    private async Task<(File<T> File, Configuration<T> Configuration, bool LocatedInPrivateRoom)> GetParamsAsync<T>(File<T> file, bool lastVersion, bool rightToRename,
        bool rightToEdit, bool editPossible, bool tryEdit, bool tryCoAuthoring, bool fillFormsPossible)
    {

        if (file == null)
        {
            throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (!string.IsNullOrEmpty(file.Error))
        {
            throw new Exception(file.Error);
        }

        var rightToReview = rightToEdit;
        var reviewPossible = editPossible;

        var rightToFillForms = fillFormsPossible;

        var rightToComment = rightToEdit;
        var commentPossible = editPossible;

        var rightModifyFilter = rightToEdit;

        rightToEdit = rightToEdit && (await fileSecurity.CanEditAsync(file) || await fileSecurity.CanCustomFilterEditAsync(file));
        if (editPossible && !rightToEdit)
        {
            editPossible = false;
        }

        rightModifyFilter = rightModifyFilter && await fileSecurity.CanEditAsync(file) && !await customFilterManager.CustomFilterEnabledForMeAsync(file);
        rightToRename = rightToRename && rightToEdit && await fileSecurity.CanRenameAsync(file);

        rightToReview = rightToReview && await fileSecurity.CanReviewAsync(file);
        if (reviewPossible && !rightToReview)
        {
            reviewPossible = false;
        }

        rightToFillForms = rightToFillForms && await fileSecurity.CanFillFormsAsync(file);
        if (fillFormsPossible && !rightToFillForms)
        {
            fillFormsPossible = false;
        }

        rightToComment = rightToComment && await fileSecurity.CanCommentAsync(file);
        if (commentPossible && !rightToComment)
        {
            commentPossible = false;
        }

        if (!(editPossible || reviewPossible || fillFormsPossible || commentPossible) && !await fileSecurity.CanReadAsync(file))
        {
            if (file.ShareRecord is { IsLink: true, Share: not FileShare.Restrict, Options.Internal: true } && !authContext.IsAuthenticated)
            {
                throw new LinkScopeException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
            }

            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
        }

        if (file.RootFolderType == FolderType.TRASH)
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_ViewTrashItem);
        }

        string strError = null;
        if ((editPossible || reviewPossible || fillFormsPossible || commentPossible) && await lockerManager.FileLockedForMeAsync(file.Id))
        {
            if (tryEdit)
            {
                strError = FilesCommonResource.ErrorMessage_LockedFile;
            }

            rightToRename = false;
            rightToEdit = editPossible = false;
            rightToReview = reviewPossible = false;
            rightToFillForms = fillFormsPossible = false;
            rightToComment = commentPossible = false;
        }

        if (editPossible && !fileUtility.CanWebEdit(file.Title))
        {
            rightToEdit = editPossible = false;
        }

        var locatedInPrivateRoom = false;
        Options options = null;
        if (file.RootFolderType is FolderType.VirtualRooms or FolderType.Archive)
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            var room = await DocSpaceHelper.GetParentRoom(file, folderDao);
            locatedInPrivateRoom = DocSpaceHelper.LocatedInPrivateRoomAsync(room);
            options = GetOptions(room);
        }

        if (file.Encrypted && file.RootFolderType != FolderType.Privacy && !locatedInPrivateRoom)
        {
            rightToEdit = editPossible = false;
            rightToReview = reviewPossible = false;
            rightToFillForms = fillFormsPossible = false;
            rightToComment = commentPossible = false;
        }

        if (!editPossible && !fileUtility.CanWebView(file.Title))
        {
            throw new NotSupportedException($"{FilesCommonResource.ErrorMessage_NotSupportedFormat} ({FileUtility.GetFileExtension(file.Title)})");
        }

        if (reviewPossible && !fileUtility.CanWebReview(file.Title))
        {
            rightToReview = reviewPossible = false;
        }

        if (fillFormsPossible && !fileUtility.CanWebRestrictedEditing(file.Title))
        {
            rightToFillForms = fillFormsPossible = false;
        }

        if (commentPossible && !fileUtility.CanWebComment(file.Title))
        {
            rightToComment = commentPossible = false;
        }

        var rightChangeHistory = rightToEdit && !file.Encrypted;

        if (await fileTracker.IsEditingAsync(file.Id))
        {
            rightChangeHistory = false;

            bool canCoAuthoring;
            if ((editPossible || reviewPossible || fillFormsPossible || commentPossible)
                && tryCoAuthoring
                && (!(canCoAuthoring = fileUtility.CanCoAuthoring(file.Title)) || await fileTracker.IsEditingAloneAsync(file.Id)))
            {
                if (tryEdit)
                {
                    var editingBy = (await fileTracker.GetEditingByAsync(file.Id)).FirstOrDefault();
                    strError = string.Format(!canCoAuthoring 
                                                 ? FilesCommonResource.ErrorMessage_EditingCoauth
                                                 : FilesCommonResource.ErrorMessage_EditingMobile,
                                             await global.GetUserNameAsync(editingBy, true));
                }
                
                rightToEdit = editPossible = reviewPossible = fillFormsPossible = commentPossible = false;
            }
        }

        var fileStable = file;
        if (lastVersion && file.Forcesave != ForcesaveType.None && tryEdit)
        {
            var fileDao = daoFactory.GetFileDao<T>();
            fileStable = await fileDao.GetFileStableAsync(file.Id, file.Version);
        }

        var docKey = await GetDocKeyAsync(fileStable);
        var modeWrite = (editPossible || reviewPossible || fillFormsPossible || commentPossible) && tryEdit;

        if (file.ParentId != null)
        {
            await entryStatusManager.SetFileStatusAsync(file);
        }

        var rightToDownload = await fileSecurity.CanDownloadAsync(file);
        var noWatermark = options?.WatermarkOnDraw == null;

        var configuration = serviceProvider.GetService<Configuration<T>>();
        configuration.Document.Key = docKey;
        configuration.Document.Permissions = new PermissionsConfig
        {
            Edit = rightToEdit && lastVersion,
            Rename = rightToRename && lastVersion && !file.ProviderEntry,
            Review = rightToReview && lastVersion,
            FillForms = rightToFillForms && lastVersion,
            Comment = rightToComment && lastVersion,
            ChangeHistory = rightChangeHistory,
            ModifyFilter = rightModifyFilter,
            Print = rightToDownload,
            Download = rightToDownload && noWatermark,
            Copy = rightToDownload && noWatermark,
            Protect = authContext.IsAuthenticated,
            Chat = file.Access != FileShare.Read   
        };
        
        configuration.Document.Options = options;
        configuration.EditorConfig.ModeWrite = modeWrite;
        configuration.Error = strError;

        if (!lastVersion)
        {
            configuration.Document.Title =  $"{file.Title} ({file.CreateOnString})";
        }

        if (fileUtility.CanWebRestrictedEditing(file.Title))
        {
            var linkDao = daoFactory.GetLinkDao<T>();
            var sourceId = await linkDao.GetSourceAsync(file.Id);
            configuration.Document.IsLinkedForMe = Equals(sourceId, default(T));
        }

        if (await externalShare.GetLinkIdAsync() == Guid.Empty)
        {
            return (file, configuration, locatedInPrivateRoom);
        }

        configuration.Document.SharedLinkParam = FilesLinkUtility.ShareKey;
        configuration.Document.SharedLinkKey = externalShare.GetKey();

        return (file, configuration, locatedInPrivateRoom);
    }

    public string GetSignature(object payload)
    {
        var signatureSecret = filesLinkUtility.DocServiceSignatureSecret;

        if (string.IsNullOrEmpty(signatureSecret))
        {
            return null;
        }

        return JsonWebToken.Encode(payload, signatureSecret);
    }

    public async Task<File<T>> CheckNeedDeletion<T>(IFileDao<T> fileDao, T fileId, FormFillingProperties<T> formFillingProperties)
    {
        var file = await fileDao.GetFileAsync(fileId);

        if (Equals(formFillingProperties.ToFolderId, file.ParentId))
        {
            await securityContext.AuthenticateMeAsync(file.CreateBy);

            var linkDao = daoFactory.GetLinkDao<T>();
            var sourceId = await linkDao.GetSourceAsync(file.Id);
            if (Equals(sourceId, default(T)))
            {
                return file;
            }
        }
        return null;
    }

    public Options GetOptions<T>(Folder<T> room)
    {
        var watermarkSettings = room?.SettingsWatermark;
        if (watermarkSettings == null)
        {
            return null;
        }
        var runs = new List<Run>();
        var paragrahs = new List<Paragraph>();
        var userInfo = userManager.GetUsers(authContext.CurrentAccount.ID);
        var ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress.ToString();

        if (watermarkSettings.Additions.HasFlag(WatermarkAdditions.UserName))
        {
            runs.Add(new Run(userInfo.DisplayUserName(false, displayUserSettingsHelper)));
            runs.Add(new Run(Environment.NewLine, false));
        }
        if(watermarkSettings.Additions.HasFlag(WatermarkAdditions.UserEmail))
        {
            runs.Add(new Run(userInfo.Email));
            runs.Add(new Run(Environment.NewLine, false));
        }
        if (watermarkSettings.Additions.HasFlag(WatermarkAdditions.UserIpAdress) && !string.IsNullOrWhiteSpace(ip))
        {
            runs.Add(new Run(ip));
            runs.Add(new Run(Environment.NewLine, false));
        }
        if (watermarkSettings.Additions.HasFlag(WatermarkAdditions.CurrentDate))
        {
            runs.Add(new Run(tenantUtil.DateTimeNow().ToString(), false));
            runs.Add(new Run(Environment.NewLine, false));
        }
        if (watermarkSettings.Additions.HasFlag(WatermarkAdditions.RoomName))
        {
            runs.Add(new Run(room.Title));
            runs.Add(new Run(Environment.NewLine, false));
        }
        if (!string.IsNullOrWhiteSpace(watermarkSettings.Text))
        {
            runs.Add(new Run(watermarkSettings.Text));
            runs.Add(new Run(Environment.NewLine, false));
        }
        if (runs.Count != 0)
        {
            runs.Remove(runs.Last());
        }
        paragrahs.Add(new Paragraph(runs));

        var options = new Options
        {
            WatermarkOnDraw = new WatermarkOnDraw(watermarkSettings.ImageWidth * watermarkSettings.ImageScale / 100, watermarkSettings.ImageHeight * watermarkSettings.ImageScale / 100 , watermarkSettings.ImageUrl, watermarkSettings.Rotate, paragrahs)
        };
        return options;
    }

    public async Task<string> GetDocKeyAsync<T>(File<T> file, string extraKey = null)
    {
        return await GetDocKeyAsync(file.Id, file.Version, file.ProviderEntry ? file.ModifiedOn : file.CreateOn, extraKey);
    }

    public async Task<string> GetDocKeyAsync<T>(T fileId, int fileVersion, DateTime modified, string extraKey = null)
    {
        var str = $"teamlab_{fileId}_{fileVersion}_{modified.GetHashCode().ToString(CultureInfo.InvariantCulture)}_{await global.GetDocDbKeyAsync()}";

        if (!string.IsNullOrEmpty(extraKey))
        {
            str += $"_{extraKey}";
        }

        var keyDoc = Encoding.UTF8.GetBytes(str)
                             .AsEnumerable()
                             .Concat(machinePseudoKeys.GetMachineConstant())
                             .ToArray();

        return DocumentServiceConnector.GenerateRevisionId(Hasher.Base64Hash(keyDoc, HashAlg.SHA256));
    }

    public string GetDocSubmitKeyAsync(string key)
    {
        var rnd = Guid.NewGuid();
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"submit_{rnd}_{key}")).TrimEnd('=');
    }

    public bool IsDocSubmitKey(string docKey, string key)
    {
        var submitKey = Encoding.UTF8.GetString(Convert.FromBase64String(FixBase64String(key)));

        var keySplit = submitKey.Split(Convert.ToChar("_"), 3);

        if (keySplit.Length == 3 && keySplit[0] == "submit" && docKey.Equals(keySplit[2]))
        {
            return true;
        }
        return false;
    }
    
    static string FixBase64String(string input)
    {
        // Convert from URL-safe Base64 to standard Base64
        var fixedInput = input.Replace('-', '+').Replace('_', '/');
    
        // Add padding if necessary
        switch (fixedInput.Length % 4)
        {
            case 2: fixedInput += "=="; break;
            case 3: fixedInput += "="; break;
        }
    
        return fixedInput;
    }

    public async Task CheckUsersForDropAsync<T>(File<T> file)
    {
        var usersDrop = new List<string>();

        foreach (var uid in await fileTracker.GetEditingByAsync(file.Id))
        {
            if (!await userManager.UserExistsAsync(uid))
            {
                usersDrop.Add(uid.ToString());
                continue;
            }

            if (!await fileSecurity.CanEditAsync(file, uid)
                && !await fileSecurity.CanCustomFilterEditAsync(file, uid)
                && !await fileSecurity.CanReviewAsync(file, uid)
                && !await fileSecurity.CanFillFormsAsync(file, uid)
                && !await fileSecurity.CanCommentAsync(file, uid))
            {
                usersDrop.Add(uid.ToString());
            }
        }

        if (usersDrop.Count == 0)
        {
            return;
        }

        var fileStable = file;
        if (file.Forcesave != ForcesaveType.None)
        {
            var fileDao = daoFactory.GetFileDao<T>();
            fileStable = await fileDao.GetFileStableAsync(file.Id, file.Version);
        }

        var docKey = await GetDocKeyAsync(fileStable);

        await DropUserAsync(docKey, usersDrop.ToArray(), file.Id);
    }

    public async Task<bool> DropUserAsync(string docKeyForTrack, string[] users, object fileId = null)
    {
        return await documentServiceConnector.CommandAsync(CommandMethod.Drop, docKeyForTrack, fileId, null, users);
    }

    public async Task<bool> RenameFileAsync<T>(File<T> file, IFileDao<T> fileDao)
    {
        if (!fileUtility.CanWebView(file.Title)
            && !fileUtility.CanWebCustomFilterEditing(file.Title)
            && !fileUtility.CanWebEdit(file.Title)
            && !fileUtility.CanWebReview(file.Title)
            && !fileUtility.CanWebRestrictedEditing(file.Title)
            && !fileUtility.CanWebComment(file.Title))
        {
            return true;
        }

        var fileStable = file.Forcesave == ForcesaveType.None ? file : await fileDao.GetFileStableAsync(file.Id, file.Version);
        var docKeyForTrack = await GetDocKeyAsync(fileStable);

        var meta = new MetaData { Title = file.Title };

        return await documentServiceConnector.CommandAsync(CommandMethod.Meta, docKeyForTrack, file.Id, meta: meta);
    }

    public async Task<Folder<T>> GetRootFolderAsync<T>(File<T> fileEntry)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var folder = await folderDao.GetFolderAsync(fileEntry.ParentId);
        if (DocSpaceHelper.IsRoom(folder.FolderType) ||
            (folder.FolderType is FolderType.FormFillingFolderInProgress or FolderType.FormFillingFolderDone))
        {
            return folder;
        }

        var (rId, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(folder);
        if (int.TryParse(rId.ToString(), out var roomId) && roomId != -1)
        {
            var room = await folderDao.GetFolderAsync((T)Convert.ChangeType(roomId, typeof(T)));
            if (room.FolderType is FolderType.FillingFormsRoom or FolderType.VirtualDataRoom)
            {
                return room;
            }
        }

        if(folder.RootFolderType == FolderType.USER)
        {
            return await folderDao.GetRootFolderAsync(folder.Id);
        }

        return folder;
    }

    public async Task<FormOpenSetup<T>> GetFormOpenSetupForFillingRoomAsync<T>(File<T> file, Folder<T> rootFolder, EditorType editorType, bool edit, EntryManager entryManager)
    {
        var result = new FormOpenSetup<T>();
        var fileDao = daoFactory.GetFileDao<T>();
        var properties = await fileDao.GetProperties(file.Id);
        var linkDao = daoFactory.GetLinkDao<T>();

        result.CanStartFilling = false;

        if (securityContext.CurrentAccount.ID.Equals(ASC.Core.Configuration.Constants.Guest.ID))
        {
            result.CanFill = true;
            result.IsSubmitOnly = true;
            result.FillingSessionId = FileConstant.AnonFillingSession + Guid.NewGuid();
            result.EditorType = editorType == EditorType.Mobile ? editorType : EditorType.Embedded;
            return result;
        }

        if (edit)
        {
            await linkDao.DeleteAllLinkAsync(file.Id);
            await fileDao.SaveProperties(file.Id, null);

            result.CanEdit = true;
            result.EditorType = editorType;
            return result;
        }

        if (!await fileSecurity.CanFillFormsAsync(rootFolder))
        {
            return result;
        }

        if (properties?.FormFilling?.StartFilling != true)
        {
            result.CanEdit = true;
            return result;
        }
        var linkedId = await linkDao.GetLinkedAsync(file.Id);
        var formDraft = !Equals(linkedId, default(T)) ? await fileDao.GetFileAsync(linkedId) : (await entryManager.GetFillFormDraftAsync(file, rootFolder.Id)).file;

        result.CanFill = true;
        result.EditorType = editorType == EditorType.Mobile ? editorType : EditorType.Embedded;
        result.Draft = formDraft;
        result.FillingSessionId = $"{formDraft.Id}_{securityContext.CurrentAccount.ID}";

        return result;
    }
    public FormOpenSetup<T> GetFormOpenSetupForFolderInProgress<T>(File<T> file, EditorType editorType)
    {
        return new FormOpenSetup<T>
        {
            CanEdit = false,
            CanFill = true,
            FillingSessionId = $"{file.Id}_{securityContext.CurrentAccount.ID}",
            EditorType = editorType == EditorType.Mobile ? editorType : EditorType.Embedded
        };
    }
    public FormOpenSetup<T> GetFormOpenSetupForFolderDone<T>(EditorType editorType)
    {
        return new FormOpenSetup<T>
        {
            CanEdit = false,
            CanFill = false,
            EditorType = editorType == EditorType.Mobile ? editorType : EditorType.Embedded
        };
    }
    public async Task<FormOpenSetup<T>> GetFormOpenSetupForVirtualDataRoomAsync<T>(File<T> file, EditorType editorType)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var (currentStep, myRoles) = await fileDao.GetUserFormRoles(file.Id, securityContext.CurrentAccount.ID);

        var result = new FormOpenSetup<T>
        {
            CanEdit = false,
            CanFill = true
        };

        if (currentStep != -1)
        {
            if (!myRoles.Any())
            {
                return result;
            }

            var role = myRoles.FirstOrDefault(role => !role.Submitted && currentStep == role.Sequence);
            if (role != null)
            {
                result.RoleName = role.RoleName;
                if (role.OpenedAt.Equals(DateTime.MinValue))
                {
                    role.OpenedAt = DateTime.UtcNow;
                    await fileDao.ChangeUserFormRoleAsync(file.Id, role);
                }
            }
            else
            {
                role = myRoles.LastOrDefault();
                result.RoleName = role.RoleName;
            }
            result.EditorType = editorType == EditorType.Mobile ? editorType : EditorType.Embedded;
        }
        else
        {
            result.CanEdit = true;
            result.CanFill = false;
            result.EditorType = editorType;
        }
        return result;
    }

    public async Task<FormOpenSetup<T>> GetFormOpenSetupForUserFolderAsync<T>(File<T> file, EditorType editorType, bool edit, bool fill)
    {
        var canEdit = await fileSecurity.CanEditAsync(file);
        var canFill = await fileSecurity.CanFillFormsAsync(file);

        FormOpenSetup<T> result = null;
        if (file.CreateBy == securityContext.CurrentAccount.ID) 
        {
            result = new FormOpenSetup<T>
            {
                CanEdit = edit,
                CanFill = fill || canFill,
                CanStartFilling = true,
                EditorType = editorType
            };
        }
        else
        {
            result = new FormOpenSetup<T>
            {
                CanEdit = canEdit,
                CanFill = canFill,
                CanStartFilling = false,
                EditorType = !edit && (fill || canFill) && editorType != EditorType.Mobile
                            ? EditorType.Embedded
                            : editorType
            };
        }
        return result;
    }
}
