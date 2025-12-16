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

namespace ASC.AppHost.Configuration;

public static class NginxConfiguration
{
    public static IResourceBuilder<ContainerResource> ConfigureOpenResty(
        IDistributedApplicationBuilder builder,
        string basePath,
        string clientBasePath,
        IResourceBuilder<ExecutableResource> startPackages,
        bool isDocker)
    {
        var openResty = builder.AddContainer(Constants.OpenRestyContainer, "openresty/openresty", "latest")
            .WithBindMount(Path.Combine(basePath, "buildtools", "config", "nginx"), "/etc/nginx/conf.d/")
            .WithBindMount(Path.Combine(basePath, "buildtools", "config", "nginx", "includes"), "/etc/nginx/includes/")
            .WithBindMount(Path.Combine(basePath, "buildtools", "install", "docker", "config", "nginx", "templates"), "/etc/nginx/templates/")
            .WithBindMount(Path.Combine(clientBasePath, "public"), "/var/www/public")
            .WithBindMount(Path.Combine(clientBasePath, "packages", "client"), "/var/www/client")
            .WithBindMount(Path.Combine(clientBasePath, "packages", "login"), "/var/www/login")
            .WithBindMount(Path.Combine(clientBasePath, "packages", "management"), "/var/www/management")
            .WithHttpEndpoint(80, Constants.RestyPort)
            .WaitFor(startPackages);

        var serviceUrls = GetServiceUrls(isDocker);

        foreach (var (key, value) in serviceUrls)
        {
            openResty.WithEnvironment(key, value);
        }

        openResty.WithArgs("/bin/sh", "-c",
            $"envsubst '{string.Join(',', serviceUrls.Select(r => $"${r.Key}"))}' < /etc/nginx/templates/upstream-aspire.conf.template > /etc/nginx/includes/onlyoffice-upstream.conf && /usr/local/openresty/bin/openresty -g 'daemon off;'");

        return openResty;
    }

    private static Dictionary<string, string> GetServiceUrls(bool isDocker)
    {
        return new Dictionary<string, string>
        {
            { "client_service_env", $"http://{Constants.HostDockerInternal}:5001" },
            { "doceditor_service_env", $"http://{Constants.HostDockerInternal}:5013" },
            { "doceditor_env", $"http://{Constants.EditorsContainer}" },
            { "management_service_env", $"http://{Constants.HostDockerInternal}:5015" },
            {
                "people_service_env", isDocker
                    ? $"http://{ProjectConfigurator.GetProjectName<ASC_People>()}:{Constants.PeoplePort}"
                    : $"http://{Constants.HostDockerInternal}:{Constants.PeoplePort}"
            },
            {
                "files_service_env", isDocker
                    ? $"http://{ProjectConfigurator.GetProjectName<ASC_Files>()}:{Constants.FilesPort}"
                    : $"http://{Constants.HostDockerInternal}:{Constants.FilesPort}"
            },
            {
                "webapi_service_env", isDocker
                    ? $"http://{ProjectConfigurator.GetProjectName<ASC_Web_Api>()}:{Constants.WebApiPort}"
                    : $"http://{Constants.HostDockerInternal}:{Constants.WebApiPort}"
            },
            {
                "api_system_env", isDocker
                    ? $"http://{ProjectConfigurator.GetProjectName<ASC_ApiSystem>()}:{Constants.ApiSystemPort}"
                    : $"http://{Constants.HostDockerInternal}:{Constants.ApiSystemPort}"
            },
            {
                "backup_service_env", isDocker
                    ? $"http://{ProjectConfigurator.GetProjectName<ASC_Data_Backup>()}:{Constants.BackupPort}"
                    : $"http://{Constants.HostDockerInternal}:{Constants.BackupPort}"
            },
            {
                "webstudio_service_env", isDocker
                    ? $"http://{ProjectConfigurator.GetProjectName<ASC_Web_Studio>()}:{Constants.WebstudioPort}"
                    : $"http://{Constants.HostDockerInternal}:{Constants.WebstudioPort}"
            },
            {
                "ai_service_env", isDocker
                    ? $"http://{ProjectConfigurator.GetProjectName<ASC_AI>()}:{Constants.AiPort}"
                    : $"http://{Constants.HostDockerInternal}:{Constants.AiPort}"
            },
            { "sockjs_node_env", $"http://{Constants.HostDockerInternal}:5001" },
            { "plugins_service_env", $"http://{Constants.HostDockerInternal}:5014" },
            { "clients_service_env", $"http://{Constants.IdentityRegistrationContainer}:{Constants.IdentityRegistrationPort}" },
            { "oauth2_service_env", $"http://{Constants.IdentityAuthorizationContainer}:{Constants.IdentityAuthorizationPort}" },
            { "sso_service_env", $"http://{Constants.HostDockerInternal}:9834" },
            { "socket_io_env", $"http://{Constants.HostDockerInternal}:9899" },
            { "api_cache_env", $"http://{Constants.HostDockerInternal}:5100" },
            { "health_checks_env", $"http://{Constants.HostDockerInternal}:5033" },
            { "login_service_env", $"http://{Constants.HostDockerInternal}:5011" },
            { "migration_service_env", $"http://{Constants.HostDockerInternal}:5034" }
        };
    }
}