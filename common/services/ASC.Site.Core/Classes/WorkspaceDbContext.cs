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
/// The mailing-list tables of the former Workspace database: <c>template_unsubscribe</c> and <c>template_subscribe</c>.
/// </summary>
/// <remarks>
/// The tables belong to the Workspace database, not to DocSpace: no DocSpace migration creates them and the model
/// maps only the columns this service writes. That database is MySQL only, and <see cref="WorkspaceSubscriptionStore"/>
/// recognises a duplicate key by the MySQL error code. <c>create_on</c> and <c>json_data</c> are left to their database
/// defaults (CURRENT_TIMESTAMP and NULL).
/// </remarks>
public class WorkspaceDbContext(DbContextOptions<WorkspaceDbContext> options) : BaseDbContext(options)
{
    /// <summary>The name of the connection string in the root <c>ConnectionStrings</c> section.</summary>
    public const string ConnectionStringName = "workspace";

    public DbSet<DbTemplateUnsubscribe> TemplateUnsubscribes { get; set; }
    public DbSet<DbTemplateSubscribe> TemplateSubscribes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbTemplateUnsubscribe>(entity =>
        {
            entity.ToTable("template_unsubscribe");
            entity.HasKey(e => e.Email);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(DbTemplateUnsubscribe.EmailMaxLength);
            entity.Property(e => e.Reason).HasColumnName("reason").IsRequired();
        });

        modelBuilder.Entity<DbTemplateSubscribe>(entity =>
        {
            entity.ToTable("template_subscribe");
            entity.HasKey(e => new { e.Email, e.Type });
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(DbTemplateUnsubscribe.EmailMaxLength);
            entity.Property(e => e.FirstName).HasColumnName("firstname").HasMaxLength(DbTemplateSubscribe.FirstNameMaxLength).IsRequired();
            entity.Property(e => e.Type).HasColumnName("type");
        });
    }

    [PreCompileQuery]
    public Task<bool> UnsubscribeExistsAsync(string email)
    {
        return WorkspaceQueries.UnsubscribeExistsAsync(this, email);
    }

    [PreCompileQuery]
    public Task<int> DeleteUnsubscribeAsync(string email)
    {
        return WorkspaceQueries.DeleteUnsubscribeAsync(this, email);
    }

    [PreCompileQuery]
    public Task<bool> SubscribeExistsAsync(string email, sbyte type)
    {
        return WorkspaceQueries.SubscribeExistsAsync(this, email, type);
    }
}

/// <summary>A row of <c>template_unsubscribe</c>: an address that asked not to get the marketing letters.</summary>
public class DbTemplateUnsubscribe
{
    /// <summary>The length of the <c>email</c> column of both tables.</summary>
    public const int EmailMaxLength = 100;

    public string Email { get; set; }

    /// <summary>NOT NULL without a default in the table; the site always left it empty.</summary>
    public string Reason { get; set; }
}

/// <summary>A row of <c>template_subscribe</c>: a confirmed subscription of an address to one list.</summary>
public class DbTemplateSubscribe
{
    /// <summary>The length of the <c>firstname</c> column.</summary>
    public const int FirstNameMaxLength = 64;

    public string Email { get; set; }
    public string FirstName { get; set; }

    /// <summary>The list, a signed TINYINT in the table.</summary>
    public sbyte Type { get; set; }
}

static file class WorkspaceQueries
{
    public static readonly Func<WorkspaceDbContext, string, Task<bool>> UnsubscribeExistsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WorkspaceDbContext ctx, string email) =>
                ctx.TemplateUnsubscribes.Any(r => r.Email == email));

    public static readonly Func<WorkspaceDbContext, string, Task<int>> DeleteUnsubscribeAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WorkspaceDbContext ctx, string email) =>
                ctx.TemplateUnsubscribes.Where(r => r.Email == email).ExecuteDelete());

    public static readonly Func<WorkspaceDbContext, string, sbyte, Task<bool>> SubscribeExistsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (WorkspaceDbContext ctx, string email, sbyte type) =>
                ctx.TemplateSubscribes.Any(r => r.Email == email && r.Type == type));
}
