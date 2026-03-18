// (c) Copyright Ascensio System SIA 2009-2026
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
        IResourceBuilder<ExecutableResource>? startPackages,
        bool isDocker,
        bool isPreview = false)
    {
        var isArm64 = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64;

        var openResty = builder.AddContainer(Constants.OpenRestyContainer, "openresty/openresty", "1.27.1.2-10-alpine" + (isArm64 ? "-arm64" : ""))
            .WithBindMount(Path.Combine(basePath, "buildtools", "config", "nginx"), "/etc/nginx/conf.d/")
            .WithBindMount(Path.Combine(basePath, "buildtools", "config", "nginx", "includes"), "/etc/nginx/includes/")
            .WithBindMount(Path.Combine(basePath, "buildtools", "install", "docker", "config", "nginx", "templates"), "/etc/nginx/templates/")
            .WithBindMount(Path.Combine(clientBasePath, "public"), "/var/www/public")
            .WithBindMount(Path.Combine(clientBasePath, "packages", "client"), "/var/www/client")
            .WithBindMount(Path.Combine(clientBasePath, "packages", "login"), "/var/www/login")
            .WithBindMount(Path.Combine(clientBasePath, "packages", "management"), "/var/www/management")
            .WithContainerRuntimeArgs("-p", $"0.0.0.0:{Constants.AppHostPort}:{Constants.RestyPort}");

        if (startPackages != null)
        {
            openResty.WaitFor(startPackages);
        }

        var serviceUrls = GetServiceUrls(isDocker, isPreview);

        foreach (var (key, value) in serviceUrls)
        {
            openResty.WithEnvironment(key, value);
        }

        openResty.WithArgs("/bin/sh", "-c",
            $"apk add --no-cache gettext && " +
            $"envsubst '{string.Join(' ', serviceUrls.Select(r => $"${r.Key}"))}' < /etc/nginx/includes/onlyoffice-upstream-map.conf.template > /etc/nginx/includes/onlyoffice-upstream-map.conf && " +
            $"/usr/local/openresty/bin/openresty -g 'daemon off;'");

        return openResty;
    }

    private static string BackendUrl<TProject>(int port, bool isDocker, bool isPreview) where TProject : IProjectMetadata, new()
    {
        if (isPreview)
        {
            return isDocker
                ? $"http://{ProjectConfigurator.GetProjectName<ASC_Monolith>()}:{Constants.MonolithPort}"
                : $"http://{Constants.HostDockerInternal}:{Constants.MonolithPort}";
        }

        return isDocker
            ? $"http://{ProjectConfigurator.GetProjectName<TProject>()}:{port}"
            : $"http://{Constants.HostDockerInternal}:{port}";
    }

    private static Dictionary<string, string> GetServiceUrls(bool isDocker, bool isPreview = false)
    {
        return new Dictionary<string, string>
        {
            { "SERVICE_CLIENT", $"http://{Constants.HostDockerInternal}:5001" },
            { "SERVICE_DOCEDITOR", $"http://{Constants.HostDockerInternal}:5013" },
            { "DOCUMENT_SERVER_URL_EXTERNAL", $"http://{Constants.EditorsContainer}" },
            { "DOCUMENT_CONTAINER_NAME", $"http://{Constants.EditorsContainer}" },
            { "SERVICE_MANAGEMENT", $"http://{Constants.HostDockerInternal}:5015" },
            { "SERVICE_PEOPLE_SERVER", BackendUrl<ASC_People>(Constants.PeoplePort, isDocker, isPreview) },
            { "SERVICE_FILES", BackendUrl<ASC_Files>(Constants.FilesPort, isDocker, isPreview) },
            { "SERVICE_API", BackendUrl<ASC_Web_Api>(Constants.WebApiPort, isDocker, isPreview) },
            { "SERVICE_API_SYSTEM", BackendUrl<ASC_ApiSystem>(Constants.ApiSystemPort, isDocker, isPreview) },
            { "SERVICE_BACKUP", BackendUrl<ASC_Data_Backup>(Constants.BackupPort, isDocker, isPreview) },
            { "SERVICE_STUDIO", BackendUrl<ASC_Web_Studio>(Constants.WebstudioPort, isDocker, isPreview) },
            { "SERVICE_AI", BackendUrl<ASC_AI>(Constants.AiPort, isDocker, isPreview) },
            { "SERVICE_API_CACHE", BackendUrl<ASC_Web_Api>(Constants.MonolithPort, isDocker, isPreview) },
            { "SERVICE_HELTHCHECKS", BackendUrl<ASC_Web_Api>(5033, isDocker, isPreview) },
            { "SERVICE_MIGRATION", BackendUrl<ASC_Web_Studio>(5034, isDocker, isPreview) },
            { "SERVICE_PLUGINS", $"http://{Constants.HostDockerInternal}:5014" },
            { "SERVICE_IDENTITY_API", $"http://{Constants.IdentityRegistrationContainer}:{Constants.IdentityRegistrationPort}" },
            { "SERVICE_IDENTITY", $"http://{Constants.IdentityAuthorizationContainer}:{Constants.IdentityAuthorizationPort}" },
            { "SERVICE_SSOAUTH", $"http://{Constants.HostDockerInternal}:9834" },
            { "SERVICE_SOCKET", $"http://{Constants.HostDockerInternal}:9899" },
            { "SERVICE_LOGIN", $"http://{Constants.HostDockerInternal}:5011" },
            { "SERVICE_SDK", $"http://{Constants.HostDockerInternal}:5099" },
            { "DASHBOARDS_CONTAINER_NAME", $"http://{Constants.HostDockerInternal}:5601" },
            { "DNS_NAMESERVER", "127.0.0.11" },
        };
    }
}