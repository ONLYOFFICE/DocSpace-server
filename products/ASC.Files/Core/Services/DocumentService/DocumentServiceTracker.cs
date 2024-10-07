// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Web.Files.Services.DocumentService;

public class DocumentServiceTracker
{
    #region Class

    public enum TrackerStatus
    {
        NotFound = 0,
        Editing = 1,
        MustSave = 2,
        Corrupted = 3,
        Closed = 4,
        MailMerge = 5,
        ForceSave = 6,
        CorruptedForceSave = 7
    }

    [DebuggerDisplay("{Status} - {Key}")]
    public class TrackerData
    {
        public List<Action> Actions { get; set; }
        public string ChangesUrl { get; set; }
        public string Filetype { get; set; }
        public ForceSaveInitiator ForceSaveType { get; set; }
        public object History { get; set; }
        public string Key { get; set; }
        public MailMergeData MailMerge { get; set; }
        public TrackerStatus Status { get; set; }
        public string Token { get; set; }
        public string Url { get; set; }
        public List<string> Users { get; set; }
        public string UserData { get; set; }
        public bool Encrypted { get; set; }
        public string FormsDataUrl { get; set; }

        [DebuggerDisplay("{Type} - {UserId}")]
        public class Action
        {
            public int Type { get; set; }
            public string UserId { get; set; }
        }

        public enum ForceSaveInitiator
        {
            Command = 0,
            User = 1,
            Timer = 2,
            UserSubmit = 3
        }
    }

    public enum MailMergeType
    {
        Html = 0,
        AttachDocx = 1,
        AttachPdf = 2
    }

    [DebuggerDisplay("{From}")]
    public class MailMergeData
    {
        public int RecordCount { get; set; }
        public int RecordErrorCount { get; set; }
        public int RecordIndex { get; set; }
        public string From { get; set; }
        public string Subject { get; set; }
        public string To { get; set; }
        public MailMergeType Type { get; set; }
        public string Title { get; set; } //attach
        public string Message { get; set; } //attach
    }

    public class TrackResponse
    {
        public int Error
        {
            get
            {
                return string.IsNullOrEmpty(Message)
                           ? 0 //error:0 - sended
                           : 1; //error:1 - some error
            }
        }

        public string Message { get; init; }

        public static string Serialize(TrackResponse response)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(response, options);
        }
    }

    public class History
    {
        public string ServerVersion { get; set; }
        public List<Change> Changes { get; set; }
    }

    public class Change
    {
        public DateTime Created { get; set; }
        public User User { get; set; }
    }

    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    #endregion
}

[Scope]
public class DocumentServiceTrackerHelper(SecurityContext securityContext,
    UserManager userManager,
    TenantManager tenantManager,
    FilesLinkUtility filesLinkUtility,
    EmailValidationKeyProvider emailValidationKeyProvider,
    BaseCommonLinkUtility baseCommonLinkUtility,
    SocketManager socketManager,
    GlobalStore globalStore,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    IDaoFactory daoFactory,
    ILogger<DocumentServiceTrackerHelper> logger,
    DocumentServiceHelper documentServiceHelper,
    EntryManager entryManager,
    FilesMessageService filesMessageService,
    DocumentServiceConnector documentServiceConnector,
    NotifyClient notifyClient,
    MailMergeTaskRunner mailMergeTaskRunner,
    FileTrackerHelper fileTracker,
    IHttpClientFactory clientFactory,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<string> GetCallbackUrlAsync<T>(T fileId)
    {
        var queryParams = HttpUtility.ParseQueryString(String.Empty);

        queryParams[FilesLinkUtility.Action] = "track";
        queryParams[FilesLinkUtility.FileId] = fileId.ToString();
        queryParams[FilesLinkUtility.AuthKey] = await emailValidationKeyProvider.GetEmailKeyAsync(fileId.ToString());

        if (httpContextAccessor?.HttpContext != null)
        {
            queryParams["request-x-real-ip"] = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            if (httpContextAccessor.HttpContext.Request.Headers.TryGetValue("User-Agent", out var header))
            {
                queryParams["request-user-agent"] = header.First();
            }
        }
        
        var callbackUrl = baseCommonLinkUtility.GetFullAbsolutePath($"{filesLinkUtility.FileHandlerPath}?{queryParams}"); 

        callbackUrl = await documentServiceConnector.ReplaceCommunityAddressAsync(callbackUrl);

        return callbackUrl;
    }

    public async Task<bool> StartTrackAsync<T>(T fileId, string docKeyForTrack)
    {
        var callbackUrl = await GetCallbackUrlAsync(fileId);

        return await documentServiceConnector.CommandAsync(CommandMethod.Info, docKeyForTrack, fileId, callbackUrl);
    }

    public async Task<TrackResponse> ProcessDataAsync<T>(T fileId, TrackerData fileData, string fillingSessionId)
    {
        switch (fileData.Status)
        {
            case TrackerStatus.NotFound:
                await fileTracker.RemoveAsync(fileId);
                await socketManager.StopEditAsync(fileId);

                break;

            case TrackerStatus.Editing:
                await ProcessEditAsync(fileId, fileData, !string.IsNullOrEmpty(fillingSessionId) );
                break;

            case TrackerStatus.MustSave:
            case TrackerStatus.Closed:
                if(fileData.Status == TrackerStatus.Closed)
                {
                    await fileTracker.RemoveAsync(fileId);
                    await socketManager.StopEditAsync(fileId);
                }
                var fileDao = daoFactory.GetFileDao<T>();
                var properties = await fileDao.GetProperties(fileId);
                if(properties?.FormFilling != null)
                {
                    var fileForDeletion = await documentServiceHelper.CheckNeedDeletion(fileDao, fileId, properties.FormFilling);
                    if (fileForDeletion != null)
                    {
                        await fileDao.SaveProperties(fileForDeletion.Id, null);
                        await socketManager.DeleteFileAsync(fileForDeletion);
                        await fileDao.DeleteFileAsync(fileForDeletion.Id);
                    }
                    else if(fileData.Status == TrackerStatus.MustSave)
                    {
                        return await ProcessSaveAsync(fileId, fileData);
                    }
                }
                else if(fileData.Status == TrackerStatus.MustSave)
                {
                    return await ProcessSaveAsync(fileId, fileData);
                }
                break;
            case TrackerStatus.Corrupted:
            case TrackerStatus.ForceSave:
            case TrackerStatus.CorruptedForceSave:
                return await ProcessSaveAsync(fileId, fileData, fillingSessionId);

            case TrackerStatus.MailMerge:
                return await ProcessMailMergeAsync(fileId, fileData);
        }
        return null;
    }

    private async Task ProcessEditAsync<T>(T fileId, TrackerData fileData, bool isFillingSession)
    {
        var users = await fileTracker.GetEditingByAsync(fileId);
        var usersDrop = new List<string>();
        File<T> file = null;

        var fileStable = await daoFactory.GetFileDao<T>().GetFileStableAsync(fileId);

        var docKey = await documentServiceHelper.GetDocKeyAsync(fileStable);

        if (!fileData.Key.Equals(docKey))
        {
            if (!documentServiceHelper.IsDocSubmitKey(docKey, fileData.Key))
            {
                logger.InformationDocServiceEditingFile(fileId.ToString(), docKey, fileData.Key, fileData.Users);
            }
            return;
        }

        foreach (var user in fileData.Users)
        {
            if (!Guid.TryParse(user, out var userId))
            {
                if (!string.IsNullOrEmpty(user) && user.StartsWith("uid-"))
                {
                    userId = Guid.Empty;
                }
                else
                {
                    logger.InformationDocServiceUserIdIsNotGuid(user);
                    continue;
                }
            }
            users.Remove(userId);

            try
            {
                file = await entryManager.TrackEditingAsync(fileId, userId, userId, await tenantManager.GetCurrentTenantIdAsync());
            }
            catch (Exception e)
            {
                logger.DebugDropCommand(fileId.ToString(), fileData.Key, user, e);
                usersDrop.Add(userId.ToString());
            }
        }

        if (usersDrop.Count > 0 && !await documentServiceHelper.DropUserAsync(fileData.Key, usersDrop.ToArray(), fileId))
        {
            logger.ErrorDocServiceDropFailed(usersDrop);
        }

        foreach (var removeUserId in users)
        {
            await fileTracker.RemoveAsync(fileId, userId: removeUserId);
        }

        await socketManager.StartEditAsync(fileId);

        if (file != null && fileData.Actions != null && fileData.Actions.Any(r => r.Type == 1))
        {
            if (Guid.TryParse(fileData.Actions.Last().UserId, out var userId))
            {
                await securityContext.AuthenticateMeWithoutCookieAsync(userId); //hack
            }
            if (isFillingSession)
            {
                var user = await userManager.GetUsersAsync(userId);
                await filesMessageService.SendAsync(MessageAction.FormOpenedForFilling, file, MessageInitiator.DocsService, user?.DisplayUserName(false, displayUserSettingsHelper), file.Title);
            }
            else
            {
                await filesMessageService.SendAsync(MessageAction.FileOpenedForChange, file, file.Title);
            }

            securityContext.Logout();
        }
    }

    private async Task<TrackResponse> ProcessSaveAsync<T>(T fileId, TrackerData fileData, string fillingSessionId = null)
    {
        var comments = new List<string>();
        if (fileData.Status is TrackerStatus.Corrupted or TrackerStatus.CorruptedForceSave)
        {
            comments.Add(FilesCommonResource.ErrorMessage_SaveCorrupted);
        }

        var forceSave = fileData.Status is TrackerStatus.ForceSave or TrackerStatus.CorruptedForceSave;

        if (fileData.Users == null || fileData.Users.Count == 0 || !Guid.TryParse(fileData.Users[0], out var userId))
        {
            userId = Guid.Empty;
        }

        var fileStable = await daoFactory.GetFileDao<T>().GetFileStableAsync(fileId);

        var docKey = await documentServiceHelper.GetDocKeyAsync(fileStable);
        if (!fileData.Key.Equals(docKey))
        {
            if (fileData.ForceSaveType != TrackerData.ForceSaveInitiator.UserSubmit ||
                !documentServiceHelper.IsDocSubmitKey(docKey, fileData.Key))
            {
                logger.ErrorDocServiceSavingFile(fileId.ToString(), docKey, fileData.Key);

                await StoringFileAfterErrorAsync(fileId, userId.ToString(), documentServiceConnector.ReplaceDocumentAddress(fileData.Url), fileData.Filetype);

                return new TrackResponse { Message = "Expected key " + docKey };
            }
        }

        UserInfo user = null;
        try
        {
            await securityContext.AuthenticateMeWithoutCookieAsync(userId);

            user = await userManager.GetUsersAsync(userId);
            var culture = string.IsNullOrEmpty(user.CultureName) ? (await tenantManager.GetCurrentTenantAsync()).GetCulture() : CultureInfo.GetCultureInfo(user.CultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
        catch (Exception ex)
        {
            logger.InformationDocServiceSaveError(userId, ex);
            if (!userId.Equals(ASC.Core.Configuration.Constants.Guest.ID))
            {
                comments.Add(FilesCommonResource.ErrorMessage_SaveAnonymous);
            }
        }

        File<T> file = null;
        var saveMessage = "Not saved";

        if (string.IsNullOrEmpty(fileData.Url))
        {
            try
            {
                comments.Add(FilesCommonResource.ErrorMessage_SaveUrlLost);

                file = await entryManager.CompleteVersionFileAsync(fileId, 0, false, false);

                await daoFactory.GetFileDao<T>().UpdateCommentAsync(file.Id, file.Version, string.Join("; ", comments));

                file = null;
                logger.ErrorDocServiceSave2(fileId.ToString(), userId, fileData.Key);
            }
            catch (Exception ex)
            {
                logger.ErrorDocServiceSaveVersionUpdate(fileId.ToString(), userId, fileData.Key, ex);
            }
        }
        else
        {
            if (fileData.Encrypted)
            {
                comments.Add(FilesCommonResource.CommentEditEncrypt);
            }

            var forceSaveType = ForcesaveType.None;
            if (forceSave)
            {
                switch (fileData.ForceSaveType)
                {
                    case TrackerData.ForceSaveInitiator.Command:
                        forceSaveType = ForcesaveType.Command;
                        comments.Add(FilesCommonResource.CommentAutosave);
                        break;
                    case TrackerData.ForceSaveInitiator.Timer:
                        forceSaveType = ForcesaveType.Timer;
                        comments.Add(FilesCommonResource.CommentAutosave);
                        break;
                    case TrackerData.ForceSaveInitiator.User:
                        forceSaveType = ForcesaveType.User;
                        comments.Add(FilesCommonResource.CommentForcesave);
                        break;
                    case TrackerData.ForceSaveInitiator.UserSubmit:
                        forceSaveType = ForcesaveType.UserSubmit;
                        comments.Add(FilesCommonResource.CommentSubmitFillForm);
                        break;
                }
            }

            try
            {
                file = await entryManager.SaveEditingAsync(fileId, fileData.Filetype, documentServiceConnector.ReplaceDocumentAddress(fileData.Url), null, string.Join("; ", comments), false, fileData.Encrypted, forceSaveType, true, fileData.FormsDataUrl, fillingSessionId);
                saveMessage = fileData.Status is TrackerStatus.MustSave or TrackerStatus.ForceSave ? null : "Status " + fileData.Status;
            }
            catch (Exception ex)
            {
                logger.ErrorDocServiceSave(fileId.ToString(), userId, fileData.Key, fileData.Url, ex);
                saveMessage = ex.Message;

                await StoringFileAfterErrorAsync(fileId, userId.ToString(), documentServiceConnector.ReplaceDocumentAddress(fileData.Url), fileData.Filetype);
            }
        }

        if (!forceSave)
        {
            await fileTracker.RemoveAsync(fileId);
            await socketManager.StopEditAsync(fileId);
        }

        if (file == null)
        {
            return new TrackResponse { Message = saveMessage };
        }

        string userName;

        if (user != null)
        {
            userName = user?.DisplayUserName(false, displayUserSettingsHelper);
        }
        else
        {
            try
            {
                var nameInEditor = JsonConvert.DeserializeObject<History>(fileData.History.ToString()).Changes
                    .OrderByDescending(x => x.Created)
                    .Select(x => x.User.Name)
                    .FirstOrDefault();

                nameInEditor = RemoveGuestPart(nameInEditor);
                
                userName = string.IsNullOrEmpty(nameInEditor) 
                    ? AuditReportResource.GuestAccount 
                    : nameInEditor;
            }
            catch
            {
                userName = AuditReportResource.GuestAccount;
            }
        }
        
        await filesMessageService.SendAsync(forceSave && fileData.ForceSaveType == TrackerData.ForceSaveInitiator.UserSubmit ? MessageAction.FormSubmit : MessageAction.UserFileUpdated, file, MessageInitiator.DocsService, userName, file.Title);

        if (!forceSave)
        {
            await SaveHistoryAsync(file, (fileData.History ?? "").ToString(), documentServiceConnector.ReplaceDocumentAddress(fileData.ChangesUrl));
        }

        return new TrackResponse { Message = saveMessage };
        
        string RemoveGuestPart(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }
            
            var index = name.LastIndexOf('(');
            if (index != -1)
            {
                name = name[..index].Trim();
            }

            return name;
        }
    }

    private async Task<TrackResponse> ProcessMailMergeAsync<T>(T fileId, TrackerData fileData)
    {
        if (fileData.Users == null || fileData.Users.Count == 0 || !Guid.TryParse(fileData.Users[0], out var userId))
        {
            userId = (await fileTracker.GetEditingByAsync(fileId)).FirstOrDefault();
        }

        string saveMessage;

        try
        {
            await securityContext.AuthenticateMeWithoutCookieAsync(userId);

            var user = await userManager.GetUsersAsync(userId);
            var culture = string.IsNullOrEmpty(user.CultureName) ? (await tenantManager.GetCurrentTenantAsync()).GetCulture() : CultureInfo.GetCultureInfo(user.CultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            if (string.IsNullOrEmpty(fileData.Url))
            {
                throw new ArgumentException("emptry url");
            }

            if (fileData.MailMerge == null)
            {
                throw new ArgumentException("MailMerge is null");
            }

            var message = fileData.MailMerge.Message;
            Stream attach = null;
            var httpClient = clientFactory.CreateClient(nameof(ASC.Files.Core.Helpers.DocumentService));
            switch (fileData.MailMerge.Type)
            {
                case MailMergeType.AttachDocx:
                case MailMergeType.AttachPdf:
                    var requestDownload = new HttpRequestMessage
                    {
                        RequestUri = new Uri(documentServiceConnector.ReplaceDocumentAddress(fileData.Url))
                    };

                    using (var responseDownload = await httpClient.SendAsync(requestDownload))
                    await using (var streamDownload = await responseDownload.Content.ReadAsStreamAsync())
                    await using (var downloadStream = new ResponseStream(streamDownload, streamDownload.Length))
                    {
                        const int bufferSize = 2048;
                        var buffer = new byte[bufferSize];
                        int readed;
                        attach = new MemoryStream();
                        while ((readed = await downloadStream.ReadAsync(buffer.AsMemory(0, bufferSize))) > 0)
                        {
                            await attach.WriteAsync(buffer.AsMemory(0, readed));
                        }

                        attach.Position = 0;
                    }

                    if (string.IsNullOrEmpty(fileData.MailMerge.Title))
                    {
                        fileData.MailMerge.Title = "Attach";
                    }

                    var attachExt = fileData.MailMerge.Type == MailMergeType.AttachDocx ? ".docx" : ".pdf";
                    var curExt = FileUtility.GetFileExtension(fileData.MailMerge.Title);
                    if (curExt != attachExt)
                    {
                        fileData.MailMerge.Title += attachExt;
                    }

                    break;

                case MailMergeType.Html:
                    var httpRequest = new HttpRequestMessage
                    {
                        RequestUri = new Uri(documentServiceConnector.ReplaceDocumentAddress(fileData.Url))
                    };

                    using (var httpResponse = await httpClient.SendAsync(httpRequest))
                    await using (var stream = await httpResponse.Content.ReadAsStreamAsync())
                    {
                        using var reader = new StreamReader(stream, Encoding.GetEncoding(Encoding.UTF8.WebName));
                        message = await reader.ReadToEndAsync();
                    }

                    break;
            }

            using (var mailMergeTask =
                new MailMergeTask
                {
                    From = fileData.MailMerge.From,
                    Subject = fileData.MailMerge.Subject,
                    To = fileData.MailMerge.To,
                    Message = message,
                    AttachTitle = fileData.MailMerge.Title,
                    Attach = attach
                })
            {
                var response = await mailMergeTaskRunner.RunAsync(mailMergeTask, clientFactory);
                logger.InformationDocServiceMailMerge(fileData.MailMerge.RecordIndex + 1, fileData.MailMerge.RecordCount, response);
            }
            saveMessage = null;
        }
        catch (Exception ex)
        {
            logger.ErrorDocServiceMailMerge(fileData.MailMerge == null ? "" : " " + fileData.MailMerge.RecordIndex + "/" + fileData.MailMerge.RecordCount,
                              userId, fileData.Url, ex);
            saveMessage = ex.Message;
        }

        if (fileData.MailMerge != null &&
            fileData.MailMerge.RecordIndex == fileData.MailMerge.RecordCount - 1)
        {
            var errorCount = fileData.MailMerge.RecordErrorCount;
            if (!string.IsNullOrEmpty(saveMessage))
            {
                errorCount++;
            }

            await notifyClient.SendMailMergeEndAsync(userId, fileData.MailMerge.RecordCount, errorCount);
        }

        return new TrackResponse { Message = saveMessage };
    }

    private async Task StoringFileAfterErrorAsync<T>(T fileId, string userId, string downloadUri, string downloadType)
    {
        if (string.IsNullOrEmpty(downloadUri))
        {
            return;
        }

        try
        {
            if (string.IsNullOrEmpty(downloadType))
            {
                downloadType = FileUtility.GetFileExtension(downloadUri).Trim('.');
            }

            var fileName = Global.ReplaceInvalidCharsAndTruncate(fileId + "." + downloadType);

            var path = $@"save_crash\{DateTime.UtcNow:yyyy_MM_dd}\{userId}_{fileName}";

            var store = await globalStore.GetStoreAsync();
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(downloadUri)
            };

            var httpClient = clientFactory.CreateClient(nameof(ASC.Files.Core.Helpers.DocumentService));
            using (var response = await httpClient.SendAsync(request))
            await using (var stream = await response.Content.ReadAsStreamAsync())
            await using (var fileStream = new ResponseStream(stream, stream.Length))
            {
                await store.SaveAsync(FileConstant.StorageDomainTmp, path, fileStream);
            }
            logger.DebugDocServiceStoring(path);
        }
        catch (Exception ex)
        {
            logger.ErrorDocServiceSaveFileToTempStore(ex);
        }
    }

    private async Task SaveHistoryAsync<T>(File<T> file, string changes, string differenceUrl)
    {
        if (file == null)
        {
            return;
        }

        if (file.ProviderEntry)
        {
            return;
        }

        if (string.IsNullOrEmpty(changes) || string.IsNullOrEmpty(differenceUrl))
        {
            return;
        }

        try
        {
            var fileDao = daoFactory.GetFileDao<T>();
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(differenceUrl)
            };

            var httpClient = clientFactory.CreateClient(nameof(ASC.Files.Core.Helpers.DocumentService));
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            await using var differenceStream = await ResponseStream.FromMessageAsync(response);
            await fileDao.SaveEditHistoryAsync(file, changes, differenceStream);
        }
        catch (Exception ex)
        {
            logger.ErrorDocServiceSavehistory(ex);
        }
    }
}
