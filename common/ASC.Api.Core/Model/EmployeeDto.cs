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

using System.Collections.Concurrent;

namespace ASC.Web.Api.Models;

/// <summary>
/// </summary>
public class EmployeeDto
{
    /// <summary>ID</summary>
    /// <type>System.Guid, System</type>
    public Guid Id { get; set; }

    /// <summary>Display name</summary>
    /// <type>System.String, System</type>
    public string DisplayName { get; set; }

    /// <summary>Title</summary>
    /// <type>System.String, System</type>
    public string Title { get; set; }

    /// <summary>Small avatar</summary>
    /// <type>System.String, System</type>
    public string AvatarSmall { get; set; }

    /// <summary>Profile URL</summary>
    /// <type>System.String, System</type>
    public string ProfileUrl { get; set; }

    /// <summary>Specifies if the user has an avatar or not</summary>
    /// <type>System.Boolean, System</type>
    public bool HasAvatar { get; set; }

    public static EmployeeDto GetSample()
    {
        return new EmployeeDto
        {
            Id = Guid.Empty,
            DisplayName = "Mike Zanyatski",
            Title = "Manager",
            AvatarSmall = "url to small avatar"
        };
    }
}

[Scope]
public class EmployeeDtoHelper(
    ApiContext httpContext,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    UserPhotoManager userPhotoManager,
    CommonLinkUtility commonLinkUtility,
    UserManager userManager,
    AuthContext authContext,
    ILogger<EmployeeDtoHelper> logger)
{
    private readonly ConcurrentDictionary<Guid, EmployeeDto> _dictionary = new();
    protected readonly ApiContext _httpContext = httpContext;
    protected  readonly UserPhotoManager _userPhotoManager = userPhotoManager;
    protected  readonly UserManager _userManager = userManager;
    protected  readonly AuthContext _authContext = authContext;
    
    public async Task<EmployeeDto> GetAsync(UserInfo userInfo)
    {
        if (!_dictionary.TryGetValue(userInfo.Id, out var employee))
        {
            employee = await InitAsync(new EmployeeDto(), userInfo);

            _dictionary.AddOrUpdate(userInfo.Id, _ => employee, (_, _) => employee);

        }
        
        return employee;
    }

    public async Task<EmployeeDto> GetAsync(Guid userId)
    {
        try
        {
            return await GetAsync(await _userManager.GetUsersAsync(userId));
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            return await GetAsync(Constants.LostUser);
        }
    }

    protected async Task<EmployeeDto> InitAsync(EmployeeDto result, UserInfo userInfo)
    {
        result.Id = userInfo.Id;
        result.DisplayName = displayUserSettingsHelper.GetFullUserName(userInfo);
        result.HasAvatar = await _userPhotoManager.UserHasAvatar(userInfo.Id);

        if (!string.IsNullOrEmpty(userInfo.Title))
        {
            result.Title = userInfo.Title;
        }

        var cacheKey = Math.Abs(userInfo.LastModified.GetHashCode());

        if (_httpContext.Check("avatarSmall"))
        {
            result.AvatarSmall = await _userPhotoManager.GetSmallPhotoURLAsync(userInfo.Id) + $"?hash={cacheKey}";
        }

        if (result.Id != Guid.Empty)
        {
            var profileUrl = await commonLinkUtility.GetUserProfileAsync(userInfo.Id, false);
            result.ProfileUrl = commonLinkUtility.GetFullAbsolutePath(profileUrl);
        }

        return result;
    }
}