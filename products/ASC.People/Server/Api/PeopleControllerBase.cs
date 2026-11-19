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

namespace ASC.People.Api;

///<summary>
/// People API.
///</summary>
public abstract class PeopleControllerBase(
    UserManager userManager,
    PermissionContext permissionContext,
    ApiContext apiContext,
    UserPhotoManager userPhotoManager,
    IHttpContextAccessor httpContextAccessor,
    IUrlValidator urlValidator,
    SetupInfo setupInfo,
    IHttpClientFactory httpClientFactory)
    : ApiControllerBase
{
    protected readonly UserManager _userManager = userManager;
    protected readonly PermissionContext _permissionContext = permissionContext;
    protected readonly ApiContext _apiContext = apiContext;
    protected readonly UserPhotoManager _userPhotoManager = userPhotoManager;
    protected readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    protected readonly SetupInfo _setupInfo = setupInfo;

    protected async Task<UserInfo> GetUserInfoAsync(string userNameOrId)
    {
        UserInfo user;
        try
        {
            var userId = new Guid(userNameOrId);
            user = await _userManager.GetUsersAsync(userId);
        }
        catch (FormatException)
        {
            user = await _userManager.GetUserByUserNameAsync(userNameOrId);
        }

        if (user == null || user.Id == Constants.LostUser.Id)
        {
            throw new ItemNotFoundException(Resource.ErrorUserNotFound);
        }

        return user;
    }

    protected async Task UpdateContactsAsync(IEnumerable<Contact> contacts, UserInfo user, bool checkPermissions = true)
    {
        if (checkPermissions)
        {
            await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser);
        }

        if (contacts == null)
        {
            return;
        }

        var values = contacts.Where(r => !string.IsNullOrEmpty(r.Value)).Select(r => $"{r.Type}|{r.Value}");
        user.Contacts = string.Join('|', values);
    }

    /// <summary>
    /// Validates photo URL against SSRF attacks. Does NOT download the file.
    /// </summary>
    /// <param name="url">The URL to validate</param>
    /// <returns>Validation result containing the parsed URI and resolved addresses, or null if URL is empty</returns>
    protected async Task<UrlValidationResult> ValidatePhotoUrlAsync(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            _httpContextAccessor.HttpContext != null)
        {
            var baseUri = new Uri(_httpContextAccessor.HttpContext.Request.GetDisplayUrl());
            url = baseUri.GetLeftPart(UriPartial.Authority) + "/" + url.TrimStart('/');
        }

        // Require HTTPS unless the current request itself is HTTP (same-origin HTTP allowed)
        var currentScheme = _httpContextAccessor.HttpContext?.Request.Scheme;
        var requireHttps = !string.Equals(currentScheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);

        var validationResult = await urlValidator.ValidateAsync(url, new UrlValidationOptions
        {
            RequireHttps = requireHttps
        });

        if (!validationResult.IsValid)
        {
            throw new SecurityException($"Photo URL validation failed: {validationResult.ErrorMessage}");
        }

        return validationResult;
    }

    /// <summary>
    /// Downloads photo from a validated URL and saves it.
    /// Uses the already-resolved IP addresses from <paramref name="photoValidation"/> to pin the TCP
    /// connection, preventing DNS rebinding between validation and download.
    /// </summary>
    /// <param name="photoValidation">The validation result containing the URI and resolved addresses</param>
    /// <param name="user">The user to update photo for</param>
    protected async Task DownloadAndSavePhotoAsync(UrlValidationResult photoValidation, UserInfo user)
    {
        if (photoValidation == null)
        {
            throw new ArgumentNullException(nameof(photoValidation));
        }

        await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser);

        // Each failure below used to leave as an exception that nothing maps (500), except an error answer, which was
        // InvalidOperationException (403) and stays so. The address that did not answer or timed out (the pinned
        // client allows 10 seconds) is a remote failure: 502, as for a Document Server that does not answer. What it
        // returned is the caller's input: 400 with the texts the photo upload of PhotoController gives.
        var httpClient = httpClientFactory.CreateClient(UrlValidator.PinnedHttpClient);
        using var request = new HttpRequestMessage(HttpMethod.Get, photoValidation.ParsedUri);
        request.Options.Set(UrlValidator.PinnedIpKey, photoValidation.ResolvedAddresses[0]);

        byte[] imageByteArray;
        try
        {
            using var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to download photo: {response.StatusCode}");
            }

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue && contentLength.Value > _setupInfo.MaxImageUploadSize)
            {
                throw new ArgumentException(PeopleResource.ErrorImageSizetLimit);
            }

            imageByteArray = await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            throw new CustomHttpException(HttpStatusCode.BadGateway, $"Failed to download photo: {e.Message}");
        }

        if (imageByteArray.Length > _setupInfo.MaxImageUploadSize)
        {
            throw new ArgumentException(PeopleResource.ErrorImageSizetLimit);
        }

        try
        {
            await _userPhotoManager.SaveOrUpdatePhoto(user.Id, imageByteArray);
        }
        catch (UnknownImageFormatException e)
        {
            throw new ArgumentException(PeopleResource.ErrorUnknownFileImageType, e);
        }
        catch (ImageWeightLimitException e)
        {
            throw new ArgumentException(PeopleResource.ErrorImageWeightLimit, e);
        }
        catch (ImageSizeLimitException e)
        {
            throw new ArgumentException(PeopleResource.ErrorImageSizetLimit, e);
        }
        // UserPhotoManager turns only an ArgumentException of ImageMagick into UnknownImageFormatException, but
        // ImageMagick reports content it cannot read with its own exceptions (a page that is not an image gives
        // MagickMissingDelegateErrorException), which left as a 500. They are the caller's input too: a resource
        // limit means the image is too large, anything else that it is not an image ImageMagick can read.
        catch (ImageMagick.MagickResourceLimitErrorException e)
        {
            throw new ArgumentException(PeopleResource.ErrorImageSizetLimit, e);
        }
        catch (ImageMagick.MagickException e)
        {
            throw new ArgumentException(PeopleResource.ErrorUnknownFileImageType, e);
        }
    }
}
