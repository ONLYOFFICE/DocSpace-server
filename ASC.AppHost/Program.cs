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

var builder = DistributedApplication.CreateBuilder(args);

var mySql = builder
    .AddMySql("mysql")
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("docspace");

var path = Path.GetFullPath(Path.Combine("..", "common", "Tools", "ASC.Migration.Runner", "bin", "Debug", "ASC.Migration.Runner.exe"));

var rabbitMq = builder
    .AddRabbitMQ("messaging")
    .WithLifetime(ContainerLifetime.Persistent);

var migrate = builder
    .AddExecutable("migrate",path, Path.GetDirectoryName(path) ?? "")
    .WithReference(mySql)
    .WaitFor(mySql);

builder.AddProject<Projects.ASC_ApiSystem>("asc-apisystem")
    .WithHttpHealthCheck("/health")
    .WithReference(mySql, "default:connectionString")
    .WithReference(rabbitMq, "rabbitMQ")
    .WaitFor(migrate)
    .WaitFor(rabbitMq);

builder.AddProject<Projects.ASC_ClearEvents>("asc-clearevents")
    .WithHttpHealthCheck("/health")
    .WithReference(mySql, "default:connectionString")
    .WithReference(rabbitMq, "rabbitMQ")
    .WaitFor(migrate)
    .WaitFor(rabbitMq);

builder.AddProject<Projects.ASC_Data_Backup>("asc-data-backup")
    .WithHttpHealthCheck("/health")
    .WithReference(mySql, "default:connectionString")
    .WithReference(rabbitMq, "rabbitMQ")
    .WaitFor(migrate)
    .WaitFor(rabbitMq);

builder.AddProject<Projects.ASC_Data_Backup_BackgroundTasks>("asc-data-backup-backgroundtasks")
    .WithHttpHealthCheck("/health")
    .WithReference(mySql, "default:connectionString")
    .WithReference(rabbitMq, "rabbitMQ")
    .WaitFor(migrate)
    .WaitFor(rabbitMq);

builder.AddProject<Projects.ASC_Notify>("asc-notify")
    .WithReference(mySql, "default:connectionString")
    .WithReference(rabbitMq, "rabbitMQ")
    .WaitFor(migrate)
    .WaitFor(rabbitMq);

builder.AddProject<Projects.ASC_Web_Api>("asc-web-api")
    .WithHttpHealthCheck("/health")
    .WithReference(mySql, "default:connectionString")
    .WithReference(rabbitMq, "rabbitMQ")
    .WaitFor(migrate)
    .WaitFor(rabbitMq);

builder.AddProject<Projects.ASC_People>("asc-people")
    .WithHttpHealthCheck("/health")
    .WithReference(mySql, "default:connectionString")
    .WithReference(rabbitMq, "rabbitMQ")
    .WaitFor(migrate)
    .WaitFor(rabbitMq);

builder.AddProject<Projects.ASC_Files>("asc-files")
    .WithHttpHealthCheck("/health")
    .WithReference(mySql, "default:connectionString")
    .WithReference(rabbitMq, "rabbitMQ")
    .WaitFor(migrate)
    .WaitFor(rabbitMq);

builder.AddProject<Projects.ASC_Files_Service>("asc-files-service")
    .WithHttpHealthCheck("/health")
    .WithReference(mySql, "default:connectionString")
    .WithReference(rabbitMq, "rabbitMQ")
    .WaitFor(migrate)
    .WaitFor(rabbitMq);

builder.AddProject<Projects.ASC_Studio_Notify>("asc-studio-notify")
    .WithHttpHealthCheck("/health")
    .WithReference(mySql, "default:connectionString")
    .WithReference(rabbitMq, "rabbitMQ")
    .WaitFor(migrate)
    .WaitFor(rabbitMq);

builder.AddProject<Projects.ASC_Web_Studio>("asc-web-studio")
    .WithHttpHealthCheck("/health")
    .WithReference(mySql, "default:connectionString")
    .WithReference(rabbitMq, "rabbitMQ")
    .WaitFor(migrate)
    .WaitFor(rabbitMq);

builder.AddNodeApp("asc-socketIO", "server.js", "../common/ASC.Socket.IO/");

builder.AddNodeApp("asc-ssoAuth", "app.js", "../common/ASC.SSoAuth/");

builder.AddNodeApp("asc-webDav", "webDavServer.js", "../common/ASC.WebDav/server/");

await builder.Build().RunAsync();
