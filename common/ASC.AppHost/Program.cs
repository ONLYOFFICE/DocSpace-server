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

using System.Net.Sockets;

var builder = DistributedApplication.CreateBuilder(args);

var connectionManager = new ConnectionStringManager(builder)
    .AddMySql()
    .AddRabbitMq()
    .AddRedis()
    .AddEditors();

var basePath = Path.GetFullPath(Path.Combine("..", "..", ".."));
var isDocker = String.Compare(builder.Configuration["Docker"], "true", StringComparison.OrdinalIgnoreCase) == 0;

var projectConfiguration = new ProjectConfigurator(builder, connectionManager, basePath, isDocker);

_ = builder.AddYarp("gateway")
    .WithHostPort(80)
    .WithStaticFiles()
    .WithContainerFiles("/wwwroot/static/", Path.Combine(basePath, "client", "public"))
    .WithConfiguration(yarp =>
    {
        projectConfiguration
            .AddProject<ASC_Files>(Constants.FilesPort, yarp)
            .AddProject<ASC_Files_Service>(Constants.FilesServicePort)
            .AddProject<ASC_People>(Constants.PeoplePort, yarp)
            .AddProject<ASC_Web_Api>(Constants.WebApiPort, yarp)
            .AddProject<ASC_ApiSystem>(Constants.ApiSystemPort, yarp)
            .AddProject<ASC_ClearEvents>(Constants.ClearEventsPort)
            .AddProject<ASC_Data_Backup>(Constants.BackupPort, yarp)
            .AddProject<ASC_Data_Backup_BackgroundTasks>(Constants.BackupBackgroundTasksPort)
            .AddProject<ASC_Notify>(0, includeHealthCheck: false)
            .AddProject<ASC_Studio_Notify>(Constants.StudioNotifyPort)
            .AddProject<ASC_Web_Studio>(Constants.WebstudioPort)
            .AddProject<ASC_AI>(Constants.AiPort, yarp)
            .AddProject<ASC_AI_Service>(Constants.AiServicePort)
            .AddSocketIO()
            .AddSsoAuth()
            .AddWebDav()
            .AddIdentity();
        
        var clientBasePath = Path.Combine(basePath, "client");
        var installPackages = builder.AddResource(new NodeAppResource("asc-install-packages", "pnpm", clientBasePath)).WithArgs("install");
        var buildPackages =  builder.AddResource(new NodeAppResource("asc-build-packages", "pnpm", clientBasePath)).WithArgs("build").WaitForCompletion(installPackages);
        var startPackages = builder.AddResource(new NodeAppResource("asc-start-packages", "pnpm", clientBasePath)).WithArgs("start").WaitForCompletion(buildPackages).WithHttpEndpoint(5001, 5001, isProxied: false);
        installPackages.WithChildRelationship(buildPackages);
        buildPackages.WithChildRelationship(startPackages);

        yarp.AddRoute("/", new EndpointReference(startPackages.Resource, new EndpointAnnotation(ProtocolType.Tcp, transport: "http", uriScheme: "http", port: 5001, targetPort: 5001)));
        yarp.AddRoute("/static/js/{**catch-all}", new EndpointReference(startPackages.Resource, new EndpointAnnotation(ProtocolType.Tcp, transport: "http", uriScheme: "http", port: 5001, targetPort: 5001)));
        yarp.AddRoute("/static/styles/{**catch-all}", new EndpointReference(startPackages.Resource, new EndpointAnnotation(ProtocolType.Tcp, transport: "http", uriScheme: "http", port: 5001, targetPort: 5001)));
        yarp.AddRoute("/locales/{**catch-all}", new EndpointReference(startPackages.Resource, new EndpointAnnotation(ProtocolType.Tcp, transport: "http", uriScheme: "http", port: 5001, targetPort: 5001)));
        yarp.AddRoute("/login", new EndpointReference(startPackages.Resource, new EndpointAnnotation(ProtocolType.Tcp, transport: "http", uriScheme: "http", port: 5011, targetPort: 5011)));
        yarp.AddRoute("/wizard", new EndpointReference(startPackages.Resource, new EndpointAnnotation(ProtocolType.Tcp, transport: "http", uriScheme: "http", port: 5011, targetPort: 5011)));
        yarp.AddRoute("/confirm", new EndpointReference(startPackages.Resource, new EndpointAnnotation(ProtocolType.Tcp, transport: "http", uriScheme: "http", port: 5011, targetPort: 5011)));
        yarp.AddRoute("/management", new EndpointReference(startPackages.Resource, new EndpointAnnotation(ProtocolType.Tcp, transport: "http", uriScheme: "http", port: 5011, targetPort: 5011)));
    });

await builder.Build().RunAsync();