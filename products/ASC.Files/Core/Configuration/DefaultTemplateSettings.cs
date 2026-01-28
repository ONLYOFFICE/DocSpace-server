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

namespace ASC.Files.Core.Configuration
{
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
                                               Global global,
                                               GlobalStore globalStore,
                                               IFileDao<int> fileDao,
                                               IFolderDao<int> folderDao,
                                               ExternalShare externalShare,
                                               CommonLinkUtility commonLinkUtility)
    {
        public async Task<DefaultTemplateSettingsDto> ToDto(DefaultTemplateSettings settings)
        {
            var fileIds = settings.Items.Where(i => i.SelectedFile != null).Select(i => i.SelectedFile.Value);
            var fileTitles = (await fileDao.GetFilesAsync(fileIds).ToListAsync()).ToDictionary(f => f.Id, f => f);

            DefaultTempalteItemDto asDto(DefaultTempalteItem item)
            {
                if (item.SelectedFile.HasValue)
                {
                    var file = fileTitles[item.SelectedFile.Value];
                    return new DefaultTempalteItemDto()
                    {
                        FileExtension = item.FileExtension,
                        SelectedFile = item.SelectedFile,
                        FileTitle = file.Title,
                        LastModified = file.ModifiedOn,
                        ViewUrl = externalShare.GetUrlWithShare(commonLinkUtility.GetFullAbsolutePath(file.DownloadUrl))
                    };
                }
                else
                {
                    return new DefaultTempalteItemDto()
                    {
                        FileExtension = item.FileExtension
                    };
                }
            }

            return new DefaultTemplateSettingsDto()
            {
                Items = settings.Items.Select(asDto)
            };
        }

        public async Task<DefaultTemplateSettings> GetSettings()
        {
            if (!await global.IsDocSpaceAdministratorAsync)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            var settings = await settingsManager.LoadAsync<DefaultTemplateSettings>();
            var templatesExtensions = await GetSampleDocumentsExtensionsList();

            _ = settings.Items.RemoveAll(item => !templatesExtensions.Contains(item.FileExtension));
            var existingExtensions = new HashSet<string>(settings.Items.Select(item => item.FileExtension));

            foreach (var key in templatesExtensions)
            {
                if (!existingExtensions.Contains(key))
                {
                    settings.Items.Add(new DefaultTempalteItem() { FileExtension = key, SelectedFile = null });
                }
            }

            return settings;
        }

        public async Task<DefaultTemplateSettings> SetTemplate(string extension, int? fileId)
        {
            if (!await global.IsDocSpaceAdministratorAsync)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            var settings = await GetSettings();
            var setting = settings.Items.FirstOrDefault(item => item.FileExtension.Equals(extension, StringComparison.OrdinalIgnoreCase));
            if (setting == null)
            {
                return settings;
            }

            var currentFileId = setting.SelectedFile;

            if (fileId != null)
            {
                var template = await fileDao.CopyFileAsync(fileId.Value, await folderDao.GetFolderIDDefaultTemplatesAsync(true));
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
            return settings;
        }

        public async Task<DefaultTemplateSettings> SetTemplate(string extension, string title, Stream stream)
        {
            try
            {
                if (!await global.IsDocSpaceAdministratorAsync)
                {
                    throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
                }

                var settings = await GetSettings();
                var setting = settings.Items.FirstOrDefault(item => item.FileExtension.Equals(extension, StringComparison.OrdinalIgnoreCase));
                if (setting == null)
                {
                    return settings;
                }

                var file = serviceProvider.GetService<File<int>>();
                file.ParentId = await folderDao.GetFolderIDDefaultTemplatesAsync(true);
                file.Title = title;
                file = await fileDao.SaveFileAsync(file, stream);

                var currentFileId = setting.SelectedFile;
                setting.SelectedFile = file.Id;

                if (currentFileId != null)
                {
                    await fileDao.DeleteFileAsync(currentFileId.Value);
                }

                _ = await settingsManager.SaveAsync(settings);
                return settings;
            }
            catch
            {
                throw;
            }
            finally
            {
                stream?.Dispose();
            }
        }

        private async Task<IEnumerable<string>> GetSampleDocumentsExtensionsList()
        {
            var storeTemplate = await globalStore.GetStoreTemplateAsync();
            var path = await globalStore.GetNewDocTemplatePath(storeTemplate, new CultureInfo("en-US"));
            var extensions = await storeTemplate.ListFilesRelativeAsync("", path, "*", false)
                .Where(f => FileUtility.GetFileTypeByFileName(f) is not (FileType.Audio or FileType.Video or FileType.Image))
                .Select(f => FileUtility.GetFileExtension(f)).Distinct()
                .ToListAsync();

            return extensions;
        }
    }
}
