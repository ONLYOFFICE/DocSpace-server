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

namespace ASC.Web.Files.ThirdPartyApp;

[DebuggerDisplay("{App} - {AccessToken}")]
public class Token(OAuth20Token oAuth20Token, string app) : OAuth20Token(oAuth20Token)
{
    public string App { get; private set; } = app;
}

[Scope]
public class TokenHelper(
    IDbContextFactory<FilesDbContext> dbContextFactory,
    InstanceCrypto instanceCrypto,
    AuthContext authContext,
    TenantManager tenantManager)
{
    public async Task SaveTokenAsync(Token token)
    {
        var dbFilesThirdpartyApp = new DbFilesThirdpartyApp
        {
            App = token.App,
            Token = await EncryptTokenAsync(token),
            UserId = authContext.CurrentAccount.ID,
            TenantId = tenantManager.GetCurrentTenantId(),
            ModifiedOn = DateTime.UtcNow
        };

        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        await filesDbContext.AddOrUpdateAsync(q => q.ThirdpartyApp, dbFilesThirdpartyApp);
        await filesDbContext.SaveChangesAsync();
    }

    public async Task<Token> GetTokenAsync(string app)
    {
        return await GetTokenAsync(app, authContext.CurrentAccount.ID);
    }

    public async Task<Token> GetTokenAsync(string app, Guid userId)
    {
        var tenant = tenantManager.GetCurrentTenant();
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        var oAuth20Token = await Queries.TokenAsync(filesDbContext, tenant.Id, userId, app);

        if (oAuth20Token == null)
        {
            return null;
        }

        return new Token(await DecryptTokenAsync(oAuth20Token), app);
    }

    public async Task DeleteTokenAsync(string app, Guid? userId = null)
    {
        var tenant = tenantManager.GetCurrentTenant();
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        await Queries.DeleteTokenAsync(filesDbContext, tenant.Id, userId ?? authContext.CurrentAccount.ID, app);
    }

    private async Task<string> EncryptTokenAsync(OAuth20Token token)
    {
        var t = token.ToJson();

        return string.IsNullOrEmpty(t) ? string.Empty : await instanceCrypto.EncryptAsync(t);
    }

    private async Task<OAuth20Token> DecryptTokenAsync(string token)
    {
        return string.IsNullOrEmpty(token) ? null : OAuth20Token.FromJson(await instanceCrypto.DecryptAsync(token));
    }
}

static file class Queries
{
    public static readonly Func<FilesDbContext, int, Guid, string, Task<string>> TokenAsync =
        EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid userId, string app) =>
                ctx.ThirdpartyApp
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.UserId == userId)
                    .Where(r => r.App == app)
                    .Select(r => r.Token)
                    .FirstOrDefault());

    public static readonly Func<FilesDbContext, int, Guid, string, Task<int>> DeleteTokenAsync =
        EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid userId, string app) =>
                ctx.ThirdpartyApp
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.UserId == userId)
                    .Where(r => r.App == app)
                    .ExecuteDelete());
}