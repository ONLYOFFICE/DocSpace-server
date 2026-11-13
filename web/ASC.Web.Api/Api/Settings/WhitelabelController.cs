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

namespace ASC.Web.Api.Controllers.Settings;

public class WhitelabelController(
    PermissionContext permissionContext,
    SettingsManager settingsManager,
    WebItemManager webItemManager,
    TenantInfoSettingsHelper tenantInfoSettingsHelper,
    TenantWhiteLabelSettingsHelper tenantWhiteLabelSettingsHelper,
    IWhiteLabelLogoConverter logoConverter,
    TenantLogoManager tenantLogoManager,
    CoreBaseSettings coreBaseSettings,
    CommonLinkUtility commonLinkUtility,
    IFusionCache fusionCache,
    CompanyWhiteLabelSettingsHelper companyWhiteLabelSettingsHelper,
    TenantManager tenantManager,
    MessageService messageService,
    TenantExtra tenantExtra,
    StorageFactory storageFactory,
    AdditionalWhiteLabelSettingsMapper additionalWhiteLabelSettingsMapper,
    CompanyWhiteLabelSettingsDtoMapper companyWhiteLabelSettingsDtoMapper)
    : BaseSettingsController(fusionCache, webItemManager)
{
    #region Logos

    /// <remarks>
    /// Replaces logo images of the current portal. The portal shows them on the login page, in the menu, in editors,
    /// and in letters. Each `logo` entry names a slot by its `type` number from
    /// `GET api/2.0/settings/whitelabel/logos`. A slot left out of the request keeps its image. Only slots 1, 2, 6,
    /// 7, and 8 have a dark image. For other slots, the call ignores `dark`. Saving slot 2 also rebuilds the
    /// notification logo, slot 8, from the light image. The call reads only `logo` from the body. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. If the
    /// plan has no customization or the installation hides branding, the call returns 402. To check this first, use
    /// `GET api/2.0/settings/enablewhitelabel`. The call changes data and returns `true`. You can repeat it with
    /// base64 images. The call deletes a used temporary file, so a repeat must send that image again. To undo the
    /// call, use `PUT api/2.0/settings/whitelabel/logos/restore`. With `isDefault=true`, the call changes the
    /// installation-wide default logos instead. Only a self-hosted installation without limited space access allows
    /// this. Elsewhere the call returns 403. The call also returns 400 if `logoText` is too long, although it does
    /// not store the text. To upload image files, use `POST api/2.0/settings/whitelabel/logos/savefromfiles`.
    /// </remarks>
    /// <summary>Save the white label logos</summary>
    /// <path>api/2.0/settings/whitelabel/logos/save</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "Always `true` once the submitted logos have been stored for the portal", typeof(bool))]
    [SwaggerResponse(400, "The `logoText` is longer than 40 characters, although this method does not store it")]
    [SwaggerResponse(402, "The portal's pricing plan has no customization option, or the branding section is switched off in the installation")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the installation does not allow default branding to be edited")]
    [HttpPost("whitelabel/logos/save")]
    public async Task<bool> SaveWhiteLabelSettings(WhiteLabelRequestsDto inDto, [FromQuery] WhiteLabelQueryRequestsDto inQueryDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (inQueryDto is { IsDefault: not null } && inQueryDto.IsDefault.Value)
        {
            await DemandRebrandingPermissionAsync();

            await SaveWhiteLabelSettingsForDefaultTenantAsync(inDto);
        }
        else
        {
            await tenantLogoManager.DemandWhiteLabelPermissionAsync();

            await SaveWhiteLabelSettingsForCurrentTenantAsync(inDto);
        }

        return true;
    }

    private async Task SaveWhiteLabelSettingsForCurrentTenantAsync(WhiteLabelRequestsDto inDto)
    {
        var settings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        var tenant = tenantManager.GetCurrentTenant();

        await SaveWhiteLabelSettingsForTenantAsync(settings, null, tenant.Id, inDto);
    }

    private async Task SaveWhiteLabelSettingsForDefaultTenantAsync(WhiteLabelRequestsDto inDto)
    {
        var settings = await settingsManager.LoadForDefaultTenantAsync<TenantWhiteLabelSettings>();

        var storage = await storageFactory.GetStorageAsync(Tenant.DefaultTenant, "static_partnerdata");

        await SaveWhiteLabelSettingsForTenantAsync(settings, storage, Tenant.DefaultTenant, inDto);
    }

    private async Task SaveWhiteLabelSettingsForTenantAsync(TenantWhiteLabelSettings settings, IDataStore storage, int tenantId, WhiteLabelRequestsDto inDto)
    {
        if (inDto.Logo != null)
        {
            var logoDict = new Dictionary<int, KeyValuePair<string, string>>();

            foreach (var l in inDto.Logo)
            {
                var key = int.Parse(l.Key);

                logoDict.Add(key, new KeyValuePair<string, string>(l.Value.Light, l.Value.Dark));
            }

            await tenantWhiteLabelSettingsHelper.SetLogo(settings, logoConverter, logoDict, storage);
        }

        await settingsManager.SaveAsync(settings, tenantId);

        await tenantLogoManager.RemoveMailLogoDataFromCacheAsync();

        messageService.Send(MessageAction.WhiteLabelSettingsLogosUpdated);
    }

    /// <remarks>
    /// Replaces logo images of the current portal with files from a `multipart/form-data` request. The call ignores
    /// form field names and reads each file name instead. A file name starts with the slot `type` number from
    /// `GET api/2.0/settings/whitelabel/logos`. It ends with the image extension, as in `2.png`. A name that contains
    /// `dark`, as in `2.dark.png`, sets the dark image of the slot. Only slots 1, 2, 6, 7, and 8 have a dark image.
    /// For other slots, the call skips dark files. Slots without a file keep their image. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. A
    /// request without files also returns 403. If the plan has no customization or the installation hides branding,
    /// the call returns 402. The call changes data and returns `true`. You can repeat it with the same files. To undo
    /// the call, use `PUT api/2.0/settings/whitelabel/logos/restore`. With `isDefault=true`, the call changes the
    /// installation-wide default logos instead. Only a self-hosted installation without limited space access allows
    /// this. Elsewhere the call returns 403. To send images as base64, use
    /// `POST api/2.0/settings/whitelabel/logos/save`.
    /// </remarks>
    /// <summary>Save the logos from files</summary>
    /// <path>api/2.0/settings/whitelabel/logos/savefromfiles</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "Always `true` once the uploaded files have been stored as the portal logos", typeof(bool))]
    [SwaggerResponse(402, "The portal's pricing plan has no customization option, or the branding section is switched off in the installation")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, the request is not a form or carries no file, or the installation does not allow default branding to be edited")]
    [HttpPost("whitelabel/logos/savefromfiles")]
    public async Task<bool> SaveWhiteLabelSettingsFromFiles([FromQuery] WhiteLabelQueryRequestsDto inQueryDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (HttpContext.Request.Form.Files == null || HttpContext.Request.Form.Files.Count == 0)
        {
            throw new InvalidOperationException("No input files");
        }

        if (inQueryDto is { IsDefault: not null } && inQueryDto.IsDefault.Value)
        {
            await DemandRebrandingPermissionAsync();

            await SaveWhiteLabelSettingsFromFilesForDefaultTenantAsync();
        }
        else
        {
            await tenantLogoManager.DemandWhiteLabelPermissionAsync();

            await SaveWhiteLabelSettingsFromFilesForCurrentTenantAsync();
        }

        return true;
    }

    private async Task SaveWhiteLabelSettingsFromFilesForCurrentTenantAsync()
    {
        var settings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        var tenant = tenantManager.GetCurrentTenant();

        await SaveWhiteLabelSettingsFromFilesForTenantAsync(settings, null, tenant.Id);
    }

    private async Task SaveWhiteLabelSettingsFromFilesForDefaultTenantAsync()
    {
        var settings = await settingsManager.LoadForDefaultTenantAsync<TenantWhiteLabelSettings>();

        var storage = await storageFactory.GetStorageAsync(Tenant.DefaultTenant, "static_partnerdata");

        await SaveWhiteLabelSettingsFromFilesForTenantAsync(settings, storage, Tenant.DefaultTenant);
    }

    private async Task SaveWhiteLabelSettingsFromFilesForTenantAsync(TenantWhiteLabelSettings settings, IDataStore storage, int tenantId)
    {
        foreach (var f in HttpContext.Request.Form.Files)
        {
            if (f.FileName.Contains("dark"))
            {
                GetParts(f.FileName, out var logoType, out var fileExt);

                await tenantWhiteLabelSettingsHelper.SetLogoFromStream(settings, logoType, fileExt, f.OpenReadStream(), true, storage);
            }
            else
            {
                GetParts(f.FileName, out var logoType, out var fileExt);

                await tenantWhiteLabelSettingsHelper.SetLogoFromStream(settings, logoType, fileExt, f.OpenReadStream(), false, storage);
            }
        }

        await settingsManager.SaveAsync(settings, tenantId);

        await tenantLogoManager.RemoveMailLogoDataFromCacheAsync();

        messageService.Send(MessageAction.WhiteLabelSettingsLogosUpdated);
    }

    private void GetParts(string fileName, out WhiteLabelLogoType logoType, out string fileExt)
    {
        var parts = fileName.Split('.');
        logoType = (WhiteLabelLogoType)Convert.ToInt32(parts[0]);
        fileExt = parts[^1];
    }

    /// <remarks>
    /// Returns the logo slots of the current portal with the image URLs to show. Anyone can call it without signing in.
    /// The call takes the portal from the request address. The call is read-only. Each item has the slot number in
    /// `type`, its name, its image size, and the URLs in `path`. If `isDark` is `true`, the call fills only `dark`. If
    /// `isDark` is `false`, it fills only `light`. Without `isDark`, the call fills both URLs. Then `dark` is empty for
    /// a slot without a separate dark image. The list leaves out the notification slot 8, which only letters use. With
    /// `isDefault=true`, the call returns the installation-wide default logos instead. To learn which slots have no
    /// image of the portal's own, use `GET api/2.0/settings/whitelabel/logos/isdefault`.
    /// </remarks>
    /// <summary>Get the white label logos</summary>
    /// <path>api/2.0/settings/whitelabel/logos</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <collection>list</collection>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "The logo slots of the portal, each with its target size and the URLs of the light and dark images", typeof(IAsyncEnumerable<WhiteLabelItemDto>))]
    [AllowNotPayment, AllowAnonymous, AllowSuspended, AllowBlocked]
    [HttpGet("whitelabel/logos")]
    public async IAsyncEnumerable<WhiteLabelItemDto> GetWhiteLabelLogos([FromQuery] WhiteLabelQueryRequestsDto inQueryDto)
    {
        var isDefault = inQueryDto is { IsDefault: not null } && inQueryDto.IsDefault.Value;

        var tenantWhiteLabelSettings = isDefault ? null : await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        foreach (var logoType in Enum.GetValues<WhiteLabelLogoType>())
        {
            if (logoType == WhiteLabelLogoType.Notification)
            {
                continue;
            }

            var result = new WhiteLabelItemDto
            {
                Type = logoType,
                Name = logoType.ToStringFast(),
                Size = WhiteLabelItemSizeDto.FromGeometry(TenantWhiteLabelSettings.GetSize(logoType))
            };

            if (inQueryDto is { IsDark: not null })
            {
                var path = commonLinkUtility.GetFullAbsolutePath(isDefault
                    ? await tenantWhiteLabelSettingsHelper.GetAbsoluteDefaultLogoPathAsync(logoType, inQueryDto.IsDark.Value)
                    : await tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, logoType, inQueryDto.IsDark.Value));

                if (inQueryDto.IsDark.Value)
                {
                    result.Path = new WhiteLabelItemPathDto
                    {
                        Dark = path
                    };
                }
                else
                {
                    result.Path = new WhiteLabelItemPathDto
                    {
                        Light = path
                    };
                }
            }
            else
            {
                var lightPath = commonLinkUtility.GetFullAbsolutePath(isDefault
                    ? await tenantWhiteLabelSettingsHelper.GetAbsoluteDefaultLogoPathAsync(logoType, false)
                    : await tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, logoType));

                var darkPath = commonLinkUtility.GetFullAbsolutePath(isDefault
                    ? await tenantWhiteLabelSettingsHelper.GetAbsoluteDefaultLogoPathAsync(logoType, true)
                    : await tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, logoType, true));

                if (lightPath == darkPath)
                {
                    darkPath = null;
                }

                result.Path = new WhiteLabelItemPathDto
                {
                    Light = lightPath,
                    Dark = darkPath
                };
            }

            yield return result;
        }
    }

    /// <remarks>
    /// Returns whether each logo slot of the current portal has no image of its own and shows the installation default.
    /// Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get
    /// 403. The call is read-only. The list includes the notification slot 8. So it has one entry more than `GET
    /// api/2.0/settings/whitelabel/logos`. Each entry has the slot name in `name`. Its `default` is `true` until the
    /// portal stores an image for the slot in either theme. Slot 8 always has the same `default` as the login-page slot
    /// 2. With `isDefault=true`, the call checks the installation-wide default logos instead. To get the logo URLs, use
    /// `GET api/2.0/settings/whitelabel/logos`. To check the logo text, use `GET
    /// api/2.0/settings/whitelabel/logotext/isdefault`.
    /// </remarks>
    /// <summary>Check the default white label logos</summary>
    /// <path>api/2.0/settings/whitelabel/logos/isdefault</path>
    /// <collection>list</collection>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "One entry per logo slot, telling whether the slot still holds the built-in image", typeof(IAsyncEnumerable<IsDefaultWhiteLabelLogosDto>))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [AllowNotPayment]
    [HttpGet("whitelabel/logos/isdefault")]
    public async IAsyncEnumerable<IsDefaultWhiteLabelLogosDto> GetIsDefaultWhiteLabelLogos([FromQuery] WhiteLabelQueryRequestsDto inQueryDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenantWhiteLabelSettings = inQueryDto is { IsDefault: not null } && inQueryDto.IsDefault.Value
            ? await settingsManager.LoadForDefaultTenantAsync<TenantWhiteLabelSettings>()
            : await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        foreach (var logoType in Enum.GetValues<WhiteLabelLogoType>())
        {
            var result = new IsDefaultWhiteLabelLogosDto
            {
                Name = logoType.ToStringFast(),
                Default = tenantWhiteLabelSettings.GetIsDefault(logoType)
            };

            yield return result;
        }
    }

    /// <remarks>
    /// Deletes all logo images of the current portal and brings back the built-in ones. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. Unlike
    /// the save calls, it works without a plan that includes customization. The call deletes the stored image files,
    /// so it cannot be undone. To get the images back, upload them again with
    /// `POST api/2.0/settings/whitelabel/logos/save`. You can repeat the call, and it returns `true` each time. It
    /// resets all slots together, not one by one. For a portal, it also resets the logo of older letter templates. It
    /// does not change the logo text. To reset the text, use `PUT api/2.0/settings/whitelabel/logotext/restore`. With
    /// `isDefault=true`, the call resets the installation-wide default logos instead. Only a self-hosted installation
    /// without limited space access allows this. Elsewhere the call returns 403.
    /// </remarks>
    /// <summary>Restore the white label logos</summary>
    /// <path>api/2.0/settings/whitelabel/logos/restore</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "Always `true` once the portal shows the built-in logos again", typeof(bool))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the installation does not allow default branding to be edited")]
    [HttpPut("whitelabel/logos/restore")]
    public async Task<bool> RestoreWhiteLabelLogos([FromQuery] WhiteLabelQueryRequestsDto inQueryDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (inQueryDto is { IsDefault: not null } && inQueryDto.IsDefault.Value)
        {
            await DemandRebrandingPermissionAsync(false);

            await RestoreWhiteLabelLogosForDefaultTenantAsync();
        }
        else
        {
            await RestoreWhiteLabelLogosForCurrentTenantAsync();
        }

        return true;
    }

    private async Task RestoreWhiteLabelLogosForCurrentTenantAsync()
    {
        var settings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();
        var tenant = tenantManager.GetCurrentTenant();

        await RestoreWhiteLabelLogosForTenantAsync(settings, null, tenant.Id);

        var tenantInfoSettings = await settingsManager.LoadAsync<TenantInfoSettings>();
        await tenantInfoSettingsHelper.RestoreDefaultLogoAsync(tenantInfoSettings, tenantLogoManager);
        await settingsManager.SaveAsync(tenantInfoSettings);
    }

    private async Task RestoreWhiteLabelLogosForDefaultTenantAsync()
    {
        var settings = await settingsManager.LoadForDefaultTenantAsync<TenantWhiteLabelSettings>();
        var storage = await storageFactory.GetStorageAsync(Tenant.DefaultTenant, "static_partnerdata");

        await RestoreWhiteLabelLogosForTenantAsync(settings, storage, Tenant.DefaultTenant);
    }

    private async Task RestoreWhiteLabelLogosForTenantAsync(TenantWhiteLabelSettings settings, IDataStore storage, int tenantId)
    {
        await tenantWhiteLabelSettingsHelper.RestoreDefaultLogos(settings, tenantLogoManager, tenantId, storage);

        messageService.Send(MessageAction.WhiteLabelSettingsLogosUpdated);
    }

    #endregion

    #region Logo Text

    /// <remarks>
    /// Changes the logo text of the current portal. The portal shows it next to or instead of a logo, on the login
    /// page, in editors, and in letters. The call reads only `logoText` from the body. An empty or blank text, or
    /// `ONLYOFFICE` itself, clears the setting. This has the same effect as
    /// `PUT api/2.0/settings/whitelabel/logotext/restore`. Requires the EditPortalSettings permission, which the
    /// portal owner and DocSpace admins have. Other callers get 403. If the plan has no customization or the
    /// installation hides branding, the call returns 402. The call changes data and returns `true`. You can repeat it
    /// with the same body. It does not change logo images and does not draw the text into them. With
    /// `isDefault=true`, the call changes the installation-wide default text instead. Only a self-hosted installation
    /// without limited space access allows this. Elsewhere the call returns 403. If the text is too long, the call
    /// returns 400. To read the text, use `GET api/2.0/settings/whitelabel/logotext`.
    /// </remarks>
    /// <summary>Save the white label logo text</summary>
    /// <path>api/2.0/settings/whitelabel/logotext/save</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "Always `true` once the logo text has been stored for the portal", typeof(bool))]
    [SwaggerResponse(400, "The `logoText` is longer than 40 characters")]
    [SwaggerResponse(402, "The portal's pricing plan has no customization option, or the branding section is switched off in the installation")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the installation does not allow default branding to be edited")]
    [HttpPost("whitelabel/logotext/save")]
    public async Task<bool> SaveWhiteLabelLogoText(WhiteLabelRequestsDto inDto, [FromQuery] WhiteLabelQueryRequestsDto inQueryDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        int tenantId;

        if (inQueryDto is { IsDefault: not null } && inQueryDto.IsDefault.Value)
        {
            await DemandRebrandingPermissionAsync();

            tenantId = Tenant.DefaultTenant;
        }
        else
        {
            await tenantLogoManager.DemandWhiteLabelPermissionAsync();

            tenantId = tenantManager.GetCurrentTenantId();
        }

        var settings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>(tenantId);

        settings.SetLogoText(inDto.LogoText);

        await settingsManager.SaveAsync(settings, tenantId);

        messageService.Send(MessageAction.WhiteLabelSettingsLogoTextUpdated);

        return true;
    }

    /// <remarks>
    /// Returns the logo text of the current portal. The answer is a string, not an object. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. The
    /// call is read-only. If the portal has no stored text, the call returns `ONLYOFFICE`, even if the installation
    /// default differs. To learn whether a text is stored, use `GET api/2.0/settings/whitelabel/logotext/isdefault`.
    /// With `isDefault=true`, the call returns the installation-wide default text instead. The text is the same for
    /// every user and is not translated. To get public branding without signing in, use
    /// `GET api/2.0/settings/whitelabel/logos`.
    /// </remarks>
    /// <summary>Get the white label logo text</summary>
    /// <path>api/2.0/settings/whitelabel/logotext</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "The wordmark stored for the portal, or the built-in `ONLYOFFICE` when none is set", typeof(string))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [AllowNotPayment]
    [HttpGet("whitelabel/logotext")]
    public async Task<string> GetWhiteLabelLogoText([FromQuery] WhiteLabelQueryRequestsDto inQueryDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = inQueryDto is { IsDefault: not null } && inQueryDto.IsDefault.Value
            ? await settingsManager.LoadForDefaultTenantAsync<TenantWhiteLabelSettings>()
            : await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        return settings.LogoText ?? TenantWhiteLabelSettings.DefaultLogoText;
    }

    /// <remarks>
    /// Returns whether the current portal has no logo text of its own. Requires the EditPortalSettings permission,
    /// which the portal owner and DocSpace admins have. Other callers get 403. The call is read-only. The answer has
    /// `name` set to `logotext`. Its `default` is `true` while the portal has no stored text. Saving `ONLYOFFICE`
    /// itself clears the setting, so `default` stays `true`. After `POST api/2.0/settings/whitelabel/logotext/save`
    /// with another text, `default` becomes `false`. After `PUT api/2.0/settings/whitelabel/logotext/restore`, it
    /// becomes `true` again. With `isDefault=true`, the call checks the installation-wide default text instead. To
    /// check the logos, use `GET api/2.0/settings/whitelabel/logos/isdefault`.
    /// </remarks>
    /// <summary>Check the default logo text</summary>
    /// <path>api/2.0/settings/whitelabel/logotext/isdefault</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "A single `logotext` entry telling whether the portal still uses the built-in wordmark", typeof(IsDefaultWhiteLabelLogosDto))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("whitelabel/logotext/isdefault")]
    public async Task<IsDefaultWhiteLabelLogosDto> GetIsDefaultWhiteLabelLogoText([FromQuery] WhiteLabelQueryRequestsDto inQueryDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenantWhiteLabelSettings = inQueryDto is { IsDefault: not null } && inQueryDto.IsDefault.Value
            ? await settingsManager.LoadForDefaultTenantAsync<TenantWhiteLabelSettings>()
            : await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        return new IsDefaultWhiteLabelLogosDto
        {
            Name = "logotext",
            Default = string.IsNullOrEmpty(tenantWhiteLabelSettings.LogoText)
        };
    }

    /// <remarks>
    /// Clears the logo text of the current portal. The portal then shows the built-in `ONLYOFFICE` again. Requires
    /// the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403.
    /// Unlike the save call, it works without a plan that includes customization. The call cannot be undone. To get
    /// the old text back, save it again. You can repeat the call, and it returns `true` each time. It does not change
    /// logo images. To reset them, use `PUT api/2.0/settings/whitelabel/logos/restore`. With `isDefault=true`, the
    /// call clears the installation-wide default text instead. Only a self-hosted installation without limited space
    /// access allows this. Elsewhere the call returns 403.
    /// </remarks>
    /// <summary>Restore the white label logo text</summary>
    /// <path>api/2.0/settings/whitelabel/logotext/restore</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "Always `true` once the portal prints the built-in wordmark again", typeof(bool))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the installation does not allow default branding to be edited")]
    [HttpPut("whitelabel/logotext/restore")]
    public async Task<bool> RestoreWhiteLabelLogoText([FromQuery] WhiteLabelQueryRequestsDto inQueryDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        int tenantId;

        if (inQueryDto is { IsDefault: not null } && inQueryDto.IsDefault.Value)
        {
            await DemandRebrandingPermissionAsync(false);

            tenantId = Tenant.DefaultTenant;
        }
        else
        {
            tenantId = tenantManager.GetCurrentTenantId();
        }

        var settings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>(tenantId);

        await tenantWhiteLabelSettingsHelper.RestoreDefaultLogoText(settings, tenantId);

        messageService.Send(MessageAction.WhiteLabelSettingsLogoTextUpdated);

        return true;
    }

    #endregion

    #region Company

    /// <remarks>
    /// Returns the licensor details that the About page and letters show as the vendor. Any signed-in user can call
    /// it. The call is read-only. The first item holds the details in effect. A reseller can replace them without
    /// marking its details as the licensor. Then a second item holds the built-in ONLYOFFICE details, so the page can
    /// show both. A single item means there is nothing else to show. The details apply to the whole installation, so
    /// all its portals return the same list. To read the details in the form the settings page edits, use
    /// `GET api/2.0/settings/rebranding/company`. To change them, use `POST api/2.0/settings/rebranding/company`.
    /// </remarks>
    /// <summary>Get the licensor data</summary>
    /// <path>api/2.0/settings/companywhitelabel</path>
    /// <collection>list</collection>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "The licensor details in effect, followed by the built-in ONLYOFFICE ones when they have been replaced", typeof(List<LicensorDetailsDto>))]
    [HttpGet("companywhitelabel")]
    public async Task<List<LicensorDetailsDto>> GetLicensorData()
    {
        var result = new List<LicensorDetailsDto>();

        var instance = await companyWhiteLabelSettingsHelper.InstanceAsync();

        result.Add(instance.Map());

        if (!companyWhiteLabelSettingsHelper.IsDefault(instance) && !instance.IsLicensor)
        {
            result.Add(settingsManager.GetDefault<CompanyWhiteLabelSettings>().Map());
        }

        return result;
    }

    /// <remarks>
    /// Changes the company details that the About page and letters show as the vendor. The `settings` object replaces
    /// the whole set, so send every field. Requires the EditPortalSettings permission, which the portal owner and
    /// DocSpace admins have. Other callers get 403. Only a self-hosted installation without limited space access
    /// allows the call. Elsewhere, including a SaaS portal, it returns 403. If the plan has no customization or the
    /// installation hides branding, the call returns 402. The details apply to the whole installation and reach all
    /// its portals. The call always stores `isLicensor` as `false`. It stores `hideAbout` only if the plan includes
    /// branding. The call changes data and returns `true`. You can repeat it with the same body. To undo the call,
    /// use `DELETE api/2.0/settings/rebranding/company`. The call returns 400 if `settings`, `email`, or `site` is
    /// missing or not valid.
    /// </remarks>
    /// <summary>Save the company white label settings</summary>
    /// <path>api/2.0/settings/rebranding/company</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "Always `true` once the company details have been stored for the installation", typeof(bool))]
    [SwaggerResponse(400, "The request carries no settings object, or the email or the site is not a valid value")]
    [SwaggerResponse(402, "The portal's pricing plan has no customization option, or the branding section is switched off in the installation")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the installation does not allow branding to be edited")]
    [HttpPost("rebranding/company")]
    public async Task<bool> SaveCompanyWhiteLabelSettings(CompanyWhiteLabelSettingsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync();

        ArgumentNullException.ThrowIfNull(inDto?.Settings, "settings");
        ArgumentNullException.ThrowIfNull(inDto.Settings.Email, "email");
        ArgumentNullException.ThrowIfNull(inDto.Settings.Site, "site");

        if (inDto.Settings.Email.TestEmailPunyCode())
        {
            throw new ArgumentException("email");
        }

        if (inDto.Settings.Site.TestUrlPunyCode())
        {
            throw new ArgumentException("site");
        }

        var quota = await tenantManager.GetCurrentTenantQuotaAsync();

        var settings = new CompanyWhiteLabelSettings
        {
            CompanyName = inDto.Settings.CompanyName,
            Site = inDto.Settings.Site,
            Email = inDto.Settings.Email,
            Address = inDto.Settings.Address,
            Phone = inDto.Settings.Phone,
            HideAbout = quota.Branding && inDto.Settings.HideAbout,
            IsLicensor = false
        };

        await settingsManager.SaveForDefaultTenantAsync(settings);

        messageService.Send(MessageAction.WhiteLabelCompanySettingsUpdated);

        return true;
    }

    /// <remarks>
    /// Returns the company details that the About page and letters show as the vendor. This is the form the settings
    /// page edits. Any signed-in user can call it, even when the portal payment has lapsed. The call is read-only.
    /// The details apply to the whole installation, so all its portals return the same values. The answer has a
    /// `Last-Modified` header. Send that value in `If-Modified-Since` to check for changes. If nothing changed, the
    /// call returns 304 with an empty body. To show both a reseller and the original vendor, use
    /// `GET api/2.0/settings/companywhitelabel`. To change the details, use
    /// `POST api/2.0/settings/rebranding/company`.
    /// </remarks>
    /// <summary>Get the company white label settings</summary>
    /// <path>api/2.0/settings/rebranding/company</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "The company details in effect, with the licensor and default flags", typeof(CompanyWhiteLabelSettingsDto))]
    [SwaggerResponse(304, "The company details have not changed since the `Last-Modified` value sent back in `If-Modified-Since`; the body is empty")]
    [AllowNotPayment]
    [HttpGet("rebranding/company")]
    public async Task<CompanyWhiteLabelSettingsDto> GetCompanyWhiteLabelSettings()
    {
        var settings = await settingsManager.LoadForDefaultTenantAsync<CompanyWhiteLabelSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(settings.LastModified) ? null : companyWhiteLabelSettingsDtoMapper.Map(settings);
    }

    /// <remarks>
    /// Deletes the stored company details of the installation and brings back the built-in ONLYOFFICE ones. The About
    /// page and letters then show the original vendor. Requires the EditPortalSettings permission, which the portal
    /// owner and DocSpace admins have. Other callers get 403. Only a self-hosted installation without limited space
    /// access allows the call. Elsewhere, including a SaaS portal, it returns 403. Unlike the save call, it works
    /// without a plan that includes customization. The call cannot be undone. To get the old details back, save them
    /// again. You can repeat it. The call returns the built-in details that are now in effect. The reset reaches
    /// every portal of the installation. To reset the help links, use
    /// `DELETE api/2.0/settings/rebranding/additional`.
    /// </remarks>
    /// <summary>Delete the company white label settings</summary>
    /// <path>api/2.0/settings/rebranding/company</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "The built-in company details that are now in effect", typeof(LicensorDetailsDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the installation does not allow branding to be edited")]
    [HttpDelete("rebranding/company")]
    public async Task<LicensorDetailsDto> DeleteCompanyWhiteLabelSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync(false);

        var defaultSettings = settingsManager.GetDefault<CompanyWhiteLabelSettings>();

        await settingsManager.SaveForDefaultTenantAsync(defaultSettings);

        messageService.Send(MessageAction.WhiteLabelCompanySettingsUpdated);

        return defaultSettings.Map();
    }

    #endregion

    #region Additional

    /// <remarks>
    /// Changes which help and community resources the interface shows. The `settings` object replaces the whole set,
    /// so send every flag. The call stores a missing flag as `false`. Requires the EditPortalSettings permission,
    /// which the portal owner and DocSpace admins have. Other callers get 403. Only a self-hosted installation
    /// without limited space access allows the call. Elsewhere, including a SaaS portal, it returns 403. If the plan
    /// has no customization or the installation hides branding, the call returns 402. The flags apply to the whole
    /// installation and reach all its portals. They control only whether entries show, not their addresses. The call
    /// changes data and returns `true`. You can repeat it with the same body. To undo the call, use
    /// `DELETE api/2.0/settings/rebranding/additional`. If `settings` is missing, the call returns 400.
    /// </remarks>
    /// <summary>Save the additional white label settings</summary>
    /// <path>api/2.0/settings/rebranding/additional</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "Always `true` once the resource flags have been stored for the installation", typeof(bool))]
    [SwaggerResponse(400, "The request carries no settings object")]
    [SwaggerResponse(402, "The portal's pricing plan has no customization option, or the branding section is switched off in the installation")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the installation does not allow branding to be edited")]
    [HttpPost("rebranding/additional")]
    public async Task<bool> SaveAdditionalWhiteLabelSettings(AdditionalWhiteLabelSettingsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync();

        ArgumentNullException.ThrowIfNull(inDto?.Settings, "settings");

        var settings = inDto.Settings.Map();

        await settingsManager.SaveForDefaultTenantAsync(settings);

        messageService.Send(MessageAction.WhiteLabelAdditionalSettingsUpdated);

        return true;
    }

    /// <remarks>
    /// Returns which help and community resources the interface may show. An app hides the entries whose flags are
    /// `false`. Any signed-in user can call it, even when the portal payment has lapsed. The call is read-only. The
    /// flags apply to the whole installation, so all its portals return the same values. They do not depend on the
    /// permissions of the caller. The answer does not include the addresses of the entries. To change the flags, use
    /// `POST api/2.0/settings/rebranding/additional`.
    /// </remarks>
    /// <summary>Get the additional white label settings</summary>
    /// <path>api/2.0/settings/rebranding/additional</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "The help and community resources the interface may offer, with the default flag", typeof(AdditionalWhiteLabelSettingsDto))]
    [AllowNotPayment]
    [HttpGet("rebranding/additional")]
    public async Task<AdditionalWhiteLabelSettingsDto> GetAdditionalWhiteLabelSettings()
    {
        var settings = await settingsManager.LoadForDefaultTenantAsync<AdditionalWhiteLabelSettings>();

        return additionalWhiteLabelSettingsMapper.Map(settings);
    }

    /// <remarks>
    /// Deletes the stored resource flags of the installation and brings back the built-in set. The interface then
    /// shows the help and community resources as it does by default. Requires the EditPortalSettings permission,
    /// which the portal owner and DocSpace admins have. Other callers get 403. Only a self-hosted installation
    /// without limited space access allows the call. Elsewhere, including a SaaS portal, it returns 403. Unlike the
    /// save call, it works without a plan that includes customization. The call cannot be undone. To get the old
    /// flags back, save them again. You can repeat it. The call returns the built-in flags that are now in effect.
    /// The reset reaches every portal of the installation. To reset the company details, use
    /// `DELETE api/2.0/settings/rebranding/company`.
    /// </remarks>
    /// <summary>Delete the additional white label settings</summary>
    /// <path>api/2.0/settings/rebranding/additional</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "The built-in resource flags that are now in effect", typeof(AdditionalResourcesDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the installation does not allow branding to be edited")]
    [HttpDelete("rebranding/additional")]
    public async Task<AdditionalResourcesDto> DeleteAdditionalWhiteLabelSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync(false);

        var defaultSettings = settingsManager.GetDefault<AdditionalWhiteLabelSettings>();

        await settingsManager.SaveForDefaultTenantAsync(defaultSettings);

        messageService.Send(MessageAction.WhiteLabelAdditionalSettingsUpdated);

        return defaultSettings.Map();
    }

    #endregion

    #region Mail

    /// <remarks>
    /// Saves the mail white label settings specified in the request.
    /// </remarks>
    /// <summary>Save the mail white label settings</summary>
    /// <path>api/2.0/settings/rebranding/mail</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(400, "Settings is empty")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [Tags("Settings / Rebranding")]
    [HttpPost("rebranding/mail")]
    public async Task<bool> SaveMailWhiteLabelSettings(MailWhiteLabelSettingsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync();

        ArgumentNullException.ThrowIfNull(inDto?.Settings, "settings");

        var settings = inDto.Settings.Map();

        await settingsManager.SaveForDefaultTenantAsync(settings);

        messageService.Send(MessageAction.WhiteLabelMailSettingsUpdated);

        return true;
    }

    /// <remarks>
    /// Returns the mail white label settings.
    /// </remarks>
    /// <summary>Get the mail white label settings</summary>
    /// <path>api/2.0/settings/rebranding/mail</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "Mail white label settings", typeof(MailWhiteLabelSettingsDto))]
    [HttpGet("rebranding/mail")]
    public async Task<MailWhiteLabelSettingsDto> GetMailWhiteLabelSettings()
    {
        var settings = await settingsManager.LoadForDefaultTenantAsync<MailWhiteLabelSettings>();

        return settings.MapToDto();
    }

    /// <remarks>
    /// Deletes the mail white label settings.
    /// </remarks>
    /// <summary>Delete the mail white label settings</summary>
    /// <path>api/2.0/settings/rebranding/mail</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "Default mail white label settings", typeof(MailWhiteLabelSettingsDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpDelete("rebranding/mail")]
    public async Task<MailWhiteLabelSettingsDto> DeleteMailWhiteLabelSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync(false);

        var defaultSettings = settingsManager.GetDefault<MailWhiteLabelSettings>();

        await settingsManager.SaveForDefaultTenantAsync(defaultSettings);

        messageService.Send(MessageAction.WhiteLabelMailSettingsUpdated);

        return defaultSettings.MapToDto();
    }

    #endregion

    /// <remarks>
    /// Returns whether the current portal can change its branding. Requires the EditPortalSettings permission, which
    /// the portal owner and DocSpace admins have. Other callers get 403. The call is read-only. The answer is `true`
    /// only if the installation does not hide branding and the portal plan includes customization. If it is `false`,
    /// every branding save call returns 402. This includes the logo and logo text saves under
    /// `api/2.0/settings/whitelabel`. It also includes the company and help link saves under
    /// `api/2.0/settings/rebranding`. The restore and delete calls work in both cases. The installation-wide branding
    /// also needs a self-hosted installation, which this answer does not check.
    /// </remarks>
    /// <summary>Check the white label availability</summary>
    /// <path>api/2.0/settings/enablewhitelabel</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "`true` when branding is enabled in this installation and included in the portal's plan", typeof(bool))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("enablewhitelabel")]
    public async Task<bool> GetEnableWhitelabel()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await tenantLogoManager.GetEnableWhitelabelAsync();
    }

    private async Task DemandRebrandingPermissionAsync(bool demandWhiteLabelPermission = true)
    {
        await tenantExtra.DemandAccessSpacePermissionAsync();

        if (coreBaseSettings.CustomMode)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        if (demandWhiteLabelPermission)
        {
            await tenantLogoManager.DemandWhiteLabelPermissionAsync();
        }
    }
}
