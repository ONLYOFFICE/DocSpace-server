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

namespace ASC.Files.Core.Configuration;

public class DefaultTemplateSettings : ISettings<DefaultTemplateSettings>
{
    public List<DefaultTempalteItem> Items { get; init; }

    public static Guid ID => new("{57E8457D-78EF-4006-B466-F4C503A01AC0}");

    public DefaultTemplateSettings GetDefault()
    {
        return new DefaultTemplateSettings { Items = [] };
    }

    public DateTime LastModified { get; set; }
}

public class DefaultTempalteItem
{
    public int? SelectedFile { get; set; }
    public string FileExtension { get; set; }
}

[Scope]
public class DefaultTemplateSettingsHelper(IServiceProvider serviceProvider,
    SettingsManager settingsManager,
    GlobalStore globalStore,
    IFileDao<int> fileDao,
    IFileDao<string> fileThirdPartyDao,
    IFolderDao<int> folderDao,
    FilesMessageService fileMessageService,
    FileChecker fileChecker)
{
    public async Task<DefaultTemplateSettings> GetSettingsAsync()
    {
        var settings = await settingsManager.LoadAsync<DefaultTemplateSettings>();
        var templatesExtensions = await GetSampleDocumentsExtensionsListAsync();

        var result = new DefaultTemplateSettings { LastModified = settings.LastModified, Items = settings.Items.ToList() };
        _ = result.Items.RemoveAll(item => !templatesExtensions.Contains(item.FileExtension));
        var existingExtensions = new HashSet<string>(result.Items.Select(item => item.FileExtension));

        foreach (var key in templatesExtensions)
        {
            if (!existingExtensions.Contains(key))
            {
                result.Items.Add(new DefaultTempalteItem { FileExtension = key, SelectedFile = null });
            }
        }

        return result;
    }

    public async Task<DefaultTemplateSettings> SetTemplateAsync(string extension, JsonElement? fileId)
    {
        var settings = await GetSettingsAsync();
        var setting = settings.Items.FirstOrDefault(item => item.FileExtension.Equals(extension, StringComparison.OrdinalIgnoreCase));
        if (setting == null)
        {
            return settings;
        }

        var currentFileId = setting.SelectedFile;

        if (fileId != null)
        {
            var template = await CheckAndCopyFile();
            setting.SelectedFile = template.Id;
        }
        else
        {
            setting.SelectedFile = null;
        }

        if (currentFileId != null)
        {
            await fileDao.DeleteFileAsync(currentFileId.Value);
        }

        _ = await settingsManager.SaveAsync(settings);

        fileMessageService.Send(MessageAction.DocumentsDefaultTemplatesSettingsUpdated, setting.FileExtension, fileId?.ToString());

        return settings;

        async Task<File<int>> CheckAndCopyFile()
        {
            FileEntry file = fileId.Value.ValueKind switch
            {
                JsonValueKind.String => await fileThirdPartyDao.GetFileAsync(fileId.Value.GetString()),
                JsonValueKind.Number => await fileDao.GetFileAsync(fileId.Value.GetInt32()),
                _ => throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound)
            };

            if (file == null)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
            }

            if (Path.GetExtension(file.Title) != extension)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_NotSupportedFormat);
            }

            return fileId.Value.ValueKind switch
            {
                JsonValueKind.String => await fileThirdPartyDao.CopyFileAsync(fileId.Value.GetString(), await folderDao.GetFolderIDDefaultTemplatesAsync(true)),
                JsonValueKind.Number => await fileDao.CopyFileAsync(fileId.Value.GetInt32(), await folderDao.GetFolderIDDefaultTemplatesAsync(true)),
                _ => throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound)
            };
        }
    }

    public async Task<DefaultTemplateSettings> SetTemplateAsync(string extension, string title, Stream stream)
    {
        try
        {
            if (Path.GetExtension(title) != extension)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_NotSupportedFormat);
            }

            var settings = await GetSettingsAsync();
            var setting = settings.Items.FirstOrDefault(item => item.FileExtension.Equals(extension, StringComparison.OrdinalIgnoreCase));
            if (setting == null)
            {
                return settings;
            }

            if (FileUtility.GetFileTypeByExtention(setting.FileExtension) == FileType.Pdf)
            {
                using var checkStream = new MemoryStream();
                await stream.CopyToAsync(checkStream);
                stream.Seek(0, SeekOrigin.Begin);
                checkStream.Seek(0, SeekOrigin.Begin);
                var result = await fileChecker.CheckExtendedPDFstream(checkStream);
                if (!result)
                {
                    throw new InvalidOperationException(FilesCommonResource.ErrorMessage_UploadToFormRoom);
                }
            }

            var file = serviceProvider.GetService<File<int>>();
            file.ParentId = await folderDao.GetFolderIDDefaultTemplatesAsync(true);
            file.Title = title;
            file.ContentLength = stream.Length;
            file = await fileDao.SaveFileAsync(file, stream);

            var currentFileId = setting.SelectedFile;
            setting.SelectedFile = file.Id;

            if (currentFileId != null)
            {
                await fileDao.DeleteFileAsync(currentFileId.Value);
            }

            _ = await settingsManager.SaveAsync(settings);

            fileMessageService.Send(MessageAction.DocumentsDefaultTemplatesSettingsUpdated, setting.FileExtension, file.Id.ToString());

            return settings;
        }
        finally
        {
            if (stream != null)
            {
                await stream.DisposeAsync();
            }
        }
    }

    public async Task<DefaultTemplateSettings> RestoreSettingsAsync()
    {
        var parentId = await folderDao.GetFolderIDDefaultTemplatesAsync(true);
        var defaultTemplates = await fileDao.GetFilesAsync(parentId).ToListAsync();
        var templates = await fileDao.GetFilesAsync(defaultTemplates).ToListAsync();
        return new DefaultTemplateSettings
        {
            Items = [.. templates.Select(f => new DefaultTempalteItem { FileExtension = Path.GetExtension(f.Title), SelectedFile = f.Id })]
        };
    }

    private async Task<IEnumerable<string>> GetSampleDocumentsExtensionsListAsync()
    {
        var storeTemplate = await globalStore.GetStoreTemplateAsync();
        var path = await globalStore.GetNewDocTemplatePath(storeTemplate, new CultureInfo("en-US"));
        var extensions = await storeTemplate.ListFilesRelativeAsync("", path, "*", false)
            .Where(f => FileUtility.GetFileTypeByFileName(f) is not (FileType.Audio or FileType.Video or FileType.Image))
            .Select(FileUtility.GetFileExtension).Distinct()
            .ToListAsync();

        return extensions;
    }
}
