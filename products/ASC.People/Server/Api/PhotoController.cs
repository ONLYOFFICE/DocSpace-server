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

using UnknownImageFormatException = ASC.Web.Core.Users.UnknownImageFormatException;

namespace ASC.People.Api;

public class PhotoController(UserManager userManager,
        PermissionContext permissionContext,
        ApiContext apiContext,
        UserPhotoManager userPhotoManager,
        MessageService messageService,
        MessageTarget messageTarget,
        DisplayUserSettingsHelper displayUserSettingsHelper,
        SecurityContext securityContext,
        SettingsManager settingsManager,
        FileSizeComment fileSizeComment,
        SetupInfo setupInfo,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        TenantManager tenantManager)
    : PeopleControllerBase(userManager, permissionContext, apiContext, userPhotoManager, httpClientFactory, httpContextAccessor)
{
    /// <summary>
    /// Creates photo thumbnails by coordinates of the original image specified in the request.
    /// </summary>
    /// <short>
    /// Create photo thumbnails
    /// </short>
    /// <category>Photos</category>
    /// <param type="System.String, System" method="url" name="userid">User ID</param>
    /// <param type="ASC.People.ApiModels.RequestDto.ThumbnailsRequestDto, ASC.People" name="inDto">Thumbnail request parameters</param>
    /// <path>api/2.0/people/{userid}/photo/thumbnails</path>
    /// <httpMethod>POST</httpMethod>
    /// <returns type="ASC.People.ApiModels.ResponseDto.ThumbnailsDataDto, ASC.People">Thumbnail parameters</returns>
    [HttpPost("{userid}/photo/thumbnails")]
    public async Task<ThumbnailsDataDto> CreateMemberPhotoThumbnails(string userid, ThumbnailsRequestDto inDto)
    {
        var user = await GetUserInfoAsync(userid);

        if (_userManager.IsSystemUser(user.Id))
        {
            throw new SecurityException();
        }

        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser);

        if (!string.IsNullOrEmpty(inDto.TmpFile))
        {
            var fileName = Path.GetFileName(inDto.TmpFile);
            var data = await _userPhotoManager.GetTempPhotoData(fileName);

            UserPhotoThumbnailSettings settings;

            if (inDto.Width == 0 && inDto.Height == 0)
            {
                using var img = Image.Load(data);
                settings = new UserPhotoThumbnailSettings(inDto.X, inDto.Y, img.Width, img.Height);
            }
            else
            {
                settings = new UserPhotoThumbnailSettings(inDto.X, inDto.Y, inDto.Width, inDto.Height);
            }

            await settingsManager.SaveAsync(settings, user.Id);

            await _userPhotoManager.RemovePhotoAsync(user.Id);
            await _userPhotoManager.SaveOrUpdatePhoto(user.Id, data);
            await _userPhotoManager.RemoveTempPhotoAsync(fileName);
        }
        else
        {
            await UserPhotoThumbnailManager.SaveThumbnails(_userPhotoManager, settingsManager, inDto.X, inDto.Y, inDto.Width, inDto.Height, user.Id);
        }

        await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);
        await messageService.SendAsync(MessageAction.UserUpdatedAvatarThumbnails, messageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper));
        return await ThumbnailsDataDto.Create(user, _userPhotoManager);
    }

    /// <summary>
    /// Deletes a photo of the user with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Delete a user photo
    /// </short>
    /// <category>Photos</category>
    /// <param type="System.String, System" method="url" name="userid">User ID</param>
    /// <returns type="ASC.People.ApiModels.ResponseDto.ThumbnailsDataDto, ASC.People">Thumbnail parameters: original photo, retina, maximum size photo, big, medium, small</returns>
    /// <path>api/2.0/people/{userid}/photo</path>
    /// <httpMethod>DELETE</httpMethod>
    [HttpDelete("{userid}/photo")]
    public async Task<ThumbnailsDataDto> DeleteMemberPhotoAsync(string userid)
    {
        var user = await GetUserInfoAsync(userid);

        if (_userManager.IsSystemUser(user.Id))
        {
            throw new SecurityException();
        }

        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser);

        var tenant = await tenantManager.GetCurrentTenantAsync();
        if (user.IsOwner(tenant) && await _userManager.IsDocSpaceAdminAsync(user.Id) && user.Id != securityContext.CurrentAccount.ID)
        {
            throw new Exception(Resource.ErrorAccessDenied);
        }

        await _userPhotoManager.RemovePhotoAsync(user.Id);
        await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);
        await messageService.SendAsync(MessageAction.UserDeletedAvatar, messageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper));

        return await ThumbnailsDataDto.Create(user, _userPhotoManager);
    }

    /// <summary>
    /// Returns a photo of the user with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Get a user photo
    /// </short>
    /// <category>Photos</category>
    /// <param type="System.String, System" method="url" name="userid">User ID</param>
    /// <returns type="ASC.People.ApiModels.ResponseDto.ThumbnailsDataDto, ASC.People">Thumbnail parameters: original photo, retina, maximum size photo, big, medium, small</returns>
    /// <path>api/2.0/people/{userid}/photo</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("{userid}/photo")]
    public async Task<ThumbnailsDataDto> GetMemberPhoto(string userid)
    {
        var user = await GetUserInfoAsync(userid);

        if (_userManager.IsSystemUser(user.Id))
        {
            throw new SecurityException();
        }

        return await ThumbnailsDataDto.Create(user, _userPhotoManager);
    }

    /// <summary>
    /// Updates a photo of the user with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Update a user photo
    /// </short>
    /// <category>Photos</category>
    /// <param type="System.String, System" method="url" name="userid">User ID</param>
    /// <param type="ASC.People.ApiModels.RequestDto.UpdateMemberRequestDto, ASC.People" name="inDto">Request parameters for updating user photo</param>
    /// <returns type="ASC.People.ApiModels.ResponseDto.ThumbnailsDataDto, ASC.People">Updated thumbnail parameters: original photo, retina, maximum size photo, big, medium, small</returns>
    /// <path>api/2.0/people/{userid}/photo</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{userid}/photo")]
    public async Task<ThumbnailsDataDto> UpdateMemberPhoto(string userid, UpdateMemberRequestDto inDto)
    {
        var user = await GetUserInfoAsync(userid);

        if (_userManager.IsSystemUser(user.Id))
        {
            throw new SecurityException();
        }

        var tenant = await tenantManager.GetCurrentTenantAsync();
        if (user.IsOwner(tenant) && await _userManager.IsDocSpaceAdminAsync(user.Id) && user.Id != securityContext.CurrentAccount.ID)
        {
            throw new Exception(Resource.ErrorAccessDenied);
        }

        if (inDto.Files != await _userPhotoManager.GetPhotoAbsoluteWebPath(user.Id))
        {
            await UpdatePhotoUrlAsync(inDto.Files, user);
        }

        await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);
        await messageService.SendAsync(MessageAction.UserAddedAvatar, messageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper));

        return await ThumbnailsDataDto.Create(user, _userPhotoManager);
    }

    /// <summary>
    /// Uploads a photo of the user with the ID specified in the request.
    /// </summary>
    /// <short>
    /// Upload a user photo
    /// </short>
    /// <category>Photos</category>
    /// <param type="System.String, System" method="url" name="userid">User ID</param>
    /// <param type="Microsoft.AspNetCore.Http.IFormCollection, Microsoft.AspNetCore.Http" name="formCollection">Image data</param>
    /// <path>api/2.0/people/{userid}/photo</path>
    /// <httpMethod>POST</httpMethod>
    /// <returns type="ASC.People.ApiModels.ResponseDto.FileUploadResultDto, ASC.People">Result of file uploading</returns>
    [HttpPost("{userid}/photo")]
    public async Task<FileUploadResultDto> UploadMemberPhoto(string userid, IFormCollection formCollection)
    {
        var result = new FileUploadResultDto();
        var autosave = bool.Parse(formCollection["Autosave"]);

        try
        {
            if (formCollection.Files.Count != 0)
            {
                Guid userId;
                try
                {
                    userId = new Guid(userid);
                }
                catch
                {
                    userId = securityContext.CurrentAccount.ID;
                }

                await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(userId), Constants.Action_EditUser);

                var tenant = await tenantManager.GetCurrentTenantAsync();
                if (securityContext.CurrentAccount.ID != tenant.OwnerId && await _userManager.IsDocSpaceAdminAsync(userId) && userId != securityContext.CurrentAccount.ID)
                {
                    throw new Exception(Resource.ErrorAccessDenied);
                }

                var userPhoto = formCollection.Files[0];

                if (userPhoto.Length > setupInfo.MaxImageUploadSize)
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
                    if (data.Length > setupInfo.MaxImageUploadSize)
                    {
                        throw new ImageSizeLimitException();
                    }

                    var mainPhoto = await _userPhotoManager.SaveOrUpdatePhoto(userId, data);
                    var userInfo = await _userManager.GetUsersAsync(userId);
                    var cacheKey = Math.Abs(userInfo.LastModified.GetHashCode());

                    result.Data =
                        new
                        {
                            main = mainPhoto.Item1 + $"?hash={cacheKey}",
                            retina = await _userPhotoManager.GetRetinaPhotoURL(userId) + $"?hash={cacheKey}",
                            max = await _userPhotoManager.GetMaxPhotoURL(userId) + $"?hash={cacheKey}",
                            big = await _userPhotoManager.GetBigPhotoURL(userId) + $"?hash={cacheKey}",
                            medium = await _userPhotoManager.GetMediumPhotoURLAsync(userId) + $"?hash={cacheKey}",
                            small = await _userPhotoManager.GetSmallPhotoURLAsync(userId) + $"?hash={cacheKey}"
                        };
                }
                else
                {
                    result.Data = await _userPhotoManager.SaveTempPhoto(data, setupInfo.MaxImageUploadSize, UserPhotoManager.OriginalFotoSize.Width, UserPhotoManager.OriginalFotoSize.Height);
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
        IImageFormat imgFormat;
        try
        {
            using var img = Image.Load(data);
            imgFormat = img.Metadata.DecodedImageFormat;
        }
        catch (OutOfMemoryException)
        {
            throw new ImageSizeLimitException();
        }
        catch (ArgumentException error)
        {
            throw new UnknownImageFormatException(error);
        }

        if (imgFormat.Name != "PNG" && imgFormat.Name != "JPEG")
        {
            throw new UnknownImageFormatException();
        }
    }
}
