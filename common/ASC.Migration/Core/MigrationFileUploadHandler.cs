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

namespace ASC.Migration.Core.Core;
public class MigrationFileUploadHandler
{
    public MigrationFileUploadHandler(RequestDelegate next)
    {

    }

    public async Task Invoke(HttpContext context,
        TenantManager tenantManager,
        IConfiguration configuration,
        StorageFactory storageFactory,
        UserManager userManager,
        AuthContext authContext,
        ILogger<MigrationFileUploadHandler> logger,
        IFusionCache hybridCache)
    {
        MigrationFileUploadResult result;
        try
        {
            if (!await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID))
            {
                throw new System.Security.SecurityException("Access denied.");
            }

            var tenantId = tenantManager.GetCurrentTenantId();
            var key = MigrationOperation.GetMigrationFolderCacheKey(tenantId);
            if (context.Request.Query["Init"].ToString() == "true")
            {

                try
                {
                    logger.Information("start migration upload file");
                    var discStore = await storageFactory.GetStorageAsync(tenantId, "migration", (IQuotaController)null) as DiscDataStore;

                    var path = await hybridCache.GetOrDefaultAsync<string>(key);
                    MigrationOperation.ClearMigrationFolder(path);

                    var newPath = Path.GetRandomFileName();
                    var newFolder = discStore.GetPhysicalPath("", newPath);
                    Directory.CreateDirectory(newFolder);
                    await hybridCache.SetAsync(key, newFolder, TimeSpan.FromDays(1));

                    int.TryParse(configuration["files:uploader:chunk-size"], out var chunkSize);
                    chunkSize = chunkSize == 0 ? 10 * 1024 * 1024 : chunkSize;

                    result = Success(chunkSize);
                }
                catch
                {
                    throw new ArgumentException("Can't start upload.");
                }
            }
            else
            {
                var path = await hybridCache.GetOrDefaultAsync<string>(key);
                var file = context.Request.Form.Files[0];
                await using var stream = file.OpenReadStream();
                var folder = Path.Combine(path, Path.GetFileName(context.Request.Query["Name"].ToString()));
                await using var fs = File.Open(folder, FileMode.Append);
                await stream.CopyToAsync(fs);

                result = Success();
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

    private MigrationFileUploadResult Success(int chunk = 0)
    {
        return new MigrationFileUploadResult
        {
            Success = true,
            ChunkSize = chunk
        };
    }

    private MigrationFileUploadResult Error(string messageFormat, params object[] args)
    {
        return new MigrationFileUploadResult
        {
            Success = false,
            Message = string.Format(messageFormat, args)
        };
    }
}

internal class MigrationFileUploadResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int ChunkSize { get; set; }
}

public static class MigrationFileUploadHandlerExtensions
{
    public static IApplicationBuilder UseMigrationFileUploadHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MigrationFileUploadHandler>();
    }
}