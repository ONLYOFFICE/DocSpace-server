// (c) Copyright Ascensio System SIA 2009-2025
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

using System.Globalization;
using System.Text.RegularExpressions;

using ASC.AI.Core.Chat;
using ASC.Common.Threading;
using ASC.Web.Files.Classes;
using ASC.Web.Files.Services.DocumentService;

using ILogger = Microsoft.Extensions.Logging.ILogger;
using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.AI.Core.Export;

public abstract partial class ExportTask<T>(IServiceScopeFactory serviceScopeFactory) : DistributedTaskProgress
    where T : ExportTaskData
{
    protected T Data { get; private set; } = null!;
    protected ILogger Logger { get; private set; } = null!;

    protected Guid UserId { get; private set; }
    protected int TenantId { get; private set; }

    public virtual void Init(int tenantId, Guid userId, T data)
    {
        TenantId = tenantId;
        UserId = userId;
        Data = data;
    }

    protected abstract IAsyncEnumerable<Message> GetMessages(IServiceProvider serviceProvider);

    protected override async Task DoJob()
    {
        ExportFolder? exportFolder = null;
        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            Logger = scope.ServiceProvider.GetRequiredService<ILogger<ExportTask<T>>>();

            try
            {
                var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
                _ = await tenantManager.SetCurrentTenantAsync(TenantId);

                var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
                await securityContext.AuthenticateMeWithoutCookieAsync(UserId);

                var commonLinkUtility = scope.ServiceProvider.GetRequiredService<CommonLinkUtility>();

                if (!string.IsNullOrEmpty(Data.BaseUri))
                {
                    commonLinkUtility.ServerUri = Data.BaseUri;
                }

                var daoFactory = scope.ServiceProvider.GetRequiredService<IDaoFactory>();
                exportFolder = ExportFolder.Create(daoFactory, Data);

                await exportFolder.GetFolder();
                await exportFolder.CheckSecurity(scope.ServiceProvider.GetRequiredService<FileSecurity>());

                var messages = GetMessages(scope.ServiceProvider);

                var builder = new StringBuilder();

                await foreach (var message in messages)
                {
                    if (message.Role == Role.User)
                    {
                        builder.Append("# ");

                        foreach (var content in message.Contents)
                        {
                            if (content is TextMessageContent textContent)
                            {
                                builder.Append(textContent.Text);
                            }
                        }

                        builder.AppendLine();

                        continue;
                    }

                    foreach (var content in message.Contents)
                    {
                        if (content is not TextMessageContent textContent ||
                            string.IsNullOrEmpty(textContent.Text))
                        {
                            continue;
                        }

                        var processedText = CutThink(textContent.Text);

                        var sourceProcessedText = MdLinksRegex().Replace(processedText, evaluator: match =>
                        {
                            try
                            {
                                var title = match.Groups[1].Value;
                                var url = match.Groups[2].Value;

                                var isRelativeUrl = Uri.IsWellFormedUriString(url, UriKind.Relative);
                                if (isRelativeUrl)
                                {
                                    url = commonLinkUtility.GetFullAbsolutePath(url);
                                }

                                return isRelativeUrl ? $"[{title}]({url})" : match.Value;
                            }
                            catch
                            {
                                return match.Value;
                            }
                        });

                        builder.AppendLine(sourceProcessedText);
                    }

                    builder.AppendLine();
                }

                if (builder.Length == 0)
                {
                    throw new Exception("Messages not found");
                }

                var pathProvider = scope.ServiceProvider.GetRequiredService<PathProvider>();

                var markdown = builder.ToString();
                var bytes = Encoding.UTF8.GetBytes(markdown);
                await using var ms = new MemoryStream(bytes);
                var fileUri = await pathProvider.GetTempUrlAsync(ms, ".md");

                var docService = scope.ServiceProvider.GetRequiredService<DocumentServiceConnector>();

                var (_, outFileUri, outFileType) = await docService.GetConvertedUriAsync(fileUri, "md", "docx", Guid.NewGuid().ToString("n"), null, CultureInfo.CurrentUICulture.Name, null, null, null, false, false);

                var fileConverter = scope.ServiceProvider.GetRequiredService<FileConverter>();

                await exportFolder.SaveFile(fileConverter, outFileUri, outFileType, Data.Title, false);

                if (Status <= DistributedTaskStatus.Running)
                {
                    Status = DistributedTaskStatus.Completed;
                }
            }
            finally
            {
                var socket = scope.ServiceProvider.GetRequiredService<ChatSocketClient>();
                if (exportFolder != null)
                {
                    await exportFolder.NotifySocket(socket, Data.ChatId);
                }
            }
        }
        catch (Exception e)
        {
            Logger.ErrorWithException(e);
            Exception = e;
            Status = DistributedTaskStatus.Failted;
        }
        finally
        {
            IsCompleted = true;
            Percentage = 100;
            await PublishChanges();
        }
    }

    private static string CutThink(string content)
    {
        var start = content.IndexOf("<think>", StringComparison.Ordinal);
        var end = content.IndexOf("</think>", StringComparison.Ordinal) + "</think>".Length + 1;

        return start < 0 || end < 0
            ? content
            : string.Concat(content.AsSpan(0, start), content.AsSpan(end));
    }

    private abstract class ExportFolder
    {
        public static ExportFolder Create(IDaoFactory daoFactory, ExportTaskData exportTaskData)
        {
            if (exportTaskData.IsFolderThirdParty)
            {
                var folderDao = daoFactory.GetFolderDao<string>();
                return new ExportFolder<string>(folderDao, exportTaskData.FolderId);
            }
            else
            {
                var folderDao = daoFactory.GetFolderDao<int>();
                return new ExportFolder<int>(folderDao, int.Parse(exportTaskData.FolderId));
            }
        }

        public abstract Task GetFolder();
        public abstract Task CheckSecurity(FileSecurity fileSecurity);
        public abstract Task SaveFile(FileConverter fileConverter, string fileUri, string fileType, string title, bool updateIfExists);
        public abstract Task NotifySocket(ChatSocketClient chatSocketClient, Guid chatId);
    }

    private class ExportFolder<TFolder>(IFolderDao<TFolder> folderDao, TFolder folderId) : ExportFolder
    {
        public Folder<TFolder>? Folder { get; private set; }
        public File<TFolder>? Result { get; private set; }

        public override async Task GetFolder()
        {
            Folder = await folderDao.GetFolderAsync(folderId);

            if (Folder == null)
            {
                throw new Exception("Folder not found");
            }

            if (Folder.FolderType is FolderType.AiRoom)
            {
                var folder = await folderDao.GetFoldersAsync(Folder.Id, FolderType.ResultStorage)
                    .FirstOrDefaultAsync() ?? throw new Exception("Folder not found");

                Folder = folder;
            }
        }

        public override async Task CheckSecurity(FileSecurity fileSecurity)
        {
            if (!await fileSecurity.CanCreateAsync(Folder))
            {
                throw new SecurityException("Access denied");
            }
        }

        public override async Task SaveFile(FileConverter fileConverter, string fileUri, string fileType, string title, bool updateIfExists)
        {
            Result = await fileConverter.SaveConvertedFileAsync(Folder, fileUri, fileType, title, updateIfExists);
        }

        public override async Task NotifySocket(ChatSocketClient chatSocketClient, Guid chatId)
        {
            await chatSocketClient.ExportCompleted(chatId, Result);
        }
    }

    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled)]
    private static partial Regex MdLinksRegex();
}
