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

using ASC.Data.Storage;
using ASC.Data.Storage.DiscStorage;

using ImageMagick;

using FileShare = System.IO.FileShare;

namespace ASC.Files.Service.Services.Thumbnail;

[Scope(GenericArguments = [typeof(int)])]
[Scope(GenericArguments = [typeof(string)])]
public class Builder<T>(ThumbnailSettings settings,
    TenantManager tenantManager,
    IDaoFactory daoFactory,
    DocumentServiceConnector documentServiceConnector,
    DocumentServiceHelper documentServiceHelper,
    Global global,
    PathProvider pathProvider,
    ILoggerProvider log,
    IHttpClientFactory clientFactory,
    FFmpegService fFmpegService,
    TempPath tempPath,
    SocketManager socketManager,
    TempStream tempStream,
    StorageFactory storageFactory,
    SecurityContext securityContext)
{
    private readonly ILogger _logger = log.CreateLogger("ASC.Files.ThumbnailBuilder");
    private IDataStore _dataStore;

    private readonly List<string> _imageFormatsCanBeCrop =
        [".bmp", ".gif", ".jpeg", ".jpg", ".pbm", ".png", ".tiff", ".tga", ".webp"];

    internal async Task BuildThumbnail(FileData<T> fileData)
    {
        try
        {
            await tenantManager.SetCurrentTenantAsync(fileData.TenantId);
            await securityContext.AuthenticateMeWithoutCookieAsync(fileData.CreatedBy);
            
            _dataStore = await storageFactory.GetStorageAsync(fileData.TenantId, FileConstant.StorageModule, (IQuotaController)null);

            var fileDao = daoFactory.GetFileDao<T>();
            if (fileDao == null)
            {
                _logger.ErrorBuildThumbnailFileDaoIsNull(fileData.TenantId);

                return;
            }

            await GenerateThumbnail(fileDao, fileData);
        }
        catch (Exception exception)
        {
            _logger.ErrorBuildThumbnailsTenantId(fileData.TenantId, exception);
        }
    }

    private async Task GenerateThumbnail(IFileDao<T> fileDao, FileData<T> fileData)
    {
        File<T> file = null;

        try
        {
            file = await fileDao.GetFileAsync(fileData.FileId);

            if (file == null)
            {
                _logger.ErrorGenerateThumbnailFileNotFound(fileData.FileId.ToString());

                return;
            }

            if (file.ThumbnailStatus != ASC.Files.Core.Thumbnail.Waiting)
            {
                _logger.InformationGenerateThumbnail(fileData.FileId.ToString());

                return;
            }

            if (!CanCreateThumbnail(file))
            {
                file.ThumbnailStatus = ASC.Files.Core.Thumbnail.NotRequired;

                await fileDao.SetThumbnailStatusAsync(file, ASC.Files.Core.Thumbnail.NotRequired);

                return;
            }

            await fileDao.SetThumbnailStatusAsync(file, ASC.Files.Core.Thumbnail.Creating);

            var ext = FileUtility.GetFileExtension(file.Title);

            if (IsVideo(ext))
            {
                await MakeThumbnailFromVideo(fileDao, file);
            }
            else if (IsImage(ext))
            {
                await MakeThumbnailFromImage(fileDao, file);
            }
            else
            {
                await MakeThumbnailFromDocs(fileDao, file);
            }

            await fileDao.SetThumbnailStatusAsync(file, ASC.Files.Core.Thumbnail.Created);

            var newFile = await fileDao.GetFileStableAsync(file.Id);

            await socketManager.UpdateFileAsync(newFile);
        }
        catch (Exception exception)
        {
            _logger.ErrorGenerateThumbnail(fileData.FileId.ToString(), exception);
            if (file != null)
            {
                file.ThumbnailStatus = ASC.Files.Core.Thumbnail.Error;

                await fileDao.SetThumbnailStatusAsync(file, ASC.Files.Core.Thumbnail.Error);
            }
        }
    }

    private async Task MakeThumbnailFromVideo(IFileDao<T> fileDao, File<T> file)
    {
        var streamFile = await fileDao.GetFileStreamAsync(file);

        var thumbPath = tempPath.GetTempFileName("jpg");
        var tempFilePath = tempPath.GetTempFileName(Path.GetExtension(file.Title));

        try
        {
            await using (var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                await streamFile.CopyToAsync(fileStream);
            }

            await fFmpegService.CreateThumbnail(tempFilePath, thumbPath);

            await using (var streamThumb = new FileStream(thumbPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                await CropAsync(fileDao, file, streamThumb);
            }
        }
        finally
        {
            if (Path.Exists(thumbPath))
            {
                File.Delete(thumbPath);
            }

            if (Path.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    private async Task MakeThumbnailFromDocs(IFileDao<T> fileDao, File<T> file)
    {            
        _logger.DebugMakeThumbnail1(file.Id.ToString());

        string thumbnailUrl = null;
        var resultPercent = 0;
        var attempt = 1;

        var maxSize = settings.Sizes.MaxBy(r => r.Width + r.Height);
        var thumbnailHeight = maxSize.Height;
        var thumbnailWidth = maxSize.Width;
        

        if (maxSize.Width > maxSize.Height) // change thumbnail orientation
        {
            (thumbnailHeight, thumbnailWidth) = (thumbnailWidth, thumbnailHeight);
        }

        do
        {
            try
            {
                (resultPercent, thumbnailUrl) = await GetThumbnailUrl(file, global.DocThumbnailExtension.ToStringFast(), thumbnailWidth, thumbnailHeight);

                if (resultPercent == 100)
                {
                    break;
                }
            }
            catch (Exception exception)
            {
                if (exception.InnerException != null)
                {
                    if (exception.InnerException is DocumentServiceException documentServiceException)
                    {
                        if (documentServiceException.Code == DocumentServiceException.ErrorCode.ConvertPassword)
                        {
                            throw new Exception($"MakeThumbnail: FileId: {file.Id}. Encrypted file.", exception);
                        }
                        if (documentServiceException.Code == DocumentServiceException.ErrorCode.Convert)
                        {
                            throw new Exception($"MakeThumbnail: FileId: {file.Id}. Could not convert.", exception);
                        }
                    }
                    else
                    {
                        _logger.WarningMakeThumbnail(file.Id.ToString(), thumbnailUrl, resultPercent, attempt, exception);
                    }
                }
                else
                {
                    _logger.WarningMakeThumbnail(file.Id.ToString(), thumbnailUrl, resultPercent, attempt, exception);
                }
            }

            if (attempt >= settings.AttemptsLimit)
            {
                throw new Exception($"MakeThumbnail: FileId: {file.Id}, ThumbnailUrl: {thumbnailUrl}, ResultPercent: {resultPercent}. Attempts limit exceeded.");
            }

            _logger.DebugMakeThumbnailAfter(file.Id.ToString(), settings.AttemptWaitInterval, attempt);
            attempt++;

            await Task.Delay(settings.AttemptWaitInterval);
        }
        while (string.IsNullOrEmpty(thumbnailUrl));
        
        _logger.DebugMakeThumbnail3(file.Id.ToString(), thumbnailUrl);

        using var request = new HttpRequestMessage();
        request.RequestUri = new Uri(thumbnailUrl);

        var httpClient = clientFactory.CreateClient();
        using var response = await httpClient.SendAsync(request);
        await using (var stream = await response.Content.ReadAsStreamAsync())
        {
            using (var sourceImg = new MagickImage(stream))
            {
                foreach (var w in settings.Sizes)
                {
                    await CropAsync(sourceImg, fileDao, file, w.Width, w.Height);
                }
            }
        }

        _logger.DebugMakeThumbnail4(file.Id.ToString());
    }

    private async Task<(int, string)> GetThumbnailUrl(File<T> file, string toExtension, uint width, uint height)
    {
        var fileUri = pathProvider.GetFileStreamUrl(file);
        fileUri = documentServiceConnector.ReplaceCommunityAddress(fileUri);

        var fileExtension = file.ConvertedExtension;
        var docKey = await documentServiceHelper.GetDocKeyAsync(file);
        var thumbnail = new ThumbnailData
        {
            Aspect = 1,
            First = true,
            Height = (int)height,
            Width = (int)width
        };
        var spreadsheetLayout = new SpreadsheetLayout
        {
            IgnorePrintArea = true,
            //Orientation = "landscape", // "297mm" x "210mm"
            FitToHeight = (int)height,
            FitToWidth = (int)width,
            Headings = false,
            GridLines = false,
            Margins = new SpreadsheetLayout.LayoutMargins
            {
                Top = "0mm",
                Right = "0mm",
                Bottom = "0mm",
                Left = "0mm"
            },
            PageSize = new SpreadsheetLayout.LayoutPageSize()
        };

        var (operationResultProgress, url, _) = await documentServiceConnector.GetConvertedUriAsync(fileUri, fileExtension, toExtension, docKey, null, CultureInfo.CurrentCulture.Name, thumbnail, spreadsheetLayout, null, false, false);

        operationResultProgress = Math.Min(operationResultProgress, 100);
        return (operationResultProgress, url);
    }


    private bool CanCreateThumbnail(File<T> file)
    {
        var ext = FileUtility.GetFileExtension(file.Title);

        if (!CanCreateThumbnail(ext) || file.Encrypted || file.RootFolderType == FolderType.TRASH)
        {
            return false;
        }

        if (IsVideo(ext) && file.ContentLength > settings.MaxVideoFileSize)
        {
            return false;
        }

        if (IsImage(ext) && file.ContentLength > settings.MaxImageFileSize)
        {
            return false;
        }

        return true;
    }

    private bool CanCreateThumbnail(string extension)
    {
        return settings.FormatsArray.Contains(extension) || IsVideo(extension) || IsImage(extension);
    }

    private bool IsImage(string extension)
    {
        return _imageFormatsCanBeCrop.Contains(extension);
    }

    private bool IsVideo(string extension)
    {
        return fFmpegService.ExistFormat(extension);
    }

    private async Task MakeThumbnailFromImage(IFileDao<T> fileDao, File<T> file)
    {
        _logger.DebugCropImage(file.Id.ToString());

        await using (var stream = await fileDao.GetFileStreamAsync(file))
        {
            await CropAsync(fileDao, file, stream);
        }

        _logger.DebugCropImageSuccessfullySaved(file.Id.ToString());
    }

    private async Task CropAsync(IFileDao<T> fileDao, File<T> file, Stream stream)
    {
        using var sourceImg = new MagickImage(stream);

        if (_dataStore is DiscDataStore)
        {
            foreach (var w in settings.Sizes)
            {
                await CropAsync(sourceImg, fileDao, file, w.Width, w.Height);
            }
        }
        else
        {
            await Parallel.ForEachAsync(settings.Sizes, new ParallelOptions { MaxDegreeOfParallelism = 3 }, async (w, _) =>
            {
                await CropAsync(sourceImg, fileDao, file, w.Width, w.Height);
            });
        }

        GC.Collect();
    }

    private async ValueTask CropAsync(
        MagickImage sourceImg,
        IFileDao<T> fileDao,
        File<T> file,
        uint width,
        uint height)
    {
        var targetImg = GetImageThumbnail(sourceImg, width, height);
        await using var targetStream = tempStream.Create();

        switch (global.ThumbnailExtension)
        {
            case ThumbnailExtension.bmp:
                await targetImg.WriteAsync(targetStream, MagickFormat.Bmp);
                break;
            case ThumbnailExtension.gif:
                await targetImg.WriteAsync(targetStream, MagickFormat.Gif);
                break;
            case ThumbnailExtension.jpg:
                await targetImg.WriteAsync(targetStream, MagickFormat.Jpg);
                break;
            case ThumbnailExtension.png:
                await targetImg.WriteAsync(targetStream, MagickFormat.Png);
                break;
            case ThumbnailExtension.pbm:
                await targetImg.WriteAsync(targetStream, MagickFormat.Pbm);
                break;
            case ThumbnailExtension.tiff:
                await targetImg.WriteAsync(targetStream, MagickFormat.Tiff);
                break;
            case ThumbnailExtension.tga:
                await targetImg.WriteAsync(targetStream, MagickFormat.Tga);
                break;
            case ThumbnailExtension.webp:
                await targetImg.WriteAsync(targetStream, MagickFormat.WebP);
                break;
        }

        await _dataStore.SaveAsync(fileDao.GetUniqThumbnailPath(file, width, height), targetStream);
    }

    private IMagickImage GetImageThumbnail(MagickImage sourceBitmap, uint thumbnailWidth, uint thumbnailHeight)
    {
        if (sourceBitmap.BoundingBox != null && 
            (sourceBitmap.BoundingBox.Width > sourceBitmap.BoundingBox.Height && thumbnailWidth < thumbnailHeight ||
            sourceBitmap.BoundingBox.Width < sourceBitmap.BoundingBox.Height && thumbnailWidth > thumbnailHeight))
        {
            (thumbnailHeight, thumbnailWidth) = (thumbnailWidth, thumbnailHeight);
        }


        sourceBitmap.Thumbnail(thumbnailWidth, thumbnailHeight);

        sourceBitmap.AutoOrient();

        return sourceBitmap;
    }
}
