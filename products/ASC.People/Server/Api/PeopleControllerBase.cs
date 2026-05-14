// (c) Copyright Ascensio System SIA 2009-2026
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
    IUrlValidator urlValidator)
    : ApiControllerBase
{
    protected readonly UserManager _userManager = userManager;
    protected readonly PermissionContext _permissionContext = permissionContext;
    protected readonly ApiContext _apiContext = apiContext;
    protected readonly UserPhotoManager _userPhotoManager = userPhotoManager;
    protected readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    protected readonly IUrlValidator _urlValidator = urlValidator;

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

        var validationResult = await _urlValidator.ValidateAsync(url, new UrlValidationOptions
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

        var pinnedIp = photoValidation.ResolvedAddresses[0];
        var port = photoValidation.ParsedUri.Port;

        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            ConnectCallback = async (context, cancellationToken) =>
            {
                var socket = new Socket(pinnedIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.NoDelay = true;
                try
                {
                    await socket.ConnectAsync(new IPEndPoint(pinnedIp, port), cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };

        using var httpClient = new HttpClient(handler, disposeHandler: true);
        using var request = new HttpRequestMessage(HttpMethod.Get, photoValidation.ParsedUri);
        using var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to download photo: {response.StatusCode}");
        }

        var imageByteArray = await response.Content.ReadAsByteArrayAsync();

        await _userPhotoManager.SaveOrUpdatePhoto(user.Id, imageByteArray);
    }
}
