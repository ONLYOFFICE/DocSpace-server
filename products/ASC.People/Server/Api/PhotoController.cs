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
    /// Crops the avatar of a profile to the rectangle given in the request and rebuilds all of its thumbnail sizes,
    /// which is the second step of changing an avatar by hand.
    /// It works in two modes: with `tmpFile` it takes the temporary image
    /// `POST api/2.0/people/{userid}/photo` produced with `autosave` off, makes the cropped result the main photo and
    /// then discards the temporary file, and without `tmpFile` it re-crops the photo the profile already has.
    /// A caller may only do this to their own profile - the ID in the route has to be the calling account, and an
    /// administrator gets 403 for anybody else - and the account must be allowed to edit its own profile.
    /// The call replaces the stored photo, so the previous crop is lost, and it can be repeated with new coordinates
    /// as often as needed.
    /// Passing `width` and `height` as 0 together with `tmpFile` keeps the whole uploaded image instead of cropping
    /// it.
    /// The answer holds the URLs of every generated size, the same shape `GET api/2.0/people/{userid}/photo`
    /// returns.
    /// </remarks>
    /// <summary>
    /// Create photo thumbnails
    /// </summary>
    /// <path>api/2.0/people/{userid}/photo/thumbnails</path>
    [Tags("People / Photos")]
    [SwaggerResponse(200, "The URLs of the rebuilt photo sizes", typeof(ThumbnailsDataDto))]
    [SwaggerResponse(403, "The ID in the route is not the calling account, or the account may not edit its own profile")]
    [SwaggerResponse(404, "No user has the specified ID")]
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
    /// Removes the avatar of a profile, so that the profile falls back to the default placeholder image.
    /// A caller may only do this to their own profile - the ID in the route has to be the calling account, and an
    /// administrator gets 403 for anybody else - and the account must be allowed to edit its own profile.
    /// The removal is permanent and cannot be undone: the stored image and all of its sizes are deleted, and a new
    /// avatar has to be uploaded through `POST api/2.0/people/{userid}/photo` to replace it.
    /// The call is idempotent, so removing an avatar from a profile that has none succeeds as well, and it raises a
    /// `UserUpdated` webhook.
    /// The answer still holds the URLs of every size, now pointing at the default image.
    /// </remarks>
    /// <summary>
    /// Delete a user photo
    /// </summary>
    /// <path>api/2.0/people/{userid}/photo</path>
    [Tags("People / Photos")]
    [SwaggerResponse(200, "The URLs of every photo size, now pointing at the default image", typeof(ThumbnailsDataDto))]
    [SwaggerResponse(403, "The ID in the route is not the calling account, or the account may not edit its own profile")]
    [SwaggerResponse(404, "No user has the specified ID")]
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
    /// Returns the URLs of the avatar of a profile in every size the portal keeps: the original, the retina and the
    /// maximum variants, and the big, medium and small thumbnails.
    /// Unlike the operations that change an avatar, this one may be called for another account, as long as the
    /// caller is allowed to see that account - a guest, for instance, only sees the accounts it is related to.
    /// The call is read-only and always answers with a full set of URLs: a profile that has no avatar of its own
    /// gets the URLs of the default placeholder image rather than an empty answer.
    /// The URLs are portal paths meant to be requested directly and may be replaced when the avatar changes, so they
    /// should not be stored for a long time.
    /// To change the avatar use `POST api/2.0/people/{userid}/photo` for an uploaded file,
    /// `PUT api/2.0/people/{userid}/photo` for one taken from a URL, and
    /// `DELETE api/2.0/people/{userid}/photo` to drop it.
    /// </remarks>
    /// <summary>
    /// Get a user photo
    /// </summary>
    /// <path>api/2.0/people/{userid}/photo</path>
    [Tags("People / Photos")]
    [SwaggerResponse(200, "The URLs of the photo in every size, or of the default image when the profile has no photo", typeof(ThumbnailsDataDto))]
    [SwaggerResponse(403, "The caller is not allowed to see the requested account")]
    [SwaggerResponse(404, "No user has the specified ID")]
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
    /// Sets the avatar of a profile from an image the portal downloads itself from the URL given in `files`, which is
    /// the way to reuse a picture that is already published somewhere.
    /// A caller may only do this to their own profile - the ID in the route has to be the calling account, and an
    /// administrator gets 403 for anybody else - and the account must be allowed to edit its own profile.
    /// The URL has to be absolute or relative to the portal, and it has to use HTTPS unless the request itself came
    /// over HTTP; an address the portal refuses to fetch, and a download that does not succeed, both answer 403.
    /// Passing the URL the profile already uses is a no-op, and an empty `files` is rejected with 400, so use
    /// `DELETE api/2.0/people/{userid}/photo` to remove an avatar rather than sending an empty value.
    /// The downloaded image replaces the stored avatar and all of its sizes at once, raises a `UserUpdated` webhook,
    /// and is subject to the portal limit on image size.
    /// To send the bytes instead of a URL, upload the file through `POST api/2.0/people/{userid}/photo`.
    /// </remarks>
    /// <summary>
    /// Update a user photo
    /// </summary>
    /// <path>api/2.0/people/{userid}/photo</path>
    [Tags("People / Photos")]
    [SwaggerResponse(200, "The URLs of the photo sizes built from the downloaded image", typeof(ThumbnailsDataDto))]
    [SwaggerResponse(400, "The files field is empty")]
    [SwaggerResponse(403, "The ID in the route is not the calling account, the account may not edit its own profile, or the URL was refused or could not be downloaded")]
    [SwaggerResponse(404, "No user has the specified ID")]
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
    /// Uploads an image as multipart form data and either makes it the avatar of a profile straight away or keeps it
    /// as a temporary file to be cropped afterwards.
    /// With `autosave` set to true the image becomes the avatar immediately, all of its sizes are built and their
    /// URLs come back in `data`, each with a `hash` query parameter that changes whenever the avatar does, so a
    /// client can cache them safely.
    /// With `autosave` left false the image is only stored as a temporary file and `data` holds its name, which has
    /// to be passed as `tmpFile` to `POST api/2.0/people/{userid}/photo/thumbnails` to choose the crop; nothing
    /// changes on the profile until that second call succeeds.
    /// A caller may only do this to their own profile, the ID in the route has to be the calling account, and the
    /// image has to be a format the portal can read and stay within the portal limit on image size.
    /// This operation reports every problem in the body instead of as a status code: it answers 200 with `success`
    /// set to false and a human-readable `message`, and it does so for a missing file, an unreadable format, an
    /// oversized image and a rejected permission alike, so a client has to check `success` and must not rely on the
    /// status alone.
    /// A successful upload raises a `UserUpdated` webhook only in the `autosave` case.
    /// </remarks>
    /// <summary>
    /// Upload a user photo
    /// </summary>
    /// <path>api/2.0/people/{userid}/photo</path>
    [Tags("People / Photos")]
    [SwaggerResponse(200, "The upload result: on success the photo URLs or the temporary file name in data, and on failure success set to false with the reason in message", typeof(FileUploadResultDto))]
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
