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
        AuthContext authContext)
    {
        MigrationFileUploadResult result = null;
        try
        {
            if (!await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID))
            {
                throw new System.Security.SecurityException("Access denied.");
            }

            var tenantId = await tenantManager.GetCurrentTenantIdAsync();
            if (context.Request.Query["Init"].ToString() == "true")
            {
                
                try
                {
                    var discStore = await storageFactory.GetStorageAsync(tenantId, "migration", (IQuotaController)null) as DiscDataStore;
                    var folder = discStore.GetPhysicalPath("", "");
                    if (Directory.Exists(folder))
                    {
                        Directory.Delete(folder, true);
                    }
                    Directory.CreateDirectory(folder);
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
                var file = context.Request.Form.Files[0];
                using var stream = file.OpenReadStream();

                var path = await GetTmpFilePathAsync(context.Request.Query["Name"].ToString(), storageFactory, tenantId);
                using var fs = File.Open(path, FileMode.Append);
                await stream.CopyToAsync(fs);

                result = Success();
            }
        }
        catch (Exception error)
        {
            result = Error(error.Message);
        }

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions()
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

    private async Task<string> GetTmpFilePathAsync(string name, StorageFactory storageFactory, int tenantId)
    {
        var discStore = await storageFactory.GetStorageAsync(tenantId, "migration", (IQuotaController)null) as DiscDataStore;
        var folder = discStore.GetPhysicalPath("", "");

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        return Path.Combine(folder, name);
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