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

namespace ASC.Api.Migration;

[DefaultRoute]
[ApiController]
public class MigrationController(
    UserManager userManager,
    AuthContext authContext,
    StudioNotifyService studioNotifyService,
    IHttpContextAccessor httpContextAccessor,
    MigrationCore migrationCore) : ControllerBase
{
    [HttpGet("list")]
    public async Task<string[]> List()
    {
        await DemandPermissionAsync();
        return migrationCore.GetAvailableMigrations();
    }

    [HttpPost("init/{migratorName}")]
    public async Task UploadAndInitAsync(string migratorName)
    {
        await DemandPermissionAsync();

        await migrationCore.StartParseAsync(migratorName);
    }

    [HttpGet("status")]
    public async Task<MigrationStatusDto> Status()
    {
        await DemandPermissionAsync();
        try
        {
            var status = await migrationCore.GetStatusAsync();
            if (status != null)
            {
                var result = new MigrationStatusDto
                {
                    Progress = status.Percentage,
                    Error = status.Exception != null ? status.Exception.Message : "",
                    IsCompleted = status.IsCompleted,
                    ParseResult = status.MigrationApiInfo
                };
                return result;
            }
        }
        catch
        {

        }
        return null;
    }

    [HttpPost("cancel")]
    public async Task CancelAsync()
    {
        await DemandPermissionAsync();

        await migrationCore.StopAsync();
    }

    [HttpPost("clear")]
    public async Task ClearAsync()
    {
        await DemandPermissionAsync();

        await migrationCore.ClearAsync();
    }

    [HttpPost("migrate")]
    public async Task MigrateAsync(MigrationApiInfo info)
    {
        await DemandPermissionAsync();

        await migrationCore.StartAsync(info);
    }

    [HttpGet("logs")]
    public async Task LogsAsync()
    {
        await DemandPermissionAsync();

        var status = await migrationCore.GetStatusAsync();
        if (status == null)
        {
            throw new Exception(MigrationResource.MigrationProgressException);
        }

        httpContextAccessor.HttpContext.Response.Headers.Append("Content-Disposition", ContentDispositionUtil.GetHeaderValue("migration.log"));
        httpContextAccessor.HttpContext.Response.ContentType = "text/plain; charset=UTF-8";
        await status.CopyLogsAsync(httpContextAccessor.HttpContext.Response.Body);
    }

    [HttpPost("finish")]
    public async Task FinishAsync(FinishDto inDto)
    {
        await DemandPermissionAsync();

        if (inDto.IsSendWelcomeEmail)
        {
            var status = await migrationCore.GetStatusAsync();
            if (status == null)
            {
                throw new Exception(MigrationResource.MigrationProgressException);
            }
            var guidUsers = status.ImportedUsers;
            foreach (var gu in guidUsers)
            {
                var u = await userManager.GetUserByEmailAsync(gu);
                await studioNotifyService.UserInfoActivationAsync(u);
            }
        }
    }

    private async Task DemandPermissionAsync()
    {
        if (!await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }
    }
}
