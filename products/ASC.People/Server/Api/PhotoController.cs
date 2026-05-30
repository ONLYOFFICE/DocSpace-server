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

using ImageMagick;

using UnknownImageFormatException = ASC.Web.Core.Users.UnknownImageFormatException;

namespace ASC.People.Api;

///<remarks>
/// Photo API.
///</remarks>
public class PhotoController(
    UserManager userManager,
    PermissionContext permissionContext,
    ApiContext apiContext,
    UserPhotoManager userPhotoManager,
    MessageService messageService,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    SecurityContext securityContext,
    SettingsManager settingsManager,
    FileSizeComment fileSizeComment,
    SetupInfo setupInfo,
    IHttpContextAccessor httpContextAccessor,
    UserWebhookManager webhookManager,
    IUrlValidator urlValidator,
    IHttpClientFactory httpClientFactory)
    : PeopleControllerBase(userManager, permissionContext, apiContext, userPhotoManager, httpContextAccessor, urlValidator, setupInfo, httpClientFactory)
{
    /// <remarks>
    /// Creates the user photo thumbnails by coordinates of the original image specified in the request.
    /// </remarks>
    /// <summary>
    /// Create photo thumbnails
    /// </summary>
    /// <path>api/2.0/people/{userid}/photo/thumbnails</path>
    [Tags("People / Photos")]
    [SwaggerResponse(200, "Thumbnail parameters", typeof(ThumbnailsDataDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "User not found")]
    [HttpPost("{userid}/photo/thumbnails")]
    public async Task<ThumbnailsDataDto> CreateMemberPhotoThumbnails(ThumbnailsRequestDto inDto)
    {
        var user = await GetUserInfoAsync(inDto.UserId);

        if (_userManager.IsSystemUser(user.Id) || !user.Id.Equals(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException();
        }

        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser);

        if (!string.IsNullOrEmpty(inDto.Thumbnails.TmpFile))
        {
            var fileName = Path.GetFileName(inDto.Thumbnails.TmpFile);
            var data = await _userPhotoManager.GetTempPhotoData(fileName);

            UserPhotoThumbnailSettings settings;

            if (inDto.Thumbnails.Width == 0 && inDto.Thumbnails.Height == 0)
            {
                using var img = new MagickImage(data);
                settings = new UserPhotoThumbnailSettings(inDto.Thumbnails.X, inDto.Thumbnails.Y, img.Width, img.Height);
            }
            else
            {
                settings = new UserPhotoThumbnailSettings(inDto.Thumbnails.X, inDto.Thumbnails.Y, inDto.Thumbnails.Width, inDto.Thumbnails.Height);
            }

            await settingsManager.SaveAsync(settings, user.Id);

            await _userPhotoManager.RemovePhotoAsync(user.Id);
            await _userPhotoManager.SaveOrUpdatePhoto(user.Id, data);
            await _userPhotoManager.RemoveTempPhotoAsync(fileName);
        }
        else
        {
            await UserPhotoThumbnailManager.SaveThumbnails(_userPhotoManager, settingsManager, inDto.Thumbnails.X, inDto.Thumbnails.Y, inDto.Thumbnails.Width, inDto.Thumbnails.Height, user.Id);
        }

        await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);
        messageService.Send(MessageAction.UserUpdatedAvatarThumbnails, MessageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper));
        return await ThumbnailsDataDto.Create(user, _userPhotoManager);
    }

    /// <remarks>
    /// Deletes a photo of the user with the ID specified in the request.
    /// </remarks>
    /// <summary>
    /// Delete a user photo
    /// </summary>
    /// <path>api/2.0/people/{userid}/photo</path>
    [Tags("People / Photos")]
    [SwaggerResponse(200, "Thumbnail parameters: original photo, retina, maximum size photo, big, medium, small", typeof(ThumbnailsDataDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "User not found")]
    [HttpDelete("{userid}/photo")]
    public async Task<ThumbnailsDataDto> DeleteMemberPhoto(GetUserPhotoRequestDto inDto)
    {
        var user = await GetUserInfoAsync(inDto.UserId);

        if (_userManager.IsSystemUser(user.Id) || !user.Id.Equals(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException();
        }

        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser);

        await _userPhotoManager.RemovePhotoAsync(user.Id);
        await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);
        messageService.Send(MessageAction.UserDeletedAvatar, MessageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper));
        await webhookManager.PublishAsync(WebhookTrigger.UserUpdated, user);

        return await ThumbnailsDataDto.Create(user, _userPhotoManager);
    }

    /// <remarks>
    /// Returns a photo of the user with the ID specified in the request.
    /// </remarks>
    /// <summary>
    /// Get a user photo
    /// </summary>
    /// <path>api/2.0/people/{userid}/photo</path>
    [Tags("People / Photos")]
    [SwaggerResponse(200, "Thumbnail parameters: original photo, retina, maximum size photo, big, medium, small", typeof(ThumbnailsDataDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "User not found")]
    [HttpGet("{userid}/photo")]
    public async Task<ThumbnailsDataDto> GetMemberPhoto(GetUserPhotoRequestDto inDto)
    {
        var user = await GetUserInfoAsync(inDto.UserId);

        if (_userManager.IsSystemUser(user.Id) || !await _userManager.CanUserViewAnotherUserAsync(securityContext.CurrentAccount.ID, user.Id))
        {
            throw new SecurityException();
        }

        return await ThumbnailsDataDto.Create(user, _userPhotoManager);
    }

    /// <remarks>
    /// Updates a photo of the user with the ID specified in the request.
    /// </remarks>
    /// <summary>
    /// Update a user photo
    /// </summary>
    /// <path>api/2.0/people/{userid}/photo</path>
    [Tags("People / Photos")]
    [SwaggerResponse(200, "Updated thumbnail parameters: original photo, retina, maximum size photo, big, medium, small", typeof(ThumbnailsDataDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "User not found")]
    [HttpPut("{userid}/photo")]
    public async Task<ThumbnailsDataDto> UpdateMemberPhoto(UpdatePhotoMemberRequestDto inDto)
    {
        var user = await GetUserInfoAsync(inDto.UserId);

        if (_userManager.IsSystemUser(user.Id) || !user.Id.Equals(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException();
        }

        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser);

        if (string.IsNullOrEmpty(inDto.UpdatePhoto.Files))
        {
            throw new ArgumentException(PeopleResource.ErrorEmptyUploadFileSelected);
        }

        if (inDto.UpdatePhoto.Files != await _userPhotoManager.GetPhotoAbsoluteWebPath(user.Id))
        {
            var photoValidation = await ValidatePhotoUrlAsync(inDto.UpdatePhoto.Files);
            await DownloadAndSavePhotoAsync(photoValidation, user);
        }

        await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);
        messageService.Send(MessageAction.UserAddedAvatar, MessageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper));
        await webhookManager.PublishAsync(WebhookTrigger.UserUpdated, user);

        return await ThumbnailsDataDto.Create(user, _userPhotoManager);
    }

    /// <remarks>
    /// Uploads a photo of the user with the ID specified in the request.
    /// </remarks>
    /// <summary>
    /// Upload a user photo
    /// </summary>
    /// <path>api/2.0/people/{userid}/photo</path>
    [Tags("People / Photos")]
    [SwaggerResponse(200, "Result of file uploading", typeof(FileUploadResultDto))]
    [SwaggerResponse(400, "The uploaded file could not be found")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(413, "Image size is too large")]
    [SwaggerResponse(415, "Unknown image file type")]
    [HttpPost("{userid}/photo")]
    public async Task<FileUploadResultDto> UploadMemberPhoto(UploadMemberPhotoRequestDto inDto)
    {
        var result = new FileUploadResultDto();
        var autosave = inDto.Autosave;

        try
        {
            if (inDto.File != null)
            {
                var user = await GetUserInfoAsync(inDto.UserId);

                if (_userManager.IsSystemUser(user.Id) || !user.Id.Equals(securityContext.CurrentAccount.ID))
                {
                    throw new SecurityException();
                }

                await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser);

                var userPhoto = inDto.File;

                if (userPhoto.Length > _setupInfo.MaxImageUploadSize)
                {
                    result.Success = false;
                    result.Message = fileSizeComment.FileImageSizeExceptionString;

                    return result;
                }

                var data = new byte[userPhoto.Length];
                await using var inputStream = userPhoto.OpenReadStream();

                var br = new BinaryReader(inputStream);
                _ = br.Read(data, 0, (int)userPhoto.Length);
                br.Close();

                CheckImgFormat(data);

                if (autosave)
                {
                    if (data.Length > _setupInfo.MaxImageUploadSize)
                    {
                        throw new ImageSizeLimitException();
                    }

                    var mainPhoto = await _userPhotoManager.SaveOrUpdatePhoto(user.Id, data);
                    var userInfo = await _userManager.GetUsersAsync(user.Id);
                    var cacheKey = Math.Abs(userInfo.LastModified.GetHashCode());

                    result.Data =
                        new
                        {
                            main = mainPhoto.Item1 + $"?hash={cacheKey}",
                            retina = await _userPhotoManager.GetRetinaPhotoURL(user.Id) + $"?hash={cacheKey}",
                            max = await _userPhotoManager.GetMaxPhotoURL(user.Id) + $"?hash={cacheKey}",
                            big = await _userPhotoManager.GetBigPhotoURL(user.Id) + $"?hash={cacheKey}",
                            medium = await _userPhotoManager.GetMediumPhotoURL(user.Id) + $"?hash={cacheKey}",
                            small = await _userPhotoManager.GetSmallPhotoURL(user.Id) + $"?hash={cacheKey}"
                        };

                    messageService.Send(MessageAction.UserAddedAvatar, MessageTarget.Create(user.Id), userInfo.DisplayUserName(false, displayUserSettingsHelper));
                    await webhookManager.PublishAsync(WebhookTrigger.UserUpdated, userInfo);
                }
                else
                {
                    result.Data = await _userPhotoManager.SaveTempPhoto(data, _setupInfo.MaxImageUploadSize, UserPhotoManager.OriginalFotoSize.Width, UserPhotoManager.OriginalFotoSize.Height);
                }

                result.Success = true;
            }
            else
            {
                result.Success = false;
                result.Message = PeopleResource.ErrorEmptyUploadFileSelected;
            }

        }
        catch (UnknownImageFormatException)
        {
            result.Success = false;
            result.Message = PeopleResource.ErrorUnknownFileImageType;
        }
        catch (ImageWeightLimitException)
        {
            result.Success = false;
            result.Message = PeopleResource.ErrorImageWeightLimit;
        }
        catch (ImageSizeLimitException)
        {
            result.Success = false;
            result.Message = PeopleResource.ErrorImageSizetLimit;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message.HtmlEncode();
        }

        return result;
    }

    private static void CheckImgFormat(byte[] data)
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
}
