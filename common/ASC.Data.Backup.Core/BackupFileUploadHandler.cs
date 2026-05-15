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

using System.Text.Json;

using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ASC.Web.Studio.Core.Backup;

public class BackupFileUploadHandler
{
    public BackupFileUploadHandler(RequestDelegate _)
    {
    }

    public async Task Invoke(HttpContext context,
        PermissionContext permissionContext,
        BackupService backupService,
        IFusionCache cache,
        TenantManager tenantManager,
        SetupInfo setupInfo)
    {
        BackupFileUploadResult result;
        try
        {
            await backupService.DemandPermissionsRestoreAsync();
            if (!await permissionContext.CheckPermissionsAsync(SecurityConstants.EditPortalSettings))
            {
                throw new ArgumentException("Access denied.");
            }
            var tenantId = tenantManager.GetCurrentTenant().Id;
            string path;
            try
            {
                path = await backupService.GetTmpFilePathAsync(tenantId);
            }
            catch
            {
                throw new Exception("backup_temp is not disc");
            }
            if (context.Request.Query["Init"].ToString() == "true")
            {
                long.TryParse(context.Request.Query["totalSize"], out var size);
                if (size <= 0)
                {
                    throw new ArgumentException("Total size must be greater than 0.");
                }

                var maxSize = (await tenantManager.GetCurrentTenantQuotaAsync()).MaxTotalSize;
                if (size > maxSize)
                {
                    throw new ArgumentException(BackupResource.LargeBackup);
                }

                try
                {
                    if (File.Exists(path + ".tar.gz"))
                    {
                        File.Delete(path + ".tar.gz");
                    }
                    if (File.Exists(path + ".tar"))
                    {
                        File.Delete(path + ".tar");
                    }

                    var info = new UploadInfo
                    {
                        Ext = context.Request.Query["extension"].ToString() == "tar" ? ".tar" : ".tar.gz",
                        Size = size
                    };
                    await cache.SetAsync($"{tenantId} backupTotalSize", info, TimeSpan.FromMinutes(20));

                    result = Success(setupInfo.ChunkUploadSize);
                }
                catch
                {
                    throw new ArgumentException("Can't start upload.");
                }
            }
            else
            {
                var info = await cache.GetOrDefaultAsync<UploadInfo>($"{tenantId} backupTotalSize");
                if (info == null)
                {
                    throw new ArgumentException("Need init upload.");
                }

                var file = context.Request.Form.Files[0];
                await using var stream = file.OpenReadStream();

                if (stream.Length > setupInfo.ChunkUploadSize)
                {
                    throw new ArgumentException("chunkSize more then maxChunkUploadSize");
                }

                await using var fs = File.Open(path + info.Ext, FileMode.Append);
                await stream.CopyToAsync(fs);

                if (fs.Length >= info.Size)
                {
                    await cache.RemoveAsync($"{tenantId} backupTotalSize");
                    result = Success(setupInfo.ChunkUploadSize, endUpload: true);
                }
                else
                {
                    result = Success(setupInfo.ChunkUploadSize);
                }
            }
        }
        catch (Exception error)
        {
            result = Error(error.Message);
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        }));
    }

    private BackupFileUploadResult Success(long chunkSize, bool endUpload = false)
    {
        return new BackupFileUploadResult
        {
            Success = true,
            ChunkSize = chunkSize,
            EndUpload = endUpload
        };
    }

    private BackupFileUploadResult Error(string messageFormat, params object[] args)
    {
        return new BackupFileUploadResult
        {
            Success = false,
            Message = string.Format(messageFormat, args)
        };
    }
}

internal class BackupFileUploadResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public long ChunkSize { get; set; }
    public bool EndUpload { get; set; }
}

internal class UploadInfo
{
    public long Size { get; init; }
    public string Ext { get; init; }
}

public static class BackupFileUploadHandlerExtensions
{
    public static IApplicationBuilder UseBackupFileUploadHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<BackupFileUploadHandler>();
    }
}