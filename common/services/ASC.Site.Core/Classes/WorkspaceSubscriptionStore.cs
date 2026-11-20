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

namespace ASC.Site.Core.Classes;

/// <summary>
/// Writes the mailing-list tables of the Workspace database (<see cref="WorkspaceDbContext"/>).
/// </summary>
/// <remarks>
/// Every method expects the email already trimmed and lowercased. Each call is idempotent: a row that is already
/// there is left as it is, and a row that another request inserts between the check and the insert (the primary key
/// refuses the second one) counts as there.
/// </remarks>
[Scope]
public class WorkspaceSubscriptionStore(IServiceProvider serviceProvider)
{
    /// <summary>Adds the address to <c>template_unsubscribe</c> unless it is there.</summary>
    public async Task AddUnsubscribeAsync(string email)
    {
        await using var context = await CreateContextAsync();

        if (await context.UnsubscribeExistsAsync(email))
        {
            return;
        }

        await context.TemplateUnsubscribes.AddAsync(new DbTemplateUnsubscribe { Email = email, Reason = string.Empty });

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            // Another request added the same address after the check: the row this call wanted is there.
        }
    }

    /// <summary>Removes the address from <c>template_unsubscribe</c>, if it is there.</summary>
    public async Task RemoveUnsubscribeAsync(string email)
    {
        await using var context = await CreateContextAsync();

        await context.DeleteUnsubscribeAsync(email);
    }

    /// <summary>
    /// Confirms a subscription: adds the row to <c>template_subscribe</c> when the address has none for that list,
    /// otherwise removes the address from <c>template_unsubscribe</c>.
    /// </summary>
    /// <returns><see langword="true"/> when the row was added, <see langword="false"/> when it was already there.</returns>
    public async Task<bool> AddSubscribeAsync(string email, string firstName, sbyte type)
    {
        await using var context = await CreateContextAsync();

        if (!await context.SubscribeExistsAsync(email, type))
        {
            await context.TemplateSubscribes.AddAsync(new DbTemplateSubscribe { Email = email, FirstName = firstName, Type = type });

            try
            {
                await context.SaveChangesAsync();

                return true;
            }
            catch (DbUpdateException ex) when (IsDuplicateKey(ex))
            {
                // Another request confirmed the same subscription after the check: go on as for an existing row.
            }
        }

        // The site removed the address from template_unsubscribe only when the subscription already existed;
        // a new row leaves template_unsubscribe as it is. Kept as it was. ExecuteDelete does not save the row the
        // refused insert left tracked in this context.
        await context.DeleteUnsubscribeAsync(email);

        return false;
    }

    private Task<WorkspaceDbContext> CreateContextAsync()
    {
        var factory = serviceProvider.GetService<IDbContextFactory<WorkspaceDbContext>>()
            ?? throw new InvalidOperationException(
                $"The \"{WorkspaceDbContext.ConnectionStringName}\" connection string is not configured: add it to the root ConnectionStrings section (site.core.json).");

        return factory.CreateDbContextAsync();
    }

    private static bool IsDuplicateKey(DbUpdateException exception)
    {
        return exception.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry };
    }
}
