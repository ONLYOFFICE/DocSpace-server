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
    /// Replaces the branding images of the current portal with the ones sent in the request, so that the logos on the
    /// login page, in the left menu, in the editors and in letters come from this portal. Every entry of `logo` names
    /// a logo slot in its `key` - the numeric type published by `GET api/2.0/settings/whitelabel/logos` - and carries
    /// the light-theme and the dark-theme image in `light` and `dark`. An image is either a
    /// `data:image/png;base64,...` payload (`png`, `jpg` and `svg` are accepted) or the name of a file already
    /// uploaded to the temporary store; a slot left out of the request keeps its image. The dark image is stored only
    /// for the slots that have a dark variant, that is `1`, `2`, `6`, `7` and `8`, and is ignored for the favicon and
    /// the editor logos; saving slot `2` also rebuilds the notification logo `8` from it. Requires a DocSpace
    /// administrator and a plan that includes branding, which `GET api/2.0/settings/enablewhitelabel` reports;
    /// otherwise the call is refused as payment required. It answers `true` and is undone by
    /// `PUT api/2.0/settings/whitelabel/logos/restore`. With `isDefault=true` it writes the installation-wide default
    /// branding instead, which only a server installation allows. Uploaded files go to
    /// `POST api/2.0/settings/whitelabel/logos/savefromfiles`.
    /// </remarks>
    /// <summary>Save the white label logos</summary>
    /// <path>api/2.0/settings/whitelabel/logos/save</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "Always `true` once the submitted logos have been stored for the portal", typeof(bool))]
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
    /// Replaces the branding images of the current portal with the files sent as `multipart/form-data`, which is the
    /// way to upload image files directly instead of embedding them as base64 in
    /// `POST api/2.0/settings/whitelabel/logos/save`. The form field names are not used: each file is routed by its
    /// own name, which has to start with the numeric logo slot published by `GET api/2.0/settings/whitelabel/logos`
    /// and end with the image extension, as in `2.png`; a name that also contains `dark`, as in `2.dark.png`, is
    /// stored as the dark-theme image of that slot. Slots that get no file keep the image they have, and a dark file
    /// is ignored for the favicon and the editor logos, which have no dark variant. A request that carries no file at
    /// all is rejected. Requires a DocSpace administrator and a plan that includes branding, which
    /// `GET api/2.0/settings/enablewhitelabel` reports; otherwise the call is refused as payment required. It answers
    /// `true`, overwrites in place and is undone by `PUT api/2.0/settings/whitelabel/logos/restore`. With
    /// `isDefault=true` it writes the installation-wide default branding, which only a server installation allows.
    /// </remarks>
    /// <summary>Save the logos from files</summary>
    /// <path>api/2.0/settings/whitelabel/logos/savefromfiles</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "Always `true` once the uploaded files have been stored as the portal logos", typeof(bool))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the installation does not allow default branding to be edited")]
    [SwaggerResponse(409, "The request carried no file to store as a logo")]
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
    /// Lists the branding logo slots of the current portal together with the image URLs to render, which is what a
    /// login page, an editor or a mail template needs before any user is known. No authentication is required, and
    /// the portal is resolved from the address the request is made to. The call is read-only and idempotent. Each
    /// item carries the slot as a number in `type`, its stable name in `name`, the size the image is fitted to in
    /// `size` (`width` and `height` in pixels), and the URLs in `path`. When `isDark` is passed, only the matching
    /// theme is filled in, `light` for `false` and `dark` for `true`; when it is omitted both are filled in and
    /// `dark` comes back empty for the slots that have no separate dark image. The notification slot is not part of
    /// this list, as it is derived from the login-page logo and used only in letters. Pass `isDefault=true` to read
    /// the installation-wide default logos instead of this portal's. To learn which slots are still untouched use
    /// `GET api/2.0/settings/whitelabel/logos/isdefault`.
    /// </remarks>
    /// <summary>Get the white label logos</summary>
    /// <path>api/2.0/settings/whitelabel/logos</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <collection>list</collection>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "The logo slots of the portal, each with its target size and the URLs of the light and dark images", typeof(IAsyncEnumerable<WhiteLabelItemDto>))]
    [AllowNotPayment, AllowAnonymous, AllowSuspended]
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
    /// Reports, slot by slot, whether the current portal still shows the built-in image or a logo that was uploaded
    /// for it, which is what an interface needs to decide where a Restore action makes sense. Requires a DocSpace
    /// administrator; the URLs themselves are public and come from `GET api/2.0/settings/whitelabel/logos`, which
    /// needs no authentication. The call is read-only and idempotent. Every logo slot is returned, including the
    /// notification logo that the public list leaves out, so the result has one entry more than that list. An entry
    /// gives the stable slot name in `name` and `default` set to `true` while the slot has never been written, and to
    /// `false` once an image has been stored for it, whether for the light or for the dark theme. A slot goes back to
    /// `true` after `PUT api/2.0/settings/whitelabel/logos/restore`. Pass `isDefault=true` to inspect the
    /// installation-wide default branding instead of this portal's. The logo text is reported separately by
    /// `GET api/2.0/settings/whitelabel/logotext/isdefault`.
    /// </remarks>
    /// <summary>Check the default white label logos</summary>
    /// <path>api/2.0/settings/whitelabel/logos/isdefault</path>
    /// <collection>list</collection>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "One entry per logo slot, telling whether the slot still holds the built-in image", typeof(IAsyncEnumerable<IsDefaultWhiteLabelLogosDto>))]
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
    /// Drops every logo uploaded for the current portal and brings back the built-in images, so the portal looks
    /// unbranded again on the login page, in the left menu, in the editors and in letters. Requires a DocSpace
    /// administrator. Unlike the two save operations it does not need a plan that includes branding, so a portal
    /// whose subscription no longer covers it can still be reset. The call is destructive: the stored image files are
    /// deleted and cannot be recovered from the portal, only re-uploaded with
    /// `POST api/2.0/settings/whitelabel/logos/save`. It is idempotent and answers `true` both when logos were
    /// removed and when there was nothing to remove. All slots are reset together; there is no way to restore a
    /// single one. For this portal the picture kept for the older mail templates is reset along with the logos, while
    /// the logo text is left as it is and has its own `PUT api/2.0/settings/whitelabel/logotext/restore`. Pass
    /// `isDefault=true` to reset the installation-wide default branding instead, which only a server installation
    /// allows. Confirm the result with `GET api/2.0/settings/whitelabel/logos/isdefault`.
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
    /// Sets the wordmark that the portal prints next to or instead of a logo image, on the login page, in the editors
    /// and in notification letters. Only `logoText` from the request body is used here, and it is limited to 40
    /// characters; a longer value is rejected as an invalid request. Sending an empty or blank text, or exactly the
    /// built-in `ONLYOFFICE`, clears the setting instead of storing it, which has the same effect as
    /// `PUT api/2.0/settings/whitelabel/logotext/restore`. Requires a DocSpace administrator and a plan that includes
    /// branding, which `GET api/2.0/settings/enablewhitelabel` reports; otherwise the call is refused as payment
    /// required. The call is mutating and idempotent: the previous text is overwritten and `true` comes back. Logo
    /// images are not touched - they are saved by `POST api/2.0/settings/whitelabel/logos/save` - and the text is not
    /// rendered into them. Pass `isDefault=true` to write the installation-wide default wordmark instead of this
    /// portal's, which only a server installation allows. Read the stored value back with
    /// `GET api/2.0/settings/whitelabel/logotext`.
    /// </remarks>
    /// <summary>Save the white label logo text</summary>
    /// <path>api/2.0/settings/whitelabel/logotext/save</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "Always `true` once the logo text has been stored for the portal", typeof(bool))]
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
    /// Returns the wordmark the current portal prints next to or instead of a logo image, as a bare string rather
    /// than an object. Requires a DocSpace administrator, because this is the settings view of the value; the
    /// branding a login page needs is served by `GET api/2.0/settings/whitelabel/logos`, which needs no
    /// authentication. The call is read-only and idempotent. When nothing has been stored for the portal, the
    /// built-in `ONLYOFFICE` is returned, so the answer is never empty and cannot be used to tell a custom text from
    /// the default one - `GET api/2.0/settings/whitelabel/logotext/isdefault` answers that question. Pass
    /// `isDefault=true` to read the installation-wide default wordmark instead of this portal's; without it the
    /// portal's own value is returned even when the installation carries a different default. Change the text with
    /// `POST api/2.0/settings/whitelabel/logotext/save` and clear it with
    /// `PUT api/2.0/settings/whitelabel/logotext/restore`. The value is stored as it was typed, at most 40 characters
    /// long, and is not translated for the caller's language, so the same wordmark is returned for every user of the
    /// portal.
    /// </remarks>
    /// <summary>Get the white label logo text</summary>
    /// <path>api/2.0/settings/whitelabel/logotext</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "The wordmark stored for the portal, or the built-in `ONLYOFFICE` when none is set", typeof(string))]
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
    /// Reports whether the current portal still uses the built-in wordmark or one that was stored for it, which is
    /// what an interface needs to decide whether a Restore action applies to the text. Requires a DocSpace
    /// administrator. The call is read-only and idempotent. The answer has the same shape as one entry of
    /// `GET api/2.0/settings/whitelabel/logos/isdefault`, with `name` fixed to `logotext` and `default` set to `true`
    /// while no text has been stored and to `false` once one has. Because `GET api/2.0/settings/whitelabel/logotext`
    /// falls back to `ONLYOFFICE` when nothing is stored, this operation is the only way to tell a portal that
    /// deliberately kept the built-in wordmark from one that saved the very same text. Pass `isDefault=true` to
    /// inspect the installation-wide default branding instead of this portal's. The flag turns back to `true` after
    /// `PUT api/2.0/settings/whitelabel/logotext/restore`, and to `false` after
    /// `POST api/2.0/settings/whitelabel/logotext/save`. Saving the built-in wordmark itself counts as clearing the
    /// setting, so the flag stays `true` in that case as well.
    /// </remarks>
    /// <summary>Check the default logo text</summary>
    /// <path>api/2.0/settings/whitelabel/logotext/isdefault</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "A single `logotext` entry telling whether the portal still uses the built-in wordmark", typeof(IsDefaultWhiteLabelLogosDto))]
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
    /// Clears the wordmark stored for the current portal, so the built-in `ONLYOFFICE` is printed again next to or
    /// instead of the logo images. Requires a DocSpace administrator. Unlike
    /// `POST api/2.0/settings/whitelabel/logotext/save` it does not need a plan that includes branding, so a portal
    /// whose subscription no longer covers branding can still be reset. The call is destructive for the stored text,
    /// which is not kept anywhere and has to be typed again to come back, and it is idempotent: `true` comes back
    /// both when a text was cleared and when there was none. Logo images are left untouched and have their own
    /// `PUT api/2.0/settings/whitelabel/logos/restore`. Pass `isDefault=true` to reset the installation-wide default
    /// wordmark instead of this portal's, which only a server installation allows. After the call
    /// `GET api/2.0/settings/whitelabel/logotext` reports `ONLYOFFICE` and
    /// `GET api/2.0/settings/whitelabel/logotext/isdefault` reports `default` as `true`. The wordmark is the only
    /// setting this operation touches, so the company details and the help links of the installation are left as they
    /// are.
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
    /// Returns the licensor details - company name, site, support email, postal address and phone - that the About
    /// page and the notification letters print as the vendor of the installation. Any authenticated user may call it,
    /// as these details are shown in the interface to everyone; no administrator permission is required. The call is
    /// read-only and idempotent. The list holds the details currently in effect as its first item; when they have
    /// been replaced by a reseller and the replacement is not itself marked as the licensor, the built-in ONLYOFFICE
    /// details are appended as a second item, so a caller can print both the reseller and the original vendor. A
    /// single-item list therefore means that the current details are the only ones to show. The values are
    /// installation-wide rather than per-portal, so every portal of a server installation reports the same ones. The
    /// same data in the form the settings interface edits is served by `GET api/2.0/settings/rebranding/company`, and
    /// it is written by `POST api/2.0/settings/rebranding/company`.
    /// </remarks>
    /// <summary>Get the licensor data</summary>
    /// <path>api/2.0/settings/companywhitelabel</path>
    /// <collection>list</collection>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "The licensor details in effect, followed by the built-in ONLYOFFICE ones when they have been replaced", typeof(List<CompanyWhiteLabelSettings>))]
    [HttpGet("companywhitelabel")]
    public async Task<List<CompanyWhiteLabelSettings>> GetLicensorData()
    {
        var result = new List<CompanyWhiteLabelSettings>();

        var instance = await companyWhiteLabelSettingsHelper.InstanceAsync();

        result.Add(instance);

        if (!companyWhiteLabelSettingsHelper.IsDefault(instance) && !instance.IsLicensor)
        {
            result.Add(settingsManager.GetDefault<CompanyWhiteLabelSettings>());
        }

        return result;
    }

    /// <remarks>
    /// Stores the company details - name, site, support email, postal address and phone - that the About page and the
    /// notification letters print as the vendor. The whole set is replaced by the `settings` object of the request,
    /// so send every field, not only the changed ones; a request without that object, or with an email or a site that
    /// is not a valid value, is rejected as an invalid request. Requires a DocSpace administrator, a server
    /// installation with unrestricted space access and a plan that includes branding, which
    /// `GET api/2.0/settings/enablewhitelabel` reports; on a SaaS portal the call is refused. The values are
    /// installation-wide, so the change reaches every portal of that installation. Two fields are not taken from the
    /// request: the licensor flag is always stored as `false`, and hiding the About page is silently kept off unless
    /// the plan allows it. The call is mutating and idempotent, and answers `true`. Read the result back with
    /// `GET api/2.0/settings/rebranding/company` and undo it with `DELETE api/2.0/settings/rebranding/company`.
    /// </remarks>
    /// <summary>Save the company white label settings</summary>
    /// <path>api/2.0/settings/rebranding/company</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "Always `true` once the company details have been stored for the installation", typeof(bool))]
    [SwaggerResponse(400, "The request carries no settings object, or the email or the site is not a valid value")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the installation does not allow branding to be edited")]
    [HttpPost("rebranding/company")]
    public async Task<bool> SaveCompanyWhiteLabelSettings(CompanyWhiteLabelSettingsWrapper wrapper)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync();

        ArgumentNullException.ThrowIfNull(wrapper?.Settings, "settings");
        ArgumentNullException.ThrowIfNull(wrapper.Settings.Email, "email");
        ArgumentNullException.ThrowIfNull(wrapper.Settings.Site, "site");

        if (wrapper.Settings.Email.TestEmailPunyCode())
        {
            throw new ArgumentException("email");
        }

        if (wrapper.Settings.Site.TestUrlPunyCode())
        {
            throw new ArgumentException("site");
        }

        var quota = await tenantManager.GetCurrentTenantQuotaAsync();
        if (!quota.Branding)
        {
            wrapper.Settings.HideAbout = false;
        }

        wrapper.Settings.IsLicensor = false;

        await settingsManager.SaveForDefaultTenantAsync(wrapper.Settings);

        messageService.Send(MessageAction.WhiteLabelCompanySettingsUpdated);

        return true;
    }

    /// <remarks>
    /// Returns the company details that the About page and the notification letters print as the vendor, in the form
    /// the settings interface edits them. Any authenticated user may call it; no administrator permission is
    /// required, and a portal whose payment has lapsed is served as well. The call is read-only and idempotent.
    /// Alongside the stored fields the answer carries `isLicensor`, which tells whether these details belong to the
    /// vendor of the product itself, and `isDefault`, which tells whether they are still the built-in ONLYOFFICE
    /// ones. The values are installation-wide, so every portal of a server installation reports the same ones. The
    /// response is revalidatable: it carries `Last-Modified`, and sending that value back in `If-Modified-Since`
    /// yields an empty body while the details have not changed, which makes polling cheap. For the About page, where
    /// the built-in vendor has to be shown next to a reseller, use `GET api/2.0/settings/companywhitelabel` instead.
    /// Change the details with `POST api/2.0/settings/rebranding/company`.
    /// </remarks>
    /// <summary>Get the company white label settings</summary>
    /// <path>api/2.0/settings/rebranding/company</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "The company details in effect, with the licensor and default flags", typeof(CompanyWhiteLabelSettingsDto))]
    [AllowNotPayment]
    [HttpGet("rebranding/company")]
    public async Task<CompanyWhiteLabelSettingsDto> GetCompanyWhiteLabelSettings()
    {
        var settings = await settingsManager.LoadForDefaultTenantAsync<CompanyWhiteLabelSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(settings.LastModified) ? null : companyWhiteLabelSettingsDtoMapper.Map(settings);
    }

    /// <remarks>
    /// Discards the company details stored for the installation and brings back the built-in ONLYOFFICE name, site,
    /// email, address and phone, so the About page and the notification letters print the original vendor again.
    /// Requires a DocSpace administrator and a server installation with unrestricted space access; on a SaaS portal
    /// the call is refused. Unlike `POST api/2.0/settings/rebranding/company` it does not need a plan that includes
    /// branding, so an installation whose subscription no longer covers it can still be reset. The call is
    /// destructive: the previous details are not kept anywhere and have to be entered again to come back. It is
    /// idempotent, and instead of a flag it answers the details that are now in effect, so no follow-up read is
    /// needed. The reset is installation-wide and reaches every portal. The help and support links are reset
    /// separately by `DELETE api/2.0/settings/rebranding/additional`, and the logos and the wordmark of a single
    /// portal by the restore operations under `api/2.0/settings/whitelabel`.
    /// </remarks>
    /// <summary>Delete the company white label settings</summary>
    /// <path>api/2.0/settings/rebranding/company</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "The built-in company details that are now in effect", typeof(CompanyWhiteLabelSettings))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the installation does not allow branding to be edited")]
    [HttpDelete("rebranding/company")]
    public async Task<CompanyWhiteLabelSettings> DeleteCompanyWhiteLabelSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync(false);

        var defaultSettings = settingsManager.GetDefault<CompanyWhiteLabelSettings>();

        await settingsManager.SaveForDefaultTenantAsync(defaultSettings);

        messageService.Send(MessageAction.WhiteLabelCompanySettingsUpdated);

        return defaultSettings;
    }

    #endregion

    #region Additional

    /// <remarks>
    /// Stores which of the ONLYOFFICE help and community resources the interface offers: the sample documents, the
    /// Help Center link, the Feedback and Support link, the user forum, the video guides and the license agreements.
    /// The whole set is replaced by the `settings` object of the request, so send every flag, not only the changed
    /// ones - a flag left out is stored as off. A request without that object is rejected as an invalid request.
    /// Requires a DocSpace administrator, a server installation with unrestricted space access and a plan that
    /// includes branding, which `GET api/2.0/settings/enablewhitelabel` reports; on a SaaS portal the call is
    /// refused. The flags are installation-wide, so the change reaches every portal of that installation. The call is
    /// mutating and idempotent, and answers `true`. Only the visibility of these entries is controlled here, not the
    /// addresses behind them. Read the result back with `GET api/2.0/settings/rebranding/additional` and undo it with
    /// `DELETE api/2.0/settings/rebranding/additional`.
    /// </remarks>
    /// <summary>Save the additional white label settings</summary>
    /// <path>api/2.0/settings/rebranding/additional</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "Always `true` once the resource flags have been stored for the installation", typeof(bool))]
    [SwaggerResponse(400, "The request carries no settings object")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the installation does not allow branding to be edited")]
    [HttpPost("rebranding/additional")]
    public async Task<bool> SaveAdditionalWhiteLabelSettings(AdditionalWhiteLabelSettingsWrapper wrapper)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync();

        ArgumentNullException.ThrowIfNull(wrapper?.Settings, "settings");

        await settingsManager.SaveForDefaultTenantAsync(wrapper.Settings);

        messageService.Send(MessageAction.WhiteLabelAdditionalSettingsUpdated);

        return true;
    }

    /// <remarks>
    /// Returns which of the ONLYOFFICE help and community resources the interface may offer - the sample documents,
    /// the Help Center link, the Feedback and Support link, the user forum, the video guides and the license
    /// agreements - so a client can hide the entries that are switched off. Any authenticated user may call it; no
    /// administrator permission is required, and a portal whose payment has lapsed is served as well. The call is
    /// read-only and idempotent. Each flag is `true` when the entry may be shown and `false` when it must be hidden,
    /// and `isDefault` tells whether the whole set is still the built-in one. The flags are installation-wide, so
    /// every portal of a server installation reports the same ones. They say nothing about the caller's own
    /// permissions, and the addresses behind the entries are not part of the answer. Change the flags with
    /// `POST api/2.0/settings/rebranding/additional` and reset them with
    /// `DELETE api/2.0/settings/rebranding/additional`.
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
    /// Discards the resource flags stored for the installation and brings back the built-in set, so the sample
    /// documents, the Help Center link, the Feedback and Support link, the user forum, the video guides and the
    /// license agreements are offered as they are out of the box. Requires a DocSpace administrator and a server
    /// installation with unrestricted space access; on a SaaS portal the call is refused. Unlike
    /// `POST api/2.0/settings/rebranding/additional` it does not need a plan that includes branding, so an
    /// installation whose subscription no longer covers it can still be reset. The call is destructive for the stored
    /// flags, which have to be set again to come back, and it is idempotent. Instead of a flag it answers the set
    /// that is now in effect, so no follow-up read is needed. The reset is installation-wide and reaches every
    /// portal, and it leaves the visibility of the About page alone. The company details are reset separately by
    /// `DELETE api/2.0/settings/rebranding/company`.
    /// </remarks>
    /// <summary>Delete the additional white label settings</summary>
    /// <path>api/2.0/settings/rebranding/additional</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "The built-in resource flags that are now in effect", typeof(AdditionalWhiteLabelSettings))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the installation does not allow branding to be edited")]
    [HttpDelete("rebranding/additional")]
    public async Task<AdditionalWhiteLabelSettings> DeleteAdditionalWhiteLabelSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync(false);

        var defaultSettings = settingsManager.GetDefault<AdditionalWhiteLabelSettings>();

        await settingsManager.SaveForDefaultTenantAsync(defaultSettings);

        messageService.Send(MessageAction.WhiteLabelAdditionalSettingsUpdated);

        return defaultSettings;
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
    public async Task<bool> SaveMailWhiteLabelSettings(MailWhiteLabelSettingsWrapper wrapper)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync();

        ArgumentNullException.ThrowIfNull(wrapper?.Settings, "settings");

        await settingsManager.SaveForDefaultTenantAsync(wrapper.Settings);

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
    [SwaggerResponse(200, "Default mail white label settings", typeof(MailWhiteLabelSettings))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpDelete("rebranding/mail")]
    public async Task<MailWhiteLabelSettings> DeleteMailWhiteLabelSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await DemandRebrandingPermissionAsync(false);

        var defaultSettings = settingsManager.GetDefault<MailWhiteLabelSettings>();

        await settingsManager.SaveForDefaultTenantAsync(defaultSettings);

        messageService.Send(MessageAction.WhiteLabelMailSettingsUpdated);

        return defaultSettings;
    }

    #endregion

    /// <remarks>
    /// Reports whether branding may be configured for the current portal at all, which is the check to make before
    /// offering the rebranding interface or calling any of the save operations under `api/2.0/settings/whitelabel`.
    /// Requires a DocSpace administrator. The call is read-only and idempotent. The answer is `true` only when both
    /// conditions hold: the branding section is not switched off in the installation configuration, and the portal's
    /// current plan includes customization. It comes back as `false` on a plan without branding, which is exactly the
    /// case in which `POST api/2.0/settings/whitelabel/logos/save`,
    /// `POST api/2.0/settings/whitelabel/logos/savefromfiles` and `POST api/2.0/settings/whitelabel/logotext/save`
    /// are refused as payment required. The restore operations do not depend on this flag and stay available, so a
    /// portal that loses branding can still be reset to the built-in logos and wordmark. The flag says nothing about
    /// the installation-wide default branding, which additionally needs a server installation with unrestricted space
    /// access, and nothing about the company details and help links under `api/2.0/settings/rebranding`.
    /// </remarks>
    /// <summary>Check the white label availability</summary>
    /// <path>api/2.0/settings/enablewhitelabel</path>
    [Tags("Settings / Rebranding")]
    [SwaggerResponse(200, "`true` when branding is enabled in this installation and included in the portal's plan", typeof(bool))]
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
