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

/// <remarks>
/// Photo API.
/// </remarks>
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
    /// Crops the avatar of your own profile and rebuilds all its thumbnail sizes.
    /// This is the second step of changing an avatar by hand.
    /// With `tmpFile`, the call crops the temporary image from `POST api/2.0/people/{userId}/photo` with `autosave`
    /// off. Then it makes the result the main photo and deletes the temporary file.
    /// Without `tmpFile`, the call crops the current photo of the profile again.
    /// A `width` and `height` of 0 keep the whole image.
    /// Any signed-in user can call it for their own profile only.
    /// A call for another profile returns 403, even from a DocSpace admin.
    /// The call changes data and replaces the previous crop.
    /// You can repeat it without `tmpFile` and with new coordinates.
    /// A repeated call with the same `tmpFile` returns 404.
    /// The call returns the URLs of every size, as `GET api/2.0/people/{userId}/photo` does.
    /// If no user has the ID or no temporary file has the name in `tmpFile`, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Create photo thumbnails
    /// </summary>
    /// <path>api/2.0/people/{userId}/photo/thumbnails</path>
    [Tags("People / Photos")]
    [SwaggerResponse(200, "The URLs of the rebuilt photo sizes", typeof(ThumbnailsDataDto))]
    [SwaggerResponse(403, "The ID in the route is not the calling account, the account may not edit its own profile, or `x` equals the width of the image or `y` its height")]
    [SwaggerResponse(404, "No user has the specified ID, or no temporary file has the name given in `tmpFile`")]
    [HttpPost("{userId}/photo/thumbnails")]
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
    /// Deletes the avatar of your own profile, so the profile shows the default image.
    /// Any signed-in user can call it for their own profile only.
    /// A call for another profile returns 403, even from a DocSpace admin.
    /// The call deletes the stored image and all its sizes, and this cannot be undone.
    /// You can repeat the call, even when the profile has no avatar.
    /// It returns the URLs of every size, which now point to the default image.
    /// To set a new avatar, use `POST api/2.0/people/{userId}/photo`.
    /// If no user has the ID, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Delete a user photo
    /// </summary>
    /// <path>api/2.0/people/{userId}/photo</path>
    [RaisesWebhook(WebhookTrigger.UserUpdated, When = "even when the profile had no avatar")]
    [Tags("People / Photos")]
    [SwaggerResponse(200, "The URLs of every photo size, now pointing at the default image", typeof(ThumbnailsDataDto))]
    [SwaggerResponse(403, "The ID in the route is not the calling account, or the account may not edit its own profile")]
    [SwaggerResponse(404, "No user has the specified ID")]
    [HttpDelete("{userId}/photo")]
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
    /// Returns the URLs of the avatar of a profile in every size the portal keeps.
    /// Any signed-in user can call it for an account they can see.
    /// If the caller cannot see the account, the call returns 403.
    /// A guest sees only the accounts related to them.
    /// The call is read-only.
    /// A profile without its own avatar gets the URLs of the default image.
    /// The URLs change with the avatar. Request them again before each use.
    /// To change the avatar, use `POST api/2.0/people/{userId}/photo` for a file or
    /// `PUT api/2.0/people/{userId}/photo` for a URL.
    /// To delete it, use `DELETE api/2.0/people/{userId}/photo`.
    /// If no user has the ID, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Get a user photo
    /// </summary>
    /// <path>api/2.0/people/{userId}/photo</path>
    [Tags("People / Photos")]
    [SwaggerResponse(200, "The URLs of the photo in every size, or of the default image when the profile has no photo", typeof(ThumbnailsDataDto))]
    [SwaggerResponse(403, "The caller is not allowed to see the requested account")]
    [SwaggerResponse(404, "No user has the specified ID")]
    [HttpGet("{userId}/photo")]
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
    /// Sets the avatar of your own profile from an image that the portal downloads from the URL in `files`.
    /// Any signed-in user can call it for their own profile only.
    /// A call for another profile returns 403, even from a DocSpace admin.
    /// The URL is absolute or relative to the portal.
    /// It must use HTTPS. Only a request that came over HTTP may pass an HTTP URL.
    /// The call changes data and replaces the avatar and all its sizes.
    /// You can repeat it with the same body.
    /// If `files` is the URL the profile already uses, the call keeps the current image.
    /// To send the image bytes, use `POST api/2.0/people/{userId}/photo`.
    /// To delete the avatar, use `DELETE api/2.0/people/{userId}/photo`.
    /// If `files` is empty, the call returns 400.
    /// It also returns 403 if the portal refuses the URL or the server behind it returns an error.
    /// If no user has the ID, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Update a user photo
    /// </summary>
    /// <path>api/2.0/people/{userId}/photo</path>
    [RaisesWebhook(WebhookTrigger.UserUpdated, When = "on every call, even when `files` is the current avatar")]
    [Tags("People / Photos")]
    [SwaggerResponse(200, "The URLs of the photo sizes built from the downloaded image", typeof(ThumbnailsDataDto))]
    [SwaggerResponse(400, "The request body cannot be read, or the files field is empty")]
    [SwaggerResponse(403, "The ID in the route is not the calling account, the account may not edit its own profile, the URL was refused, or the server behind it answered with an error status")]
    [SwaggerResponse(404, "No user has the specified ID")]
    [HttpPut("{userId}/photo")]
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
    /// Uploads an image as the avatar of your own profile, or as a temporary file.
    /// With `autosave` set to true, the image immediately becomes the avatar.
    /// The call builds all its sizes and returns their URLs in `data`.
    /// Each URL has a `hash` parameter that changes with the avatar. So an app can cache these URLs.
    /// With `autosave` false, the call only saves a temporary file and returns it in `data`.
    /// Pass this value as `tmpFile` to `POST api/2.0/people/{userId}/photo/thumbnails` to choose the crop.
    /// The profile does not change until that call succeeds.
    /// Any signed-in user can call it for their own profile only.
    /// A call for another profile gets 200 with `success` set to false.
    /// With `autosave` true, the call changes data. You can repeat it with the same file.
    /// The image must be PNG or JPEG and fit the image size limit.
    /// The call reports every error in the body, with the status 200.
    /// It sets `success` to false and puts the reason in `message`.
    /// This covers a missing file, a wrong format, and a too large image.
    /// So check `success`, not the status.
    /// </remarks>
    /// <summary>
    /// Upload a user photo
    /// </summary>
    /// <path>api/2.0/people/{userId}/photo</path>
    [RaisesWebhook(WebhookTrigger.UserUpdated, When = "only when `autosave` is true and the image is saved to the profile")]
    [Tags("People / Photos")]
    [SwaggerResponse(200, "The upload result: on success the photo URLs or the temporary file name in data, and on failure success set to false with the reason in message", typeof(FileUploadResultDto))]
    [HttpPost("{userId}/photo")]
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
