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

namespace ASC.Files.Core.Core.Thirdparty;
public interface IThirdPartyStorage
{
    bool IsOpened { get; }
    AuthScheme AuthScheme { get; }

    void Open(AuthData authData);
    void Close();
    Task<long> GetMaxUploadSizeAsync();
    Task<bool> CheckAccessAsync();
    Task<Stream> GetThumbnailAsync(string fileId, uint width, uint height);
}

public interface IThirdPartyItemStorage<TItem> : IThirdPartyStorage
{
    Task<List<TItem>> GetItemsAsync(string folderId);
    Task DeleteItemAsync(TItem item);
}

public interface IGoogleDriveItemStorage<TItem> : IThirdPartyItemStorage<TItem>
{
    Task<List<TItem>> GetItemsAsync(string folderId, bool? folder);
}

public interface IThirdPartyFileStorage<TFile> : IThirdPartyStorage
{
    Task<TFile> GetFileAsync(string fileId);
    Task<TFile> CreateFileAsync(Stream fileStream, string title, string parentId);
    Task<Stream> DownloadStreamAsync(TFile file, int offset = 0);
    Task<TFile> MoveFileAsync(string fileId, string newFileName, string toFolderId);
    Task<TFile> CopyFileAsync(string fileId, string newFileName, string toFolderId);
    Task<TFile> RenameFileAsync(string fileId, string newName);
    Task<TFile> SaveStreamAsync(string fileId, Stream fileStream);
    Task<long> GetFileSizeAsync(TFile file);
}

public interface IThirdPartyFolderStorage<TFolder> : IThirdPartyStorage
{
    Task<TFolder> GetFolderAsync(string folderId);
    Task<TFolder> CreateFolderAsync(string title, string parentId);
    Task<TFolder> MoveFolderAsync(string folderId, string newFolderName, string toFolderId);
    Task<TFolder> CopyFolderAsync(string folderId, string newFolderName, string toFolderId);
    Task<TFolder> RenameFolderAsync(string folderId, string newName);
}

[Transient]
public interface IThirdPartyStorage<TFile, TFolder, TItem> : IThirdPartyFileStorage<TFile>, IThirdPartyFolderStorage<TFolder>, IThirdPartyItemStorage<TItem>
{
    IDataWriteOperator CreateDataWriteOperator(CommonChunkedUploadSession chunkedUploadSession, CommonChunkedUploadSessionHolder sessionHolder);
}