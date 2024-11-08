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

using System.Net;
using System.Text.Json.Serialization;

using ASC.Common.Utils;
using ASC.Data.Storage;
using ASC.Files.Core.Log;
using ASC.Files.ThumbnailBuilder;
using ASC.Security.Cryptography;
using ASC.Web.Files.Services.FFmpegService;

using JWT.Exceptions;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ASC.Files.Api;

[AllowAnonymous]
[ConstraintRoute("int")]
public class FileHandlerControllerInternal(
    IHttpContextAccessor httpContextAccessor,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    FilesLinkUtility filesLinkUtility,
    AuthContext authContext,
    SecurityContext securityContext,
    GlobalStore globalStore,
    ILogger<FileHandlerService> logger,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    FileMarker fileMarker,
    FileUtility fileUtility,
    Global global,
    EmailValidationKeyProvider emailValidationKeyProvider,
    PathProvider pathProvider,
    FilesMessageService filesMessageService,
    FileConverter fileConverter,
    FFmpegService fFmpegService,
    ThumbnailSettings thumbnailSettings,
    ExternalLinkHelper externalLinkHelper,
    ExternalShare externalShare,
    EntryManager entryManager,
    FileHandlerControllerHelper handlerControllerHelper,
    DocumentServiceTrackerHelper documentServiceTrackerHelper)
    : FileHandlerController<int>(httpContextAccessor, folderDtoHelper, fileDtoHelper, filesLinkUtility, authContext, securityContext, globalStore, logger, daoFactory, fileSecurity, fileMarker, fileUtility, global, emailValidationKeyProvider,
        pathProvider, filesMessageService, fileConverter, fFmpegService, externalLinkHelper, externalShare, entryManager, handlerControllerHelper, documentServiceTrackerHelper)
{
    private readonly GlobalStore _globalStore = globalStore;

    [HttpGet("thumb/{fileId}")]
    public async Task ThumbnailFile(int fileId, int version, string size, bool view)
    {
        await ThumbnailOrPreviewFile(fileId, version, size, view);
    }
    
    [HttpGet("preview/{fileId}")]
    public async Task PreviewFile(int fileId, int version, string size, bool view)
    {
        await ThumbnailOrPreviewFile(fileId, version, size, view, true);
    }
    
    private async Task ThumbnailOrPreviewFile(int id, int version, string size, bool view, bool force = false)
    {                
        var context = _httpContextAccessor.HttpContext;
        
        IFileDao<int> fileDao = null;
        File<int> file = null;
        try
        {
            var defaultSize = thumbnailSettings.Sizes.FirstOrDefault();

            if (defaultSize == null)
            {
                throw new ItemNotFoundException();
            }

            var width = defaultSize.Width;
            var height = defaultSize.Height;

            var sizes = size.Split('x');
            if (sizes.Length == 2)
            {
                _ = int.TryParse(sizes[0], out width);
                _ = int.TryParse(sizes[1], out height);
            }

            fileDao = _daoFactory.GetFileDao<int>();
            file = version > 0
               ? await fileDao.GetFileAsync(id, version)
               : await fileDao.GetFileAsync(id);

            if (file == null)
            {
                throw new ItemNotFoundException();
            }

            if (!await CanDownloadAsync(file))
            {
                throw new SecurityException();
            }

            if (!string.IsNullOrEmpty(file.Error))
            {
                throw new Exception(file.Error);
            }

            if (file.ThumbnailStatus != Thumbnail.Created && !force)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }

            
            if (view)
            {
                var t1 = TryMarkAsRecentByLink(file);
                var t2 = _fileMarker.RemoveMarkAsNewAsync(file).AsTask();
                
                await Task.WhenAll(t1, t2);
            }

            if (force)
            {
                context.Response.ContentType = MimeMapping.GetMimeMapping(".jpeg");
                context.Response.Headers.Append("Content-Disposition", ContentDispositionUtil.GetHeaderValue(".jpeg", true));

                await using var stream = await fileDao.GetFileStreamAsync(file);
                var processedImage = await Image.LoadAsync(stream);

                processedImage.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Crop
                }));

                // save as jpeg more fast, then webp
                await processedImage.SaveAsJpegAsync(context.Response.Body);
            }
            else
            {
                context.Response.ContentType = MimeMapping.GetMimeMapping("." + _global.ThumbnailExtension);
                context.Response.Headers.Append("Content-Disposition", ContentDispositionUtil.GetHeaderValue("." + _global.ThumbnailExtension));

                var thumbnailFilePath = fileDao.GetUniqThumbnailPath(file, width, height);

                await using var stream = await (await _globalStore.GetStoreAsync()).GetReadStreamAsync(thumbnailFilePath);
                context.Response.Headers.Append("Content-Length", stream.Length.ToString(CultureInfo.InvariantCulture));
                await stream.CopyToAsync(context.Response.Body);
            }

        }
        catch (FileNotFoundException ex)
        {
            await fileDao.SetThumbnailStatusAsync(file, Thumbnail.Waiting);
            _logger.ErrorForUrl(context.Request.Url(), ex);
            throw new ItemNotFoundException();
        }
        
        try
        {
            await context.Response.Body.FlushAsync();
            await context.Response.CompleteAsync();
        }
        catch (HttpException he)
        {
            logger.ErrorThumbnail(he);
        }
    }
}

[AllowAnonymous]
public class FileHandlerControllerThirdparty(
    IHttpContextAccessor httpContextAccessor,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    FilesLinkUtility filesLinkUtility,
    AuthContext authContext,
    SecurityContext securityContext,
    GlobalStore globalStore,
    ILogger<FileHandlerService> logger,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    FileMarker fileMarker,
    FileUtility fileUtility,
    Global global,
    EmailValidationKeyProvider emailValidationKeyProvider,
    PathProvider pathProvider,
    FilesMessageService filesMessageService,
    FileConverter fileConverter,
    FFmpegService fFmpegService,
    ThumbnailSettings thumbnailSettings,
    ExternalLinkHelper externalLinkHelper,
    ExternalShare externalShare,
    EntryManager entryManager,
    FileHandlerControllerHelper handlerControllerHelper,
    DocumentServiceTrackerHelper documentServiceTrackerHelper)
    : FileHandlerController<string>(httpContextAccessor, folderDtoHelper, fileDtoHelper, filesLinkUtility, authContext, securityContext, globalStore, logger, daoFactory, fileSecurity, fileMarker, fileUtility, global, emailValidationKeyProvider,
         pathProvider, filesMessageService, fileConverter, fFmpegService,  externalLinkHelper, externalShare, entryManager, handlerControllerHelper, documentServiceTrackerHelper)
{
    [HttpGet("thumb/{fileId}")]
    public async Task ThumbnailFile(string fileId, string size, bool view)
    {
        await ThumbnailFileFromThirdParty(fileId, size, view);
    }
    
    [HttpGet("preview/{fileId}")]
    public async Task PreviewFile(string fileId, string size, bool view)
    {
        await ThumbnailFileFromThirdParty(fileId, size, view);
    }
    
    public async Task ThumbnailFileFromThirdParty(string id, string size, bool view)
    {            
        var context = _httpContextAccessor.HttpContext;
        
        try
        {
            var defaultSize = thumbnailSettings.Sizes.FirstOrDefault();
            if (defaultSize == null)
            {
                throw new ItemNotFoundException();
            }
            
            var fileDao = _daoFactory.GetFileDao<string>();
            var file = await fileDao.GetFileAsync(id);
            
            if (file == null)
            {
                throw new ItemNotFoundException();
            }
            
            if (!await CanDownloadAsync(file))
            {                
                throw new SecurityException();
            }

            var width = defaultSize.Width;
            var height = defaultSize.Height;

            var sizes = size.Split('x');
            if (sizes.Length == 2)
            {
                _ = int.TryParse(sizes[0], out width);
                _ = int.TryParse(sizes[1], out height);
            }

            context.Response.Headers.Append("Content-Disposition", ContentDispositionUtil.GetHeaderValue("." + _global.ThumbnailExtension));
            context.Response.ContentType = MimeMapping.GetMimeMapping("." + _global.ThumbnailExtension);
            
            if (view)
            {
                await _fileMarker.RemoveMarkAsNewAsync(file);
            }

            await using var stream = await fileDao.GetThumbnailAsync(id, width, height);
            await stream.CopyToAsync(context.Response.Body);
        }
        catch (FileNotFoundException ex)
        {
            _logger.ErrorForUrl(context.Request.Url(), ex);
            throw new ItemNotFoundException();
        }
        
        try
        {
            await context.Response.Body.FlushAsync();
            await context.Response.CompleteAsync();
        }
        catch (HttpException he)
        {
            logger.ErrorThumbnail(he);
        }
    }
}

[DefaultRoute("filehandler")]
public abstract class FileHandlerController<T>(
    IHttpContextAccessor httpContextAccessor,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    FilesLinkUtility filesLinkUtility,
    AuthContext authContext,
    SecurityContext securityContext,
    GlobalStore globalStore,
    ILogger<FileHandlerService> logger,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    FileMarker fileMarker,
    FileUtility fileUtility,
    Global global,
    EmailValidationKeyProvider emailValidationKeyProvider,
    PathProvider pathProvider,
    FilesMessageService filesMessageService,
    FileConverter fileConverter,
    FFmpegService fFmpegService,
    ExternalLinkHelper externalLinkHelper,
    ExternalShare externalShare,
    EntryManager entryManager,
    FileHandlerControllerHelper handlerControllerHelper,
    DocumentServiceTrackerHelper documentServiceTrackerHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    protected readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    protected readonly ILogger<FileHandlerService> _logger = logger;
    protected readonly IDaoFactory _daoFactory = daoFactory;
    protected readonly FileMarker _fileMarker = fileMarker;
    protected readonly Global _global = global;
    
    [HttpGet("download/{fileId}")]
    public async Task DownloadFile(T fileId, int version, string outputtype, bool convpreview)
    {
        await DownloadOrViewFile(fileId, version, outputtype, convpreview);
    }
    
    [HttpGet("view/{fileId}")]
    public async Task ViewFile(T fileId, int version, string outputtype, bool convpreview)
    {
        await DownloadOrViewFile(fileId, version, outputtype, convpreview, true);
    }
    
    [HttpGet("stream/{fileId}")]
    public async Task StreamFile(T fileId, int version, string stream_auth)
    {
        var context = _httpContextAccessor.HttpContext;

        var fileDao = _daoFactory.GetFileDao<T>();
        
        await fileDao.InvalidateCacheAsync(fileId);

        var (linkRight, file) = await CheckLinkAsync(fileId, version, fileDao);

        if (linkRight == FileShare.Restrict && !securityContext.IsAuthenticated)
        {
            var auth = stream_auth;
            var validateResult = await emailValidationKeyProvider.ValidateEmailKeyAsync(fileId.ToString() + version, auth ?? "", _global.StreamUrlExpire);
            if (validateResult != EmailValidationKeyProvider.ValidationResult.Ok)
            {
                var exc = new HttpException((int)HttpStatusCode.Forbidden, FilesCommonResource.ErrorMessage_SecurityException);

                _logger.Error(FilesLinkUtility.AuthKey, validateResult, context.Request.Url(), exc);

                throw new SecurityException();
            }

            if (!string.IsNullOrEmpty(fileUtility.SignatureSecret))
            {
                try
                {
                    var header = context.Request.Headers[fileUtility.SignatureHeader].FirstOrDefault();
                    if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer "))
                    {
                        var requestHeaderTrace = new StringBuilder();

                        foreach (var requestHeader in context.Request.Headers)
                        {
                            requestHeaderTrace.Append($"{requestHeader.Key}={requestHeader.Value}" + Environment.NewLine);
                        }

                        var exceptionMessage = $"Invalid signature header {fileUtility.SignatureHeader} with value {header}." +
                                               $"Trace headers: {requestHeaderTrace}  ";


                        throw new Exception(exceptionMessage);
                    }

                    header = header["Bearer ".Length..];

                    var stringPayload = JsonWebToken.Decode(header, fileUtility.SignatureSecret);

                    _logger.DebugDocServiceStreamFilePayload(stringPayload);
                }
                catch (Exception ex)
                {
                    _logger.ErrorDownloadStreamHeader(context.Request.Url(), ex);
                    throw new SecurityException();
                }
            }
        }

        if (file == null || version > 0 && file.Version != version)
        {
            file = version > 0
                       ? await fileDao.GetFileAsync(fileId, version)
                       : await fileDao.GetFileAsync(fileId);
        }

        if (file == null)
        {
            throw new ItemNotFoundException();
        }

        if (linkRight == FileShare.Restrict && securityContext.IsAuthenticated && !await fileSecurity.CanDownloadAsync(file))
        {
            throw new SecurityException();
        }

        if (!string.IsNullOrEmpty(file.Error))
        {
            throw new ArgumentException(file.Error);
        }

        var fullLength = await fileDao.GetFileSizeAsync(file);
        
        long offset = 0;
        var length = handlerControllerHelper.ProcessRangeHeader(fullLength, ref offset);

        await using var stream = await fileDao.GetFileStreamAsync(file, offset, length);
        await handlerControllerHelper.SendStreamByChunksAsync(length, offset, fullLength, file.Title, stream);
        
        try
        {
            await context.Response.Body.FlushAsync();
            await context.Response.CompleteAsync();
        }
        catch (HttpException he)
        {
            logger.ErrorStreamFile(he);
        }
    }

    [HttpGet("create/{folderid}")]
    public async Task CreateFile(T folderId, string response, string fileUri, string title, string docType, bool openfolder)
    {
        await handlerControllerHelper.CreateFile(folderId, response, fileUri, title, docType, openfolder);
    }

    [HttpGet("createform/{folderid}")]
    public async Task CreateForm(T folderId, string response, string fileUri, string title, string docType, bool openfolder)
    {
        await handlerControllerHelper.CreateFile(folderId, response, fileUri, title, docType, openfolder, true);
    }
    
    [HttpGet("redirect")]
    public async Task RedirectAsync(T folderId, T fileId)
    {
        var urlRedirect = string.Empty;
        if (folderId != null)
        {
            urlRedirect = await pathProvider.GetFolderUrlByIdAsync(folderId);
        }

        if (fileId != null)
        {
            var fileDao = _daoFactory.GetFileDao<T>();
            var file = await fileDao.GetFileAsync(fileId);
            if (file == null)
            {
                throw new ItemNotFoundException();
            }

            urlRedirect = filesLinkUtility.GetFileWebPreviewUrl(fileUtility, file.Title, file.Id);
        }

        if (string.IsNullOrEmpty(urlRedirect))
        {
            throw new ArgumentException(); 
        }

        _httpContextAccessor.HttpContext?.Response.Redirect(urlRedirect);
    }

    [HttpGet("diff/{fileid}")]
    public async Task DifferenceFile(T fileid, int version, string stream_auth)
    {
        var context = _httpContextAccessor.HttpContext;
        
        var fileDao = _daoFactory.GetFileDao<T>();
        
        var (linkRight, file) = await CheckLinkAsync(fileid, version, fileDao);
        if (linkRight == FileShare.Restrict && !securityContext.IsAuthenticated)
        {
            var auth = stream_auth;
            var validateResult = await emailValidationKeyProvider.ValidateEmailKeyAsync(fileid.ToString() + version, auth ?? "", _global.StreamUrlExpire);
            if (validateResult != EmailValidationKeyProvider.ValidationResult.Ok)
            {
                var exc = new HttpException((int)HttpStatusCode.Forbidden, FilesCommonResource.ErrorMessage_SecurityException);

                _logger.Error(FilesLinkUtility.AuthKey, validateResult, context.Request.Url(), exc);
                
                throw new SecurityException();
            }
        }

        await fileDao.InvalidateCacheAsync(fileid);

        if (file == null
            || version > 0 && file.Version != version)
        {
            file = version > 0
                       ? await fileDao.GetFileAsync(fileid, version)
                       : await fileDao.GetFileAsync(fileid);
        }

        if (file == null)
        {
            throw new ItemNotFoundException();
        }

        if (linkRight == FileShare.Restrict && securityContext.IsAuthenticated && !await fileSecurity.CanReadAsync(file))
        {
            throw new SecurityException();
        }

        if (!string.IsNullOrEmpty(file.Error))
        {
            throw new Exception(file.Error);
        }

        context.Response.Headers.Append("Content-Disposition", ContentDispositionUtil.GetHeaderValue(".zip"));
        context.Response.ContentType = MimeMapping.GetMimeMapping(".zip");

        await using var stream = await fileDao.GetDifferenceStreamAsync(file);
        context.Response.Headers.Append("Content-Length", stream.Length.ToString(CultureInfo.InvariantCulture));
        await stream.CopyToAsync(context.Response.Body);
    }

    [HttpPost("track/{fileid}")]
    public async Task TrackFile(T fileId, string stream_auth)
    {
        var context = _httpContextAccessor.HttpContext;
        var auth = stream_auth;
        _logger.DebugDocServiceTrackFileid(fileId.ToString());

        var callbackSpan = TimeSpan.FromDays(128);
        var validateResult = await emailValidationKeyProvider.ValidateEmailKeyAsync(fileId.ToString(), auth ?? "", callbackSpan);
        if (validateResult != EmailValidationKeyProvider.ValidationResult.Ok)
        {
            _logger.ErrorDocServiceTrackAuth(validateResult, FilesLinkUtility.AuthKey, auth);
            throw new SecurityException();
        }
        
        DocumentServiceTracker.TrackerData fileData;
        try
        {
            var receiveStream = context.Request.Body;
            using var readStream = new StreamReader(receiveStream);
            var body = await readStream.ReadToEndAsync();

            _logger.DebugDocServiceTrackBody(body);
            if (string.IsNullOrEmpty(body))
            {
                throw new ArgumentException("DocService request body is incorrect");
            }

            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true
            };
            fileData = JsonSerializer.Deserialize<DocumentServiceTracker.TrackerData>(body, options);
        }
        catch (JsonException e)
        {
            _logger.ErrorDocServiceTrackReadBody(e);
            throw new ArgumentException("DocService request is incorrect");
        }
        catch (Exception e)
        {
            _logger.ErrorDocServiceTrackReadBody(e);
            throw new ArgumentException(e.Message);
        }

        var lastfileDataAction = fileData.Actions?.LastOrDefault();
        var fillingSessionId = lastfileDataAction != null
            ? (lastfileDataAction.UserId.StartsWith(FileConstant.AnonFillingSession)
                ? lastfileDataAction.UserId
                : $"{fileId}_{lastfileDataAction.UserId}")
            : string.Empty;

        if (!string.IsNullOrEmpty(fileUtility.SignatureSecret))
        {
            if (!string.IsNullOrEmpty(fileData.Token))
            {
                try
                {
                    var dataString = JsonWebToken.Decode(fileData.Token, fileUtility.SignatureSecret);

                    var data = JObject.Parse(dataString);
                    if (data == null)
                    {
                        throw new ArgumentException("DocService request token is incorrect");
                    }
                    fileData = data.ToObject<DocumentServiceTracker.TrackerData>();
                }
                catch (SignatureVerificationException ex)
                {
                    _logger.ErrorDocServiceTrackHeader(ex);
                    throw new SecurityException(ex.Message);
                }
            }
            else
            {
                //todo: remove old scheme
                var header = context.Request.Headers[fileUtility.SignatureHeader].FirstOrDefault();
                if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer "))
                {
                    _logger.ErrorDocServiceTrackHeaderIsNull();
                    throw new HttpException((int)HttpStatusCode.Forbidden, FilesCommonResource.ErrorMessage_SecurityException);
                }
                header = header["Bearer ".Length..];

                try
                {
                    var stringPayload = JsonWebToken.Decode(header, fileUtility.SignatureSecret);

                    _logger.DebugDocServiceTrackPayload(stringPayload);
                    var jsonPayload = JObject.Parse(stringPayload);
                    var data = jsonPayload["payload"];
                    if (data == null)
                    {
                        throw new ArgumentException("DocService request header is incorrect");
                    }
                    fileData = data.ToObject<DocumentServiceTracker.TrackerData>();
                }
                catch (SignatureVerificationException ex)
                {
                    _logger.ErrorDocServiceTrackHeader(ex);
                    throw new SecurityException(ex.Message);
                }
            }
        }

        DocumentServiceTracker.TrackResponse result;
        try
        {
            result = await documentServiceTrackerHelper.ProcessDataAsync(fileId, fileData, fillingSessionId);
        }
        catch (Exception e)
        {
            _logger.ErrorDocServiceTrack(e);                    
            throw new SecurityException(e.Message);
        }
        result ??= new DocumentServiceTracker.TrackResponse();

        await context.Response.WriteAsync(DocumentServiceTracker.TrackResponse.Serialize(result));
        await context.Response.Body.FlushAsync();
        await context.Response.CompleteAsync();
    }
        
    private async Task DownloadOrViewFile(T fileId, int version, string outputtype, bool convpreview, bool forView = false)
    {
        var context = _httpContextAccessor.HttpContext;
        var fileDao = _daoFactory.GetFileDao<T>();
        
        await fileDao.InvalidateCacheAsync(fileId);

        var file = version > 0 
            ? await fileDao.GetFileAsync(fileId, version) 
            : await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new ItemNotFoundException();
        }

        if (!await CanDownloadAsync(file))
        {                
            throw new SecurityException();
        }

        if (!string.IsNullOrEmpty(file.Error))
        {
            throw new Exception(file.Error);
        }

        if (!await fileDao.IsExistOnStorageAsync(file))
        {
            _logger.ErrorDownloadFile2(file.Id.ToString());
            throw new ItemNotFoundException();
        }
        
        var t1 = TryMarkAsRecentByLink(file);
        var t2 = _fileMarker.RemoveMarkAsNewAsync(file).AsTask();
        
        await Task.WhenAll(t1, t2);

        var range = (context.Request.Headers["Range"].FirstOrDefault() ?? "").Split('=', '-');
        var isNeedSendAction = range.Length < 2 || Convert.ToInt64(range[1]) == 0;

        if (isNeedSendAction)
        {
            if (forView)
            {
                await filesMessageService.SendAsync(MessageAction.FileReaded, file, file.Title);
            }
            else
            {
                if (version == 0)
                {
                    await filesMessageService.SendAsync(MessageAction.FileDownloaded, file, file.Title);
                }
                else
                {
                    await filesMessageService.SendAsync(MessageAction.FileRevisionDownloaded, file, file.Title, file.Version.ToString());
                }
            }
        }

        if (string.Equals(context.Request.Headers["If-None-Match"], GetEtag(file)))
        {
            //Its cached. Reply 304
            context.Response.StatusCode = (int)HttpStatusCode.NotModified;
            //context.Response.Cache.SetETag(GetEtag(file));
        }
        else
        {
            //context.Response.CacheControl = "public";
            //context.Response.Cache.SetETag(GetEtag(file));
            //context.Response.Cache.SetCacheability(HttpCacheability.Public);

            Stream fileStream = null;
            try
            {
                var title = file.Title;

                var ext = FileUtility.GetFileExtension(file.Title);

                var outType = outputtype?.Trim();
                var extsConvertible = await fileUtility.GetExtsConvertibleAsync();
                var convertible = extsConvertible.TryGetValue(ext, out var value);

                if (!string.IsNullOrEmpty(outType) && convertible && value.Contains(outType))
                {
                    ext = outType;
                }

                if (convertible)
                {
                    var folderDao = _daoFactory.GetFolderDao<T>();
                    if (await DocSpaceHelper.IsWatermarkEnabled(file, folderDao) && value.Contains(FileUtility.WatermarkedDocumentExt))
                    {
                        ext = FileUtility.WatermarkedDocumentExt;
                    }
                }

                long offset = 0;
                long length;
                long fullLength;

                if (!file.ProviderEntry
                    && convpreview
                    && fFmpegService.IsConvertable(ext))
                {
                    const string mp4Name = "content.mp4";
                    var mp4Path = fileDao.GetUniqFilePath(file, mp4Name);
                    var store = await globalStore.GetStoreAsync();
                    if (!await store.IsFileAsync(mp4Path))
                    {
                        fileStream = await fileDao.GetFileStreamAsync(file);

                        _logger.InformationConvertingToMp4(file.Title, file.Id.ToString());
                        var stream = await fFmpegService.ConvertAsync(fileStream, ext);
                        await store.SaveAsync(string.Empty, mp4Path, stream, mp4Name);
                    }

                    fullLength = await store.GetFileSizeAsync(string.Empty, mp4Path);

                    length = handlerControllerHelper.ProcessRangeHeader(fullLength, ref offset);
                    fileStream = await store.GetReadStreamAsync(string.Empty, mp4Path, offset);

                    title = FileUtility.ReplaceFileExtension(title, ".mp4");
                }
                else
                {
                    if (!await fileConverter.EnableConvertAsync(file, ext))
                    {
                        if (await fileDao.IsSupportedPreSignedUriAsync(file))
                        {
                            var url = (await fileDao.GetPreSignedUriAsync(file, TimeSpan.FromHours(1), externalShare.GetKey()));

                            context.Response.Redirect(url, false);

                            return;
                        }

                        fullLength = await fileDao.GetFileSizeAsync(file);

                        length = handlerControllerHelper.ProcessRangeHeader(fullLength, ref offset);
                        fileStream = await fileDao.GetFileStreamAsync(file, offset, length);
                    }
                    else
                    {
                        title = FileUtility.ReplaceFileExtension(title, ext);
                        fileStream = await fileConverter.ExecAsync(file, ext);

                        length = fileStream.Length;
                        fullLength = length;
                    }
                }

                await handlerControllerHelper.SendStreamByChunksAsync(length, offset, fullLength, title, fileStream);

            }
            catch (ThreadAbortException tae)
            {
                _logger.ErrorDownloadFile(tae);
            }
            catch (HttpException e)
            {
                _logger.ErrorDownloadFile(e);
                throw new HttpException((int)HttpStatusCode.BadRequest, e.Message);
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                    await fileStream.DisposeAsync();
                }
            }
            
            try
            {
                await context.Response.Body.FlushAsync();
                await context.Response.CompleteAsync();
            }
            catch (HttpException ex)
            {
                logger.ErrorDownloadFile(ex);
            }
        }
    }
    
    protected async Task<bool> CanDownloadAsync(File<T> file)
    {
        if (await fileSecurity.CanDownloadAsync(file))
        {
            return true;
        }

        return (fileUtility.CanImageView(file.Title) || fileUtility.CanMediaView(file.Title)) &&
               file.ShareRecord is { IsLink: true, Share: not FileShare.Restrict } or { IsLink: false, Share: FileShare.Read, SubjectType: SubjectType.User  };
    }
    
    protected async Task TryMarkAsRecentByLink(File<T> file)
    {
        if (authContext.IsAuthenticated && file.RootFolderType == FolderType.USER && !file.ProviderEntry && file.CreateBy != authContext.CurrentAccount.ID
            && (fileUtility.CanImageView(file.Title) || fileUtility.CanMediaView(file.Title) || !fileUtility.CanWebView(file.Title)))
        {
            var linkId = await externalShare.GetLinkIdAsync();
            if (linkId != Guid.Empty)
            {
                await entryManager.MarkFileAsRecentByLink(file, linkId);
            }
        }
    }
    
    private static string GetEtag(File<T> file)
    {
        return file.Id + ":" + file.Version + ":" + file.Title.GetHashCode() + ":" + file.ContentLength;
    }
    
    private async Task<(FileShare, File<T>)> CheckLinkAsync(T id, int version, IFileDao<T> fileDao)
    {
        var linkRight = FileShare.Restrict;

        var key = externalShare.GetKey();
        if (string.IsNullOrEmpty(key))
        {
            return (linkRight, null);
        }

        var result = await externalLinkHelper.ValidateAsync(key);
        if (result.Access == FileShare.Restrict)
        {
            return (linkRight, null);
        }

        var file = version > 0
            ? await fileDao.GetFileAsync(id, version)
            : await fileDao.GetFileAsync(id);

        if (file != null && await fileSecurity.CanDownloadAsync(file))
        {
            linkRight = result.Access;
        }

        return (linkRight, file);
    }
}

[AllowAnonymous]
[DefaultRoute("filehandler")]
public abstract class FileHandlerControllerCommon(
    IHttpContextAccessor httpContextAccessor,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    AuthContext authContext,
    SecurityContext securityContext,
    GlobalStore globalStore,
    ILogger<FileHandlerService> logger,
    FileUtility fileUtility,
    Global global,
    EmailValidationKeyProvider emailValidationKeyProvider,
    GlobalFolderHelper globalFolderHelper,
    CompressToArchive compressToArchive,
    InstanceCrypto instanceCrypto,
    ExternalShare externalShare,
    FileHandlerControllerHelper handlerControllerHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    [AllowAnonymous]
    [HttpGet("bulk")]
    public async Task BulkFile(string filename, string session, string ext)
    {
        if (!securityContext.IsAuthenticated && string.IsNullOrEmpty(session))
        {
            throw new SecurityException();
        }

        if (String.IsNullOrEmpty(filename))
        {
            ext = await compressToArchive.GetExt(ext);
            filename = FileConstant.DownloadTitle + ext;
        }
        else
        {
            filename = await instanceCrypto.DecryptAsync(Uri.UnescapeDataString(filename));
        }

        string path;

        if (!string.IsNullOrEmpty(session))
        {
            var sessionData = await externalShare.ParseDownloadSessionKeyAsync(session);
            var sessionId = await externalShare.GetSessionIdAsync();

            if (sessionData != null && sessionId != Guid.Empty && sessionData.Id == sessionId &&
                (await externalShare.ValidateAsync(sessionData.LinkId, securityContext.IsAuthenticated)) == Status.Ok)
            {
                path = $@"{sessionData.LinkId}\{sessionData.Id}\{filename}";
            }
            else
            {
                throw new SecurityException();
            }
        }
        else
        {
            path = $@"{securityContext.CurrentAccount.ID}\{filename}";
        }

        var store = await globalStore.GetStoreAsync();

        if (!await store.IsFileAsync(FileConstant.StorageDomainTmp, path))
        {
            logger.ErrorBulkDownloadFile(authContext.CurrentAccount.ID);
            throw new ItemNotFoundException();
        }

        var context = httpContextAccessor.HttpContext;
        if (store.IsSupportedPreSignedUri)
        {
            var headers = securityContext.IsAuthenticated ? null : new[] { await SecureHelper.GenerateSecureKeyHeaderAsync(path, emailValidationKeyProvider) };

            var tmp = await store.GetPreSignedUriAsync(FileConstant.StorageDomainTmp, path, TimeSpan.FromHours(1), headers);
            var url = tmp.ToString();
            context.Response.Redirect(HttpUtility.UrlPathEncode(url));
            return;
        }

        try
        { 
            var fullLength = await store.GetFileSizeAsync(FileConstant.StorageDomainTmp, path);
            
            long offset = 0;
            var length = handlerControllerHelper.ProcessRangeHeader(fullLength, ref offset);
            await using var stream = await store.GetReadStreamAsync(FileConstant.StorageDomainTmp, path, offset, length);
            
            await handlerControllerHelper.SendStreamByChunksAsync(length, offset, fullLength, filename, stream);
            await context.Response.Body.FlushAsync();
            await context.Response.CompleteAsync();
        }
        catch (Exception e)
        {
            logger.ErrorBulkDownloadFileFailed(securityContext.CurrentAccount.ID, e);
            throw new ArgumentException();
        }
    }
    
    [HttpGet("empty")]
    public async Task EmptyFile(string title)
    {
        var context = httpContextAccessor.HttpContext;
        
        var fileName = title;
        if (!string.IsNullOrEmpty(fileUtility.SignatureSecret))
        {
            try
            {
                var header = context.Request.Headers[fileUtility.SignatureHeader].FirstOrDefault();
                if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer "))
                {
                    throw new Exception("Invalid header " + header);
                }

                header = header["Bearer ".Length..];

                var stringPayload = JsonWebToken.Decode(header, fileUtility.SignatureSecret);

                logger.DebugDocServiceStreamFilePayload(stringPayload);
            }
            catch (Exception ex)
            {
                throw new SecurityException(ex.Message);
            }
        }

        var toExtension = FileUtility.GetFileExtension(fileName);
        var fileExtension = fileUtility.GetInternalExtension(toExtension);

        var storeTemplate = await globalStore.GetStoreTemplateAsync();
        var path = await globalStore.GetNewDocTemplatePath(storeTemplate, fileExtension);

        if (!await storeTemplate.IsFileAsync("", path))
        {
            throw new ItemNotFoundException();
        }

        fileName = Path.GetFileName(path);

        context.Response.Headers.Append("Content-Disposition", ContentDispositionUtil.GetHeaderValue(fileName));
        context.Response.ContentType = MimeMapping.GetMimeMapping(fileName);

        await using var stream = await storeTemplate.GetReadStreamAsync("", path);
        context.Response.Headers.Append("Content-Length",
            stream.CanSeek
            ? stream.Length.ToString(CultureInfo.InvariantCulture)
            : (await storeTemplate.GetFileSizeAsync("", path)).ToString(CultureInfo.InvariantCulture));
        await stream.CopyToAsync(context.Response.Body);
    }
    
    [HttpGet("temp")]
    public async Task TempFile(string title, string stream_auth)
    {
        var fileName = title;
        var auth = stream_auth;
        var context = httpContextAccessor.HttpContext;
        
        var validateResult = await emailValidationKeyProvider.ValidateEmailKeyAsync(fileName, auth ?? "", global.StreamUrlExpire);
        if (validateResult != EmailValidationKeyProvider.ValidationResult.Ok)
        {
            var exc = new HttpException((int)HttpStatusCode.Forbidden, FilesCommonResource.ErrorMessage_SecurityException);

            logger.Error(FilesLinkUtility.AuthKey, validateResult, context.Request.Url(), exc);

            throw new SecurityException();
        }

        context.Response.Clear();
        context.Response.ContentType = MimeMapping.GetMimeMapping(fileName);
        context.Response.Headers.Append("Content-Disposition", ContentDispositionUtil.GetHeaderValue(fileName));

        var store = await globalStore.GetStoreAsync();

        var path = CrossPlatform.PathCombine("temp_stream", fileName);

        if (!await store.IsFileAsync(FileConstant.StorageDomainTmp, path))
        {
            throw new ItemNotFoundException();
        }

        await using (var readStream = await store.GetReadStreamAsync(FileConstant.StorageDomainTmp, path))
        {
            context.Response.Headers.Append("Content-Length", readStream.Length.ToString(CultureInfo.InvariantCulture));
            await readStream.CopyToAsync(context.Response.Body);
        }

        await store.DeleteAsync(FileConstant.StorageDomainTmp, path);
    }

    [HttpGet("create")]
    public async Task CreateFile(string response, string fileUri, string title, string docType, bool openfolder)
    {
        await handlerControllerHelper.CreateFile(await globalFolderHelper.FolderMyAsync, response, fileUri, title, docType, openfolder);
    }
}

[Scope]
public class FileHandlerControllerHelper(
    ILogger<FileHandlerService> logger,
    IHttpContextAccessor httpContextAccessor,
    FileDtoHelper fileDtoHelper,
    FilesLinkUtility filesLinkUtility,
    SecurityContext securityContext,
    GlobalStore globalStore,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    FileMarker fileMarker,
    FileUtility fileUtility,
    PathProvider pathProvider,
    UserManager userManager,
    IServiceProvider serviceProvider,
    TempStream tempStream,
    SocketManager socketManager,
    IHttpClientFactory clientFactory)
{
    public long ProcessRangeHeader(long fullLength, ref long offset)
    {
        var context = httpContextAccessor.HttpContext;
        
        var rangeHeader = context.Request.Headers["Range"].FirstOrDefault();
        if (rangeHeader == null)
        {
            return fullLength;
        }

        long endOffset = -1;

        var range = rangeHeader.Split('=', '-');
        offset = Convert.ToInt64(range[1]);
        if (range.Length > 2 && !string.IsNullOrEmpty(range[2]))
        {
            endOffset = Convert.ToInt64(range[2]);
        }
        if (endOffset < 0 || endOffset >= fullLength)
        {
            endOffset = fullLength - 1;
        }

        var length = endOffset - offset + 1;

        if (length <= 0)
        {
            throw new HttpException(HttpStatusCode.RequestedRangeNotSatisfiable);
        }

        logger.InformationStartingFileDownLoad(offset, endOffset);

        return length;
    }
    
    public async Task SendStreamByChunksAsync(long toRead, long offset, long fullLength, string title, Stream fileStream)
    {
        var context = httpContextAccessor.HttpContext;
        var cancellationToken = context.RequestAborted;
        context.Response.Headers.Append("Accept-Ranges", "bytes");
        context.Response.ContentLength = toRead;
        context.Response.Headers.Append("Content-Disposition", ContentDispositionUtil.GetHeaderValue(title));
        context.Response.ContentType = MimeMapping.GetMimeMapping(title);

        if (toRead != fullLength)
        {
            context.Response.Headers.Append("Connection", "Keep-Alive");
            context.Response.StatusCode = (int)HttpStatusCode.PartialContent;
            context.Response.Headers.Append("Content-Range", $" bytes {offset}-{offset + toRead - 1}/{fullLength}");
        }

        var bufferSize = Convert.ToInt32(Math.Min(80 * 1024, toRead));
        var buffer = new byte[bufferSize];
        while (toRead > 0)
        {
            var length = await fileStream.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken);
            await context.Response.Body.WriteAsync(buffer.AsMemory(0, length), cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
            toRead -= length;
        }
    }
    
    public async Task CreateFile<T>(T folderId, string response, string fileUri, string title, string docType, bool openfolder, bool isForm = false)
    {
        var context = httpContextAccessor.HttpContext;
        var responseMessage = response == "message";

        var folderDao = daoFactory.GetFolderDao<T>();
        var folder = await folderDao.GetFolderAsync(folderId);

        if (folder == null)
        {
            throw new ItemNotFoundException();
        }

        var canCreate = await fileSecurity.CanCreateAsync(folder);
        if (!canCreate)
        {
            throw new SecurityException();
        }

        File<T> file;
        var fileTitle = title;
        try
        {
            if (!string.IsNullOrEmpty(fileUri))
            {
                file = await CreateFileFromUriAsync(folder, fileUri, fileTitle);
            }
            else
            {
                file = await CreateFileFromTemplateAsync(folder, fileTitle, docType);
            }

            await socketManager.CreateFileAsync(file);

        }
        catch (Exception ex)
        {
            await WriteError(context, ex, responseMessage);
            return;
        }

        await fileMarker.MarkAsNewAsync(file);

        if (isForm && file.IsForm)
        {
            if (responseMessage)
            {
                await FormWriteOk(context, folder, file);
                return;
            }
        }
        else
        {
            if (responseMessage)
            {
                await WriteOk(context, folder, file);
                return;
            }
        }

        context.Response.Redirect(openfolder ? 
            await pathProvider.GetFolderUrlByIdAsync(file.ParentId) : 
            (filesLinkUtility.GetFileWebEditorUrl(file.Id) + "&message=" + HttpUtility.UrlEncode(string.Format(FilesCommonResource.MessageFileCreated, folder.Title))));
    }

    private async Task WriteError(HttpContext context, Exception ex, bool responseMessage)
    {
        logger.ErrorFileHandler(ex);

        if (responseMessage)
        {
            await context.Response.WriteAsync("error: " + ex.Message);
            return;
        }
        context.Response.Redirect($"{filesLinkUtility.GetFileWebEditorUrl("")}&error={HttpUtility.UrlEncode(ex.Message)}", true);
    }

    private async Task WriteOk<T>(HttpContext context, Folder<T> folder, File<T> file)
    {
        var message = string.Format(FilesCommonResource.MessageFileCreated, folder.Title);
        if (fileUtility.CanWebRestrictedEditing(file.Title))
        {
            message = string.Format(FilesCommonResource.MessageFileCreatedForm, folder.Title);
        }

        await context.Response.WriteAsync("ok: " + message);
    }

    private async Task FormWriteOk<T>(HttpContext context, Folder<T> folder, File<T> file)
    {
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(
                new CreatedFormData<T>
                {
                        Message = string.Format(FilesCommonResource.MessageFileCreatedForm, folder.Title),
                        Form = await fileDtoHelper.GetAsync(file)
                },
                new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })
        );
    }
    
    private async Task<File<T>> CreateFileFromTemplateAsync<T>(Folder<T> folder, string fileTitle, string docType)
    {
        var storeTemplate = await globalStore.GetStoreTemplateAsync();

        var lang = (await userManager.GetUsersAsync(securityContext.CurrentAccount.ID)).GetCulture();

        var fileExt = fileUtility.InternalExtension[FileType.Document];
        if (!string.IsNullOrEmpty(docType))
        {
            var tmpFileType = Configuration<T>.DocType.FirstOrDefault(r => r.Value.Equals(docType, StringComparison.OrdinalIgnoreCase));
            fileUtility.InternalExtension.TryGetValue(tmpFileType.Key, out var tmpFileExt);
            if (!string.IsNullOrEmpty(tmpFileExt))
            {
                fileExt = tmpFileExt;
            }
        }

        var templatePath = await globalStore.GetNewDocTemplatePath(storeTemplate, fileExt, lang);

        if (string.IsNullOrEmpty(fileTitle))
        {
            fileTitle = Path.GetFileName(templatePath);
        }
        else
        {
            fileTitle += fileExt;
        }

        var file = serviceProvider.GetService<File<T>>();
        file.Title = fileTitle;
        file.ParentId = folder.Id;
        file.Comment = FilesCommonResource.CommentCreate;

        var fileDao = daoFactory.GetFileDao<T>();
        var stream = await storeTemplate.GetReadStreamAsync("", templatePath, 0);
        file.ContentLength = stream.CanSeek ? stream.Length : await storeTemplate.GetFileSizeAsync(templatePath);
        return await fileDao.SaveFileAsync(file, stream);
    }

    private async Task<File<T>> CreateFileFromUriAsync<T>(Folder<T> folder, string fileUri, string fileTitle)
    {
        if (string.IsNullOrEmpty(fileTitle))
        {
            fileTitle = Path.GetFileName(HttpUtility.UrlDecode(fileUri));
        }

        var file = serviceProvider.GetService<File<T>>();
        file.Title = fileTitle;
        file.ParentId = folder.Id;
        file.Comment = FilesCommonResource.CommentCreate;

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(fileUri)
        };

        var fileDao = daoFactory.GetFileDao<T>();
        var httpClient = clientFactory.CreateClient();
        using var response = await httpClient.SendAsync(request);
        await using var fileStream = await response.Content.ReadAsStreamAsync();

        if (fileStream.CanSeek)
        {
            file.ContentLength = fileStream.Length;
            return await fileDao.SaveFileAsync(file, fileStream);
        }

        var (buffered, isNew) = await tempStream.TryGetBufferedAsync(fileStream);
        try
        {
            file.ContentLength = buffered.Length;
            return await fileDao.SaveFileAsync(file, buffered);
        }
        finally
        {
            if (isNew)
            {
                await buffered.DisposeAsync();
            }
        }
    }
}

