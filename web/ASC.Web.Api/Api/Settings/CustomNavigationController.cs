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

namespace ASC.Web.Api.Controllers.Settings;

[DefaultRoute("customnavigation")]
public class CustomNavigationController(MessageService messageService,
        ApiContext apiContext,
        PermissionContext permissionContext,
        SettingsManager settingsManager,
        WebItemManager webItemManager,
        StorageHelper storageHelper,
        IMemoryCache memoryCache,
        IHttpContextAccessor httpContextAccessor)
    : BaseSettingsController(apiContext, memoryCache, webItemManager, httpContextAccessor)
{
    /// <summary>
    /// Returns a list of the custom navigation items.
    /// </summary>
    /// <short>Get the custom navigation items</short>
    /// <path>api/2.0/settings/customnavigation/getall</path>
    /// <collection>list</collection>
    [Tags("Settings / Custom Navigation")]
    [SwaggerResponse(200, "List of the custom navigation items", typeof(List<CustomNavigationItem>))]
    [HttpGet("getall")]
    public async Task<List<CustomNavigationItem>> GetCustomNavigationItemsAsync()
    {
        return (await settingsManager.LoadAsync<CustomNavigationSettings>()).Items;
    }

    /// <summary>
    /// Returns a custom navigation item sample.
    /// </summary>
    /// <short>Get a custom navigation item sample</short>
    /// <path>api/2.0/settings/customnavigation/getsample</path>
    [Tags("Settings / Custom Navigation")]
    [SwaggerResponse(200, "Custom navigation item", typeof(CustomNavigationItem))]
    [HttpGet("getsample")]
    public CustomNavigationItem GetCustomNavigationItemSample()
    {
        return CustomNavigationItem.GetSample();
    }

    /// <summary>
    /// Returns a custom navigation item by the ID specified in the request.
    /// </summary>
    /// <short>Get a custom navigation item by ID</short>
    /// <path>api/2.0/settings/customnavigation/get/{id}</path>
    [Tags("Settings / Custom Navigation")]
    [SwaggerResponse(200, "Custom navigation item", typeof(CustomNavigationItem))]
    [HttpGet("get/{id:guid}")]
    public async Task<CustomNavigationItem> GetCustomNavigationItemAsync(IdRequestDto<Guid> inDto)
    {
        return (await settingsManager.LoadAsync<CustomNavigationSettings>()).Items.Find(item => item.Id == inDto.Id);
    }

    /// <summary>
    /// Adds a custom navigation item with the parameters specified in the request.
    /// </summary>
    /// <short>Add a custom navigation item</short>
    /// <path>api/2.0/settings/customnavigation/create</path>
    [Tags("Settings / Custom Navigation")]
    [SwaggerResponse(200, "Custom navigation item", typeof(CustomNavigationItem))]
    [HttpPost("create")]
    public async Task<CustomNavigationItem> CreateCustomNavigationItem(CustomNavigationItem inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = await settingsManager.LoadAsync<CustomNavigationSettings>();

        var exist = false;

        foreach (var existItem in settings.Items)
        {
            if (existItem.Id != inDto.Id)
            {
                continue;
            }

            existItem.Label = inDto.Label;
            existItem.Url = inDto.Url;
            existItem.ShowInMenu = inDto.ShowInMenu;
            existItem.ShowOnHomePage = inDto.ShowOnHomePage;

            if (existItem.SmallImg != inDto.SmallImg)
            {
                await storageHelper.DeleteLogoAsync(existItem.SmallImg);
                existItem.SmallImg = await storageHelper.SaveTmpLogo(inDto.SmallImg);
            }

            if (existItem.BigImg != inDto.BigImg)
            {
                await storageHelper.DeleteLogoAsync(existItem.BigImg);
                existItem.BigImg = await storageHelper.SaveTmpLogo(inDto.BigImg);
            }

            exist = true;
            break;
        }

        if (!exist)
        {
            inDto.Id = Guid.NewGuid();
            inDto.SmallImg = await storageHelper.SaveTmpLogo(inDto.SmallImg);
            inDto.BigImg = await storageHelper.SaveTmpLogo(inDto.BigImg);

            settings.Items.Add(inDto);
        }

        await settingsManager.SaveAsync(settings);

        messageService.Send(MessageAction.CustomNavigationSettingsUpdated);

        return inDto;
    }

    /// <summary>
    /// Deletes a custom navigation item with the ID specified in the request.
    /// </summary>
    /// <short>Delete a custom navigation item</short>
    /// <path>api/2.0/settings/customnavigation/delete/{id}</path>
    [Tags("Settings / Custom Navigation")]
    [HttpDelete("delete/{id:guid}")]
    public async Task DeleteCustomNavigationItem(IdRequestDto<Guid> inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = await settingsManager.LoadAsync<CustomNavigationSettings>();

        var target = settings.Items.Find(item => item.Id == inDto.Id);

        if (target == null)
        {
            return;
        }

        await storageHelper.DeleteLogoAsync(target.SmallImg);
        await storageHelper.DeleteLogoAsync(target.BigImg);

        settings.Items.Remove(target);
        await settingsManager.SaveAsync(settings);

        messageService.Send(MessageAction.CustomNavigationSettingsUpdated);
    }
}
