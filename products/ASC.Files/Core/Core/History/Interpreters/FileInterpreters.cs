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

namespace ASC.Files.Core.Core.History.Interpreters;

public abstract class FileActionInterpreterBase : ActionInterpreter
{
    protected static IDictionary<Accessibility, bool> GetAccessibility(IServiceProvider serviceProvider, string fileName)
    {
        var fileUtility = serviceProvider.GetRequiredService<FileUtility>();

        var result = new Dictionary<Accessibility, bool>();

        foreach (var r in Enum.GetValues<Accessibility>())
        {
            var val = r switch
            {
                Accessibility.ImageView => fileUtility.CanImageView(fileName),
                Accessibility.MediaView => fileUtility.CanMediaView(fileName),
                Accessibility.WebView => fileUtility.GetWebViewAccessibility(fileName),
                Accessibility.WebEdit => fileUtility.CanWebEdit(fileName),
                Accessibility.WebReview => fileUtility.CanWebReview(fileName),
                Accessibility.WebCustomFilterEditing => fileUtility.CanWebCustomFilterEditing(fileName),
                Accessibility.WebRestrictedEditing => fileUtility.CanWebRestrictedEditing(fileName),
                Accessibility.WebComment => fileUtility.CanWebComment(fileName),
                Accessibility.MustConvert => fileUtility.MustConvert(fileName),
                _ => false
            };

            result.Add(r, val);
        }

        return result;
    }

    protected static string GetViewUrl(IServiceProvider serviceProvider, string fileId)
    {
        var filesLinkUtility = serviceProvider.GetRequiredService<FilesLinkUtility>();
        var commonLinkUtility = serviceProvider.GetRequiredService<CommonLinkUtility>();

        return commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileDownloadUrl(fileId));
    }
}

#region Data

#endregion

#region Interpreters

public class FileCreateInterpreter : FileActionInterpreterBase
{
    protected override ValueTask<HistoryDataDto> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var desc = GetAdditionalDescription(description);
        var accessibility = GetAccessibility(serviceProvider, description[0]);

        return new ValueTask<HistoryDataDto>(new FileHistoryDataDto(target, description[0], desc.ParentId, desc.ParentTitle, desc.ParentType, accessibility: accessibility));
    }
}

public class FileMovedInterpreter : FileActionInterpreterBase
{
    protected override ValueTask<HistoryDataDto> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var splitTarget = target.Split(',');
        var desc = GetAdditionalDescription(description);
        var accessibility = GetAccessibility(serviceProvider, description[0]);
        var viewUrl = GetViewUrl(serviceProvider, splitTarget[0]);

        return new ValueTask<HistoryDataDto>(
            new FileOperationHistoryDataDto(
                splitTarget[0],
                description[0],
                splitTarget[1],
                desc.ParentTitle,
                desc.ParentType,
                desc.FromParentTitle,
                desc.FromParentType,
                desc.FromFolderId,
                accessibility,
                viewUrl));
    }
}

public class UserFileUpdatedInterpreter : FileActionInterpreterBase
{
    protected override ValueTask<HistoryDataDto> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var desc = GetAdditionalDescription(description);
        var accessibility = GetAccessibility(serviceProvider, description[1]);
        var viewUrl = GetViewUrl(serviceProvider, target);

        return new ValueTask<HistoryDataDto>(new UserFileUpdateHistoryDataDto(
            target,
            description[1],
            desc.ParentId,
            desc.ParentTitle,
            desc.ParentType,
            description[0],
            accessibility,
            viewUrl));
    }
}

public class FileUpdatedInterpreter : FileActionInterpreterBase
{
    protected override ValueTask<HistoryDataDto> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var desc = GetAdditionalDescription(description);
        var accessibility = GetAccessibility(serviceProvider, description[1]);
        var viewUrl = GetViewUrl(serviceProvider, target);

        return new ValueTask<HistoryDataDto>(new FileHistoryDataDto(
            target,
            description[1],
            desc.ParentId,
            desc.ParentTitle,
            desc.ParentType,
            accessibility: accessibility,
            viewUrl: viewUrl));
    }
}

public class FileDeletedInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryDataDto> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        return new ValueTask<HistoryDataDto>(new EntryHistoryDataDto(target, description[0]));
    }
}

public class FileVersionDeletedInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryDataDto> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var desc = GetAdditionalDescription(description);
        return new ValueTask<HistoryDataDto>(new FileVersionRemovedHistoryDataDto(
            target,
            description[0],
            desc.ParentId,
            desc.ParentTitle,
            desc.ParentType,
            description[1]));
    }
}

public class FileRenamedInterpreter : FileActionInterpreterBase
{
    protected override ValueTask<HistoryDataDto> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var desc = GetAdditionalDescription(description);
        var accessibility = GetAccessibility(serviceProvider, description[0]);
        var viewUrl = GetViewUrl(serviceProvider, target);

        return new ValueTask<HistoryDataDto>(new FileRenameHistoryDataDto(
            target,
            description[1],
            description[0],
            desc.ParentId,
            desc.ParentTitle,
            desc.ParentType,
            accessibility: accessibility,
            viewUrl: viewUrl));
    }
}

public class FileUploadedInterpreter : FileActionInterpreterBase
{
    protected override ValueTask<HistoryDataDto> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var desc = GetAdditionalDescription(description);
        var accessibility = GetAccessibility(serviceProvider, description[0]);
        var viewUrl = GetViewUrl(serviceProvider, target);

        return new ValueTask<HistoryDataDto>(new FileHistoryDataDto(
            target,
            description[0],
            desc.ParentId,
            desc.ParentTitle,
            desc.ParentType,
            accessibility: accessibility,
            viewUrl: viewUrl));
    }
}

public class FileCopiedInterpreter : FileActionInterpreterBase
{
    protected override ValueTask<HistoryDataDto> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var splitTarget = target.Split(',');
        var desc = GetAdditionalDescription(description);
        var accessibility = GetAccessibility(serviceProvider, description[0]);
        var viewUrl = GetViewUrl(serviceProvider, splitTarget[0]);

        return new ValueTask<HistoryDataDto>(
            new FileOperationHistoryDataDto(
                splitTarget[0],
                description[0],
                splitTarget[1],
                desc.ParentTitle,
                desc.ParentType,
                desc.FromParentTitle,
                desc.FromParentType,
                desc.FromFolderId,
                accessibility,
                viewUrl));
    }
}

public class FileConvertedInterpreter : FileActionInterpreterBase
{
    protected override ValueTask<HistoryDataDto> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var desc = GetAdditionalDescription(description);

        return new ValueTask<HistoryDataDto>(new EntryHistoryDataDto(target, description[0], desc.ParentId, desc.ParentTitle, desc.ParentType));
    }
}

public class FileLockInterpreter : FileActionInterpreterBase
{
    protected override ValueTask<HistoryDataDto> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var desc = GetAdditionalDescription(description);
        var accessibility = GetAccessibility(serviceProvider, description[0]);
        var viewUrl = GetViewUrl(serviceProvider, target);

        return new ValueTask<HistoryDataDto>(new FileHistoryDataDto(
            target,
            description[0],
            desc.ParentId,
            desc.ParentTitle,
            desc.ParentType,
            accessibility: accessibility,
            viewUrl: viewUrl));
    }
}

public class FileIndexChangedInterpreter : FileActionInterpreterBase
{
    protected override ValueTask<HistoryDataDto> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var desc = GetAdditionalDescription(description);
        var oldIndex = int.Parse(description[1]);
        var newIndex = int.Parse(description[2]);
        var accessibility = GetAccessibility(serviceProvider, description[0]);
        var viewUrl = GetViewUrl(serviceProvider, target);

        string context = null;
        if (description.Count >= 4)
        {
            context = description[3];
        }

        return new ValueTask<HistoryDataDto>(new FileIndexChangedHistoryDataDto(
            oldIndex,
            newIndex,
            target,
            description[0],
            desc.ParentId,
            desc.ParentTitle,
            desc.ParentType,
            accessibility,
            viewUrl,
            context));
    }
}

public class FileCustomFilterInterpreter : FileActionInterpreterBase
{
    protected override ValueTask<HistoryDataDto> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var desc = GetAdditionalDescription(description);
        var accessibility = GetAccessibility(serviceProvider, description[0]);
        var viewUrl = GetViewUrl(serviceProvider, target);

        return new ValueTask<HistoryDataDto>(new FileHistoryDataDto(
            target,
            description[0],
            desc.ParentId,
            desc.ParentTitle,
            desc.ParentType,
            accessibility: accessibility,
            viewUrl: viewUrl));
    }
}

#endregion
