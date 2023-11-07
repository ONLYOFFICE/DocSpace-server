// (c) Copyright Ascensio System SIA 2010-2022
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

namespace ASC.Web.Files.HttpHandlers;

public class ChunkedUploaderHandler
{
    public ChunkedUploaderHandler(RequestDelegate next)
    {
    }

    public async Task Invoke(HttpContext context, ChunkedUploaderHandlerService chunkedUploaderHandlerService)
    {
        await chunkedUploaderHandlerService.Invoke(context);
    }
}

[Scope]
public class ChunkedUploaderHandlerService
{
    private readonly TenantManager _tenantManager;
    private readonly FileUploader _fileUploader;
    private readonly FilesMessageService _filesMessageService;
    private readonly ChunkedUploadSessionHolder _chunkedUploadSessionHolder;
    private readonly ChunkedUploadSessionHelper _chunkedUploadSessionHelper;
    private readonly SocketManager _socketManager;
    private readonly FileDtoHelper _filesWrapperHelper;
    private readonly ILogger<ChunkedUploaderHandlerService> _logger;
    private readonly AuthContext _authContext;

    public ChunkedUploaderHandlerService(
        ILogger<ChunkedUploaderHandlerService> logger,
        TenantManager tenantManager,
        FileUploader fileUploader,
        FilesMessageService filesMessageService,
        ChunkedUploadSessionHolder chunkedUploadSessionHolder,
        ChunkedUploadSessionHelper chunkedUploadSessionHelper,
        SocketManager socketManager,
        FileDtoHelper filesWrapperHelper,
        AuthContext authContext)
    {
        _tenantManager = tenantManager;
        _fileUploader = fileUploader;
        _filesMessageService = filesMessageService;
        _chunkedUploadSessionHolder = chunkedUploadSessionHolder;
        _chunkedUploadSessionHelper = chunkedUploadSessionHelper;
        _socketManager = socketManager;
        _filesWrapperHelper = filesWrapperHelper;
        _logger = logger;
        _authContext = authContext;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await Invoke<int>(context);
        }
        catch (Exception)
        {
            await Invoke<string>(context);
        }
    }

    private async Task Invoke<T>(HttpContext context)
    {
        try
        {
            if (context.Request.Method == "OPTIONS")
            {
                context.Response.StatusCode = 200;

                return;
            }

            var request = new ChunkedRequestHelper<T>(context.Request);

            if (!TryAuthorize(request))
            {
                await WriteError(context, "Not authorized or session with specified upload id already expired");

                return;
            }

            if ((await _tenantManager.GetCurrentTenantAsync()).Status != TenantStatus.Active)
            {
                await WriteError(context, "Can't perform upload for deleted or transferring portals");

                return;
            }

            switch (request.Type())
            {
                case ChunkedRequestType.Abort:
                    await _fileUploader.AbortUploadAsync<T>(request.UploadId);
                    await WriteSuccess(context, null);

                    return;

                case ChunkedRequestType.Initiate:
                    var createdSession = await _fileUploader.InitiateUploadAsync(request.FolderId, request.FileId, request.FileName, request.FileSize, request.Encrypted);
                    await WriteSuccess(context, await _chunkedUploadSessionHelper.ToResponseObjectAsync(createdSession, true));

                    return;

                case ChunkedRequestType.Upload:
                    {
                        var resumedSession = await _fileUploader.UploadChunkAsync<T>(request.UploadId, request.ChunkStream, request.ChunkSize);

                        var chunks = await _chunkedUploadSessionHolder.GetChunksAsync(resumedSession);
                        var bytesUploaded = chunks.Sum(c => c.Value == null ? 0 : c.Value.Length);
                        if (bytesUploaded == resumedSession.BytesTotal)
                        {
                            resumedSession = await _fileUploader.FinalizeUploadSessionAsync<T>(request.UploadId);
                            await WriteSuccess(context, await ToResponseObject(resumedSession.File), (int)HttpStatusCode.Created);
                            _ = _filesMessageService.SendAsync(MessageAction.FileUploaded, resumedSession.File, resumedSession.File.Title);

                            await _socketManager.CreateFileAsync(resumedSession.File);
                        }
                        else
                        {
                            await WriteSuccess(context, await _chunkedUploadSessionHelper.ToResponseObjectAsync(resumedSession));
                        }

                        return;
                    }
                case ChunkedRequestType.UploadAsync:
                    {
                        var resumedSession = await _fileUploader.UploadChunkAsync<T>(request.UploadId, request.ChunkStream, request.ChunkSize, request.ChunkNumber);
                        await WriteSuccess(context, await _chunkedUploadSessionHelper.ToResponseObjectAsync(resumedSession));
                        return;
                    }
                case ChunkedRequestType.Finalize:
                    var session = await _fileUploader.FinalizeUploadSessionAsync<T>(request.UploadId);

                    await WriteSuccess(context, await ToResponseObject(session.File), (int)HttpStatusCode.Created);
                    _ = _filesMessageService.SendAsync(MessageAction.FileUploaded, session.File, session.File.Title);

                    await _socketManager.CreateFileAsync(session.File);
                    return;
                default:
                    await WriteError(context, "Unknown request type.");
                    return;
            }
        }
        catch (FileNotFoundException error)
        {
            _logger.ErrorChunkedUploaderHandlerService(error);
            await WriteError(context, FilesCommonResource.ErrorMassage_FileNotFound);
        }
        catch (Exception error)
        {
            _logger.ErrorChunkedUploaderHandlerService(error);
            await WriteError(context, error.Message);
        }
    }

    private bool TryAuthorize<T>(ChunkedRequestHelper<T> request)
    {
        if (!_authContext.IsAuthenticated)
        {
            return false;
        }

        if (request.Type() == ChunkedRequestType.Initiate)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(request.UploadId))
        {
            var uploadSession = _chunkedUploadSessionHolder.GetSession<T>(request.UploadId);
            if (uploadSession != null && _authContext.CurrentAccount.ID == uploadSession.UserId)
            {
                return true;
            }
        }

        return false;
    }

    private static Task WriteError(HttpContext context, string message)
    {
        return WriteResponse(context, false, null, message, (int)HttpStatusCode.OK);
    }

    private static Task WriteSuccess(HttpContext context, object data, int statusCode = (int)HttpStatusCode.OK)
    {
        return WriteResponse(context, true, data, string.Empty, statusCode);
    }

    private static Task WriteResponse(HttpContext context, bool success, object data, string message, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsync(JsonSerializer.Serialize(new { success, data, message }, new JsonSerializerOptions()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    private async Task<object> ToResponseObject<T>(File<T> file)
    {
        return new
        {
            id = file.Id,
            folderId = file.ParentId,
            version = file.Version,
            title = file.Title,
            provider_key = file.ProviderKey,
            uploaded = true,
            file = await _filesWrapperHelper.GetAsync(file)
        };
    }
}

public enum ChunkedRequestType
{
    None,
    Initiate,
    Abort,
    Upload,
    UploadAsync,
    Finalize
}

[DebuggerDisplay("{Type} ({UploadId})")]
public class ChunkedRequestHelper<T>
{
    private readonly HttpRequest _request;
    private IFormFile _file;
    private int? _tenantId;
    private long? _fileContentLength;

    public ChunkedRequestType Type()
    {
        if (_request.Query["initiate"] == "true" && IsFileDataSet())
        {
            return ChunkedRequestType.Initiate;
        }

        if (_request.Query["abort"] == "true" && !string.IsNullOrEmpty(UploadId))
        {
            return ChunkedRequestType.Abort;
        }

        if (_request.Query["finalize"] == "true" && !string.IsNullOrEmpty(UploadId))
        {
            return ChunkedRequestType.Finalize;
        }

        if (_request.Query["upload"] == "true" && !string.IsNullOrEmpty(UploadId))
        {
            return ChunkedRequestType.UploadAsync;
        }

        return !string.IsNullOrEmpty(UploadId)
                    ? ChunkedRequestType.Upload
                    : ChunkedRequestType.None;
    }

    public string UploadId => _request.Query["uid"];

    public int TenantId
    {
        get
        {
            if (!_tenantId.HasValue)
            {
                if (int.TryParse(_request.Query["tid"], out var v))
                {
                    _tenantId = v;
                }
                else
                {
                    _tenantId = -1;
                }
            }

            return _tenantId.Value;
        }
    }

    public T FolderId
    {
        get
        {
            var queryValue = _request.Query[FilesLinkUtility.FolderId];

            if (queryValue.Count == 0)
            {
                return default(T);
            }

            return IdConverter.Convert<T>(queryValue[0]);
        }
    }

    public T FileId
    {
        get
        {
            var queryValue = _request.Query[FilesLinkUtility.FileId];

            if (queryValue.Count == 0)
            {
                return default(T);
            }

            return IdConverter.Convert<T>(queryValue[0]);
        }
    }

    public string FileName => _request.Query[FilesLinkUtility.FileTitle];

    public long FileSize
    {
        get
        {
            if (!_fileContentLength.HasValue)
            {
                long.TryParse(_request.Query["fileSize"], out var v);
                _fileContentLength = v;
            }

            return _fileContentLength.Value;
        }
    }

    public long ChunkSize => File.Length;

    public Stream ChunkStream => File.OpenReadStream();

    public bool Encrypted => _request.Query["encrypted"] == "true";

    private int? _chunkNumber;
    public int? ChunkNumber
    {
        get
        {
            if (!_chunkNumber.HasValue)
            {
                var result = int.TryParse(_request.Query["chunkNumber"], out var i);
                if (result)
                {
                    _chunkNumber = i;
                }
            }
            return _chunkNumber;
        }
    }

    private IFormFile File
    {
        get
        {
            if (_file != null)
            {
                return _file;
            }

            if (_request.Form.Files.Count > 0)
            {
                return _file = _request.Form.Files[0];
            }

            throw new Exception("HttpRequest.Files is empty");
        }
    }

    public ChunkedRequestHelper(HttpRequest request)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
    }

    private bool IsFileDataSet()
    {
        return !string.IsNullOrEmpty(FileName) && !EqualityComparer<T>.Default.Equals(FolderId, default(T));
    }
}

public static class ChunkedUploaderHandlerExtention
{
    public static IApplicationBuilder UseChunkedUploaderHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ChunkedUploaderHandler>();
    }
}