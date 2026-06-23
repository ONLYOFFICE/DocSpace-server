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

namespace ASC.Web.Core.Users;

public static class UserPhotoThumbnailManager
{
    public static async Task SaveThumbnails(UserPhotoManager userPhotoManager, SettingsManager settingsManager, int x, int y, uint width, uint height, Guid userId)
    {
        var thumbnailSettings = new UserPhotoThumbnailSettings(x, y, width, height);
        var thumbnailsData = new ThumbnailsData(userId, userPhotoManager);

        var resultBitmaps = new List<ThumbnailItem>();

        var (mainImg, _) = await thumbnailsData.MainImgBitmapAsync();

        using var img = mainImg;

        if (img == null)
        {
            return;
        }

        if (thumbnailSettings.Size.Width == 0 && thumbnailSettings.Size.Height == 0)
        {
            thumbnailSettings.Size = new MagickGeometry(img.Width, img.Height);
        }

        foreach (var thumbnail in await thumbnailsData.ThumbnailList())
        {
            thumbnail.Image = GetImage(img, thumbnail.Size, thumbnailSettings);

            resultBitmaps.Add(thumbnail);
        }

        await thumbnailsData.SaveAsync(resultBitmaps);

        await settingsManager.SaveAsync(thumbnailSettings, userId);
    }

    public static IMagickImage GetImage(MagickImage mainImg, IMagickGeometry size, UserPhotoThumbnailSettings thumbnailSettings)
    {
        var x = thumbnailSettings.Point.X > 0 ? thumbnailSettings.Point.X : 0;
        var y = thumbnailSettings.Point.Y > 0 ? thumbnailSettings.Point.Y : 0;
        var width = x + thumbnailSettings.Size.Width >= mainImg.Width ? mainImg.Width - x : thumbnailSettings.Size.Width;
        var height = y + thumbnailSettings.Size.Height >= mainImg.Height ? mainImg.Height - y : thumbnailSettings.Size.Height;

        var result = mainImg.CloneAndMutate(a =>
        {
            a.Colorize(MagickColors.White, new Percentage());
        });

        result.Crop(new MagickGeometry(x, y, (uint)width, (uint)height));
        result.Resize(size.Width, size.Height);

        return result;
    }

    public static void CheckImgFormat(byte[] data)
    {
        MagickFormat imgFormat;
        try
        {
            using var img = new MagickImage(data);
            imgFormat = img.Format;
        }
        catch (OutOfMemoryException)
        {
            throw new ImageSizeLimitException();
        }
        catch (ArgumentException error)
        {
            throw new UnknownImageFormatException(error);
        }

        if (imgFormat != MagickFormat.Png && imgFormat != MagickFormat.Jpeg)
        {
            throw new UnknownImageFormatException();
        }
    }

    public static async Task<byte[]> TryParseImage(byte[] data, long maxFileSize, IMagickGeometry maxsize)
    {
        if (data is not { Length: > 0 })
        {
            throw new UnknownImageFormatException();
        }

        if (maxFileSize != -1 && data.Length > maxFileSize)
        {
            throw new ImageSizeLimitException();
        }

        //data = ImageHelper.RotateImageByExifOrientationData(data, Log);

        try
        {
            using var img = new MagickImage(data);
            var width = img.Width;
            var height = img.Height;
            var maxWidth = maxsize.Width;
            var maxHeight = maxsize.Height;

            if (img.Height > maxHeight || img.Width > maxWidth)
            {
                #region calulate height and width

                if (width > maxWidth && height > maxHeight)
                {

                    if (width > height)
                    {
                        height = (uint)(height * (double)maxWidth / width + 0.5);
                        width = maxWidth;
                    }
                    else
                    {
                        width = (uint)(width * (double)maxHeight / height + 0.5);
                        height = maxHeight;
                    }
                }

                if (width > maxWidth && height <= maxHeight)
                {
                    height = (uint)(height * (double)maxWidth / width + 0.5);
                    width = maxWidth;
                }

                if (width <= maxWidth && height > maxHeight)
                {
                    width = (uint)(width * (double)maxHeight / height + 0.5);
                    height = maxHeight;
                }

                var tmpW = width;
                var tmpH = height;
                #endregion

                using var destRound = img.CloneAndMutate(x => x.Resize(tmpW, tmpH));

                data = await CommonPhotoManager.SaveToBytes(destRound);
            }
            return data;
        }
        catch (OutOfMemoryException)
        {
            throw new ImageSizeLimitException();
        }
        catch (ArgumentException error)
        {
            throw new UnknownImageFormatException(error);
        }
    }
}

public class ThumbnailItem
{
    public IMagickGeometry Size { get; init; }
    public string ImgUrl { get; set; }
    public IMagickImage Image { get; set; }
}

public class ThumbnailsData(Guid userId, UserPhotoManager userPhotoManager)
{
    public async Task<(MagickImage, MagickFormat)> MainImgBitmapAsync()
    {
        var (img, imageFormat) = await userPhotoManager.GetPhotoImageAsync(userId);
        return (img, imageFormat);
    }

    public async Task<List<ThumbnailItem>> ThumbnailList()
    {
        return
        [
            new ThumbnailItem { Size = UserPhotoManager.RetinaFotoSize, ImgUrl = await userPhotoManager.GetRetinaPhotoURL(userId) },

            new ThumbnailItem { Size = UserPhotoManager.MaxFotoSize, ImgUrl = await userPhotoManager.GetMaxPhotoURL(userId) },

            new ThumbnailItem { Size = UserPhotoManager.BigFotoSize, ImgUrl = await userPhotoManager.GetBigPhotoURL(userId) },

            new ThumbnailItem { Size = UserPhotoManager.MediumFotoSize, ImgUrl = await userPhotoManager.GetMediumPhotoURL(userId) },

            new ThumbnailItem { Size = UserPhotoManager.SmallFotoSize, ImgUrl = await userPhotoManager.GetSmallPhotoURL(userId) }
        ];
    }

    public async Task SaveAsync(List<ThumbnailItem> bitmaps)
    {
        foreach (var item in bitmaps)
        {
            var (mainImgBitmap, format) = await MainImgBitmapAsync();
            using var mainImg = mainImgBitmap;
            await userPhotoManager.SaveThumbnail(userId, item.Image, format);
        }
    }
}