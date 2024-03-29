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

[Scope(Additional = typeof(ConfigurationFilesExtension))]
public class DocumentServiceHelper(IDaoFactory daoFactory,
        FileShareLink fileShareLink,
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
        AuthContext authContext)
    {
    public async Task<(File<T> File, Configuration<T> Configuration, bool LocatedInPrivateRoom)> GetParamsAsync<T>(T fileId, int version, string doc, bool editPossible, bool tryEdit, bool tryCoauth)
    {
        var lastVersion = true;

        var fileDao = daoFactory.GetFileDao<T>();

        var fileOptions = await fileShareLink.CheckAsync(doc, fileDao);
        var file = fileOptions.File;
        var linkRight = fileOptions.FileShare;

        if (file == null)
        {
            var curFile = await fileDao.GetFileAsync(fileId);

            if (curFile != null && 0 < version && version < curFile.Version)
            {
                file = await fileDao.GetFileAsync(fileId, version);
                lastVersion = false;
            }
            else
            {
                file = curFile;
            }
        }

        return await GetParamsAsync(file, lastVersion, linkRight, true, true, editPossible, tryEdit, tryCoauth);
    }

    public async Task<(File<T> File, Configuration<T> Configuration, bool LocatedInPrivateRoom)> GetParamsAsync<T>(File<T> file, bool lastVersion, FileShare linkRight, bool rightToRename, bool rightToEdit, bool editPossible, bool tryEdit, bool tryCoauth)
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

        var rightToFillForms = rightToEdit;
        var fillFormsPossible = editPossible;

        var rightToComment = rightToEdit;
        var commentPossible = editPossible;

        var rightModifyFilter = rightToEdit;

        rightToEdit = rightToEdit
                      && (linkRight == FileShare.ReadWrite || linkRight == FileShare.CustomFilter
                          || await fileSecurity.CanEditAsync(file) || await fileSecurity.CanCustomFilterEditAsync(file));
        if (editPossible && !rightToEdit)
        {
            editPossible = false;
        }

        rightModifyFilter = rightModifyFilter
            && (linkRight == FileShare.ReadWrite
                || await fileSecurity.CanEditAsync(file));

        rightToRename = rightToRename && rightToEdit && await fileSecurity.CanRenameAsync(file);

        rightToReview = rightToReview
                        && (linkRight == FileShare.Review || linkRight == FileShare.ReadWrite
                            || await fileSecurity.CanReviewAsync(file));
        if (reviewPossible && !rightToReview)
        {
            reviewPossible = false;
        }

        rightToFillForms = rightToFillForms
                           && (linkRight == FileShare.FillForms || linkRight == FileShare.Review || linkRight == FileShare.ReadWrite
                               || await fileSecurity.CanFillFormsAsync(file));
        if (fillFormsPossible && !rightToFillForms)
        {
            fillFormsPossible = false;
        }

        rightToComment = rightToComment
                         && (linkRight == FileShare.Comment || linkRight == FileShare.Review || linkRight == FileShare.ReadWrite
                             || await fileSecurity.CanCommentAsync(file));
        if (commentPossible && !rightToComment)
        {
            commentPossible = false;
        }

        if (linkRight == FileShare.Restrict
            && !(editPossible || reviewPossible || fillFormsPossible || commentPossible)
            && !await fileSecurity.CanReadAsync(file))
        {
            if (file.ShareRecord is { IsLink: true, Share: not FileShare.Restrict, Options.Internal: true } && !authContext.IsAuthenticated)
            {
                throw new LinkScopeException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
            }
            
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
        }

        if (file.RootFolderType == FolderType.TRASH)
        {
            throw new Exception(FilesCommonResource.ErrorMessage_ViewTrashItem);
        }

        string strError = null;
        if ((editPossible || reviewPossible || fillFormsPossible || commentPossible)
            && await lockerManager.FileLockedForMeAsync(file.Id))
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

        if (editPossible
            && !fileUtility.CanWebEdit(file.Title))
        {
            rightToEdit = editPossible = false;
        }

        var locatedInPrivateRoom = false;

        if (file.RootFolderType == FolderType.VirtualRooms)
        {
            var folderDao = daoFactory.GetFolderDao<T>();

            locatedInPrivateRoom = await DocSpaceHelper.LocatedInPrivateRoomAsync(file, folderDao);
        }

        if (file.Encrypted
            && file.RootFolderType != FolderType.Privacy && !locatedInPrivateRoom)
        {
            rightToEdit = editPossible = false;
            rightToReview = reviewPossible = false;
            rightToFillForms = fillFormsPossible = false;
            rightToComment = commentPossible = false;
        }


        if (!editPossible && !fileUtility.CanWebView(file.Title))
        {
            throw new Exception($"{FilesCommonResource.ErrorMessage_NotSupportedFormat} ({FileUtility.GetFileExtension(file.Title)})");
        }

        if (reviewPossible &&
            !fileUtility.CanWebReview(file.Title))
        {
            rightToReview = reviewPossible = false;
        }

        if (fillFormsPossible &&
            !fileUtility.CanWebRestrictedEditing(file.Title))
        {
            rightToFillForms = fillFormsPossible = false;
        }

        if (commentPossible &&
            !fileUtility.CanWebComment(file.Title))
        {
            rightToComment = commentPossible = false;
        }

        var rightChangeHistory = rightToEdit && !file.Encrypted;

        if (fileTracker.IsEditing(file.Id))
        {
            rightChangeHistory = false;

            bool coauth;
            if ((editPossible || reviewPossible || fillFormsPossible || commentPossible)
                && tryCoauth
                && (!(coauth = fileUtility.CanCoAuthoring(file.Title)) || fileTracker.IsEditingAlone(file.Id)))
            {
                if (tryEdit)
                {
                    var editingBy = fileTracker.GetEditingBy(file.Id).FirstOrDefault();
                    strError = string.Format(!coauth
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

        var rightToDownload = await CanDownloadAsync(fileSecurity, file, linkRight);

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
            var linkDao = daoFactory.GetLinkDao();
            var sourceId = await linkDao.GetSourceAsync(file.Id.ToString());
            configuration.Document.IsLinkedForMe = !string.IsNullOrEmpty(sourceId);
        }

        if (await externalShare.GetLinkIdAsync() == Guid.Empty)
        {
            return (file, configuration, locatedInPrivateRoom);
        }

        configuration.Document.SharedLinkParam = FilesLinkUtility.ShareKey;
        configuration.Document.SharedLinkKey = externalShare.GetKey();

        return (file, configuration, locatedInPrivateRoom);
    }

    private async Task<bool> CanDownloadAsync<T>(FileSecurity fileSecurity, File<T> file, FileShare linkRight)
    {
        var canDownload = linkRight != FileShare.Restrict && linkRight != FileShare.Read && linkRight != FileShare.Comment;

        if (canDownload)
        {
            return true;
        }

        if (linkRight is FileShare.Read or FileShare.Comment)
        {
            var fileDao = daoFactory.GetFileDao<T>();
            file = await fileDao.GetFileAsync(file.Id); // reset Access prop
        }

        canDownload = await fileSecurity.CanDownloadAsync(file);

        return canDownload;
    }

    public string GetSignature(object payload)
    {
        if (string.IsNullOrEmpty(fileUtility.SignatureSecret))
        {
            return null;
        }

        return JsonWebToken.Encode(payload, fileUtility.SignatureSecret);
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

    public async Task CheckUsersForDropAsync<T>(File<T> file)
    {
        var usersDrop = new List<string>();

        foreach (var uid in fileTracker.GetEditingBy(file.Id))
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
