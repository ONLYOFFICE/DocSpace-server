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

namespace ASC.Web.Api.Models;

/// <summary>
/// The user parameters.
/// </summary>
public class EmployeeDto
{
    /// <summary>
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The HTML-encoded user's display name formatted according to the default format for the current culture.
    /// </summary>
    /// <example>Mike Zanyatski</example>
    public string DisplayName { get; set; }

    // /// <summary>
    // /// The user title.
    // /// </summary>
    // /// <example>Manager</example>
    // public string Title { get; set; }

    /// <summary>
    /// The user avatar.
    /// </summary>
    /// <example>https://example.com/avatar.jpg</example>
    public string Avatar { get; set; }

    /// <summary>
    /// The user original size avatar.
    /// </summary>
    /// <example>https://example.com/avatar_original.jpg</example>
    public string AvatarOriginal { get; set; }

    /// <summary>
    /// The user maximum size avatar.
    /// </summary>
    /// <example>https://example.com/avatar_max.jpg</example>
    public string AvatarMax { get; set; }

    /// <summary>
    /// The user medium size avatar.
    /// </summary>
    /// <example>https://example.com/avatar_medium.jpg</example>
    public string AvatarMedium { get; set; }

    /// <summary>
    /// The user small size avatar.
    /// </summary>
    /// <example>https://example.com/avatar_small.jpg</example>
    public string AvatarSmall { get; set; }

    /// <summary>
    /// The user profile URL.
    /// </summary>
    /// <example>https://example.com/profile/user123</example>
    public string ProfileUrl { get; set; }

    /// <summary>
    /// Specifies if the user has an avatar or not.
    /// </summary>
    /// <example>true</example>
    public bool HasAvatar { get; set; }

    /// <summary>
    /// Specifies if the user is anonymous or not.
    /// </summary>
    /// <example>false</example>
    public bool IsAnonim { get; set; }

    [JsonIgnore]
    public static EmployeeDto Default => new()
    {
        Id = Guid.Empty,
        DisplayName = string.Empty,
        //Title = string.Empty,
        Avatar = string.Empty,
        AvatarOriginal = string.Empty,
        AvatarMax = string.Empty,
        AvatarMedium = string.Empty,
        AvatarSmall = string.Empty,
        ProfileUrl = string.Empty,
        HasAvatar = false,
    };
}

[Scope]
public class EmployeeDtoHelper(
    DisplayUserSettingsHelper displayUserSettingsHelper,
    UserPhotoManager userPhotoManager,
    UserFieldsHelper userFieldsHelper,
    UserManager userManager,
    AuthContext authContext,
    ILogger<EmployeeDtoHelper> logger)
{
    private readonly ConcurrentDictionary<Guid, EmployeeDto> _dictionary = new();
    protected readonly UserPhotoManager _userPhotoManager = userPhotoManager;
    protected readonly UserManager _userManager = userManager;
    protected readonly AuthContext _authContext = authContext;
    protected readonly DisplayUserSettingsHelper _displayUserSettingsHelper = displayUserSettingsHelper;
    protected readonly UserFieldsHelper _userFieldsHelper = userFieldsHelper;

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
            if (_dictionary.TryGetValue(userId, out var employee))
            {
                return employee;
            }

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
        result.DisplayName = _displayUserSettingsHelper.GetFullUserName(userInfo);
        result.HasAvatar = await _userPhotoManager.UserHasAvatar(userInfo.Id);
        result.IsAnonim = userInfo.Id.Equals(ASC.Core.Configuration.Constants.Guest.ID);

        // if (!string.IsNullOrEmpty(userInfo.Title))
        // {
        //     result.Title = userInfo.Title;
        // }

        var avatars = await _userFieldsHelper.GetAvatarUrlsAsync(userInfo, absolute: false);

        result.AvatarSmall = avatars.AvatarSmall;
        result.AvatarOriginal = avatars.AvatarOriginal;
        result.AvatarMax = avatars.AvatarMax;
        result.AvatarMedium = avatars.AvatarMedium;
        result.Avatar = avatars.Avatar;

        // The profile link is a people search by email, so it would hand a User or a Guest the email of someone
        // else (bug 81093); neither of them has access to the contacts anyway.
        if (!await IsWithoutContactsAccessAsync(userInfo.Id))
        {
            result.ProfileUrl = await _userFieldsHelper.GetProfileUrlAsync(userInfo.Id);
        }

        return result;
    }

    /// <summary>
    /// Whether the current account may not see the contacts of the user: a User or a Guest looking at someone else.
    /// </summary>
    protected async Task<bool> IsWithoutContactsAccessAsync(Guid userId)
    {
        var currentId = _authContext.CurrentAccount.ID;

        return currentId != userId && await _userManager.IsWithoutContactsAccessAsync(currentId);
    }
}
