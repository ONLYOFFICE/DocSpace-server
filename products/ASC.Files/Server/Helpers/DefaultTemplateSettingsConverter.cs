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
