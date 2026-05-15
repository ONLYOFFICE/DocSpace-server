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

using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ASC.Web.Core;

public class LogoUploader
{
    public LogoUploader(RequestDelegate _)
    {
    }

    public async Task Invoke
        (HttpContext context,
        PermissionContext permissionContext,
        SetupInfo setupInfo,
        UserPhotoManager userPhotoManager,
        TenantLogoManager tenantLogoManager)
    {
        var result = new FileUploadResult();
        try
        {
            await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
            await tenantLogoManager.DemandWhiteLabelPermissionAsync();

            var type = (WhiteLabelLogoType)Convert.ToInt32(context.Request.Form["logotype"]);
            var width = Convert.ToUInt32(context.Request.Form["width"]);
            var height = Convert.ToUInt32(context.Request.Form["height"]);
            var size = new MagickGeometry(width, height);

            if (context.Request.Form.Files.Count != 0)
            {
                const string imgContentType = @"image";

                var logo = context.Request.Form.Files[0];
                var ext = Path.GetExtension(logo.FileName).ToLowerInvariant();

                if (!logo.ContentType.StartsWith(imgContentType) ||
                    !TenantWhiteLabelSettings.AvailableExtensions.Contains(ext))
                {
                    throw new Exception(Resource.ErrorFileNotImage);
                }

                var maxSize = TenantWhiteLabelSettings.GetSize(type);
                if (size.Height > maxSize.Height || size.Width > maxSize.Width)
                {
                    throw new ImageSizeLimitException();
                }

                var data = new byte[logo.Length];

                var reader = new BinaryReader(logo.OpenReadStream());
                _ = reader.Read(data, 0, (int)logo.Length);
                reader.Close();

                if (logo.ContentType.Contains("image/x-icon") || logo.ContentType.Contains("image/vnd.microsoft.icon"))
                {
                    result.Success = true;
                    result.Message = await userPhotoManager.SaveTempPhoto(data, setupInfo.MaxImageUploadSize, "ico");
                }
                else if (logo.ContentType.Contains("image/svg+xml"))
                {
                    result.Success = true;
                    result.Message = await userPhotoManager.SaveTempPhoto(data, setupInfo.MaxImageUploadSize, "svg");
                }
                else
                {
                    using (var stream = new MemoryStream(data))
                    using (var image = new MagickImage(stream))
                    {
                        if (image.Height != size.Height && image.Width != size.Width)
                        {
                            throw new ImageSizeLimitException();
                        }
                    }
                    result.Success = true;
                    result.Message = await userPhotoManager.SaveTempPhoto(data, setupInfo.MaxImageUploadSize, size.Width, size.Height);
                }
            }
            else
            {
                result.Success = false;
                result.Message = Resource.ErrorEmptyUploadFileSelected;
            }
        }
        catch (ImageWeightLimitException)
        {
            result.Success = false;
            result.Message = Resource.ErrorImageWeightLimit;
        }
        catch (ImageSizeLimitException)
        {
            result.Success = false;
            result.Message = Resource.ErrorImageSize;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message.HtmlEncode();
        }
        await context.Response.WriteAsync(JsonSerializer.Serialize(result));
    }
}

public static class LogoUploaderExtensions
{
    public static IApplicationBuilder UseLogoUploader(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LogoUploader>();
    }
}