// (c) Copyright Ascensio System SIA 2010-2023
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

using Microsoft.Net.Http.Headers;

namespace ASC.Web.Files.HttpHandlers;

public class ChunkedUploaderHandler
{
    public ChunkedUploaderHandler(RequestDelegate _)
    {
    }

    public async Task Invoke(HttpContext context, ChunkedUploaderHandlerService chunkedUploaderHandlerService)
    {
        await chunkedUploaderHandlerService.Invoke(context);
    }
}

[Scope]
public class ChunkedUploaderHandlerService(ILogger<ChunkedUploaderHandlerService> logger,
    TenantManager tenantManager,
    FileUploader fileUploader,
    FilesMessageService filesMessageService,
    ChunkedUploadSessionHolder chunkedUploadSessionHolder,
    ChunkedUploadSessionHelper chunkedUploadSessionHelper,
    SocketManager socketManager,
    FileDtoHelper filesWrapperHelper,
    AuthContext authContext)
{
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

            if (!(await TryAuthorizeAsync(request)))
            {
                await WriteError(context, "Not authorized or session with specified upload id already expired");

                return;
            }

            if ((await tenantManager.GetCurrentTenantAsync()).Status != TenantStatus.Active)
            {
                await WriteError(context, "Can't perform upload for deleted or transferring portals");

                return;
            }

            switch (request.Type())
            {
                case ChunkedRequestType.Abort:
                    await fileUploader.AbortUploadAsync<T>(request.UploadId);
                    await WriteSuccess(context, null);

                    return;

                case ChunkedRequestType.Initiate:
                    var createdSession = await fileUploader.InitiateUploadAsync(request.FolderId, request.FileId, request.FileName, request.FileSize, request.Encrypted);
                    await WriteSuccess(context, await chunkedUploadSessionHelper.ToResponseObjectAsync(createdSession, true));

                    return;

                case ChunkedRequestType.Upload:
                    {
                        var resumedSession = await fileUploader.UploadChunkAsync<T>(request.UploadId, await request.ChunkStream(), await request.ChunkSize());
                        await chunkedUploadSessionHolder.StoreSessionAsync(resumedSession);
                        
                        var transferredBytes = await fileUploader.GetTransferredBytesCountAsync(resumedSession);
                        if (transferredBytes == resumedSession.BytesTotal)
                        {
                            if (resumedSession.UseChunks)
                            {
                                resumedSession = await fileUploader.FinalizeUploadSessionAsync<T>(request.UploadId);
                            }

                            await WriteSuccess(context, await ToResponseObject(resumedSession.File), (int)HttpStatusCode.Created);
                            _ = filesMessageService.SendAsync(MessageAction.FileUploaded, resumedSession.File, resumedSession.File.Title);

                            await socketManager.CreateFileAsync(resumedSession.File);
                        }
                        else
                        {
                            await WriteSuccess(context, await chunkedUploadSessionHelper.ToResponseObjectAsync(resumedSession));
                        }

                        return;
                    }

                case ChunkedRequestType.UploadAsync:
                    {
                        var boundary = MultipartRequestHelper.GetBoundary(
                            Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse(context.Request.ContentType),
                            100);
                        var reader = new MultipartReader(boundary, context.Request.Body);
                        var section = await reader.ReadNextSectionAsync();
                        var headersLength = 0;
                        boundary = HeaderUtilities.RemoveQuotes(new StringSegment(boundary)).ToString();
                        var boundaryLength = Encoding.UTF8.GetBytes("\r\n--" + boundary).Length + 2;
                        foreach (var h in section.Headers)
                        {
                            headersLength += h.Value.Sum(r => r.Length) + h.Key.Length + "\n\n".Length;
                        }
                       
                        var resumedSession = await fileUploader.UploadChunkAsync<T>(request.UploadId, section.Body, context.Request.ContentLength.Value - headersLength - boundaryLength * 2 - 6, request.ChunkNumber);
                        await chunkedUploadSessionHolder.StoreSessionAsync(resumedSession);
                        await WriteSuccess(context,
                            await chunkedUploadSessionHelper.ToResponseObjectAsync(resumedSession));
                        return;
                    }
                case ChunkedRequestType.Finalize:
                    var session = await chunkedUploadSessionHolder.GetSessionAsync<T>(request.UploadId);
                    if (session.UseChunks)
                    {
                        session = await fileUploader.FinalizeUploadSessionAsync<T>(request.UploadId);
                    }

                    await WriteSuccess(context, await ToResponseObject(session.File), (int)HttpStatusCode.Created);
                    _ = filesMessageService.SendAsync(MessageAction.FileUploaded, session.File, session.File.Title);

                    await socketManager.CreateFileAsync(session.File);
                    return;
            }
        }
        catch (System.Text.Json.JsonException)
        {
            throw;
        }
        catch (FileNotFoundException error)
        {
            logger.ErrorChunkedUploaderHandlerService(error);
            await WriteError(context, FilesCommonResource.ErrorMessage_FileNotFound);
        }
        catch (Exception error)
        {
            logger.ErrorChunkedUploaderHandlerService(error);
            await WriteError(context, error.Message);
        }
    }

    private async Task<bool> TryAuthorizeAsync<T>(ChunkedRequestHelper<T> request)
    {
        if (!authContext.IsAuthenticated)
        {
            return false;
        }

        if (request.Type() == ChunkedRequestType.Initiate)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(request.UploadId))
        {
            var uploadSession = await chunkedUploadSessionHolder.GetSessionAsync<T>(request.UploadId);
            if (uploadSession != null && authContext.CurrentAccount.ID == uploadSession.UserId)
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

        return context.Response.WriteAsync(JsonSerializer.Serialize(new { success, data, message }, new JsonSerializerOptions
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
            file = await filesWrapperHelper.GetAsync(file)
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
public class ChunkedRequestHelper<T>(HttpRequest request)
{
    private readonly HttpRequest _request = request ?? throw new ArgumentNullException(nameof(request));
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
                return default;
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
                return default;
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

    public async Task<long> ChunkSize() => (await File()).Length;

    public async Task<Stream> ChunkStream() => (await File()).OpenReadStream();

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

    private async Task<IFormFile> File()
    {
        try
        {
            return _file ??= (await _request.ReadFormAsync(CancellationToken.None)).Files[0];
        }
        catch
        {
            throw new Exception("HttpRequest.Files is empty");
        }
    }

    private bool IsFileDataSet()
    {
        return !string.IsNullOrEmpty(FileName) && !EqualityComparer<T>.Default.Equals(FolderId, default);
    }
}

public static class MultipartRequestHelper
{
    public static string GetBoundary(Microsoft.Net.Http.Headers.MediaTypeHeaderValue contentType, int lengthLimit)
    {
        var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

        if (string.IsNullOrWhiteSpace(boundary))
        {
            throw new InvalidDataException("Missing content-type boundary.");
        }

        if (boundary.Length > lengthLimit)
        {
            throw new InvalidDataException($"Multipart boundary length limit {lengthLimit} exceeded.");
        }

        return boundary;
    }
}

public static class ChunkedUploaderHandlerExtension
{
    public static IApplicationBuilder UseChunkedUploaderHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ChunkedUploaderHandler>();
    }
}
