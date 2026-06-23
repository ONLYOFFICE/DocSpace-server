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

[Scope]
internal interface IDaoBase<TFile, TFolder, TItem>
    where TFile : class, TItem
    where TFolder : class, TItem
    where TItem : class
{
    void Init(string pathPrefix, IProviderInfo<TFile, TFolder, TItem> providerInfo);
    string GetName(TItem item);
    string GetId(TItem item);
    bool IsRoot(TFolder folder);
    string MakeThirdId(object entryId);
    string GetParentFolderId(TItem item);
    string MakeId(TItem item);
    string MakeId(string path = null);
    string MakeFolderTitle(TFolder folder);
    string MakeFileTitle(TFile file);
    Folder<string> ToFolder(TFolder folder);
    File<string> ToFile(TFile file);
    Task<Folder<string>> GetRootFolderAsync();
    Task<TFolder> CreateFolderAsync(string title, string folderId);
    Task<TFolder> GetFolderAsync(string folderId);
    Task<TFile> GetFileAsync(string fileId);
    Task<IEnumerable<string>> GetChildrenAsync(string folderId);
    Task<List<TItem>> GetItemsAsync(string parentId, bool? folder = null);
    bool CheckInvalidFilter(FilterType filterType);
    bool CheckInvalidFilters(IEnumerable<FilterType> filterTypes);
    Task UpdateIdAsync(string oldValue, string newValue);
    Folder<string> GetErrorRoom();
    bool IsRoom(string folderId);
}