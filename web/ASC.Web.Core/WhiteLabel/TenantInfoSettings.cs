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

namespace ASC.Web.Core.WhiteLabel;

public class TenantInfoSettings : ISettings<TenantInfoSettings>
{
    [JsonPropertyName("LogoSize")]
    public Size CompanyLogoSize { get; internal set; }

    [JsonPropertyName("LogoFileName")]
    public string CompanyLogoFileName { get; set; }

    [JsonPropertyName("Default")]
    internal bool IsDefault { get; set; }

    public TenantInfoSettings GetDefault()
    {
        return new TenantInfoSettings
        {
            IsDefault = true
        };
    }

    public DateTime LastModified { get; set; }

    public static Guid ID => new("{5116B892-CCDD-4406-98CD-4F18297C0C0A}");
}

/// <summary>
/// Represents dimensions with width and height values.
/// </summary>
public class Size
{
    /// <summary>
    /// Gets or sets the height dimension of an object, typically measured in pixels or other unit.
    /// It defines the vertical size of the object.
    /// </summary>
    /// <example>10</example>
    public uint Height { get; set; }
    
    /// <summary>
    /// Gets or sets the width dimension of an object, typically measured in pixels or other unit.
    /// </summary>
    /// <example>10</example>
    public uint Width { get; set; }

    public static implicit operator Size(MagickGeometry cache)
    {
        return new Size
        {
            Height = cache.Height,
            Width = cache.Width
        };
    }
}

public record Point(int X, int Y);

[Scope]
public class TenantInfoSettingsHelper(WebImageSupplier webImageSupplier,
    StorageFactory storageFactory,
    TenantManager tenantManager,
    IConfiguration configuration)
{
    public async Task RestoreDefaultAsync(TenantInfoSettings tenantInfoSettings, TenantLogoManager tenantLogoManager)
    {
        await RestoreDefaultTenantNameAsync();
        await RestoreDefaultLogoAsync(tenantInfoSettings, tenantLogoManager);
    }

    public async Task RestoreDefaultTenantNameAsync()
    {
        var currentTenant = tenantManager.GetCurrentTenant();
        currentTenant.Name = configuration["web:portal-name"] ?? "";
        await tenantManager.SaveTenantAsync(currentTenant);
    }

    public async Task RestoreDefaultLogoAsync(TenantInfoSettings tenantInfoSettings, TenantLogoManager tenantLogoManager)
    {
        tenantInfoSettings.IsDefault = true;

        var store = await storageFactory.GetStorageAsync(tenantManager.GetCurrentTenantId(), "logo");
        try
        {
            await store.DeleteFilesAsync("", "*", false);
        }
        catch
        {
        }
        tenantInfoSettings.CompanyLogoSize = null;

        await tenantLogoManager.RemoveMailLogoDataFromCacheAsync();
    }

    public async Task SetCompanyLogoAsync(string companyLogoFileName, byte[] data, TenantInfoSettings tenantInfoSettings, TenantLogoManager tenantLogoManager)
    {
        var store = await storageFactory.GetStorageAsync(tenantManager.GetCurrentTenantId(), "logo");

        if (!tenantInfoSettings.IsDefault)
        {
            try
            {
                await store.DeleteFilesAsync("", "*", false);
            }
            catch
            {
            }
        }
        using (var memory = new MemoryStream(data))
        using (var image = new MagickImage(memory))
        {
            tenantInfoSettings.CompanyLogoSize = new MagickGeometry(image.Width, image.Height);

            memory.Seek(0, SeekOrigin.Begin);
            await store.SaveAsync(companyLogoFileName, memory);
            tenantInfoSettings.CompanyLogoFileName = companyLogoFileName;
        }
        tenantInfoSettings.IsDefault = false;

        await tenantLogoManager.RemoveMailLogoDataFromCacheAsync();
    }

    public async Task<string> GetAbsoluteCompanyLogoPathAsync(TenantInfoSettings tenantInfoSettings)
    {
        if (tenantInfoSettings.IsDefault)
        {
            return webImageSupplier.GetAbsoluteWebPath("notifications/logo.png");
        }

        var store = await storageFactory.GetStorageAsync(tenantManager.GetCurrentTenantId(), "logo");
        return (await store.GetUriAsync(tenantInfoSettings.CompanyLogoFileName ?? "")).ToString();
    }

    /// <summary>
    /// Get logo stream or null in case of default logo
    /// </summary>
    public async Task<Stream> GetStorageLogoData(TenantInfoSettings tenantInfoSettings)
    {
        if (tenantInfoSettings.IsDefault)
        {
            return null;
        }

        var storage = await storageFactory.GetStorageAsync(tenantManager.GetCurrentTenantId(), "logo");

        if (storage == null)
        {
            return null;
        }

        var fileName = tenantInfoSettings.CompanyLogoFileName ?? "";

        return await storage.IsFileAsync(fileName) ? await storage.GetReadStreamAsync(fileName) : null;
    }
}