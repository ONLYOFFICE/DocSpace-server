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

using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var mySql = builder
    .AddMySql("mysql")
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("docspace");

var path = Path.GetFullPath(Path.Combine("..", "Tools", "ASC.Migration.Runner", "bin", "Debug", "ASC.Migration.Runner.exe"));

var rabbitMq = builder
    .AddRabbitMQ("messaging")
    .WithLifetime(ContainerLifetime.Persistent);

var redis = builder
    .AddRedis("cache")
    .WithLifetime(ContainerLifetime.Persistent);

var migrate = builder
    .AddExecutable("migrate",path, Path.GetDirectoryName(path) ?? "")
    .WithReference(mySql)
    .WaitFor(mySql);

AddProjectWithDefaultConfiguration<ASC_ApiSystem>();
AddProjectWithDefaultConfiguration<ASC_ClearEvents>();
AddProjectWithDefaultConfiguration<ASC_Data_Backup>();
AddProjectWithDefaultConfiguration<ASC_Data_Backup_BackgroundTasks>();
AddProjectWithDefaultConfiguration<ASC_Notify>(false);
AddProjectWithDefaultConfiguration<ASC_Web_Api>();
AddProjectWithDefaultConfiguration<ASC_People>( );
AddProjectWithDefaultConfiguration<ASC_Files>();
AddProjectWithDefaultConfiguration<ASC_Files_Service>();
AddProjectWithDefaultConfiguration<ASC_Studio_Notify>();
AddProjectWithDefaultConfiguration<ASC_Web_Studio>();

builder.AddNpmApp("asc-socketIO", "../ASC.Socket.IO/", "start:build");
builder.AddNpmApp("asc-ssoAuth", "../ASC.SSoAuth/", "start:build");
builder.AddNpmApp("asc-webDav", "../ASC.WebDav/", "start:build");

await builder.Build().RunAsync();

return;

void AddProjectWithDefaultConfiguration<TProject>(bool includeHealthCheck = true) where TProject : IProjectMetadata, new()
{
    var project = builder.AddProject<TProject>(typeof(TProject).Name.ToLower().Replace('_', '-'));
    
    if (includeHealthCheck)
    {
        project.WithHttpHealthCheck("/health");
    }

    project
        .WithReference(mySql, "default:connectionString")
        .WithReference(rabbitMq, "rabbitMQ")
        .WithReference(redis, "redis")
        .WaitFor(migrate)
        .WaitFor(rabbitMq)
        .WaitFor(redis);
}