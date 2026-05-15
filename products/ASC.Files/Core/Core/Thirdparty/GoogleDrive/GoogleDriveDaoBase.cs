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

using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace ASC.Files.Thirdparty.GoogleDrive;

[Scope(typeof(IDaoBase<DriveFile, DriveFile, DriveFile>))]
internal class GoogleDriveDaoBase(
    IDaoFactory daoFactory,
    IServiceProvider serviceProvider,
    UserManager userManager,
    TenantManager tenantManager,
    TenantUtil tenantUtil,
    IDbContextFactory<FilesDbContext> dbContextFactory,
    FileUtility fileUtility,
    RegexDaoSelectorBase<DriveFile, DriveFile, DriveFile> regexDaoSelectorBase)
    : ThirdPartyProviderDao<DriveFile, DriveFile, DriveFile>(daoFactory, serviceProvider, userManager, tenantManager, tenantUtil, dbContextFactory, fileUtility, regexDaoSelectorBase),
        IDaoBase<DriveFile, DriveFile, DriveFile>
{
    private GoogleDriveProviderInfo _providerInfo;

    public void Init(string pathPrefix, IProviderInfo<DriveFile, DriveFile, DriveFile> providerInfo)
    {
        PathPrefix = pathPrefix;
        ProviderInfo = providerInfo;
        _providerInfo = providerInfo as GoogleDriveProviderInfo;
    }

    public string GetName(DriveFile item)
    {
        return item.Name;
    }

    public string GetId(DriveFile item)
    {
        return item.Id;
    }

    public bool isFile(DriveFile item)
    {
        return !IsDriveFolder(item);
    }

    public string MakeThirdId(object entryId)
    {
        var id = Convert.ToString(entryId, CultureInfo.InvariantCulture);

        return string.IsNullOrEmpty(id)
                   ? "root"
                   : id.TrimStart('/');
    }

    public string GetParentFolderId(DriveFile driveEntry)
    {
        return driveEntry?.Parents == null || driveEntry.Parents.Count == 0
                   ? null
                   : driveEntry.Parents[0];
    }

    public string MakeId(DriveFile driveEntry)
    {
        var path = string.Empty;
        if (driveEntry != null)
        {
            path = IsRoot(driveEntry) ? "root" : driveEntry.Id;
        }

        return MakeId(path);
    }

    public override string MakeId(string path = null)
    {
        var p = string.IsNullOrEmpty(path) || path == "root" || path == ProviderInfo.RootFolderId ? "" : "-|" + path.TrimStart('/');

        return $"{PathPrefix}{p}";
    }

    public string MakeFolderTitle(DriveFile driveFolder)
    {
        if (driveFolder == null || IsRoot(driveFolder))
        {
            return ProviderInfo.CustomerTitle;
        }

        return Global.ReplaceInvalidCharsAndTruncate(driveFolder.Name);
    }

    public string MakeFileTitle(DriveFile driveFile)
    {
        if (driveFile == null || string.IsNullOrEmpty(driveFile.Name))
        {
            return ProviderInfo.ProviderKey;
        }

        var title = driveFile.Name;

        var gExt = MimeMapping.GetExtention(driveFile.MimeType);
        if (!GoogleLoginProvider.GoogleDriveExt.Contains(gExt))
        {
            return Global.ReplaceInvalidCharsAndTruncate(title);
        }

        var downloadableExtension = _fileUtility.GetGoogleDownloadableExtension(gExt);
        if (!downloadableExtension.Equals(FileUtility.GetFileExtension(title)))
        {
            title += downloadableExtension;
        }

        return Global.ReplaceInvalidCharsAndTruncate(title);
    }

    public Folder<string> ToFolder(DriveFile driveEntry)
    {
        switch (driveEntry)
        {
            case null:
                return null;
            case ErrorDriveEntry entry:
                return ToErrorFolder(entry);
        }

        if (driveEntry.MimeType != GoogleLoginProvider.GoogleDriveMimeTypeFolder)
        {
            return null;
        }

        var isRoot = IsRoot(driveEntry);

        var folder = GetFolder();

        folder.Id = MakeId(driveEntry);
        folder.ParentId = isRoot ? null : MakeId(GetParentFolderId(driveEntry));
        folder.Title = MakeFolderTitle(driveEntry);
        folder.CreateOn = isRoot ? ProviderInfo.CreateOn : driveEntry.CreatedTimeDateTimeOffset?.DateTime ?? default;
        folder.ModifiedOn = isRoot ? ProviderInfo.ModifiedOn : driveEntry.ModifiedTimeDateTimeOffset?.DateTime ?? default;
        folder.SettingsPrivate = ProviderInfo.Private;
        folder.SettingsHasLogo = ProviderInfo.HasLogo;
        folder.SettingsColor = ProviderInfo.Color;
        folder.SettingsCover = ProviderInfo.Cover;
        ProcessFolderAsRoom(folder);

        if (folder.CreateOn != DateTime.MinValue && folder.CreateOn.Kind == DateTimeKind.Utc)
        {
            folder.CreateOn = _tenantUtil.DateTimeFromUtc(folder.CreateOn);
        }

        if (folder.ModifiedOn != DateTime.MinValue && folder.ModifiedOn.Kind == DateTimeKind.Utc)
        {
            folder.ModifiedOn = _tenantUtil.DateTimeFromUtc(folder.ModifiedOn);
        }

        return folder;
    }

    public bool IsRoot(DriveFile driveFolder)
    {
        return IsDriveFolder(driveFolder) && GetParentFolderId(driveFolder) == null;
    }

    private static bool IsDriveFolder(DriveFile driveFolder)
    {
        return driveFolder != null && driveFolder.MimeType == GoogleLoginProvider.GoogleDriveMimeTypeFolder;
    }

    private File<string> ToErrorFile(ErrorDriveEntry driveEntry)
    {
        if (driveEntry == null)
        {
            return null;
        }

        var file = GetErrorFile(new ErrorEntry(driveEntry.Error, driveEntry.ErrorId));

        file.Title = MakeFileTitle(driveEntry);

        return file;
    }

    private Folder<string> ToErrorFolder(ErrorDriveEntry driveEntry)
    {
        if (driveEntry == null)
        {
            return null;
        }

        var folder = GetErrorFolder(new ErrorEntry(driveEntry.Error, driveEntry.ErrorId));

        folder.Title = MakeFolderTitle(driveEntry);

        return folder;
    }

    public File<string> ToFile(DriveFile driveFile)
    {
        switch (driveFile)
        {
            case null:
                return null;
            case ErrorDriveEntry entry:
                return ToErrorFile(entry);
        }

        var file = GetFile();

        file.Id = MakeId(driveFile.Id);
        file.ContentLength = driveFile.Size ?? 0;
        file.CreateOn = driveFile.CreatedTimeDateTimeOffset.HasValue ? _tenantUtil.DateTimeFromUtc(driveFile.CreatedTimeDateTimeOffset.Value.UtcDateTime) : default;
        file.ParentId = MakeId(GetParentFolderId(driveFile));
        file.ModifiedOn = driveFile.ModifiedTimeDateTimeOffset.HasValue ? _tenantUtil.DateTimeFromUtc(driveFile.ModifiedTimeDateTimeOffset.Value.UtcDateTime) : default;
        file.Title = MakeFileTitle(driveFile);
        file.ThumbnailStatus = driveFile.HasThumbnail.HasValue && driveFile.HasThumbnail.Value ? Thumbnail.Created : Thumbnail.Creating;
        file.Encrypted = ProviderInfo.Private;

        return file;
    }

    public async Task<Folder<string>> GetRootFolderAsync()
    {
        return ToFolder(await GetFolderAsync(""));
    }

    public async Task<DriveFile> GetFileAsync(string entryId)
    {
        var driveId = MakeThirdId(entryId);
        try
        {
            var entry = await _providerInfo.GetFileAsync(driveId);

            return entry;
        }
        catch (Exception ex)
        {
            return new ErrorDriveEntry(ex, driveId);
        }
    }

    public async Task<DriveFile> CreateFolderAsync(string title, string folderId)
    {
        return await _providerInfo.CreateFolderAsync(title, MakeThirdId(folderId), GetId);
    }

    public async Task<DriveFile> GetFolderAsync(string entryId)
    {
        var driveId = MakeThirdId(entryId);
        try
        {
            var entry = await _providerInfo.GetFolderAsync(driveId);

            return entry;
        }
        catch (Exception ex)
        {
            return new ErrorDriveEntry(ex, driveId);
        }
    }

    public override async Task<IEnumerable<string>> GetChildrenAsync(string folderId)
    {
        var entries = await GetItemsAsync(folderId);

        return entries.Select(entry => MakeId(entry.Id));
    }

    public async Task<List<DriveFile>> GetItemsAsync(string parentId, bool? folder = null)
    {
        var parentDriveId = MakeThirdId(parentId);
        if (folder == null)
        {
            return await _providerInfo.GetItemsAsync(parentDriveId, GetId, isFile);
        }

        return await _providerInfo.GetItemsAsync(parentDriveId, folder, GetId, isFile);
    }

    private sealed class ErrorDriveEntry : DriveFile, IErrorItem
    {
        public string Error { get; }
        public string ErrorId { get; }

        public ErrorDriveEntry(Exception e, object id)
        {
            ErrorId = id.ToString();
            if (id.ToString() == "root")
            {
                MimeType = GoogleLoginProvider.GoogleDriveMimeTypeFolder;
            }
            if (e != null)
            {
                Error = e.Message;
            }
        }
    }
}