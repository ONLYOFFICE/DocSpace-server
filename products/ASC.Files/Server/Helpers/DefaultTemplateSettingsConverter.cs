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

using ASC.Files.Core.Configuration;

namespace ASC.Files.Helpers;

[Scope]
public class DefaultTemplateSettingsConverter(
    IFileDao<int> fileDao,
    ExternalShare externalShare,
    CommonLinkUtility commonLinkUtility,
    FilesLinkUtility filesLinkUtility)
{
    private static readonly List<string> _extensionsSortOrder = [".docx", ".xlsx", ".pptx", ".pdf"];

    public async Task<DefaultTemplateSettingsDto> ConvertToDtoAsync(DefaultTemplateSettings settings)
    {
        var fileIds = settings.Items.Where(i => i.SelectedFile != null).Select(i => i.SelectedFile.Value);
        var fileTitles = (await fileDao.GetFilesAsync(fileIds).ToListAsync()).ToDictionary(f => f.Id, f => f);

        return new DefaultTemplateSettingsDto
        {
            Items = settings.Items
                .Select(AsDto)
                .OrderBy(template =>
                {
                    var index = _extensionsSortOrder.IndexOf(template.FileExtension);
                    return index == -1 ? int.MaxValue : index;
                })
                .ThenBy(template => template.FileExtension)
        };

        DefaultTemplateItemDto AsDto(DefaultTempalteItem item)
        {
            var result = new DefaultTemplateItemDto
            {
                FileExtension = item.FileExtension
            };

            if (item.SelectedFile.HasValue)
            {
                var file = fileTitles[item.SelectedFile.Value];
                result.SelectedFile = item.SelectedFile;
                result.FileTitle = file.Title;
                result.LastModified = file.ModifiedOn;
                result.ViewUrl = externalShare.GetUrlWithShare(commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileDownloadUrl(file.Id)));
                result.FileSize = file.ContentLength;
            }

            return result;
        }
    }
}
