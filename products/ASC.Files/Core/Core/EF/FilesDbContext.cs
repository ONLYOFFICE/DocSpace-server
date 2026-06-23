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

using User = ASC.Core.Common.EF.User;

namespace ASC.Files.Core.EF;

public partial class FilesDbContext(DbContextOptions<FilesDbContext> dbContextOptions) : BaseDbContext(dbContextOptions)
{
    public DbSet<DbFile> Files { get; set; }
    public DbSet<DbFolder> Folders { get; set; }
    public DbSet<DbFolderTree> Tree { get; set; }
    public DbSet<DbFilesBunchObjects> BunchObjects { get; set; }
    public DbSet<DbFilesSecurity> Security { get; set; }
    public DbSet<DbFilesThirdpartyIdMapping> ThirdpartyIdMapping { get; set; }
    public DbSet<DbFilesThirdpartyAccount> ThirdpartyAccount { get; set; }
    public DbSet<DbFilesTagLink> TagLink { get; set; }
    public DbSet<DbFilesTag> Tag { get; set; }
    public DbSet<DbFilesThirdpartyApp> ThirdpartyApp { get; set; }
    public DbSet<DbFilesLink> FilesLink { get; set; }
    public DbSet<DbFilesProperties> FilesProperties { get; set; }
    public DbSet<DbTenant> Tenants { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<DbFileOrder> FileOrder { get; set; }
    public DbSet<DbRoomSettings> RoomSettings { get; set; }
    public DbSet<DbGroup> Groups { get; set; }
    public DbSet<UserGroup> UserGroup { get; set; }
    public DbSet<DbFilesAuditReference> FilesAuditReference { get; set; }
    public DbSet<DbUserRelation> UserRelations { get; set; }
    public DbSet<DbFilesFormRoleMapping> FilesFormRoleMapping { get; set; }
    public DbSet<DbFilesGroup> RoomGroup { get; set; }
    public DbSet<DbFilesRoomGroup> RoomGroupRef { get; set; }
    public DbSet<DbChat> Chats { get; set; }
    public DbSet<DbChatMessage> ChatMessages { get; set; }
    public DbSet<DbFileVectorization> FileVectorization { get; set; }
    public DbSet<DbUserChatSettings> UserChatSettings { get; set; }
    public DbSet<DbMcpServerSettings> McpServerSettings { get; set; }
    public DbSet<DbAiProvider> AiProviders { get; set; }
    public DbSet<DbChatMessageAttachment> MessageAttachments { get; set; }
    public DbSet<DbAiModelSettings> AiModelSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ModelBuilderWrapper
            .From(modelBuilder, Database)
            .AddDbFiles()
            .AddDbFolder()
            .AddDbFolderTree()
            .AddDbFilesThirdpartyAccount()
            .AddDbFilesBunchObjects()
            .AddDbFilesSecurity()
            .AddDbFilesThirdpartyIdMapping()
            .AddDbFilesFormRoleMapping()
            .AddDbFilesTagLink()
            .AddDbFilesTag()
            .AddDbFilesGroup()
            .AddDbFilesRoomGroup()
            .AddDbDbFilesThirdpartyApp()
            .AddDbFilesLink()
            .AddDbFilesProperties()
            .AddDbTenant()
            .AddDbFileOrder()
            .AddUser()
            .AddDbRoomSettings()
            .AddDbGroup()
            .AddUserGroup()
            .AddFilesAuditReference()
            .AddUserRelation()
            .AddDbChats()
            .AddDbChatsMessages()
            .AddDbFileVectorization()
            .AddDbUserChatSettings()
            .AddDbMcpServerSettings()
            .AddDbAiProviders()
            .AddDbAiModelSettings()
            .AddDbChatMessageAttachment()
            .AddDbFunctions();
    }
}