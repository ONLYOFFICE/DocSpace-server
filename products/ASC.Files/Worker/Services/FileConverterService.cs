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

using System.Security;

using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.Files.Worker.Services;

[Singleton(GenericArguments = [typeof(int)])]
[Singleton(GenericArguments = [typeof(string)])]
public class FileConverterService<T>(
        IServiceScopeFactory scopeFactory,
        ILogger<FileConverterService<T>> logger)
     : ActivePassiveBackgroundService<FileConverterService<T>>(logger, scopeFactory)
{
    private static readonly int _timerDelay = 1000;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.FromMilliseconds(_timerDelay);
    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var serviceScope = _scopeFactory.CreateAsyncScope();

            var fileConverterQueue = serviceScope.ServiceProvider.GetService<FileConverterQueue>();

            var conversionQueue = (await fileConverterQueue.GetAllTaskAsync<T>()).ToList();

            if (conversionQueue.Count == 0)
            {
                return;
            }

            logger.DebugRunCheckConvertFilesStatus(conversionQueue.Count);
            var filesIsConverting = conversionQueue
                                    .Where(x => string.IsNullOrEmpty(x.Processed))
                                    .ToList();

            foreach (var converter in filesIsConverting)
            {
                converter.Processed = "1";

                var parsed = JsonDocument.Parse(converter.Source).RootElement;
                var fileId = parsed.GetProperty("id").Deserialize<T>();
                var fileVersion = parsed.GetProperty("version").Deserialize<int>();
                var updateIfExist = parsed.GetProperty("updateIfExist").Deserialize<bool>();

                int operationResultProgress;
                var password = converter.Password;
                var outputType = converter.OutputType;

                var commonLinkUtility = serviceScope.ServiceProvider.GetService<CommonLinkUtility>();
                commonLinkUtility.ServerUri = converter.ServerRootPath;

                var scopeClass = serviceScope.ServiceProvider.GetService<FileConverterQueueScope>();
                var (tenantManager, userManager, securityContext, daoFactory, fileSecurity, pathProvider, fileUtility, documentServiceHelper, documentServiceConnector, entryManager, fileConverter) = scopeClass;

                await tenantManager.SetCurrentTenantAsync(converter.TenantId);

                await securityContext.AuthenticateMeWithoutCookieAsync(converter.Account);

                var file = await daoFactory.GetFileDao<T>().GetFileAsync(fileId, fileVersion);
                if (file == null)
                {
                    continue;
                }

                var fileUri = file.Id.ToString();

                string convertedFileUrl;
                string convertedFileType;

                try
                {
                    var user = await userManager.GetUsersAsync(converter.Account);

                    var culture = string.IsNullOrEmpty(user.CultureName) ? tenantManager.GetCurrentTenant().GetCulture() : CultureInfo.GetCultureInfo(user.CultureName);

                    CultureInfo.CurrentCulture = culture;
                    CultureInfo.CurrentUICulture = culture;

                    if (!await fileSecurity.CanReadAsync(file) && file.RootFolderType != FolderType.BUNCH)
                    {
                        //No rights in CRM after upload before attach
                        throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
                    }

                    fileUri = pathProvider.GetFileStreamUrl(file);

                    var toExtension = fileUtility.GetInternalConvertExtension(file.Title);

                    if (!string.IsNullOrEmpty(outputType) && await fileConverter.EnableConvertAsync(file, outputType, false))
                    {
                        toExtension = outputType;
                    }

                    var fileExtension = file.ConvertedExtension;
                    var docKey = await documentServiceHelper.GetDocKeyAsync(file);

                    fileUri = documentServiceConnector.ReplaceCommunityAddress(fileUri);
                    (operationResultProgress, convertedFileUrl, convertedFileType) = await documentServiceConnector.GetConvertedUriAsync(fileUri, fileExtension, toExtension, docKey, password, CultureInfo.CurrentUICulture.Name, null, null, null, true, false);
                }
                catch (Exception exception)
                {
                    var password1 = exception.InnerException is DocumentServiceException { Code: DocumentServiceException.ErrorCode.ConvertPassword };

                    logger.ErrorConvertFileWithUrl(file.Id.ToString(), fileUri, exception);

                    if (converter.Delete)
                    {
                        conversionQueue.Remove(converter);
                    }
                    else
                    {
                        converter.Progress = 100;
                        converter.StopDateTime = DateTime.UtcNow;
                        converter.Error = exception.Message;

                        if (password1)
                        {
                            converter.Result = "password";
                        }
                    }

                    continue;
                }

                operationResultProgress = Math.Min(operationResultProgress, 100);

                if (operationResultProgress < 100)
                {
                    if (DateTime.UtcNow - converter.StartDateTime > TimeSpan.FromMinutes(10))
                    {
                        converter.StopDateTime = DateTime.UtcNow;
                        converter.Error = FilesCommonResource.ErrorMessage_ConvertTimeout;

                        logger.ErrorCheckConvertFilesStatus(file.Id.ToString(), file.ContentLength);
                    }
                    else
                    {
                        converter.Processed = "";
                    }

                    converter.Progress = operationResultProgress;

                    logger.DebugCheckConvertFilesStatusIterationContinue();

                    continue;
                }

                File<T> newFile = null;

                var operationResultError = string.Empty;

                try
                {
                    newFile = await fileConverter.SaveConvertedFileAsync(file, convertedFileUrl, convertedFileType, updateIfExist);
                }
                catch (Exception e)
                {
                    operationResultError = e.Message;

                    logger.ErrorOperation(operationResultError, convertedFileUrl, fileUri, convertedFileType, e);

                    continue;
                }
                finally
                {
                    if (converter.Delete)
                    {
                        conversionQueue.Remove(converter);
                    }
                    else
                    {
                        if (newFile != null)
                        {
                            var folderDao = daoFactory.GetFolderDao<T>();
                            var folder = await folderDao.GetFolderAsync(newFile.ParentId);
                            var folderTitle = await fileSecurity.CanReadAsync(folder) ? folder.Title : null;

                            converter.Result = await fileConverterQueue.FileJsonSerializerAsync(entryManager, newFile, folderTitle);
                        }

                        converter.Progress = 100;
                        converter.StopDateTime = DateTime.UtcNow;
                        converter.Processed = "1";

                        if (!string.IsNullOrEmpty(operationResultError))
                        {
                            converter.Error = operationResultError;
                        }
                    }
                }

                logger.DebugCheckConvertFilesStatusIterationEnd();
            }

            await fileConverterQueue.SetAllTask<T>(conversionQueue);

        }
        catch (Exception exception)
        {
            logger.ErrorWithException(exception);
        }
    }
}

[Scope]
public record FileConverterQueueScope(
    TenantManager TenantManager,
    UserManager UserManager,
    SecurityContext SecurityContext,
    IDaoFactory DaoFactory,
    FileSecurity FileSecurity,
    PathProvider PathProvider,
    FileUtility FileUtility,
    DocumentServiceHelper DocumentServiceHelper,
    DocumentServiceConnector DocumentServiceConnector,
    EntryStatusManager EntryManager,
    FileConverter FileConverter);