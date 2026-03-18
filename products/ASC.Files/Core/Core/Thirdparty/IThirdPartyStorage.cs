// (c) Copyright Ascensio System SIA 2009-2026
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