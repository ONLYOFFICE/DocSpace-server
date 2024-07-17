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

namespace ASC.Web.Files.Services.DocumentService;

[Scope]
public class DocumentServiceHelper(IDaoFactory daoFactory,
        UserManager userManager,
        FileSecurity fileSecurity,
        FileUtility fileUtility,
        MachinePseudoKeys machinePseudoKeys,
        Global global,
        DocumentServiceConnector documentServiceConnector,
        LockerManager lockerManager,
        FileTrackerHelper fileTracker,
        EntryStatusManager entryStatusManager,
        IServiceProvider serviceProvider,
        ExternalShare externalShare,
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

        rightModifyFilter = rightModifyFilter && await fileSecurity.CanEditAsync(file);
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

        if (file.RootFolderType == FolderType.VirtualRooms)
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            locatedInPrivateRoom = await DocSpaceHelper.LocatedInPrivateRoomAsync(file, folderDao);
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
                && (!(canCoAuthoring = fileUtility.CanCoAuthoring(file.Title)) || fileTracker.IsEditingAlone(file.Id)))
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
            Download = rightToDownload,
            Copy = rightToDownload,
            Protect = authContext.IsAuthenticated,
            Chat = file.Access != FileShare.Read
        };

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
        if (string.IsNullOrEmpty(fileUtility.SignatureSecret))
        {
            return null;
        }

        return JsonWebToken.Encode(payload, fileUtility.SignatureSecret);
    }

    public async Task<File<T>> CheckNeedDeletion<T>(IFileDao<T> fileDao, T fileId, FormFillingProperties formFillingProperties)
    {
        var file = await fileDao.GetFileAsync(fileId);

        if (formFillingProperties.ToFolderId == file.ParentId.ToString())
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

    public async Task<string> GetDocKeyAsync<T>(File<T> file)
    {
        return await GetDocKeyAsync(file.Id, file.Version, file.ProviderEntry ? file.ModifiedOn : file.CreateOn);
    }

    public async Task<string> GetDocKeyAsync<T>(T fileId, int fileVersion, DateTime modified)
    {
        var str = $"teamlab_{fileId}_{fileVersion}_{modified.GetHashCode().ToString(CultureInfo.InvariantCulture)}_{await global.GetDocDbKeyAsync()}";

        var keyDoc = Encoding.UTF8.GetBytes(str)
                             .AsEnumerable()
                             .Concat(machinePseudoKeys.GetMachineConstant())
                             .ToArray();

        return DocumentServiceConnector.GenerateRevisionId(Hasher.Base64Hash(keyDoc, HashAlg.SHA256));
    }

    public string GetDocSubmitKeyAsync(string key)
    {
        var rnd = Guid.NewGuid();
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"submit_{rnd}_{key}"));
    }

    public bool IsDocSubmitKey(string docKey, string key)
    {
        var submitKey = Encoding.UTF8.GetString(Convert.FromBase64String(ReplaceLastUnderscoresWithEquals(key)));

        var keySplit = submitKey.Split(Convert.ToChar("_"), 3);

        if (keySplit.Length == 3 && keySplit[0] == "submit" && docKey.Equals(keySplit[2]))
        {
            return true;
        }
        return false;
    }
    private string ReplaceLastUnderscoresWithEquals(string inputString)
    {
        var charToReplace = '_';
        var replaceWith = '=';

        var lastCharIndex = inputString.LastIndexOf(charToReplace);

        while (lastCharIndex != -1)
        {
            inputString = inputString.Substring(0, lastCharIndex) + replaceWith + inputString.Substring(lastCharIndex + 1);
            lastCharIndex = inputString.LastIndexOf(charToReplace);
        }

        return inputString;
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
}
